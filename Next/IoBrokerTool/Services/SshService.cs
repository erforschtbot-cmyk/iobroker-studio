using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using IoBrokerTool.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace IoBrokerTool.Services;

public sealed class SshService
{
    private static readonly Regex ExitMarker =
        new(@"__IOBROKER_STUDIO_EXIT__(\d+)", RegexOptions.Compiled);
    private static readonly Regex PrivilegePasswordPrompt =
        new(@"(?:\[sudo\]\s*)?(?:(?:password|passwort|mot de passe|contrase(?:ñ|n)a)" +
            @".*?:|doas.*?(?:password|passwort).*?:)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private const string BeginMarker = "__IOBROKER_STUDIO_BEGIN__";
    public Func<string, CancellationToken, Task<string?>>? PrivilegePasswordRequested { get; set; }

    public async Task<string> GetHostKeyFingerprintAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            string fingerprint = "";
            using var client = new SshClient(
                profile.Host, profile.Port, profile.Username, profile.Password)
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ConnectionInfo = { Timeout = TimeSpan.FromSeconds(15) }
            };
            client.HostKeyReceived += (_, args) =>
            {
                fingerprint = Convert.ToHexString(args.FingerPrint).ToLowerInvariant();
                // Deliberately reject this probe connection. SSH host-key exchange
                // happens before password authentication, so no credential is sent.
                args.CanTrust = false;
            };

            try
            {
                client.Connect();
            }
            catch (SshConnectionException) when (!string.IsNullOrWhiteSpace(fingerprint))
            {
                return fingerprint;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new SshConnectionException(
                    "Der SSH-Schlüssel des Zielsystems konnte nicht abgerufen werden.");
            return fingerprint;
        }, cancellationToken);
    }

    public async Task<ConnectionTestResult> TestAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            string fingerprint = "";
            try
            {
                using var client = CreateClient(profile, false, value => fingerprint = value);
                client.Connect();
                cancellationToken.ThrowIfCancellationRequested();
                var command = client.RunCommand(
                    "printf 'SSH OK | '; uname -srm; " +
                    "if command -v iobroker >/dev/null 2>&1; then " +
                    "printf 'ioBroker '; iobroker version 2>/dev/null || true; fi");
                var info = (command.Result + command.Error).Trim();
                client.Disconnect();
                return new ConnectionTestResult(
                    command.ExitStatus == 0,
                    command.ExitStatus == 0 ? "Verbindung erfolgreich" : "Remote-Test fehlgeschlagen",
                    fingerprint,
                    info);
            }
            catch (SshConnectionException ex)
            {
                return new ConnectionTestResult(false, ex.Message, fingerprint, "");
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult(false, ex.Message, fingerprint, "");
            }
        }, cancellationToken);
    }

    public async Task<int> ExecuteScriptAsync(
        ConnectionProfile profile,
        string script,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            using var client = CreateClient(profile, false, _ => { });
            client.Connect();
            var remotePath = $"/tmp/iobroker-studio-{Guid.NewGuid():N}.sh";

            using (var sftp = CreateSftpClient(profile))
            {
                sftp.Connect();
                using var content = new MemoryStream(Encoding.UTF8.GetBytes(script.Replace("\r\n", "\n")));
                sftp.UploadFile(content, remotePath);
                // SSH.NET expects chmod digits (700), not the decimal value of 0700 (448).
                sftp.ChangePermissions(remotePath, 700);
                sftp.Disconnect();
            }

            using var shell = client.CreateShellStream("xterm", 120, 40, 1280, 800, 32768);
            var markerCommand =
                $"stty -echo 2>/dev/null; export TERM=dumb; " +
                $"printf '__IOBROKER_''STUDIO_BEGIN__\\n'; " +
                $"bash '{remotePath}'; __code=$?; rm -f '{remotePath}'; " +
                "printf '\\n__IOBROKER_STUDIO_EXIT__%s\\n' \"$__code\"";
            shell.WriteLine(markerCommand);

            var allOutput = new StringBuilder();
            var lastPasswordPromptAt = -1;
            var deliveredLength = -1;
            var started = Stopwatch.StartNew();

            try
            {
                while (started.Elapsed < TimeSpan.FromHours(3))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (shell.DataAvailable)
                    {
                        var chunk = shell.Read();
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            allOutput.Append(chunk);
                            var completeOutput = allOutput.ToString();
                            var tail = completeOutput.Length > 1200
                                ? completeOutput[^1200..]
                                : completeOutput;
                            var passwordPrompt = PrivilegePasswordPrompt.Matches(tail).LastOrDefault();
                            if (passwordPrompt is not null)
                            {
                                var absolutePromptAt =
                                    completeOutput.Length - tail.Length + passwordPrompt.Index;
                                if (absolutePromptAt > lastPasswordPromptAt)
                                {
                                    lastPasswordPromptAt = absolutePromptAt;
                                    var password = PrivilegePasswordRequested is null
                                        ? profile.Password
                                        : PrivilegePasswordRequested(profile.Username, cancellationToken)
                                            .GetAwaiter().GetResult();
                                    if (password is null)
                                        throw new OperationCanceledException(
                                            "Passworteingabe wurde abgebrochen.", cancellationToken);
                                    shell.WriteLine(password);
                                }
                            }

                            if (deliveredLength < 0)
                            {
                                var beginAt = completeOutput.IndexOf(
                                    BeginMarker, StringComparison.Ordinal);
                                if (beginAt >= 0)
                                    deliveredLength = beginAt + BeginMarker.Length;
                            }

                            var match = ExitMarker.Match(completeOutput);
                            var visibleEnd = match.Success ? match.Index : completeOutput.Length;
                            if (deliveredLength >= 0 && visibleEnd > deliveredLength)
                            {
                                onOutput(completeOutput[deliveredLength..visibleEnd]);
                                deliveredLength = visibleEnd;
                            }

                            if (match.Success)
                            {
                                client.Disconnect();
                                return int.Parse(match.Groups[1].Value);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(80);
                    }
                }
                throw new TimeoutException("Remote-Aktion hat das Zeitlimit überschritten.");
            }
            finally
            {
                if (client.IsConnected)
                {
                    try { client.RunCommand($"rm -f '{remotePath}'"); } catch { }
                    client.Disconnect();
                }
            }
        }, cancellationToken);
    }

    public async Task UploadFileAsync(
        ConnectionProfile profile,
        string localPath,
        string remotePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var client = CreateSftpClient(profile);
            client.Connect();
            using var input = File.OpenRead(localPath);
            var length = Math.Max(1, input.Length);
            client.UploadFile(input, remotePath, true, uploaded =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)uploaded / length);
            });
            client.Disconnect();
        }, cancellationToken);
    }

    public async Task DownloadFileAsync(
        ConnectionProfile profile,
        string remotePath,
        string localPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var client = CreateSftpClient(profile);
            client.Connect();
            var attributes = client.GetAttributes(remotePath);
            var remoteLength = attributes.Size;
            if (remoteLength <= 0)
                throw new IOException($"Die entfernte Datei '{remotePath}' ist leer.");
            var length = Math.Max(1L, remoteLength);
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            var partialPath = localPath + ".part";
            try
            {
                if (File.Exists(partialPath))
                    File.Delete(partialPath);
                using (var output = new FileStream(
                           partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    client.DownloadFile(remotePath, output, downloaded =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report((double)downloaded / length);
                    });
                    output.Flush(true);
                }

                var downloadedLength = new FileInfo(partialPath).Length;
                if (downloadedLength != remoteLength)
                    throw new IOException(
                        $"Der Download ist unvollständig: erwartet {remoteLength} Byte, " +
                        $"empfangen {downloadedLength} Byte.");

                File.Move(partialPath, localPath, true);
                progress?.Report(1);

                // Der lokale Download ist bereits vollständig und geprüft. Eine
                // fehlende Berechtigung zum Aufräumen auf Linux darf ihn nicht
                // nachträglich als fehlgeschlagen behandeln oder wieder löschen.
                try { client.DeleteFile(remotePath); } catch { }
                client.Disconnect();
            }
            catch
            {
                try { if (File.Exists(partialPath)) File.Delete(partialPath); } catch { }
                throw;
            }
        }, cancellationToken);
    }

    public async Task<(bool Ping, bool Ssh, string Address, long Milliseconds)> ProbeAsync(
        string host,
        int port,
        CancellationToken cancellationToken)
    {
        var pingSuccess = false;
        var elapsed = -1L;
        string address;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            address = addresses.FirstOrDefault()?.ToString() ?? host;
            using var ping = new Ping();
            cancellationToken.ThrowIfCancellationRequested();
            var reply = await ping.SendPingAsync(host, 2000);
            pingSuccess = reply.Status == IPStatus.Success;
            elapsed = reply.RoundtripTime;
        }
        catch
        {
            address = host;
        }

        var sshSuccess = false;
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            sshSuccess = true;
        }
        catch { }
        return (pingSuccess, sshSuccess, address, elapsed);
    }

    private static SshClient CreateClient(
        ConnectionProfile profile,
        bool allowUnknownHost,
        Action<string> fingerprintCallback)
    {
        var client = new SshClient(profile.Host, profile.Port, profile.Username, profile.Password)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(20),
            ConnectionInfo = { Timeout = TimeSpan.FromSeconds(15) }
        };
        client.HostKeyReceived += (_, e) =>
            ValidateHostKey(e, profile, allowUnknownHost, fingerprintCallback);
        return client;
    }

    private static SftpClient CreateSftpClient(ConnectionProfile profile)
    {
        var client = new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(20),
            ConnectionInfo = { Timeout = TimeSpan.FromSeconds(15) }
        };
        client.HostKeyReceived += (_, e) => ValidateHostKey(e, profile, false, null);
        return client;
    }

    private static void ValidateHostKey(
        HostKeyEventArgs args,
        ConnectionProfile profile,
        bool allowUnknownHost,
        Action<string>? callback)
    {
        var fingerprint = Convert.ToHexString(args.FingerPrint).ToLowerInvariant();
        callback?.Invoke(fingerprint);
        args.CanTrust = allowUnknownHost ||
                        (!string.IsNullOrWhiteSpace(profile.KnownFingerprint) &&
                         string.Equals(profile.KnownFingerprint, fingerprint,
                             StringComparison.OrdinalIgnoreCase));
    }
}
