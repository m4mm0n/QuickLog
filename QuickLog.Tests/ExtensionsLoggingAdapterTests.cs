using Microsoft.Extensions.Logging;
using QuickLog.Extensions.Logging;
using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

public sealed class ExtensionsLoggingAdapterTests
{
    [Fact]
    public void Provider_ForwardsCategoryEventStateScopeAndException()
    {
        using var quickLogger = new QuickLogger { EnableAsyncLogging = true, AsyncOnly = true };
        using var factory = LoggerFactory.Create(builder =>
            builder.ClearProviders().AddQuickLog(quickLogger));
        var logger = factory.CreateLogger("Game.Network");
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["Session"] = "alpha",
            ["Category"] = "caller-supplied"
        });

        logger.LogWarning(
            new EventId(301, "PacketRetry"),
            new InvalidOperationException("socket closed"),
            "Retrying packet {PacketId} after {DelayMs} ms",
            17,
            250);
        quickLogger.Shutdown();

        var entry = Assert.Single(quickLogger.GetRecentLogs());
        Assert.Equal(LogType.Warn, entry.LoggingType);
        Assert.Equal(new LogEventId(301, "PacketRetry"), entry.EventId);
        Assert.Equal("Game.Network", entry.Properties["Category"]);
        Assert.Equal(17, entry.Properties["PacketId"]);
        Assert.Equal(250, entry.Properties["DelayMs"]);
        Assert.Equal("alpha", entry.Properties["Session"]);
        Assert.Contains("socket closed", entry.Message);
    }

    [Fact]
    public void Provider_RespectsQuickLogMinimumLevel()
    {
        using var quickLogger = new QuickLogger
        {
            EnableAsyncLogging = true,
            AsyncOnly = true,
            MinimumLevel = LogType.Error
        };
        using var provider = new QuickLogLoggerProvider(quickLogger);
        var logger = provider.CreateLogger("Filtered");

        logger.LogInformation("ignored");
        logger.LogError("kept");
        quickLogger.Shutdown();

        var entry = Assert.Single(quickLogger.GetRecentLogs());
        Assert.Equal("kept", entry.Message);
    }
}
