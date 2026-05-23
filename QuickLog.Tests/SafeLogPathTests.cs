using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class SafeLogPathTests
{
    [Fact]
    public void CreateSessionDirectory_SanitizesName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ql_safe_{Guid.NewGuid():N}");
        var path = SafeLogPath.CreateSessionDirectory(root, "bad:name?");

        Assert.True(Directory.Exists(path));
        Assert.DoesNotContain(":", Path.GetFileName(path));
        Assert.DoesNotContain("?", Path.GetFileName(path));
    }

    /// <summary>
    /// Verifies that reserved Windows filename characters are sanitized even when tests run on Linux.
    /// </summary>
    [Fact]
    public void SafeFileName_SanitizesPortableReservedCharacters()
    {
        var name = SafeLogPath.SafeFileName("<>:\"\\|?*");

        Assert.DoesNotContain("<", name);
        Assert.DoesNotContain(">", name);
        Assert.DoesNotContain(":", name);
        Assert.DoesNotContain("\"", name);
        Assert.DoesNotContain("\\", name);
        Assert.DoesNotContain("|", name);
        Assert.DoesNotContain("?", name);
        Assert.DoesNotContain("*", name);
    }
}
