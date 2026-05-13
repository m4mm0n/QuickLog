namespace QuickLog.Tools.Diagnostics;

public readonly record struct DiagnosticPortProbeResult(bool Available, string Detail);

public static class DiagnosticPortProbe
{
    public static DiagnosticPortProbeResult Probe(int pid)
    {
        if (OperatingSystem.IsWindows())
        {
            return new DiagnosticPortProbeResult(
                false,
                "Default .NET diagnostic named-pipe probing is intentionally not opened without a diagnostics client dependency.");
        }

        var temp = Path.GetTempPath();
        var matches = Directory.Exists(temp)
            ? Directory.EnumerateFiles(temp, $"dotnet-diagnostic-{pid}-*-socket", SearchOption.TopDirectoryOnly).ToArray()
            : [];

        return matches.Length > 0
            ? new DiagnosticPortProbeResult(true, string.Join(", ", matches.Select(Path.GetFileName)))
            : new DiagnosticPortProbeResult(false, "No default diagnostic socket was found.");
    }
}
