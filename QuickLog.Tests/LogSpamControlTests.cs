using QuickLog.Core;
using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogSpamControlTests
{
    [Fact]
    public void DuplicateMessages_AreCoalescedIntoSummary()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.SpamControl = new LogSpamControlOptions
        {
            Enabled = true,
            DuplicateThreshold = 3
        };

        logger.Log(LogType.Warn, "same warning");
        logger.Log(LogType.Warn, "same warning");
        logger.Log(LogType.Warn, "same warning");
        logger.Log(LogType.Warn, "different warning");
        logger.Shutdown();

        var logs = logger.GetRecentLogs();
        Assert.Contains(logs, e => e.Message != null && e.Message.Contains("repeated 2 times"));
        Assert.Contains(logs, e => e.Message == "different warning");
    }

    [Fact]
    public void DuplicateMessages_BelowThreshold_ArePreserved()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.SpamControl = new LogSpamControlOptions
        {
            Enabled = true,
            DuplicateThreshold = 4
        };

        logger.Log(LogType.Info, "short burst");
        logger.Log(LogType.Info, "short burst");
        logger.Log(LogType.Info, "next");
        logger.Shutdown();

        var logs = logger.GetRecentLogs();
        Assert.Equal(2, logs.Count(e => e.Message == "short burst"));
        Assert.DoesNotContain(logs, e => e.Message != null && e.Message.Contains("repeated"));
    }
}
