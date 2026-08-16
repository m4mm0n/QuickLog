namespace QuickLog.Tools.Diagnostics;

/// <summary>Describes whether a default .NET diagnostic endpoint is available for a process.</summary>
public readonly record struct DiagnosticPortProbeResult(bool Available, string Detail);

/// <summary>Provides a lightweight probe for platform diagnostic endpoints.</summary>
public static class DiagnosticPortProbe
{
    /// <summary>Checks whether a default diagnostic endpoint can be discovered for a process.</summary>
    /// <param name="pid">The process identifier to inspect.</param>
    /// <returns>The endpoint availability and a human-readable diagnostic detail.</returns>
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
