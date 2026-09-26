using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IoBrokerTool.Models;

namespace IoBrokerTool.Services;

public sealed class SettingsService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ioBroker-Studio-v1");
    private readonly string _settingsPath;

    public SettingsService()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(baseDirectory, "Elzershark", "ioBroker Studio");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return CreateDefaults();
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefaults();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, options));
    }

    public string Protect(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "";
        var bytes = Encoding.UTF8.GetBytes(password);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            bytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    public string Unprotect(string protectedPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(protectedPassword))
                return "";
            var bytes = Convert.FromBase64String(protectedPassword);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                bytes = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }

    private static AppSettings CreateDefaults() => new()
    {
        BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ioBroker Backups")
    };
}
