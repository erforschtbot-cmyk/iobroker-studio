namespace IoBrokerTool.Models;

public sealed class AppSettings
{
    public string Host { get; set; } = "raspberrypi";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string ProtectedPassword { get; set; } = "";
    public bool RememberPassword { get; set; }
    public string HostKeyFingerprint { get; set; } = "";
    public string BackupDirectory { get; set; } = "";
}

public sealed record ConnectionProfile(
    string Host,
    int Port,
    string Username,
    string Password,
    string KnownFingerprint);

public sealed record ConnectionTestResult(
    bool Success,
    string Message,
    string Fingerprint,
    string SystemInfo);
