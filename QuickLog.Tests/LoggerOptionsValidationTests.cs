using Xunit;

namespace QuickLog.Tests;

public sealed class LoggerOptionsValidationTests
{
    [Fact]
    public void Validate_FlagsMissingAsyncSink()
    {
        var result = new LoggerOptions()
            .WithAsyncOnly()
            .Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "QL001");
    }

    [Fact]
    public void Validate_FlagsInvalidRotation()
    {
        var opts = new LoggerOptions().WithRotation(0, maxFiles: 0);

        var result = opts.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "QL002");
    }

    [Fact]
    public void Validate_AcceptsEngineProfile()
    {
        var result = LoggerOptions.ForEngine("logs").Validate();

        Assert.True(result.IsValid);
    }
}
