using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

public sealed class QuickLoggerCloneTests
{
    [Fact]
    public void CloneDeep_WithNewFilePath_DoesNotRedirectOriginalLogger()
    {
        var root = Path.Combine(Path.GetTempPath(), $"quicklog-clone-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var originalPath = Path.Combine(root, "original.log");
        var clonePath = Path.Combine(root, "clone.log");

        using (var original = new QuickLogger(originalPath, fileLogging: true))
        using (var clone = original.CloneDeep(clonePath))
        {
            original.Log(LogType.Info, "original-only");
            clone.Log(LogType.Info, "clone-only");
        }

        var originalText = File.ReadAllText(originalPath);
        var cloneText = File.ReadAllText(clonePath);
        Assert.Contains("original-only", originalText, StringComparison.Ordinal);
        Assert.DoesNotContain("clone-only", originalText, StringComparison.Ordinal);
        Assert.Contains("clone-only", cloneText, StringComparison.Ordinal);
        Assert.DoesNotContain("original-only", cloneText, StringComparison.Ordinal);

        Directory.Delete(root, recursive: true);
    }
}
