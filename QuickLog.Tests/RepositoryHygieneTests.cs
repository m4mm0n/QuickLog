using Xunit;

namespace QuickLog.Tests;

public sealed class RepositoryHygieneTests
{
    [Fact]
    public void GeneratedSourceBannerSystem_IsAbsent()
    {
        var root = FindRepoRoot();
        var projectLabel = "Project" + new string(' ', 8) + ":";
        var bodyMarker = "CRC32" + "-BODY:";

        Assert.False(File.Exists(Path.Combine(root, ".headerconfig.json")));
        Assert.False(File.Exists(Path.Combine(root, "scripts", "update-file-headers.ps1")));
        Assert.False(Directory.Exists(Path.Combine(root, "docs", "super" + "powers")));

        foreach (var sourceRoot in SourceRoots(root))
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain(projectLabel, text, StringComparison.Ordinal);
                Assert.DoesNotContain(bodyMarker, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void RepositoryLicense_IsPresent()
    {
        var license = File.ReadAllText(Path.Combine(FindRepoRoot(), "LICENSE"));

        Assert.StartsWith("MIT License", license, StringComparison.Ordinal);
        Assert.Contains("ZeroLinez Softworx", license, StringComparison.Ordinal);
    }

    private static IEnumerable<string> SourceRoots(string root)
    {
        yield return Path.Combine(root, "QuickLog");
        yield return Path.Combine(root, "QuickLog.Sample");
        yield return Path.Combine(root, "QuickLog.Tests");
        yield return Path.Combine(root, "QuickLog.Tools");
        yield return Path.Combine(root, "samples");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }
}
