namespace QuickLog.Tools;

public sealed record ToolParseResult(ToolCommand? Command, string? Error)
{
    public bool Success => Command is not null && Error is null;

    public static ToolParseResult Ok(ToolCommand command) => new(command, null);
    public static ToolParseResult Fail(string error) => new(null, error);
}

public abstract record ToolCommand;

public sealed record DoctorToolCommand(string Path, bool Recursive) : ToolCommand;

public sealed record InspectToolCommand(
    string Path,
    string? Level,
    string? Contains,
    string? Correlation,
    DateTime? From,
    DateTime? To,
    int? Limit) : ToolCommand;

public sealed record ReplayToolCommand(string Path, string To, string? Out) : ToolCommand;

public sealed record BenchmarkToolCommand(int Iterations, string Mode) : ToolCommand;

public sealed record BundleToolCommand(
    string Out,
    string? Logs,
    string? Crashes,
    bool IncludeEnv,
    bool IncludeExports,
    long? MaxFileBytes,
    bool Redact) : ToolCommand;

public sealed record LaunchToolCommand(
    string App,
    IReadOnlyList<string> AppArgs,
    string Out,
    string? Name,
    bool DiagnosticEnv,
    bool WaitForExit) : ToolCommand;

public sealed record ObserveToolCommand(int Pid, int DurationSeconds, string Out) : ToolCommand;

public sealed record ProfilerExplainToolCommand : ToolCommand;

public sealed record ProfilerEnvToolCommand(Guid Clsid, string Path) : ToolCommand;

/// <summary>Represents a tail command for printing the end of a text log.</summary>
public sealed record TailToolCommand(string Path, int Lines, bool Follow) : ToolCommand;

/// <summary>Represents a grep command for finding messages across log files.</summary>
public sealed record GrepToolCommand(string Pattern, string Path, bool Recursive) : ToolCommand;

/// <summary>Represents a diff command for comparing two log files.</summary>
public sealed record DiffToolCommand(string Left, string Right) : ToolCommand;

/// <summary>Represents a stats command for summarizing one log file.</summary>
public sealed record StatsToolCommand(string Path) : ToolCommand;

/// <summary>Represents a redact command for writing a masked copy of a text log.</summary>
public sealed record RedactToolCommand(string Input, string Out) : ToolCommand;

/// <summary>Represents a summarize command for writing a JSON summary of one log file.</summary>
public sealed record SummarizeToolCommand(string Path, string Out) : ToolCommand;

/// <summary>Represents a report command for writing a static HTML diagnostics report.</summary>
public sealed record ReportToolCommand(string Out, string? Logs, string? Crashes) : ToolCommand;

/// <summary>Represents a repair command for salvaging readable QLOG records.</summary>
public sealed record RepairToolCommand(string Path, string Out) : ToolCommand;

/// <summary>Represents a merge command for combining multiple QLOG files.</summary>
public sealed record MergeToolCommand(IReadOnlyList<string> Inputs, string Out) : ToolCommand;

/// <summary>Represents a timeline command for showing log timing information.</summary>
public sealed record TimelineToolCommand(string Path) : ToolCommand;

/// <summary>Represents a configuration doctor command for validating serialized logger options.</summary>
public sealed record DoctorConfigToolCommand(string Path) : ToolCommand;

public static class ToolCommandParser
{
    /// <summary>Parses command-line arguments into a typed tool command.</summary>
    public static ToolParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return ToolParseResult.Fail("Command is required.");

        return args[0].ToLowerInvariant() switch
        {
            "doctor" => ParseDoctor(args),
            "inspect" => ParseInspect(args),
            "replay" => ParseReplay(args),
            "benchmark" => ParseBenchmark(args),
            "bundle" => ParseBundle(args),
            "launch" => ParseLaunch(args),
            "observe" => ParseObserve(args),
            "profiler" => ParseProfiler(args),
            "tail" => ParseTail(args),
            "grep" => ParseGrep(args),
            "diff" => ParseDiff(args),
            "stats" => ParseStats(args),
            "redact" => ParseRedact(args),
            "summarize" => ParseSummarize(args),
            "report" => ParseReport(args),
            "repair" => ParseRepair(args),
            "merge" => ParseMerge(args),
            "timeline" => ParseTimeline(args),
            "doctor-config" => ParseDoctorConfig(args),
            _ => ToolParseResult.Fail($"Unknown command '{args[0]}'.")
        };
    }

    private static ToolParseResult ParseDoctor(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("doctor requires a path.");

        return ToolParseResult.Ok(new DoctorToolCommand(path, Has(args, "--recursive")));
    }

    private static ToolParseResult ParseInspect(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("inspect requires a path.");

        if (!TryReadOptionalInt(args, "--limit", out var limit, out var error))
            return ToolParseResult.Fail(error);

        if (!TryReadOptionalDate(args, "--from", out var from, out error))
            return ToolParseResult.Fail(error);

        if (!TryReadOptionalDate(args, "--to", out var to, out error))
            return ToolParseResult.Fail(error);

        return ToolParseResult.Ok(new InspectToolCommand(
            path,
            Value(args, "--level"),
            Value(args, "--contains"),
            Value(args, "--correlation"),
            from,
            to,
            limit));
    }

    private static ToolParseResult ParseReplay(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("replay requires a path.");

        var to = Value(args, "--to") ?? "console";
        if (to is not ("console" or "text" or "jsonl"))
            return ToolParseResult.Fail("replay --to must be console, text, or jsonl.");

        return ToolParseResult.Ok(new ReplayToolCommand(path, to, Value(args, "--out")));
    }

    private static ToolParseResult ParseBenchmark(IReadOnlyList<string> args)
    {
        if (!TryReadOptionalInt(args, "--iterations", out var iterations, out var error))
            return ToolParseResult.Fail(error);

        var mode = Value(args, "--mode") ?? "async";
        if (mode is not ("sync" or "async" or "binary" or "json" or "redaction" or "spam"))
            return ToolParseResult.Fail("benchmark --mode must be sync, async, binary, json, redaction, or spam.");

        return ToolParseResult.Ok(new BenchmarkToolCommand(iterations ?? 10_000, mode));
    }

    private static ToolParseResult ParseBundle(IReadOnlyList<string> args)
    {
        var output = Value(args, "--out");
        if (string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("bundle requires --out <zip>.");

        if (!TryReadOptionalLong(args, "--max-file-bytes", out var maxFileBytes, out var error))
            return ToolParseResult.Fail(error);

        return ToolParseResult.Ok(new BundleToolCommand(
            output,
            Value(args, "--logs"),
            Value(args, "--crashes"),
            Has(args, "--include-env"),
            Has(args, "--include-exports"),
            maxFileBytes,
            Has(args, "--redact")));
    }

    private static ToolParseResult ParseLaunch(IReadOnlyList<string> args)
    {
        var separator = IndexOf(args, "--");
        if (separator < 0 || separator == args.Count - 1)
            return ToolParseResult.Fail("launch requires -- <app> [args...].");

        var output = Value(args, "--out") ?? Path.Combine("quicklog-sessions", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        var app = args[separator + 1];
        var appArgs = args.Skip(separator + 2).ToArray();

        return ToolParseResult.Ok(new LaunchToolCommand(
            app,
            appArgs,
            output,
            Value(args, "--name"),
            Has(args, "--diagnostic-env"),
            Has(args, "--wait-for-exit")));
    }

    private static ToolParseResult ParseObserve(IReadOnlyList<string> args)
    {
        if (!TryReadRequiredInt(args, "--pid", out var pid, out var error))
            return ToolParseResult.Fail(error);

        if (!TryReadOptionalInt(args, "--duration", out var duration, out error))
            return ToolParseResult.Fail(error);

        var output = Value(args, "--out") ?? Path.Combine("quicklog-sessions", $"observe-{pid}");
        return ToolParseResult.Ok(new ObserveToolCommand(pid, duration ?? 10, output));
    }

    private static ToolParseResult ParseProfiler(IReadOnlyList<string> args)
    {
        if (args.Count < 2)
            return ToolParseResult.Fail("profiler requires explain or env.");

        if (args[1].Equals("explain", StringComparison.OrdinalIgnoreCase))
            return ToolParseResult.Ok(new ProfilerExplainToolCommand());

        if (!args[1].Equals("env", StringComparison.OrdinalIgnoreCase))
            return ToolParseResult.Fail("profiler requires explain or env.");

        var clsidText = Value(args, "--clsid");
        if (!Guid.TryParse(clsidText, out var clsid))
            return ToolParseResult.Fail("profiler env requires --clsid <guid>.");

        var path = Value(args, "--path");
        if (string.IsNullOrWhiteSpace(path))
            return ToolParseResult.Fail("profiler env requires --path <profiler-path>.");

        return ToolParseResult.Ok(new ProfilerEnvToolCommand(clsid, path));
    }

    /// <summary>Parses tail command arguments.</summary>
    private static ToolParseResult ParseTail(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("tail requires a path.");

        if (!TryReadOptionalInt(args, "--lines", out var lines, out var error))
            return ToolParseResult.Fail(error);

        return ToolParseResult.Ok(new TailToolCommand(path, lines ?? 10, Has(args, "--follow")));
    }

    /// <summary>Parses grep command arguments.</summary>
    private static ToolParseResult ParseGrep(IReadOnlyList<string> args)
    {
        var positionals = Positionals(args, 1);
        if (positionals.Count < 2)
            return ToolParseResult.Fail("grep requires a pattern and path.");

        return ToolParseResult.Ok(new GrepToolCommand(positionals[0], positionals[1], Has(args, "--recursive")));
    }

    /// <summary>Parses diff command arguments.</summary>
    private static ToolParseResult ParseDiff(IReadOnlyList<string> args)
    {
        var positionals = Positionals(args, 1);
        if (positionals.Count < 2)
            return ToolParseResult.Fail("diff requires left and right paths.");

        return ToolParseResult.Ok(new DiffToolCommand(positionals[0], positionals[1]));
    }

    /// <summary>Parses stats command arguments.</summary>
    private static ToolParseResult ParseStats(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("stats requires a path.");

        return ToolParseResult.Ok(new StatsToolCommand(path));
    }

    /// <summary>Parses redact command arguments.</summary>
    private static ToolParseResult ParseRedact(IReadOnlyList<string> args)
    {
        var input = FirstPositional(args, 1);
        var output = Value(args, "--out");
        if (input is null || string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("redact requires <input> --out <output>.");

        return ToolParseResult.Ok(new RedactToolCommand(input, output));
    }

    /// <summary>Parses summarize command arguments.</summary>
    private static ToolParseResult ParseSummarize(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        var output = Value(args, "--out");
        if (path is null || string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("summarize requires <path> --out <summary.json>.");

        return ToolParseResult.Ok(new SummarizeToolCommand(path, output));
    }

    /// <summary>Parses report command arguments.</summary>
    private static ToolParseResult ParseReport(IReadOnlyList<string> args)
    {
        var output = Value(args, "--out");
        if (string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("report requires --out <report.html>.");

        return ToolParseResult.Ok(new ReportToolCommand(output, Value(args, "--logs"), Value(args, "--crashes")));
    }

    /// <summary>Parses repair command arguments.</summary>
    private static ToolParseResult ParseRepair(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        var output = Value(args, "--out");
        if (path is null || string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("repair requires <path> --out <fixed.qlog>.");

        return ToolParseResult.Ok(new RepairToolCommand(path, output));
    }

    /// <summary>Parses merge command arguments.</summary>
    private static ToolParseResult ParseMerge(IReadOnlyList<string> args)
    {
        var inputs = Positionals(args, 1);
        var output = Value(args, "--out");
        if (inputs.Count < 2 || string.IsNullOrWhiteSpace(output))
            return ToolParseResult.Fail("merge requires at least two inputs and --out <merged.qlog>.");

        return ToolParseResult.Ok(new MergeToolCommand(inputs, output));
    }

    /// <summary>Parses timeline command arguments.</summary>
    private static ToolParseResult ParseTimeline(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("timeline requires a path.");

        return ToolParseResult.Ok(new TimelineToolCommand(path));
    }

    /// <summary>Parses doctor-config command arguments.</summary>
    private static ToolParseResult ParseDoctorConfig(IReadOnlyList<string> args)
    {
        var path = FirstPositional(args, 1);
        if (path is null)
            return ToolParseResult.Fail("doctor-config requires a path.");

        return ToolParseResult.Ok(new DoctorConfigToolCommand(path));
    }

    private static string? FirstPositional(IReadOnlyList<string> args, int start)
    {
        for (var i = start; i < args.Count; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                return args[i];

            i++;
        }

        return null;
    }

    /// <summary>Returns positional arguments while skipping option values.</summary>
    private static IReadOnlyList<string> Positionals(IReadOnlyList<string> args, int start)
    {
        var values = new List<string>();
        for (var i = start; i < args.Count; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                if (i < args.Count - 1 && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    i++;
                continue;
            }

            values.Add(args[i]);
        }

        return values;
    }

    private static bool Has(IReadOnlyList<string> args, string option)
        => args.Any(a => a.Equals(option, StringComparison.OrdinalIgnoreCase));

    private static string? Value(IReadOnlyList<string> args, string option)
    {
        var index = IndexOf(args, option);
        if (index < 0 || index == args.Count - 1)
            return null;

        var value = args[index + 1];
        return value.StartsWith("--", StringComparison.Ordinal) ? null : value;
    }

    private static int IndexOf(IReadOnlyList<string> args, string option)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i].Equals(option, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static bool TryReadRequiredInt(IReadOnlyList<string> args, string option, out int value, out string error)
    {
        var text = Value(args, option);
        if (text is null)
        {
            value = 0;
            error = $"{option} is required.";
            return false;
        }

        if (int.TryParse(text, out value))
        {
            error = string.Empty;
            return true;
        }

        error = $"{option} must be an integer.";
        return false;
    }

    private static bool TryReadOptionalInt(IReadOnlyList<string> args, string option, out int? value, out string error)
    {
        var text = Value(args, option);
        if (text is null)
        {
            value = null;
            error = string.Empty;
            return true;
        }

        if (int.TryParse(text, out var parsed))
        {
            value = parsed;
            error = string.Empty;
            return true;
        }

        value = null;
        error = $"{option} must be an integer.";
        return false;
    }

    private static bool TryReadOptionalLong(IReadOnlyList<string> args, string option, out long? value, out string error)
    {
        var text = Value(args, option);
        if (text is null)
        {
            value = null;
            error = string.Empty;
            return true;
        }

        if (long.TryParse(text, out var parsed))
        {
            value = parsed;
            error = string.Empty;
            return true;
        }

        value = null;
        error = $"{option} must be an integer.";
        return false;
    }

    private static bool TryReadOptionalDate(IReadOnlyList<string> args, string option, out DateTime? value, out string error)
    {
        var text = Value(args, option);
        if (text is null)
        {
            value = null;
            error = string.Empty;
            return true;
        }

        if (DateTime.TryParse(text, out var parsed))
        {
            value = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            error = string.Empty;
            return true;
        }

        value = null;
        error = $"{option} must be a date.";
        return false;
    }
}
