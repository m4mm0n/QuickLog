using Microsoft.Extensions.Logging;
using QuickLog.Extensions.Logging;
using QuickLog.Loggers;

namespace QuickLog.MobileSmoke;

/// <summary>Compile-time smoke surface for Android and iOS consumers.</summary>
public static class MobileLoggingSmoke
{
    /// <summary>Creates a platform profile and emits a structured event through both public APIs.</summary>
    /// <param name="outputDirectory">The application-owned log directory.</param>
    public static async Task RunAsync(string outputDirectory)
    {
        var options = OperatingSystem.IsAndroid()
            ? LoggerOptions.ForAndroid(logDirectory: outputDirectory)
            : LoggerOptions.ForIOS(logDirectory: outputDirectory);
        var logger = options.CreateLogger();
        logger.Log(
            LogType.Info,
            "mobile startup",
            new LogEventId(3100, "MobileStartup"),
            LogProperties.Create(new LogProperty("platform", OperatingSystem.IsAndroid() ? "android" : "ios")));

        using var factory = LoggerFactory.Create(builder => builder.ClearProviders().AddQuickLog(logger));
        factory.CreateLogger("Mobile").LogInformation("Bridge ready");
        await logger.DisposeAsync();
    }
}
