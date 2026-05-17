using System.Net;
using System.Text;

namespace QuickLog.Tools.Reporting;

/// <summary>Writes a dependency-free single-file HTML report for QuickLog diagnostics.</summary>
internal static class StaticHtmlReportWriter
{
    /// <summary>Writes the supplied report model to a static HTML file.</summary>
    public static void Write(ReportModel model, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllText(outputPath, Render(model), Encoding.UTF8);
    }

    /// <summary>Renders the supplied report model as static HTML.</summary>
    public static string Render(ReportModel model)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>QuickLog Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f6f7f9;color:#1d1f23}main{max-width:1040px;margin:0 auto;padding:32px}section{margin:24px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d9dde3;padding:8px;text-align:left;vertical-align:top}th{background:#eceff3}.muted{color:#5d6570}.pill{display:inline-block;padding:2px 6px;border-radius:4px;background:#e7edf7;margin:2px 4px 2px 0}");
        html.AppendLine("</style></head><body><main>");
        html.AppendLine("<h1>QuickLog Diagnostics Report</h1>");
        html.AppendLine($"<p class=\"muted\">Generated UTC: {Encode(model.GeneratedUtc.ToString("O"))}</p>");
        WriteInputs(html, model);
        WriteQlogs(html, model);
        WriteTextFiles(html, model);
        WriteCrashes(html, model);
        WriteList(html, "Top Problems", model.TopProblems);
        WriteList(html, "Suggested Next Checks", model.SuggestedNextChecks);
        html.AppendLine("</main></body></html>");
        return html.ToString();
    }

    /// <summary>Writes the scanned input list section.</summary>
    private static void WriteInputs(StringBuilder html, ReportModel model)
    {
        html.AppendLine("<section><h2>Inputs</h2>");
        if (model.InputDirectories.Count == 0)
            html.AppendLine("<p class=\"muted\">No input directories were supplied.</p>");
        else
            WriteListItems(html, model.InputDirectories);
        html.AppendLine("</section>");
    }

    /// <summary>Writes the QLOG summaries section.</summary>
    private static void WriteQlogs(StringBuilder html, ReportModel model)
    {
        html.AppendLine("<section><h2>QLOG Summaries</h2>");
        if (model.QlogSummaries.Count == 0)
        {
            html.AppendLine("<p class=\"muted\">No QLOG files found.</p></section>");
            return;
        }

        html.AppendLine("<table><thead><tr><th>File</th><th>Entries</th><th>Levels</th><th>Top Messages</th></tr></thead><tbody>");
        foreach (var item in model.QlogSummaries)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(item.Path)}</td>");
            html.Append($"<td>{item.Summary.EntryCount}</td>");
            html.Append("<td>");
            foreach (var pair in item.Summary.LevelCounts)
                html.Append($"<span class=\"pill\">{Encode(pair.Key.ToString())}: {pair.Value}</span>");
            html.Append("</td><td>");
            foreach (var message in item.Summary.TopMessages.Take(5))
                html.Append($"<div>{message.Count} x {Encode(message.Message)}</div>");
            html.AppendLine("</td></tr>");
        }

        html.AppendLine("</tbody></table></section>");
    }

    /// <summary>Writes the text log counts section.</summary>
    private static void WriteTextFiles(StringBuilder html, ReportModel model)
    {
        html.AppendLine("<section><h2>Text Logs</h2>");
        if (model.TextFileCounts.Count == 0)
        {
            html.AppendLine("<p class=\"muted\">No text or JSONL logs found.</p></section>");
            return;
        }

        html.AppendLine("<table><thead><tr><th>File</th><th>Lines</th></tr></thead><tbody>");
        foreach (var item in model.TextFileCounts)
            html.AppendLine($"<tr><td>{Encode(item.Path)}</td><td>{item.LineCount}</td></tr>");
        html.AppendLine("</tbody></table></section>");
    }

    /// <summary>Writes the crash artifact section.</summary>
    private static void WriteCrashes(StringBuilder html, ReportModel model)
    {
        html.AppendLine("<section><h2>Crash Artifacts</h2>");
        if (model.CrashSummaries.Count == 0)
        {
            html.AppendLine("<p class=\"muted\">No crash artifacts found.</p></section>");
            return;
        }

        html.AppendLine("<table><thead><tr><th>File</th><th>Bytes</th><th>Last Write UTC</th></tr></thead><tbody>");
        foreach (var item in model.CrashSummaries)
            html.AppendLine($"<tr><td>{Encode(item.Path)}</td><td>{item.Bytes}</td><td>{Encode(item.LastWriteUtc.ToString("O"))}</td></tr>");
        html.AppendLine("</tbody></table></section>");
    }

    /// <summary>Writes a titled unordered list section.</summary>
    private static void WriteList(StringBuilder html, string title, IReadOnlyList<string> values)
    {
        html.AppendLine($"<section><h2>{Encode(title)}</h2>");
        if (values.Count == 0)
            html.AppendLine("<p class=\"muted\">None.</p>");
        else
            WriteListItems(html, values);
        html.AppendLine("</section>");
    }

    /// <summary>Writes unordered list items with encoded values.</summary>
    private static void WriteListItems(StringBuilder html, IEnumerable<string> values)
    {
        html.AppendLine("<ul>");
        foreach (var value in values)
            html.AppendLine($"<li>{Encode(value)}</li>");
        html.AppendLine("</ul>");
    }

    /// <summary>HTML-encodes dynamic report text.</summary>
    private static string Encode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
