using QuickLog;
using QuickLog.Tools.Reporting;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the report command for writing single-file static HTML diagnostics reports.</summary>
public static class ReportCommand
{
    /// <summary>Executes the report command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        ReportToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var model = BuildModel(command, cancellationToken);
        StaticHtmlReportWriter.Write(model, command.Out);
        console.WriteLine($"Wrote {command.Out}");
        return Task.FromResult(CommandResult.Ok());
    }

    /// <summary>Builds a report model from command inputs.</summary>
    private static ReportModel BuildModel(ReportToolCommand command, CancellationToken cancellationToken)
    {
        var model = new ReportModel { GeneratedUtc = DateTime.UtcNow };

        if (!string.IsNullOrWhiteSpace(command.Logs))
        {
            model.InputDirectories.Add(command.Logs);
            AddLogs(model, command.Logs, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(command.Crashes))
        {
            model.InputDirectories.Add(command.Crashes);
            AddCrashes(model, command.Crashes, cancellationToken);
        }

        if (model.QlogSummaries.Count == 0)
            model.SuggestedNextChecks.Add("Add --logs <directory> with QLOG files to include level counts and top messages.");
        if (model.CrashSummaries.Count > 0)
            model.SuggestedNextChecks.Add("Review crash artifacts and correlate timestamps against the QLOG top problems.");
        if (model.TopProblems.Count == 0)
            model.SuggestedNextChecks.Add("No error or warning hotspots were found in scanned QLOG files.");

        return model;
    }

    /// <summary>Adds log summaries and text counts from a log directory to the report model.</summary>
    private static void AddLogs(ReportModel model, string path, CancellationToken cancellationToken)
    {
        foreach (var file in ToolLogUtilities.EnumerateLogFiles(path, recursive: true))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ToolLogUtilities.IsBinaryLog(file))
            {
                var entries = ToolLogUtilities.ReadEntries(file);
                var summary = BinaryLogSummary.FromEntries(entries);
                model.QlogSummaries.Add(new ReportLogSummary(file, summary));
                model.TopProblems.AddRange(entries
                    .Where(e => e.Level is LogType.Warn or LogType.Error or LogType.Crit or LogType.Exception)
                    .GroupBy(e => e.Message)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => $"{g.Count()} x {g.Key}"));
            }
            else
            {
                model.TextFileCounts.Add(new ReportFileCount(file, ToolLogUtilities.ReadTextLines(file).Count()));
            }
        }

        model.TopProblems.Sort(StringComparer.Ordinal);
    }

    /// <summary>Adds crash artifact summaries from a crash directory to the report model.</summary>
    private static void AddCrashes(ReportModel model, string path, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            model.CrashSummaries.Add(new ReportCrashSummary(file, info.Length, info.LastWriteTimeUtc));
        }
    }
}
