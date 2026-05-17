using QuickLog.Utilities;

namespace QuickLog.Tools.Reporting;

/// <summary>Represents all data needed to render a static QuickLog diagnostics report.</summary>
internal sealed class ReportModel
{
    /// <summary>Gets or sets the report generation timestamp in UTC.</summary>
    public DateTime GeneratedUtc { get; set; }

    /// <summary>Gets the input directories that were scanned.</summary>
    public List<string> InputDirectories { get; } = [];

    /// <summary>Gets the per-QLOG summaries included in the report.</summary>
    public List<ReportLogSummary> QlogSummaries { get; } = [];

    /// <summary>Gets the text and JSONL file counts included in the report.</summary>
    public List<ReportFileCount> TextFileCounts { get; } = [];

    /// <summary>Gets the crash dump summaries included in the report.</summary>
    public List<ReportCrashSummary> CrashSummaries { get; } = [];

    /// <summary>Gets the highest-impact warning and error messages observed across QLOG files.</summary>
    public List<string> TopProblems { get; } = [];

    /// <summary>Gets suggested follow-up checks inferred from the scanned files.</summary>
    public List<string> SuggestedNextChecks { get; } = [];
}

/// <summary>Represents the summary of one QLOG file in a report.</summary>
internal sealed record ReportLogSummary(string Path, BinaryLogSummary Summary);

/// <summary>Represents a counted text or JSONL file in a report.</summary>
internal sealed record ReportFileCount(string Path, int LineCount);

/// <summary>Represents a crash artifact in a report.</summary>
internal sealed record ReportCrashSummary(string Path, long Bytes, DateTime LastWriteUtc);
