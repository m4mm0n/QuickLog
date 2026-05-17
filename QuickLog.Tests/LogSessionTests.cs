using QuickLog.Core;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogSessionTests
{
    [Fact]
    public void BeginCheckpointBookmarkEnd_WriteExpectedMarkers()
    {
        using var logger = new QuickLog.Loggers.QuickLogger
        {
            EnableAsyncLogging = true,
            AsyncOnly = true
        };

        using (var session = LogSession.Begin(logger, "startup", "session-1"))
        {
            session.Checkpoint("assets-loaded");
            session.Bookmark("BossFightStarted");
        }

        logger.Shutdown();

        var text = string.Join("\n", logger.GetRecentLogs().Select(entry => entry.Message));
        Assert.Contains("SESSION BEGIN startup session=session-1", text);
        Assert.Contains("CHECKPOINT assets-loaded", text);
        Assert.Contains("BOOKMARK BossFightStarted", text);
        Assert.Contains("SESSION END startup session=session-1", text);
    }

    [Fact]
    public void StartupAndShutdownSummary_WriteCompactRuntimeFacts()
    {
        using var logger = new QuickLog.Loggers.QuickLogger
        {
            EnableAsyncLogging = true,
            AsyncOnly = true,
            EmitStartupBanner = true,
            EmitShutdownSummary = true,
            SessionName = "test",
            SessionId = "fixed"
        };

        logger.EmitStartup();
        logger.Shutdown();

        var text = string.Join("\n", logger.GetRecentLogs().Select(entry => entry.Message));
        Assert.Contains("STARTUP app=", text);
        Assert.Contains("session=fixed", text);
        Assert.Contains("SHUTDOWN durationMs=", text);
    }
}
