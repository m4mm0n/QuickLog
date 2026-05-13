/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : CrashDumpWriter.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Exceptions;

/// <summary>
/// Writes a structured JSON crash-dump report to disk when an unhandled exception is captured.
/// </summary>
internal static class CrashDumpWriter
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes a crash dump for <paramref name="exception"/> and returns the full path of the
    /// file created, or <see langword="null"/> if writing failed.
    /// </summary>
    internal static string? Write(
        Exception exception,
        ExceptionSource source,
        bool isTerminating,
        CrashDumpOptions opts,
        IReadOnlyList<LogEventArgs>? recentLogs = null,
        LogDispatcherStats? dispatcherStats = null)
    {
        try
        {
            var dir = opts.ResolvedOutputDirectory;
            Directory.CreateDirectory(dir);

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var typeName = SanitizeFileName(exception.GetType().Name);
            var fileName = $"crash_{stamp}_{typeName}.json";
            var fullPath = Path.Combine(dir, fileName);

            var report = BuildReport(exception, source, isTerminating, opts, recentLogs, dispatcherStats);
            var json = JsonSerializer.Serialize(report, _json);
            File.WriteAllText(fullPath, json, Encoding.UTF8);

            Rotate(dir, opts.MaxDumpFiles);
            return fullPath;
        }
        catch
        {
            return null;
        }
    }

    private static CrashReport BuildReport(
        Exception exception,
        ExceptionSource source,
        bool isTerminating,
        CrashDumpOptions opts,
        IReadOnlyList<LogEventArgs>? recentLogs,
        LogDispatcherStats? dispatcherStats)
    {
        var proc = Process.GetCurrentProcess();
        var redactor = new LogRedactor(opts.Redaction);

        return new CrashReport
        {
            Timestamp     = DateTime.UtcNow,
            Source        = source.ToString(),
            IsTerminating = isTerminating,
            RestartCount  = RestartOptions.CurrentRestartCount,
            Exception     = BuildExceptionInfo(exception, redactor),
            Process       = new ProcessInfo
            {
                Id          = proc.Id,
                Name        = proc.ProcessName,
                Executable  = proc.MainModule?.FileName,
                StartTime   = proc.StartTime.ToUniversalTime(),
                ThreadCount = proc.Threads.Count,
                MemoryBytes = proc.WorkingSet64
            },
            Environment = new EnvironmentInfo
            {
                MachineName       = System.Environment.MachineName,
                UserName          = System.Environment.UserName,
                OsVersion         = System.Environment.OSVersion.ToString(),
                RuntimeVersion    = System.Environment.Version.ToString(),
                Is64BitProcess    = System.Environment.Is64BitProcess,
                WorkingDirectory  = System.Environment.CurrentDirectory,
                CommandLine       = redactor.Redact(System.Environment.CommandLine),
                Variables         = opts.IncludeEnvironmentVariables ? GetEnvVars(redactor) : null
            },
            RecentLogs = opts.IncludeRecentLogs
                ? BuildRecentLogs(recentLogs, opts.RecentLogCount, redactor)
                : null,
            Dispatcher = opts.IncludeDispatcherStats ? dispatcherStats : null
        };
    }

    private static List<RecentLogInfo>? BuildRecentLogs(
        IReadOnlyList<LogEventArgs>? recentLogs,
        int count,
        LogRedactor redactor)
    {
        if (recentLogs is null || recentLogs.Count == 0 || count <= 0)
            return null;

        return recentLogs
            .Skip(Math.Max(0, recentLogs.Count - count))
            .Select(e => new RecentLogInfo
            {
                Timestamp     = e.Timestamp,
                Level         = e.LoggingType.ToString(),
                Message       = e.Message is null ? null : redactor.Redact(e.Message),
                Exception     = e.Exception is null ? null : redactor.Redact(e.Exception.ToStringDemystified()),
                Member        = e.CallerName,
                File          = e.CallerFilePath,
                Line          = e.CallerLineNumber,
                Scope         = e.Scope,
                CorrelationId = e.CorrelationId,
                TraceId       = e.TraceId,
                SpanId        = e.SpanId
            })
            .ToList();
    }

    private static ExceptionInfo BuildExceptionInfo(Exception? ex, LogRedactor redactor)
    {
        if (ex is null) return new ExceptionInfo { Type = "null", Message = string.Empty };

        var info = new ExceptionInfo
        {
            Type       = ex.GetType().FullName ?? ex.GetType().Name,
            Message    = redactor.Redact(ex.Message),
            StackTrace = redactor.Redact(ex.ToStringDemystified())
        };

        if (ex is AggregateException agg && agg.InnerExceptions.Count > 0)
            info.InnerExceptions = agg.InnerExceptions.Select(inner => BuildExceptionInfo(inner, redactor)).ToList();
        else if (ex.InnerException is not null)
            info.InnerExceptions = [BuildExceptionInfo(ex.InnerException, redactor)];

        return info;
    }

    private static Dictionary<string, string>? GetEnvVars(LogRedactor redactor)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
            if (entry.Key is string k && entry.Value is string v)
                result[k] = redactor.Redact($"{k}={v}")[(k.Length + 1)..];
        return result;
    }

    private static void Rotate(string dir, int max)
    {
        var files = Directory.GetFiles(dir, "crash_*.json")
            .OrderByDescending(File.GetCreationTimeUtc)
            .Skip(max)
            .ToList();

        foreach (var f in files)
            try { File.Delete(f); } catch { /* best-effort rotation */ }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}

// ── Report model ─────────────────────────────────────────────────────────────

internal sealed class CrashReport
{
    public DateTime       Timestamp     { get; init; }
    public string         Source        { get; init; } = string.Empty;
    public bool           IsTerminating { get; init; }
    public int            RestartCount  { get; init; }
    public ExceptionInfo  Exception     { get; init; } = new();
    public ProcessInfo    Process       { get; init; } = new();
    public EnvironmentInfo Environment  { get; init; } = new();
    public List<RecentLogInfo>? RecentLogs { get; init; }
    public LogDispatcherStats? Dispatcher { get; init; }
}

internal sealed class ExceptionInfo
{
    public string              Type             { get; init; } = string.Empty;
    public string              Message          { get; init; } = string.Empty;
    public string?             StackTrace       { get; init; }
    public List<ExceptionInfo>? InnerExceptions { get; set; }
}

internal sealed class ProcessInfo
{
    public int     Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Executable  { get; init; }
    public DateTime StartTime  { get; init; }
    public int     ThreadCount { get; init; }
    public long    MemoryBytes { get; init; }
}

internal sealed class EnvironmentInfo
{
    public string  MachineName      { get; init; } = string.Empty;
    public string  UserName         { get; init; } = string.Empty;
    public string  OsVersion        { get; init; } = string.Empty;
    public string  RuntimeVersion   { get; init; } = string.Empty;
    public bool    Is64BitProcess   { get; init; }
    public string  WorkingDirectory { get; init; } = string.Empty;
    public string  CommandLine      { get; init; } = string.Empty;
    public Dictionary<string, string>? Variables { get; init; }
}

internal sealed class RecentLogInfo
{
    public string  Timestamp     { get; init; } = string.Empty;
    public string  Level         { get; init; } = string.Empty;
    public string? Message       { get; init; }
    public string? Exception     { get; init; }
    public string  Member        { get; init; } = string.Empty;
    public string  File          { get; init; } = string.Empty;
    public int     Line          { get; init; }
    public string? Scope         { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId       { get; init; }
    public string? SpanId        { get; init; }
}
