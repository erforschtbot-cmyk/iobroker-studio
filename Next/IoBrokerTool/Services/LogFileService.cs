using System.Text;

namespace IoBrokerTool.Services;

/// Schreibt jede Meldung dauerhaft mit, ohne dass jemand "Speichern" drücken muss.
///
/// Anlass: Ein Fehlerdialog war nach einem erneuten Lauf verschwunden und damit
/// unwiederbringlich. Das Live-Protokoll war reine Anzeige. Ab hier liegt jede
/// Sitzung als Textdatei unter %LOCALAPPDATA%, auch nach einem Absturz.
public sealed class LogFileService
{
    private readonly object _gate = new();
    private bool _started;

    public LogFileService()
    {
        // Neutraler Ordnername: kein Herstellername, damit das Werkzeug auf
        // fremden Rechnern nicht wie ein personenbezogener Ordner wirkt.
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        LogDirectory = Path.Combine(baseDirectory, "ioBroker Studio", "Logs");
        FilePath = Path.Combine(LogDirectory, $"ioBroker-Studio_{DateTime.Now:yyyy-MM-dd}.log");
    }

    public string LogDirectory { get; }
    public string FilePath { get; }

    /// Sorgt dafuer, dass die Datei existiert (leerer Aufruf legt sie an),
    /// damit "Log oeffnen" auch vor der ersten Ausgabe funktioniert.
    public void EnsureFile()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                if (!File.Exists(FilePath))
                    File.AppendAllText(FilePath, $"ioBroker Studio | Sitzung {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n", new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }

    /// Kopfdaten der Sitzung. Wird beim Start einmal geschrieben.
    public void Start(string version, string target)
    {
        lock (_gate)
        {
            if (_started) return;
            _started = true;
            try
            {
                Directory.CreateDirectory(LogDirectory);
                TrimOldLogs();
                AppendRaw(
                    new string('=', 78) + "\n" +
                    $"ioBroker Studio {version} | Sitzung {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Ziel: {target}\n" +
                    $"Rechner: {Environment.MachineName} | {Environment.OSVersion}\n" +
                    new string('=', 78) + "\n");
            }
            catch
            {
                // Ein Protokollfehler darf das Werkzeug nie blockieren.
            }
        }
    }

    /// Trennt eine Aktion im Protokoll sichtbar ab.
    public void Section(string title)
    {
        lock (_gate)
        {
            AppendRaw($"\n----- {DateTime.Now:HH:mm:ss} | {title} -----\n");
        }
    }

    /// Eigene Meldung des Werkzeugs, mit Zeitstempel.
    public void Line(string message)
    {
        lock (_gate)
        {
            AppendRaw($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }

    /// Unveränderte Remote-Ausgabe. Zeitstempel entfällt, damit Fortschritts-
    /// zeilen und Fehlerblöcke so stehen bleiben, wie das Zielsystem sie liefert.
    public void Raw(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_gate)
        {
            AppendRaw(text.TrimEnd('\r', '\n') + "\n");
        }
    }

    public void Footer(string text)
    {
        lock (_gate)
        {
            AppendRaw($"\n----- {DateTime.Now:HH:mm:ss} | {text} -----\n");
        }
    }

    private void AppendRaw(string text)
    {
        try
        {
            File.AppendAllText(FilePath, text, new UTF8Encoding(false));
        }
        catch
        {
            // Still: Schreibfehler (voller Datentraeger, Rechte) duerfen die
            // laufende Aktion nicht unterbrechen.
        }
    }

    private void TrimOldLogs()
    {
        try
        {
            var grenze = DateTime.Now.AddDays(-60);
            foreach (var file in Directory.EnumerateFiles(LogDirectory, "ioBroker-Studio_*.log"))
            {
                if (File.GetLastWriteTime(file) < grenze)
                    File.Delete(file);
            }
        }
        catch
        {
        }
    }
}
