using System.Diagnostics;
using QuickLog.Core;
using QuickLog.Loggers;

namespace QuickLog.Tools.Commands;

/// <summary>Measures logging throughput, allocation, output size, and dispatcher counters.</summary>
public static class BenchmarkCommand
{
    /// <summary>Executes a logging benchmark using the requested sink mode and iteration count.</summary>
    /// <param name="command">The benchmark options.</param>
    /// <param name="console">The destination for benchmark results.</param>
    /// <param name="cancellationToken">A token that cancels the benchmark.</param>
    /// <returns>A task containing the command result.</returns>
    public static Task<CommandResult> ExecuteAsync(
        BenchmarkToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var iterations = Math.Max(1, command.Iterations);
        var directory = Path.Combine(Path.GetTempPath(), $"quicklog_bench_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();

        using var logger = CreateLogger(command.Mode, directory);
        for (var i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.Log(LogType.Info, command.Mode is "redaction"
                ? $"benchmark token=secret-{i}"
                : $"benchmark message {i}");
        }

        logger.Shutdown();
        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var stats = logger.GetStats();
        var bytes = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path).Length)
            .Sum();

        var logsPerSecond = iterations / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.000001);
        console.WriteLine($"Mode: {command.Mode}");
        console.WriteLine($"Iterations: {iterations}");
        console.WriteLine($"ElapsedMs: {stopwatch.Elapsed.TotalMilliseconds:F2}");
        console.WriteLine($"logs/sec: {logsPerSecond:F0}");
        console.WriteLine($"AllocatedBytes: {allocated}");
        console.WriteLine($"OutputBytes: {bytes}");
        console.WriteLine($"Dropped: {stats.DroppedTotal}");
        console.WriteLine($"SinkFailures: {stats.SinkFailures}");

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Benchmark output is best-effort cleanup.
        }

        return Task.FromResult(CommandResult.Ok());
    }

    private static QuickLogger CreateLogger(string mode, string directory)
    {
        var logger = new QuickLogger
        {
            EnableConsoleLogging = false,
            EnableFileLogging = false,
            EnableTraceLogging = false,
            EnableEventLogging = false,
            EnableAsyncLogging = mode is not "sync",
            AsyncOnly = mode is not "sync",
            AsyncQueueCapacity = 65536
        };

        switch (mode)
        {
            case "binary":
                logger.EnableAsyncBinaryLogging = true;
                logger.BinaryLogPath = Path.Combine(directory, "bench.qlog");
                break;
            case "json":
                logger.JsonLogPath = Path.Combine(directory, "bench.jsonl");
                break;
            case "redaction":
                logger.JsonLogPath = Path.Combine(directory, "bench.jsonl");
                logger.Redaction = new LogRedactionOptions();
                break;
            case "spam":
                logger.JsonLogPath = Path.Combine(directory, "bench.jsonl");
                logger.SpamControl = new LogSpamControlOptions { Enabled = true, DuplicateThreshold = 2 };
                break;
            case "async":
                logger.JsonLogPath = Path.Combine(directory, "bench.jsonl");
                break;
        }

        return logger;
    }
}
