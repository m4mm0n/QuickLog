using Xunit;

namespace QuickLog.Tests;

public sealed class LogRateLimiterTests
{
    [Fact]
    public void LogOnce_WritesOnlyFirstEntryForKey()
    {
        using var logger = new QuickLog.Loggers.QuickLogger { EnableAsyncLogging = true, AsyncOnly = true };

        logger.LogOnce("missing-texture", LogType.Warn, "Missing texture");
        logger.LogOnce("missing-texture", LogType.Warn, "Missing texture");
        logger.Shutdown();

        Assert.Single(logger.GetRecentLogs(), entry => entry.Message == "Missing texture");
    }

    [Fact]
    public void LogEvery_SuppressesEntriesInsideInterval()
    {
        using var logger = new QuickLog.Loggers.QuickLogger { EnableAsyncLogging = true, AsyncOnly = true };

        logger.LogEvery("network-spam", TimeSpan.FromMinutes(1), LogType.Warn, "Network retrying");
        logger.LogEvery("network-spam", TimeSpan.FromMinutes(1), LogType.Warn, "Network retrying");
        logger.Shutdown();

        Assert.Single(logger.GetRecentLogs(), entry => entry.Message == "Network retrying");
    }

    [Fact]
    public void LogFrameTime_FlagsHitchAboveThreshold()
    {
        using var logger = new QuickLog.Loggers.QuickLogger { EnableAsyncLogging = true, AsyncOnly = true };

        logger.LogFrameTime(42, TimeSpan.FromMilliseconds(38), TimeSpan.FromMilliseconds(16));
        logger.Shutdown();

        Assert.Contains(logger.GetRecentLogs(), entry => entry.Message?.Contains("FRAME HITCH frame=42") == true);
    }
}
