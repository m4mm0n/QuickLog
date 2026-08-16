using QuickLog.Core;
using QuickLog.Sinks;
using Xunit;

namespace QuickLog.Tests;

public sealed class ConsoleFormattingTests
{
    [Fact]
    public void CompactFormatter_OmitsCallerFilePath()
    {
        var text = ConsoleLogFormatter.Format(
            new LogEntry(DateTime.UnixEpoch, LogType.Warn, "hello", "test", null, "Member", "C:/source/file.cs", 10, 1, ThreadRole.Main),
            compact: true,
            useLocalTime: false,
            ansi: false);

        Assert.DoesNotContain("C:/source/file.cs", text);
        Assert.Contains("[Warn]", text);
    }

    [Fact]
    public void AnsiFormatter_AddsEscapeCode()
    {
        var text = ConsoleLogFormatter.Format(
            new LogEntry(DateTime.UnixEpoch, LogType.Error, "boom", "test", null, "Member", string.Empty, 0, 1, ThreadRole.Main),
            compact: true,
            useLocalTime: false,
            ansi: true);

        Assert.Contains("\u001b[", text);
    }
}
