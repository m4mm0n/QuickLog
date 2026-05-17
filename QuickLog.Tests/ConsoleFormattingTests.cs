/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ConsoleFormattingTests.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-17 20:07:43 +02:00
 *  Last Modified  : 2026-05-17 20:35:51 +02:00
 *  CRC32          : 72FEE737
 *  
 *  Description    :
 *
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 72FEE737

using QuickLog.Core;
using QuickLog.Sinks;
using Xunit;

namespace QuickLog.Tests;

public sealed class ConsoleFormattingTests
{
    [Fact]
    public void CompactFormatter_OmitsCallerFilePath()
    {
        var text = ConsoleLogFormatter.Format(
            new LogEntry(DateTime.UnixEpoch, LogType.Warn, "hello", "test", null, "Member", "C:/source/file.cs", 10, 1, ThreadRole.Main),
            compact: true,
            useLocalTime: false,
            ansi: false);

        Assert.DoesNotContain("C:/source/file.cs", text);
        Assert.Contains("[Warn]", text);
    }

    [Fact]
    public void AnsiFormatter_AddsEscapeCode()
    {
        var text = ConsoleLogFormatter.Format(
            new LogEntry(DateTime.UnixEpoch, LogType.Error, "boom", "test", null, "Member", string.Empty, 0, 1, ThreadRole.Main),
            compact: true,
            useLocalTime: false,
            ansi: true);

        Assert.Contains("\u001b[", text);
    }
}
