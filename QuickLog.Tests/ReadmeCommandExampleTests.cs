using QuickLog.Tools;
using Xunit;

namespace QuickLog.Tests;

/// <summary>
/// Verifies that marked QuickLog CLI examples in the README stay parseable.
/// </summary>
public sealed class ReadmeCommandExampleTests
{
    /// <summary>
    /// Parses every README command line marked with the quicklog-test marker.
    /// </summary>
    [Fact]
    public void MarkedQuickLogExamples_Parse()
    {
        var root = FindRepoRoot();
        var readme = File.ReadAllLines(Path.Combine(root, "README.md"));

        for (var i = 0; i < readme.Length; i++)
        {
            if (!readme[i].Contains("# quicklog-test: parses", StringComparison.Ordinal))
                continue;

            Assert.True(i + 1 < readme.Length, "Marker must be followed by a command.");
            var command = readme[i + 1];
            var marker = "-- ";
            var index = command.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(index >= 0, $"Example must use dotnet run --project QuickLog.Tools -- : {command}");

            var args = SplitArgs(command[(index + marker.Length)..]);
            var parsed = ToolCommandParser.Parse(args);
            Assert.True(parsed.Success, parsed.Error);
        }
    }

    private static string[] SplitArgs(string command)
        => command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }
}
