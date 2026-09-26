using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using IoBrokerTool.Models;

namespace IoBrokerTool.Services;

public sealed class DesktopService
{
    public void OpenUrl(string url) => Start(new ProcessStartInfo(url) { UseShellExecute = true });

    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public bool OpenTerminal(ConnectionProfile profile)
    {
        var putty = FindExecutable(
            Path.Combine(AppContext.BaseDirectory, "Tools", "putty.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PuTTY", "putty.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PuTTY", "putty.exe"),
            "putty.exe");
        if (putty is not null)
        {
            var info = new ProcessStartInfo(putty)
            {
                UseShellExecute = true
            };
            info.ArgumentList.Add("-P");
            info.ArgumentList.Add(profile.Port.ToString());

            string? passwordFile = null;
            if (!string.IsNullOrEmpty(profile.Password))
            {
                var tempDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Elzershark", "ioBroker Studio", "Temp");
                Directory.CreateDirectory(tempDirectory);
                foreach (var staleFile in Directory.EnumerateFiles(tempDirectory, "putty-*.txt"))
                {
                    try
                    {
                        if (File.GetCreationTimeUtc(staleFile) < DateTime.UtcNow.AddMinutes(-2))
                            File.Delete(staleFile);
                    }
                    catch { }
                }
                passwordFile = Path.Combine(tempDirectory, $"putty-{Guid.NewGuid():N}.txt");
                File.WriteAllText(passwordFile, profile.Password);
                File.SetAttributes(passwordFile, FileAttributes.Hidden | FileAttributes.Temporary);
                info.ArgumentList.Add("-pwfile");
                info.ArgumentList.Add(passwordFile);
            }
            info.ArgumentList.Add($"{profile.Username}@{profile.Host}");
            Start(info);
            if (passwordFile is not null)
                _ = DeleteTemporaryPasswordFile(passwordFile);
            return true;
        }

        var ssh = FindExecutable(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OpenSSH", "ssh.exe"),
            "ssh.exe");
        if (ssh is null)
            return false;
        Start(new ProcessStartInfo(ssh)
        {
            UseShellExecute = true,
            ArgumentList = { "-p", profile.Port.ToString(), $"{profile.Username}@{profile.Host}" }
        });
        return true;
    }

    public bool OpenWinScp(ConnectionProfile profile)
    {
        var winscp = FindExecutable(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinSCP", "WinSCP.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinSCP", "WinSCP.exe"),
            "WinSCP.exe");
        if (winscp is null)
            return false;
        var user = Uri.EscapeDataString(profile.Username);
        var host = profile.Host.Contains(':') ? $"[{profile.Host}]" : profile.Host;
        Start(new ProcessStartInfo(winscp)
        {
            UseShellExecute = true,
            ArgumentList = { $"sftp://{user}@{host}:{profile.Port}/" }
        });
        return true;
    }

    public string? GetDefaultGateway()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up)
            .SelectMany(network => network.GetIPProperties().GatewayAddresses)
            .Select(gateway => gateway.Address)
            .FirstOrDefault(address =>
                address.AddressFamily == AddressFamily.InterNetwork &&
                !address.Equals(IPAddress.Any))
            ?.ToString();
    }

    private static string? FindExecutable(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (Path.IsPathRooted(candidate) && File.Exists(candidate))
                return candidate;
            if (!Path.IsPathRooted(candidate))
            {
                var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                var match = paths.Select(path => Path.Combine(path, candidate)).FirstOrDefault(File.Exists);
                if (match is not null)
                    return match;
            }
        }
        return null;
    }

    private static void Start(ProcessStartInfo info) => Process.Start(info);

    private static async Task DeleteTemporaryPasswordFile(string path)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15));
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // The next app start can safely clean stale files.
        }
    }
}
