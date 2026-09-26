using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using IoBrokerTool.Dialogs;
using IoBrokerTool.Models;
using IoBrokerTool.Services;

namespace IoBrokerTool;

public partial class MainWindow : Window
{
    private static readonly Regex RemoteProgress =
        new(@"STUDIO_PROGRESS\|(\d{1,3})\|([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex RemoteIssue =
        new(@"STUDIO_ISSUE\|(\d+)\|([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex RemoteDiagnostic =
        new(@"STUDIO_DIAG\|([^|\r\n]+)\|([^|\r\n]+)\|([^|\r\n]+)\|([^|\r\n]+)\|([^\r\n]+)", RegexOptions.Compiled);
    private static readonly Regex AnsiEscape =
        new(@"\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\x07]*(?:\x07|\x1B\\))",
            RegexOptions.Compiled);
    private static readonly Regex TerminalControl =
        new(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled);
    private const string RemoteBackup = "/tmp/iobroker-tool-backup.tar.gz";
    private const string RemoteRestore = "/tmp/iobroker-tool-restore.tar.gz";
    private const string RemoteCheckpointExport = ".iobroker-studio-checkpoint.tar.gz";
    private readonly SettingsService _settingsService = new();
    private readonly SshService _ssh = new();
    private readonly EmbeddedTerminalService _embeddedTerminal = new();
    private readonly DesktopService _desktop = new();
    private Window? _detachedTerminalWindow;
    private AppSettings _settings;
    private CancellationTokenSource? _operationCancellation;
    private DispatcherTimer? _operationTimer;
    private DateTimeOffset _lastOperationActivity;
    private string _currentOperationText = "";
    private readonly List<string> _operationIssues = [];
    private readonly List<string> _remoteFailures = [];
    private readonly List<string> _diagnosticEntries = [];

    public MainWindow()
    {
        InitializeComponent();
        var productVersion = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "unbekannt";
        VersionText.Text = $"v{productVersion} · 64-Bit";
        _settings = _settingsService.Load();
        HostBox.Text = _settings.Host;
        UserBox.Text = _settings.Username;
        PortBox.Text = _settings.Port.ToString();
        RememberPasswordBox.IsChecked = _settings.RememberPassword;
        PasswordBox.Text = _settings.RememberPassword
            ? _settingsService.Unprotect(_settings.ProtectedPassword)
            : "";
        _ssh.PrivilegePasswordRequested = PromptPrivilegePasswordAsync;
        TerminalView.Model = _embeddedTerminal.Model;
        _embeddedTerminal.StatusChanged += (text, connected) =>
            Dispatcher.UIThread.Post(() =>
            {
                TerminalStatus.Text = text;
                TerminalStatus.Foreground = Brush.Parse(connected ? "#65E0BD" : "#8FA1B8");
            });
        _embeddedTerminal.Model.Feed(
            "\u001b[38;2;85;225;200mioBroker Studio Terminal\u001b[0m\r\n" +
            "Oben Zugangsdaten eintragen und „Verbinden“ wählen.\r\n");
        AppendLog("ioBroker Studio gestartet.");
        AppendLog("PuTTY steht als direkter Schnellzugriff bereit.");
        Opened += (_, _) => FitWindowToWorkingArea();
        Closing += (_, eventArgs) =>
        {
            if (_operationCancellation is not null)
            {
                eventArgs.Cancel = true;
                _ = ShowInfo("Aktion läuft noch",
                    "ioBroker Studio arbeitet noch. Bitte das Fenster erst schließen, wenn „vollständig fertig“ angezeigt wird.");
                return;
            }
            if (_detachedTerminalWindow is not null)
            {
                _detachedTerminalWindow.Content = null;
                _detachedTerminalWindow.Close();
                _detachedTerminalWindow = null;
            }
            SaveSettings();
            // Never wait for an SSH socket on the UI thread. A stalled remote
            // connection must not prevent the application window from closing.
            _ = Task.Run(_embeddedTerminal.Dispose);
        };
    }

    private void OnDetachTerminal(object? sender, RoutedEventArgs e)
    {
        if (_detachedTerminalWindow is not null)
        {
            _detachedTerminalWindow.Activate();
            return;
        }

        TerminalHost.Content = null;
        DetachTerminalButton.IsEnabled = false;

        var window = new Window
        {
            Title = "ioBroker Studio – Terminal",
            Width = 920,
            Height = 600,
            MinWidth = 560,
            MinHeight = 360,
            Background = Brush.Parse("#05090E"),
            Content = TerminalView,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        _detachedTerminalWindow = window;
        window.Closing += (_, _) =>
        {
            window.Content = null;
            TerminalHost.Content = TerminalView;
            DetachTerminalButton.IsEnabled = true;
            _detachedTerminalWindow = null;
        };
        window.Show(this);
        TerminalView.Focus();
    }

    private async Task<string?> PromptPrivilegePasswordAsync(
        string username,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SetOperationText("Administrator-Passwort erforderlich …");
            AppendLog($"Das Zielsystem fordert Administratorrechte für {username} an.");
            return await new PasswordPromptDialog(username).ShowDialog(this);
        });
    }

    private void FitWindowToWorkingArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        var usableWidth = screen.WorkingArea.Width / screen.Scaling;
        var usableHeight = screen.WorkingArea.Height / screen.Scaling;
        MaxWidth = Math.Max(MinWidth, usableWidth);
        MaxHeight = Math.Max(MinHeight, usableHeight);
        Width = Math.Min(1180, Math.Max(MinWidth, usableWidth - 36));
        Height = Math.Min(760, Math.Max(MinHeight, usableHeight - 36));
        var physicalWidth = (int)Math.Round(Width * screen.Scaling);
        var physicalHeight = (int)Math.Round(Height * screen.Scaling);
        Position = new PixelPoint(
            screen.WorkingArea.X + Math.Max(0, (screen.WorkingArea.Width - physicalWidth) / 2),
            screen.WorkingArea.Y + Math.Max(0, (screen.WorkingArea.Height - physicalHeight) / 2));
    }

    private ConnectionProfile? ReadProfile(bool requirePassword = true)
    {
        var host = HostBox.Text?.Trim() ?? "";
        var username = UserBox.Text?.Trim() ?? "";
        var password = PasswordBox.Text ?? "";
        if (!int.TryParse(PortBox.Text, out var port) || port is < 1 or > 65535)
        {
            _ = ShowInfo("Ungültiger Port", "Bitte einen Port zwischen 1 und 65535 eingeben.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
            requirePassword && string.IsNullOrEmpty(password))
        {
            _ = ShowInfo("Zugangsdaten fehlen",
                "Bitte Host/IP, Benutzername und SSH-Passwort vollständig eingeben.");
            return null;
        }
        return new ConnectionProfile(host, port, username, password, _settings.HostKeyFingerprint);
    }

    private void SaveSettings()
    {
        _settings.Host = HostBox.Text?.Trim() ?? "";
        _settings.Username = UserBox.Text?.Trim() ?? "";
        _settings.Port = int.TryParse(PortBox.Text, out var port) ? port : 22;
        _settings.RememberPassword = RememberPasswordBox.IsChecked == true;
        _settings.ProtectedPassword = _settings.RememberPassword
            ? _settingsService.Protect(PasswordBox.Text ?? "")
            : "";
        _settingsService.Save(_settings);
    }

    private async void OnTestConnection(object? sender, RoutedEventArgs e)
    {
        var profile = ReadProfile();
        if (profile is null) return;
        await RunUiOperation("Verbindung wird geprüft …", async token =>
        {
            AppendLog($"Verbindungsaufbau zu {profile.Username}@{profile.Host}:{profile.Port} …");
            var fingerprint = await _ssh.GetHostKeyFingerprintAsync(profile, token);
            if (string.IsNullOrEmpty(profile.KnownFingerprint) ||
                !string.Equals(profile.KnownFingerprint, fingerprint,
                    StringComparison.OrdinalIgnoreCase))
            {
                var trusted = await Dispatcher.UIThread.InvokeAsync(() =>
                    new ConfirmDialog(
                        "SSH-Identität bestätigen",
                        $"Der Server meldet diesen Schlüssel-Fingerabdruck:\n\n{fingerprint}\n\n" +
                        "Nur bestätigen, wenn dies wirklich dein Zielsystem ist. " +
                        "Das Passwort wurde für diese Prüfung noch nicht übertragen. " +
                        "Bei späteren Änderungen blockiert ioBroker Studio die Verbindung.",
                        "Diesem Server vertrauen").ShowDialog(this));
                if (!trusted)
                {
                    SetConnectionState(false, "Nicht bestätigt");
                    AppendLog("SSH-Schlüssel wurde nicht bestätigt.");
                    return;
                }
                _settings.HostKeyFingerprint = fingerprint;
            }

            SaveSettings();
            profile = profile with { KnownFingerprint = fingerprint };
            var result = await _ssh.TestAsync(profile, token);
            if (!result.Success)
            {
                SetConnectionState(false, "Nicht verbunden");
                AppendLog($"FEHLER: {result.Message}");
                return;
            }
            SetConnectionState(true, "Verbunden");
            AppendLog(result.SystemInfo);
            AppendLog("SSH-Identität gespeichert und Verbindung erfolgreich.");
            SetOperationText("Integriertes Terminal wird verbunden …");
            try
            {
                await _embeddedTerminal.ConnectAsync(
                    profile with { KnownFingerprint = _settings.HostKeyFingerprint },
                    token);
                AppendLog("Integriertes Terminal ist bereit.");
            }
            catch (Exception ex)
            {
                AppendLog($"Terminal konnte nicht verbunden werden: {ex.Message}");
            }
        });
    }

    private async Task<ConnectionProfile?> EnsureTrustedProfile()
    {
        var profile = ReadProfile();
        if (profile is null) return null;
        AppendLog("SSH-Schlüssel wird vor der Anmeldung geprüft …");
        string fingerprint;
        try
        {
            fingerprint = await _ssh.GetHostKeyFingerprintAsync(
                profile, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await ShowInfo("Verbindung fehlgeschlagen", ex.Message);
            return null;
        }
        if (!string.Equals(profile.KnownFingerprint, fingerprint,
                StringComparison.OrdinalIgnoreCase))
        {
            var changed = !string.IsNullOrWhiteSpace(profile.KnownFingerprint);
            var trusted = await new ConfirmDialog(
                changed ? "SSH-Identität hat sich geändert" : "SSH-Identität bestätigen",
                $"Fingerabdruck des Zielsystems:\n\n{fingerprint}\n\n" +
                (changed
                    ? "Der Schlüssel weicht vom gespeicherten Schlüssel ab. Nur bestätigen, wenn der Server bewusst neu installiert oder sein SSH-Schlüssel geändert wurde.\n\n"
                    : "") +
                "Das Passwort wurde noch nicht übertragen. Dieser Schlüssel wird gespeichert und künftig kontrolliert.",
                changed ? "Neuen Schlüssel übernehmen" : "Vertrauen",
                changed).ShowDialog(this);
            if (!trusted) return null;
        }
        _settings.HostKeyFingerprint = fingerprint;
        SaveSettings();
        SetConnectionState(true, "Verbunden");
        return profile with { KnownFingerprint = fingerprint };
    }

    private async void OnCreateBackup(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var confirmed = await new ConfirmDialog(
            "Backup erstellen",
            "ioBroker wird für ein konsistentes Backup kurz gestoppt und danach automatisch wieder gestartet. " +
            "Gesichert werden iobroker-data und node_modules.",
            "Backup starten").ShowDialog(this);
        if (!confirmed) return;

        await RunUiOperation("Backup wird erstellt …", async token =>
        {
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("backup-create.sh"), AppendRemote, token);
            if (exitCode != 0)
                throw new InvalidOperationException(ScriptFailure("Backup", exitCode));

            var directory = GetBackupDirectory();
            Directory.CreateDirectory(directory);
            var localFile = Path.Combine(directory,
                $"ioBroker-Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.tar.gz");
            AppendLog($"Download nach {localFile} …");
            await _ssh.DownloadFileAsync(profile, RemoteBackup, localFile,
                new Progress<double>(value => SetOperationProgress("Backup wird heruntergeladen …", value)), token);
            AppendLog($"Backup erfolgreich: {localFile}");
            await Dispatcher.UIThread.InvokeAsync(() =>
                new InfoDialog("Backup abgeschlossen",
                    $"Das Backup wurde geprüft und gespeichert:\n\n{localFile}").ShowDialog(this));
        });
    }

    private async void OnCreateCheckpoint(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var downloadAfterCreation = DownloadCheckpointBox.IsChecked == true;
        var confirmed = await new ConfirmDialog(
            "Schnell-Checkpoint erstellen",
            "Dieser Checkpoint ist für Experimente gedacht. Er bleibt direkt auf dem ioBroker-Rechner, " +
            "enthält iobroker-data und node_modules und kann deshalb besonders schnell zurückgerollt werden.\n\n" +
            "Ein eventuell vorhandener Schnell-Checkpoint wird ersetzt." +
            (downloadAfterCreation
                ? "\n\nAnschließend wird er zusätzlich als Datei nach Windows heruntergeladen."
                : ""),
            "Checkpoint erstellen").ShowDialog(this);
        if (!confirmed) return;

        await RunUiOperation("Schnell-Checkpoint wird erstellt …", async token =>
        {
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("checkpoint-create.sh"), AppendRemote, token);
            if (exitCode != 0)
                throw new InvalidOperationException(ScriptFailure("Checkpoint", exitCode));
            AppendLog("Schnell-Checkpoint ist einsatzbereit.");

            string? localFile = null;
            if (downloadAfterCreation)
            {
                SetOperationText("Vorhandene gepackte Checkpoint-Kopie wird für den Download vorbereitet …");
                exitCode = await _ssh.ExecuteScriptAsync(
                    profile, ScriptResources.Load("checkpoint-export.sh"), AppendRemote, token);
                if (exitCode != 0)
                    throw new InvalidOperationException(ScriptFailure("Checkpoint-Download", exitCode));

                var directory = GetBackupDirectory();
                Directory.CreateDirectory(directory);
                localFile = Path.Combine(directory,
                    $"ioBroker-Checkpoint_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.tar.gz");
                await _ssh.DownloadFileAsync(profile, RemoteCheckpointExport, localFile,
                    new Progress<double>(value => SetOperationProgress("Checkpoint wird heruntergeladen …", value)), token);
                AppendLog($"Checkpoint zusätzlich heruntergeladen: {localFile}");
            }

            var resultMessage = localFile is null
                ? "Du kannst jetzt an ioBroker arbeiten. Wenn etwas schiefgeht, genügt „Linux zurücksetzen“."
                : $"Der Linux-Checkpoint ist sofort einsatzbereit und wurde zusätzlich gespeichert:\n\n{localFile}";
            await Dispatcher.UIThread.InvokeAsync(() =>
                new InfoDialog("Checkpoint bereit",
                    resultMessage).ShowDialog(this));
        });
    }

    private async void OnRestoreCheckpoint(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var confirmed = await new ConfirmDialog(
            "Sofort auf Checkpoint zurücksetzen",
            "Der aktuelle ioBroker-Zustand wird gegen den vorbereiteten Schnell-Checkpoint getauscht. " +
            "Es findet kein Upload und kein Entpacken statt.\n\n" +
            "Der vorhandene Checkpoint wird zurückkopiert und bleibt danach weiterhin vorhanden. " +
            "Es wird keine zusätzliche Vorher-/Nachher-Sicherung behalten.",
            "Jetzt zurücksetzen", true).ShowDialog(this);
        if (!confirmed) return;

        await RunUiOperation("Schnell-Rollback läuft …", async token =>
        {
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("checkpoint-restore.sh"), AppendRemote, token);
            if (exitCode != 0)
                throw new InvalidOperationException(ScriptFailure("Schnell-Rollback", exitCode));
            AppendLog("Schnell-Rollback erfolgreich; ioBroker wurde gestartet.");
            await Dispatcher.UIThread.InvokeAsync(() =>
                new InfoDialog("Sofort-Rollback abgeschlossen",
                    "Der vorbereitete Zustand läuft wieder. Du kannst direkt weiterarbeiten.").ShowDialog(this));
        });
    }

    private async void OnChooseRestore(object? sender, RoutedEventArgs e)
    {
        var source = await new RestoreChoiceDialog().ShowDialog<RestoreSource>(this);
        switch (source)
        {
            case RestoreSource.LinuxCheckpoint:
                OnRestoreCheckpoint(sender, e);
                break;
            case RestoreSource.LocalFile:
                OnRestoreBackup(sender, e);
                break;
        }
    }

    private async void OnRestoreBackup(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "ioBroker-Backup auswählen",
            AllowMultiple = false,
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(GetBackupDirectory()),
            FileTypeFilter =
            [
                new FilePickerFileType("ioBroker-Backup") { Patterns = ["*.tar.gz", "*.tgz"] },
                FilePickerFileTypes.All
            ]
        });
        var file = files.FirstOrDefault();
        if (file is null) return;
        var localPath = file.TryGetLocalPath();
        if (localPath is null)
        {
            await ShowInfo("Datei nicht verfügbar", "Die ausgewählte Datei ist nicht lokal erreichbar.");
            return;
        }

        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var confirmed = await new ConfirmDialog(
            "Backup wiederherstellen",
            $"Die Datei wird hochgeladen und anschließend direkt nach /opt/iobroker entpackt.\n\n{Path.GetFileName(localPath)}\n\n" +
            "ioBroker wird dafür gestoppt und danach wieder gestartet.",
            "Wiederherstellen", true).ShowDialog(this);
        if (!confirmed) return;

        await RunUiOperation("Backup wird wiederhergestellt …", async token =>
        {
            AppendLog($"Upload: {localPath}");
            await _ssh.UploadFileAsync(profile, localPath, RemoteRestore,
                new Progress<double>(value => SetOperationProgress("Backup wird hochgeladen …", value)), token);
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("backup-restore.sh"), AppendRemote, token);
            if (exitCode != 0)
                throw new InvalidOperationException(ScriptFailure("Wiederherstellung", exitCode));
            AppendLog("Wiederherstellung erfolgreich abgeschlossen.");
            await Dispatcher.UIThread.InvokeAsync(() =>
                new InfoDialog("Wiederherstellung abgeschlossen",
                    "Das Backup wurde direkt entpackt und ioBroker wieder gestartet.").ShowDialog(this));
        });
    }

    private async void OnRepair(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var options = await new RepairOptionsDialog().ShowDialog<RepairOptions?>(this);
        if (options is null) return;
        var selected = string.Join(", ", options.SelectedLabels());

        await RunUiOperation("ioBroker wird repariert …", async token =>
        {
            AppendLog($"Gewählte Reparaturstufen: {selected}");
            var repairScript =
                $"export STUDIO_REPAIR_BASE={(options.BaseRepair ? 1 : 0)}\n" +
                $"export STUDIO_REPAIR_NODE={(options.UpdateNode ? 1 : 0)}\n" +
                $"export STUDIO_REPAIR_CONTROLLER={(options.UpdateController ? 1 : 0)}\n" +
                $"export STUDIO_REPAIR_NATIVE={(options.RebuildNativeModules ? 1 : 0)}\n" +
                $"export STUDIO_REPAIR_ADAPTERS={(options.UpdateAdapters ? 1 : 0)}\n" +
                ScriptResources.Load("repair.sh");
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, repairScript, AppendRemote, token);
            if (exitCode == 10)
            {
                AppendLog("Reparatur abgeschlossen: ioBroker läuft, einzelne Stufen meldeten Hinweise.");
                var issueText = _operationIssues.Count == 0
                    ? "Details stehen im Live-Protokoll."
                    : string.Join(Environment.NewLine, _operationIssues);
                await Dispatcher.UIThread.InvokeAsync(() =>
                    new InfoDialog(
                        "Reparatur abgeschlossen – Hinweise",
                        "ioBroker läuft wieder. Eine oder mehrere Nebenstufen konnten nicht vollständig " +
                        $"ausgeführt werden.\n\n{issueText}")
                    .ShowDialog(this));
                return;
            }
            if (exitCode != 0)
            {
                var issueText = _operationIssues.Count == 0
                    ? "Ursache und betroffene Stufen stehen im Protokoll."
                    : string.Join(Environment.NewLine, _operationIssues);
                throw new InvalidOperationException(
                    exitCode == 20
                        ? $"ioBroker konnte nach der Reparatur nicht gestartet werden.\n\n{issueText}"
                        : $"Reparaturlauf wurde unerwartet mit Code {exitCode} beendet. Details siehe Protokoll.");
            }
            AppendLog("Reparatur ohne erkannten Fehler abgeschlossen.");
        });
    }

    private async void OnShowErrors(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        await RunUiOperation("Offizielle ioBroker-Diagnose läuft …", async token =>
        {
            var reportOutput = new StringBuilder();
            void CaptureDiagnosticOutput(string text)
            {
                AppendRemote(text);
                var clean = RemoteProgress.Replace(text, "");
                clean = AnsiEscape.Replace(clean, "");
                clean = TerminalControl.Replace(clean, "");
                reportOutput.Append(clean);
            }
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("error-diagnostics.sh"), CaptureDiagnosticOutput, token);
            if (exitCode != 0)
                throw new InvalidOperationException(ScriptFailure("Fehlerdiagnose", exitCode));

            var report = reportOutput.ToString().Trim();
            if (string.IsNullOrWhiteSpace(report))
                report = "Die offizielle ioBroker-Diagnose hat keine Ausgabe geliefert.";
            await Dispatcher.UIThread.InvokeAsync(() =>
                new DiagnosticReportDialog(report).ShowDialog(this));
        });
    }

    private async void OnInstall(object? sender, RoutedEventArgs e)
    {
        var profile = await EnsureTrustedProfile();
        if (profile is null) return;
        var confirmed = await new ConfirmDialog(
            "ioBroker neu installieren",
            "Diese Funktion ist ausschließlich für ein vorbereitetes 64-Bit-Linux ohne vorhandene ioBroker-Installation. " +
            "Sie lädt den jeweils aktuellen offiziellen ioBroker-Installer; die empfohlene Node.js-Version bestimmt ioBroker selbst.",
            "Installation starten", true).ShowDialog(this);
        if (!confirmed) return;

        await RunUiOperation("ioBroker wird installiert …", async token =>
        {
            var exitCode = await _ssh.ExecuteScriptAsync(
                profile, ScriptResources.Load("iobroker-install.sh"), AppendRemote, token);
            if (exitCode != 0)
            {
                var message = exitCode switch
                {
                    42 => "Das Zielsystem verwendet ein Betriebssystem, das der offizielle ioBroker-Installer nicht unterstützt.",
                    43 => "Das Zielsystem ist kein 64-Bit-System. Aktuelle ioBroker-Versionen benötigen ein 64-Bit-Linux.",
                    44 => "Auf dem Zielsystem ist ioBroker bereits installiert.\n\n" +
                          "Verwende in der Systemzentrale „Reparatur starten“. " +
                          "Die Neuinstallation wurde zum Schutz des vorhandenen Systems nicht ausgeführt.",
                    45 => "Für die Installation fehlen Administratorrechte. Auf dem Zielsystem wurde weder root noch sudo oder doas gefunden.",
                    46 => "Der offizielle ioBroker-Installer konnte nicht heruntergeladen werden. Bitte Internetverbindung, DNS und Systemzeit prüfen.",
                    47 => "Der Download war leer. Der offizielle ioBroker-Installer wurde nicht ausgeführt.",
                    48 => "Die heruntergeladene Datei war kein gültiger ioBroker-Bash-Installer und wurde aus Sicherheitsgründen nicht ausgeführt.",
                    49 => "ioBroker wurde installiert, aber die Besitzerrechte unter /opt/iobroker konnten nicht vollständig auf den Benutzer iobroker gesetzt werden.",
                    _ => $"Der offizielle ioBroker-Installer wurde mit Code {exitCode} beendet. " +
                         "Die genaue Ursache steht unmittelbar darüber im Live-Protokoll."
                };
                throw new InvalidOperationException(message);
            }
            AppendLog("ioBroker-Installation erfolgreich abgeschlossen.");
        });
    }

    private async void OnNetworkProbe(object? sender, RoutedEventArgs e)
    {
        var profile = ReadProfile(requirePassword: false);
        if (profile is null) return;
        await RunUiOperation("Netzwerk wird geprüft …", async token =>
        {
            var result = await _ssh.ProbeAsync(profile.Host, profile.Port, token);
            AppendLog($"Adresse: {result.Address}");
            AppendLog($"Ping: {(result.Ping ? $"OK ({result.Milliseconds} ms)" : "keine Antwort")}");
            AppendLog($"SSH-Port {profile.Port}: {(result.Ssh ? "offen" : "nicht erreichbar")}");
        });
    }

    private void OnOpenRouter(object? sender, RoutedEventArgs e)
    {
        var gateway = _desktop.GetDefaultGateway();
        if (gateway is null)
        {
            _ = ShowInfo("Router nicht gefunden", "Das Standard-Gateway konnte nicht ermittelt werden.");
            return;
        }
        AppendLog($"Router-Seite wird geöffnet: http://{gateway}/");
        _desktop.OpenUrl($"http://{gateway}/");
    }

    private void OnOpenWinScp(object? sender, RoutedEventArgs e)
    {
        var profile = ReadProfile(requirePassword: false);
        if (profile is null) return;
        if (!_desktop.OpenWinScp(profile))
        {
            _desktop.OpenUrl("https://winscp.net/eng/download.php");
            _ = ShowInfo("WinSCP nicht installiert",
                "Die offizielle WinSCP-Downloadseite wurde geöffnet. " +
                "Nach der Installation verbindet ioBroker Studio direkt mit deinem Zielsystem.");
        }
        else
        {
            AppendLog("WinSCP wurde ohne Passwort in der Prozesszeile gestartet.");
        }
    }

    private void OnOpenImager(object? sender, RoutedEventArgs e) =>
        _desktop.OpenUrl("https://www.raspberrypi.com/software/");

    private async void OnOpenHelp(object? sender, RoutedEventArgs e) =>
        await new HelpDialog().ShowDialog(this);

    private void OnOpenBackupFolder(object? sender, RoutedEventArgs e) =>
        _desktop.OpenFolder(GetBackupDirectory());

    private void OnClearLog(object? sender, RoutedEventArgs e) => LogBox.Text = "";

    private async void OnSaveLog(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Protokoll speichern",
            SuggestedFileName = $"ioBroker-Studio_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log",
            DefaultExtension = "log",
            FileTypeChoices = [new FilePickerFileType("Protokoll") { Patterns = ["*.log", "*.txt"] }]
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(LogBox.Text ?? "");
    }

    private void OnCancelOperation(object? sender, RoutedEventArgs e)
    {
        _operationCancellation?.Cancel();
        AppendLog("Abbruch angefordert …");
    }

    private async Task RunUiOperation(string title, Func<CancellationToken, Task> operation)
    {
        if (_operationCancellation is not null)
        {
            await ShowInfo("Aktion läuft", "Bitte die aktuelle Aktion zuerst abschließen oder abbrechen.");
            return;
        }
        _operationCancellation = new CancellationTokenSource();
        _currentOperationText = title;
        _lastOperationActivity = DateTimeOffset.Now;
        BusyText.Text = title;
        BusyDetailBox.Text = "";
        _operationIssues.Clear();
        _remoteFailures.Clear();
        _diagnosticEntries.Clear();
        BusyProgress.IsIndeterminate = true;
        BusyProgress.Value = 0;
        BusyPercent.Text = "";
        BusyElapsed.Text = "00:00";
        CancelOperationButton.IsEnabled = true;
        OperationLabel.Text = title;
        BusyOverlay.IsVisible = true;
        var stopwatch = Stopwatch.StartNew();
        _operationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _operationTimer.Tick += (_, _) =>
        {
            BusyElapsed.Text = stopwatch.Elapsed.ToString(@"mm\:ss");
            var silence = DateTimeOffset.Now - _lastOperationActivity;
            BusyText.Text = silence >= TimeSpan.FromSeconds(5)
                ? $"{_currentOperationText} · keine neue Rückmeldung seit {(int)silence.TotalSeconds}s – Abbrechen ist möglich"
                : _currentOperationText;
        };
        _operationTimer.Start();
        try
        {
            await operation(_operationCancellation.Token);
            OperationLabel.Text = $"Abgeschlossen in {stopwatch.Elapsed:mm\\:ss}";
        }
        catch (OperationCanceledException)
        {
            AppendLog("Aktion abgebrochen.");
            OperationLabel.Text = "Abgebrochen";
        }
        catch (Exception ex)
        {
            AppendLog($"FEHLER: {ex.Message}");
            AppendLog(ex.ToString());
            OperationLabel.Text = "Fehler";
            await ShowInfo("Aktion fehlgeschlagen", ex.Message);
        }
        finally
        {
            _operationTimer?.Stop();
            _operationTimer = null;
            BusyOverlay.IsVisible = false;
            CancelOperationButton.IsEnabled = true;
            _operationCancellation.Dispose();
            _operationCancellation = null;
        }
    }

    private string GetBackupDirectory()
    {
        if (string.IsNullOrWhiteSpace(_settings.BackupDirectory))
            _settings.BackupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ioBroker Backups");
        return _settings.BackupDirectory;
    }

    private void AppendRemote(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(text))
                _lastOperationActivity = DateTimeOffset.Now;
            var safeText = text;
            var password = PasswordBox.Text;
            if (!string.IsNullOrEmpty(password))
                safeText = safeText.Replace(password, "********", StringComparison.Ordinal);
            var visibleText = RemoteProgress.Replace(safeText, "");
            foreach (Match diagnostic in RemoteDiagnostic.Matches(visibleText))
            {
                var entry =
                    $"[{diagnostic.Groups[1].Value}] {diagnostic.Groups[2].Value}\n" +
                    $"Bereich: {diagnostic.Groups[3].Value}\n" +
                    $"Deutsch: {diagnostic.Groups[4].Value}\n" +
                    $"Original: {diagnostic.Groups[5].Value}";
                if (!_diagnosticEntries.Contains(entry))
                    _diagnosticEntries.Add(entry);
            }
            visibleText = RemoteDiagnostic.Replace(visibleText, "");
            visibleText = Regex.Replace(visibleText, @"STUDIO_DIAG_SUMMARY\|\d+\r?\n?", "");
            foreach (Match issue in RemoteIssue.Matches(visibleText))
            {
                var item = $"Fehler {issue.Groups[1].Value}: {issue.Groups[2].Value.Trim()}";
                if (!_operationIssues.Contains(item))
                    _operationIssues.Add(item);
            }
            visibleText = RemoteIssue.Replace(visibleText, "");
            visibleText = AnsiEscape.Replace(visibleText, "");
            visibleText = TerminalControl.Replace(visibleText, "");
            foreach (var line in visibleText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if ((trimmed.Contains("FEHLER", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("Read-only file system", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("No space left", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("Input/output error", StringComparison.OrdinalIgnoreCase)) &&
                    !_remoteFailures.Contains(trimmed))
                    _remoteFailures.Add(trimmed);
            }
            LogBox.Text += visibleText;
            LogBox.CaretIndex = LogBox.Text?.Length ?? 0;

            var progressMatch = RemoteProgress.Matches(safeText).LastOrDefault();
            if (progressMatch is not null)
            {
                var percent = Math.Clamp(int.Parse(progressMatch.Groups[1].Value), 0, 100);
                SetProgressUi(percent, progressMatch.Groups[2].Value.Trim());
            }

            BusyDetailBox.Text += visibleText;
            if (BusyDetailBox.Text?.Length > 6000)
                BusyDetailBox.Text = BusyDetailBox.Text[^5000..];
            BusyDetailBox.CaretIndex = BusyDetailBox.Text?.Length ?? 0;
        });
    }

    private string ScriptFailure(string action, int exitCode)
    {
        var cause = _remoteFailures.Count == 0
            ? "Das Zielsystem lieferte keine zusätzliche Fehlerzeile. Das vollständige Live-Protokoll enthält die vorausgehenden Befehlsausgaben."
            : string.Join(Environment.NewLine, _remoteFailures.TakeLast(8));
        return $"{action} fehlgeschlagen.\n\n{cause}\n\nTechnischer Exitcode: {exitCode}";
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            LogBox.CaretIndex = LogBox.Text?.Length ?? 0;
        });
    }

    private void SetOperationText(string text) =>
        Dispatcher.UIThread.Post(() =>
        {
            _currentOperationText = text;
            _lastOperationActivity = DateTimeOffset.Now;
            BusyText.Text = text;
        });

    private void SetOperationProgress(string text, double value) =>
        Dispatcher.UIThread.Post(() =>
        {
            var percent = (int)Math.Round(Math.Clamp(value, 0, 1) * 100);
            SetProgressUi(percent, text);
        });

    private void SetProgressUi(int percent, string text)
    {
        _currentOperationText = text;
        _lastOperationActivity = DateTimeOffset.Now;
        BusyProgress.IsIndeterminate = false;
        BusyProgress.Value = percent;
        BusyPercent.Text = $"{percent} %";
        BusyText.Text = text;
        if (text.StartsWith("ioBroker ist bereit", StringComparison.OrdinalIgnoreCase))
            CancelOperationButton.IsEnabled = false;
    }

    private void SetConnectionState(bool connected, string text) =>
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionText.Text = text;
            ConnectionDot.Fill = Brush.Parse(connected ? "#39D98A" : "#E36773");
            ConnectionPill.Background = Brush.Parse(connected ? "#173A2D" : "#40242A");
        });

    private Task ShowInfo(string title, string message) =>
        new InfoDialog(title, message).ShowDialog(this);
}
