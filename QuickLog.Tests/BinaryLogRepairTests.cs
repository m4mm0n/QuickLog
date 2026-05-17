/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogRepairTests.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-17 20:07:08 +02:00
 *  Last Modified  : 2026-05-17 20:35:51 +02:00
 *  CRC32          : BFCBF2C8
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
// CRC32-BODY: BFCBF2C8

using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class BinaryLogRepairTests
{
    [Fact]
    public void Repair_SalvagesEntriesAfterGarbagePrefix()
    {
        var source = Path.Combine(Path.GetTempPath(), $"ql_bad_{Guid.NewGuid():N}.qlog");
        var repaired = Path.Combine(Path.GetTempPath(), $"ql_repaired_{Guid.NewGuid():N}.qlog");

        try
        {
            File.WriteAllBytes(source, [0, 1, 2, 3, 4, 5]);
            using (var sink = new BinaryLogSink(source))
            {
                sink.Write(new LogEntry(DateTime.UtcNow, LogType.Error, "after garbage", "test", null, "m", "", 0, 1, ThreadRole.Main));
            }

            var result = BinaryLogRepair.Repair(source, repaired);

            Assert.True(result.RecoveredEntries >= 1);
            Assert.True(result.SkippedBytes >= 6);
            Assert.Equal(repaired, result.OutputPath);
            Assert.Contains(BinaryLogReader.Read(repaired), e => e.Message == "after garbage");
        }
        finally
        {
            TryDelete(source);
            TryDelete(repaired);
        }
    }

    [Fact]
    public void ReadWithDiagnostics_ReportsInvalidMagicOffset()
    {
        var source = Path.Combine(Path.GetTempPath(), $"ql_diag_{Guid.NewGuid():N}.qlog");

        try
        {
            File.WriteAllBytes(source, [1, 2, 3, 4]);

            var result = BinaryLogReader.ReadWithDiagnostics(source);

            Assert.Empty(result.Entries);
            Assert.Contains(result.Diagnostics, d => d.Offset == 0 && d.Kind == BinaryLogDiagnosticKind.InvalidMagic);
        }
        finally
        {
            TryDelete(source);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort cleanup only
        }
    }
}
