using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

/// <summary>
/// Covers dependency-free QLOG attribute helpers.
/// </summary>
public sealed class QLogAttributeTests
{
    /// <summary>
    /// Verifies that invoking a marked method logs entry, exit, and timing markers.
    /// </summary>
    [Fact]
    public void Invoke_WritesEntryExitAndTiming_ForMarkedMethod()
    {
        using var logger = new MemoryQuickLogger();
        var fixture = new QLogFixture();

        var value = QLogRunner.Invoke(logger, fixture.AddOne);

        Assert.Equal(2, value);
        var text = string.Join("\n", logger.Snapshot().Select(entry => entry.Message));
        Assert.Contains("QLOG ENTER QuickLog.Tests.QLogAttributeTests+QLogFixture.AddOne", text);
        Assert.Contains("QLOG EXIT QuickLog.Tests.QLogAttributeTests+QLogFixture.AddOne durationMs=", text);
    }

    /// <summary>
    /// Verifies that a class-level marker is applied to methods without their own marker.
    /// </summary>
    [Fact]
    public void Invoke_UsesClassLevelAttribute_WhenMethodIsUnmarked()
    {
        using var logger = new MemoryQuickLogger();
        var fixture = new ClassMarkedFixture();

        QLogRunner.Invoke(logger, fixture.Touch);

        var text = string.Join("\n", logger.Snapshot().Select(entry => entry.Message));
        Assert.Contains("QLOG ENTER class-fixture", text);
        Assert.DoesNotContain("QLOG EXIT class-fixture", text);
    }

    /// <summary>
    /// Verifies that exceptions are logged and rethrown when the marker requests exception logging.
    /// </summary>
    [Fact]
    public void Invoke_LogsAndRethrowsException_ForMarkedMethod()
    {
        using var logger = new MemoryQuickLogger();
        var fixture = new QLogFixture();

        var ex = Assert.Throws<InvalidOperationException>(() => QLogRunner.Invoke(logger, fixture.Fail));

        Assert.Equal("sample failure", ex.Message);
        var entry = Assert.Single(logger.Snapshot(), item => item.LoggingType == LogType.Error);
        Assert.Contains("QLOG EXCEPTION QuickLog.Tests.QLogAttributeTests+QLogFixture.Fail", entry.Message);
        Assert.Same(ex, entry.Exception);
    }

    /// <summary>
    /// Verifies that a one-line scope helper can be used inside a marked method.
    /// </summary>
    [Fact]
    public void Scope_UsesCallerAttribute_ForOneLineMethodInstrumentation()
    {
        using var logger = new MemoryQuickLogger();
        var fixture = new QLogFixture();

        fixture.Scoped(logger);

        var text = string.Join("\n", logger.Snapshot().Select(entry => entry.Message));
        Assert.Contains("QLOG ENTER scoped-name", text);
        Assert.Contains("QLOG EXIT scoped-name", text);
    }

    /// <summary>
    /// Verifies that asynchronous delegates are instrumented without dependencies.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WritesMarkers_ForMarkedMethod()
    {
        using var logger = new MemoryQuickLogger();
        var fixture = new QLogFixture();

        var value = await QLogRunner.InvokeAsync(logger, fixture.AddOneAsync);

        Assert.Equal(3, value);
        var text = string.Join("\n", logger.Snapshot().Select(entry => entry.Message));
        Assert.Contains("QLOG ENTER QuickLog.Tests.QLogAttributeTests+QLogFixture.AddOneAsync", text);
        Assert.Contains("QLOG EXIT QuickLog.Tests.QLogAttributeTests+QLogFixture.AddOneAsync durationMs=", text);
    }

    /// <summary>
    /// Verifies that discovery reports explicitly marked types and methods.
    /// </summary>
    [Fact]
    public void Discovery_ReturnsExplicitlyMarkedTargets()
    {
        var targets = QLogDiscovery.Scan(typeof(QLogFixture));

        Assert.Contains(targets, target =>
            target.Kind == QLogTargetKind.Method
            && target.Name.EndsWith(".AddOne", StringComparison.Ordinal)
            && target.Options.HasFlag(QLogOption.Timing));
        Assert.Contains(QLogDiscovery.Scan(typeof(ClassMarkedFixture)), target =>
            target.Kind == QLogTargetKind.Type
            && target.Name.EndsWith("+ClassMarkedFixture", StringComparison.Ordinal)
            && target.DisplayName == "class-fixture");
    }

    private sealed class QLogFixture
    {
        [QLOG(QLogOption.Entry | QLogOption.Exit | QLogOption.Timing)]
        public int AddOne() => 2;

        [QLOG(QLogOption.Entry | QLogOption.Exit | QLogOption.Timing)]
        public Task<int> AddOneAsync() => Task.FromResult(3);

        [QLOG(QLogOption.Exceptions)]
        public void Fail() => throw new InvalidOperationException("sample failure");

        [QLOG(LoggingOption.Default, Name = "scoped-name")]
        public void Scoped(IQuickLog logger)
        {
            using var scope = QLogScope.Enter(logger);
        }
    }

    [QLOG(QLogOption.Entry, Name = "class-fixture")]
    private sealed class ClassMarkedFixture
    {
        public void Touch()
        {
        }
    }
}
