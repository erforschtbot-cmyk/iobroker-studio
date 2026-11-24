using System.Reflection;

namespace IoBrokerTool.Services;

public static class ScriptResources
{
    /// <summary>
    /// Entfernt die Heredoc-Huelle <c>bash -s <<'MARKER' ... MARKER</c>.
    /// Ohne das sieht eine Syntaxpruefung nur einen Datenblock und meldet
    /// immer Erfolg; ein beschaedigtes Skript faellt dann erst im Lauf auf.
    /// </summary>
    private static string Unwrap(string text, string fileName)
    {
        var normalized = text.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        if (lines.Length < 3) return normalized;

        var first = lines[0].Trim();
        const string prefix = "bash -s <<'";
        if (!first.StartsWith(prefix, StringComparison.Ordinal)) return normalized;
        if (!first.EndsWith("'", StringComparison.Ordinal)) return normalized;

        var marker = first[prefix.Length..^1];
        if (marker.Length == 0) return normalized;

        var last = -1;
        for (var i = lines.Length - 1; i > 0; i--)
        {
            if (lines[i].Trim().Length == 0) continue;
            last = i;
            break;
        }
        if (last <= 0 || lines[last].Trim() != marker)
            throw new InvalidOperationException(
                $"Skript '{fileName}' hat keine schliessende Heredoc-Marke '{marker}'.");

        var body = string.Join("\n", lines[1..last]).TrimEnd('\n');
        return body + "\n";
    }

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
        return Unwrap(reader.ReadToEnd(), fileName);
    }
}
