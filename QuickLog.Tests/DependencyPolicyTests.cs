using Xunit;

namespace QuickLog.Tests;

public sealed class DependencyPolicyTests
{
    [Fact]
    public void RuntimeProjects_HaveNoPackageReferences()
    {
        var root = FindRepoRoot();
        var runtimeProjects = new[]
        {
            Path.Combine(root, "QuickLog", "QuickLog.csproj"),
            Path.Combine(root, "QuickLog.Tools", "QuickLog.Tools.csproj"),
            Path.Combine(root, "QuickLog.Sample", "QuickLog.Sample.csproj"),
            Path.Combine(root, "samples", "QuickLog.LinuxSmoke", "QuickLog.LinuxSmoke.csproj")
        };

        foreach (var project in runtimeProjects)
        {
            var xml = File.ReadAllText(project);
            Assert.DoesNotContain("<PackageReference", xml, StringComparison.OrdinalIgnoreCase);
        }
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
