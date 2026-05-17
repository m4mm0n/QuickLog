using QuickLog.Core;
using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogRedactionTests
{
    [Fact]
    public void Redactor_MasksDefaultSecretKeys()
    {
        var redactor = new LogRedactor(new LogRedactionOptions());

        var result = redactor.Redact("password=hunter2 token=abc123 safe=value");

        Assert.Contains("password=***", result);
        Assert.Contains("token=***", result);
        Assert.Contains("safe=value", result);
        Assert.DoesNotContain("hunter2", result);
        Assert.DoesNotContain("abc123", result);
    }

    [Fact]
    public void Redactor_MasksJsonStyleSecrets()
    {
        var redactor = new LogRedactor(new LogRedactionOptions());

        var result = redactor.Redact("{\"api_key\":\"abc123\",\"safe\":\"value\"}");

        Assert.Contains("\"api_key\":\"***\"", result);
        Assert.Contains("\"safe\":\"value\"", result);
        Assert.DoesNotContain("abc123", result);
    }

    [Fact]
    public void QuickLogger_RedactsAsyncRecentLogs()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.Redaction = new LogRedactionOptions();

        logger.Log(LogType.Warn, "token=abc123 safe=value");
        logger.Shutdown();

        var entry = logger.GetRecentLogs().Single();
        Assert.Equal("token=*** safe=value", entry.Message);
    }

    [Fact]
    public void CrashSafePreset_RedactsCommonSecretsAndUserPaths()
    {
        var options = LogRedactionOptions.CrashSafe();
        var redactor = new LogRedactor(options);

        var text = redactor.Redact("api_key=abc C:\\Users\\Alice\\AppData\\Local\\Game");

        Assert.DoesNotContain("abc", text);
        Assert.DoesNotContain("Alice", text);
    }
}
