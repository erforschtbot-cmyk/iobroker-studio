using AvaloniaTerminal;
using IoBrokerTool.Models;
using Renci.SshNet;

namespace IoBrokerTool.Services;

public sealed class EmbeddedTerminalService : IDisposable
{
    private readonly object _writeLock = new();
    private SshClient? _client;
    private ShellStream? _shell;
    private CancellationTokenSource? _readCancellation;
    private Task? _readTask;

    public TerminalControlModel Model { get; } = new(new TerminalOptions
    {
        Cols = 80,
        Rows = 18,
        Scrollback = 3000,
        ReflowOnResize = false,
        TermName = "xterm-256color"
    });

    public event Action<string, bool>? StatusChanged;

    public EmbeddedTerminalService()
    {
        Model.SizeChanged += (_, e) =>
        {
            lock (_writeLock)
            {
                if (_shell is null)
                    return;
                try
                {
                    _shell.ChangeWindowSize(
                        (uint)Math.Max(1, e.Cols),
                        (uint)Math.Max(1, e.Rows),
                        (uint)Math.Max(1, e.Width),
                        (uint)Math.Max(1, e.Height));
                }
                catch { }
            }
        };
        Model.UserInput += (_, e) =>
        {
            lock (_writeLock)
            {
                if (_shell is not { CanWrite: true })
                    return;
                try
                {
                    _shell.Write(e.Data.Span);
                    _shell.Flush();
                }
                catch
                {
                    StatusChanged?.Invoke("Verbindung getrennt", false);
                }
            }
        };
    }

    public async Task ConnectAsync(
        ConnectionProfile profile,
        CancellationToken cancellationToken)
    {
        await DisconnectAsync();
        Model.Feed("\u001bc");
        Model.Feed(
            $"\u001b[38;2;85;225;200mioBroker Studio Terminal\u001b[0m\r\n" +
            $"Verbinde mit {profile.Username}@{profile.Host}:{profile.Port} …\r\n");
        StatusChanged?.Invoke("Verbindung wird aufgebaut …", false);

        var client = new SshClient(
            profile.Host,
            profile.Port,
            profile.Username,
            profile.Password)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(20),
            ConnectionInfo = { Timeout = TimeSpan.FromSeconds(15) }
        };
        client.HostKeyReceived += (_, e) =>
        {
            var fingerprint = Convert.ToHexString(e.FingerPrint).ToLowerInvariant();
            e.CanTrust =
                !string.IsNullOrWhiteSpace(profile.KnownFingerprint) &&
                string.Equals(
                    profile.KnownFingerprint,
                    fingerprint,
                    StringComparison.OrdinalIgnoreCase);
        };

        try
        {
            await Task.Run(client.Connect, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var shell = client.CreateShellStream(
                "xterm-256color",
                80,
                18,
                1000,
                500,
                64 * 1024);

            _client = client;
            _shell = shell;
            _readCancellation = new CancellationTokenSource();
            _readTask = ReadLoopAsync(shell, _readCancellation.Token);
            StatusChanged?.Invoke("Terminal verbunden", true);
        }
        catch
        {
            client.Dispose();
            StatusChanged?.Invoke("Terminal nicht verbunden", false);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        var cancellation = _readCancellation;
        var shell = _shell;
        var client = _client;
        var readTask = _readTask;
        _readCancellation = null;
        _shell = null;
        _client = null;
        _readTask = null;

        if (cancellation is not null)
            await cancellation.CancelAsync();
        try { shell?.Dispose(); } catch { }
        try
        {
            if (client?.IsConnected == true)
                client.Disconnect();
        }
        catch { }
        client?.Dispose();
        cancellation?.Dispose();

        if (readTask is not null)
        {
            try { await readTask.WaitAsync(TimeSpan.FromSeconds(1)); }
            catch { }
        }
    }

    private async Task ReadLoopAsync(
        ShellStream shell,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested && shell.CanRead)
            {
                var read = await shell.ReadAsync(buffer, cancellationToken);
                if (read <= 0)
                    break;
                Model.Feed(buffer, read);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Model.Feed($"\r\n\u001b[31mTerminalverbindung beendet: {ex.Message}\u001b[0m\r\n");
        }
        finally
        {
            StatusChanged?.Invoke("Verbindung getrennt", false);
        }
    }

    public void Dispose()
    {
        try { DisconnectAsync().GetAwaiter().GetResult(); } catch { }
    }
}
