/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ConsoleSink.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:45:55 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 58119F7C
 *  
 *  Description    :
 *                   Provides a log sink that writes log entries to the console output.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 58119F7C

using QuickLog.Core;

namespace QuickLog.Sinks;

/// <summary>
/// Provides a log sink that writes log entries to the console output.
/// </summary>
/// <remarks>This sink formats each log entry with a timestamp, log level, and message, and writes it to the
/// standard output stream. It is typically used for development or diagnostic scenarios where log visibility in the
/// console is desired.</remarks>
internal sealed class ConsoleSink : ILogSink
{
    private readonly bool _compact;
    private readonly bool _useLocalTime;
    private readonly bool _ansi;

    /// <summary>
    /// Creates a console sink with optional formatting controls.
    /// </summary>
    /// <param name="compact">Whether to emit compact single-line output.</param>
    /// <param name="useLocalTime">Whether timestamps should be local time.</param>
    /// <param name="ansi">Whether ANSI color escape sequences should be emitted.</param>
    public ConsoleSink(bool compact = false, bool useLocalTime = false, bool ansi = false)
    {
        _compact = compact;
        _useLocalTime = useLocalTime;
        _ansi = ansi;
    }

    /// <summary>
    /// Writes one entry to the console.
    /// </summary>
    /// <param name="entry">Entry to write.</param>
    public void Write(in LogEntry entry)
    {
        Console.WriteLine(ConsoleLogFormatter.Format(entry, _compact, _useLocalTime, _ansi));
    }

    /// <summary>
    /// Flushes console output.
    /// </summary>
    public void Flush() { }

    /// <summary>
    /// Releases sink resources.
    /// </summary>
    public void Dispose() { }
}
