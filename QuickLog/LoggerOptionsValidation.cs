namespace QuickLog;

/// <summary>
/// Describes one validation issue found in a <see cref="LoggerOptions"/> instance.
/// </summary>
/// <param name="Code">Stable issue code suitable for tooling output.</param>
/// <param name="Message">Human-readable explanation of the issue.</param>
/// <param name="Severity">Whether the issue is an error or warning.</param>
public sealed record LoggerOptionsIssue(string Code, string Message, LoggerOptionsIssueSeverity Severity);

/// <summary>
/// Severity assigned to a <see cref="LoggerOptionsIssue"/>.
/// </summary>
public enum LoggerOptionsIssueSeverity
{
    /// <summary>
    /// The configuration is usable, but the issue may surprise the caller.
    /// </summary>
    Warning = 0,

    /// <summary>
    /// The configuration is invalid or would discard expected output.
    /// </summary>
    Error = 1
}

/// <summary>
/// Contains the validation result for a <see cref="LoggerOptions"/> instance.
/// </summary>
public sealed class LoggerOptionsValidationResult
{
    /// <summary>
    /// Creates a validation result from the collected issue list.
    /// </summary>
    /// <param name="issues">Validation issues found by the validator.</param>
    public LoggerOptionsValidationResult(IReadOnlyList<LoggerOptionsIssue> issues)
    {
        Issues = issues;
    }

    /// <summary>
    /// Gets the validation issues found in the options.
    /// </summary>
    public IReadOnlyList<LoggerOptionsIssue> Issues { get; }

    /// <summary>
    /// Gets whether no error-level validation issues were found.
    /// </summary>
    public bool IsValid => Issues.All(issue => issue.Severity != LoggerOptionsIssueSeverity.Error);
}
