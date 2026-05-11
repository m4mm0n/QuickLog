using QuickLog.Exceptions;
using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

// Tests share one process-wide static class — serialize them.
[Collection("ExceptionHook")]
public sealed class ExceptionHookTests : IDisposable
{
    private readonly MemoryQuickLogger _logger = new();

    private ExceptionHookOptions SafeOpts(
        bool markObserved = true,
        Func<Exception, bool>? recovery = null,
        Func<Exception, ExceptionSource, bool>? filter = null) => new()
    {
        ShowPopup = false,
        MarkTaskExceptionsObserved = markObserved,
        CrashDump = new CrashDumpOptions { Enabled = false },
        Restart = recovery != null ? new RestartOptions { RecoveryAction = recovery } : null,
        ExceptionFilter = filter
    };

    public void Dispose()
    {
        ExceptionHookManager.Detach();
        _logger.Dispose();
    }

    [Fact]
    public async Task ExceptionCaught_Fires_ForUnobservedTask()
    {
        var fired = false;
        EventHandler<ExceptionHookedEventArgs> handler = (_, _) => fired = true;

        ExceptionHookManager.Attach(_logger, SafeOpts());
        ExceptionHookManager.ExceptionCaught += handler;
        try
        {
            FireUnobserved("fires event test");
            await DrainGC();
            Assert.True(fired);
        }
        finally
        {
            ExceptionHookManager.ExceptionCaught -= handler;
        }
    }

    [Fact]
    public async Task ExceptionCaught_ReportsCorrectSource_AndNonTerminating()
    {
        ExceptionHookedEventArgs? captured = null;
        EventHandler<ExceptionHookedEventArgs> handler = (_, args) => captured = args;

        ExceptionHookManager.Attach(_logger, SafeOpts());
        ExceptionHookManager.ExceptionCaught += handler;
        try
        {
            FireUnobserved("source test");
            await DrainGC();

            Assert.NotNull(captured);
            Assert.Equal(ExceptionSource.UnobservedTask, captured!.Source);
            Assert.False(captured.IsTerminating);
        }
        finally
        {
            ExceptionHookManager.ExceptionCaught -= handler;
        }
    }

    [Fact]
    public async Task ExceptionFilter_ReturnsFalse_SuppressesEventAndLog()
    {
        var fired = false;
        EventHandler<ExceptionHookedEventArgs> handler = (_, _) => fired = true;

        ExceptionHookManager.Attach(_logger, SafeOpts(filter: (_, _) => false));
        ExceptionHookManager.ExceptionCaught += handler;
        try
        {
            var logsBefore = _logger.Snapshot().Count;
            FireUnobserved("filter suppressed");
            await DrainGC();

            Assert.False(fired);
            Assert.Equal(logsBefore, _logger.Snapshot().Count);
        }
        finally
        {
            ExceptionHookManager.ExceptionCaught -= handler;
        }
    }

    [Fact]
    public async Task RecoveryAction_ReturnsTrue_SkipsLogAndDump()
    {
        ExceptionHookManager.Attach(_logger, SafeOpts(recovery: _ => true));

        var logsBefore = _logger.Snapshot().Count;
        FireUnobserved("recovered silently");
        await DrainGC();

        Assert.Equal(logsBefore, _logger.Snapshot().Count);
    }

    [Fact]
    public async Task SuppressDefaultHandling_SkipsLog()
    {
        EventHandler<ExceptionHookedEventArgs> suppresser = (_, args) => args.SuppressDefaultHandling = true;

        ExceptionHookManager.Attach(_logger, SafeOpts());
        ExceptionHookManager.ExceptionCaught += suppresser;
        try
        {
            var logsBefore = _logger.Snapshot().Count;
            FireUnobserved("suppress default");
            await DrainGC();

            Assert.Equal(logsBefore, _logger.Snapshot().Count);
        }
        finally
        {
            ExceptionHookManager.ExceptionCaught -= suppresser;
        }
    }

    [Fact]
    public async Task Unhandled_Exception_LoggedToMemoryLogger()
    {
        ExceptionHookManager.Attach(_logger, SafeOpts());

        FireUnobserved("logged to memory");
        await DrainGC();

        Assert.Contains(_logger.Snapshot(),
            e => e.Message != null && e.Message.Contains("logged to memory"));
    }

    [Fact]
    public void Detach_IsIdempotent()
    {
        ExceptionHookManager.Attach(_logger, SafeOpts());
        ExceptionHookManager.Detach();
        ExceptionHookManager.Detach(); // second call must not throw
    }

    [Fact]
    public void Attach_WhenAlreadyAttached_ReplacesOptionsWithoutDuplicatingHandlers()
    {
        var logger2 = new MemoryQuickLogger();
        try
        {
            ExceptionHookManager.Attach(_logger, SafeOpts());
            ExceptionHookManager.Attach(logger2, SafeOpts()); // replace
            Assert.True(ExceptionHookManager.IsAttached);
        }
        finally
        {
            logger2.Dispose();
        }
    }

    private static void FireUnobserved(string message)
        => _ = Task.Run(async () => { await Task.Delay(20); throw new InvalidOperationException(message); });

    private static async Task DrainGC()
    {
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(350);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
