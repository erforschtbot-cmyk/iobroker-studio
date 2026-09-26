using System.Reflection;

namespace IoBrokerTool.Services;

public static class ScriptResources
{
    public static string Load(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var suffix = $".Scripts.{fileName}";
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Script resource '{fileName}' not found.");
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Script resource '{resourceName}' cannot be read.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
