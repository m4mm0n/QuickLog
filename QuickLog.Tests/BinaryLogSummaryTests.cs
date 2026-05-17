/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogSummaryTests.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-17 20:07:19 +02:00
 *  Last Modified  : 2026-05-17 20:35:51 +02:00
 *  CRC32          : A0EF0181
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
// CRC32-BODY: A0EF0181

using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class BinaryLogSummaryTests
{
    [Fact]
    public void Summary_CountsLevelsAndTopMessages()
    {
        var summary = BinaryLogSummary.FromEntries([
            new LogEntry(DateTime.UnixEpoch.AddSeconds(2), LogType.Error, "boom", "test", null, "m", "", 0, 1, ThreadRole.Main, "corr-b"),
            new LogEntry(DateTime.UnixEpoch.AddSeconds(1), LogType.Error, "boom", "test", null, "m", "", 0, 1, ThreadRole.Main, "corr-a"),
            new LogEntry(DateTime.UnixEpoch.AddSeconds(3), LogType.Warn, "careful", "test", null, "m", "", 0, 1, ThreadRole.Main, "corr-a")
        ]);

        Assert.Equal(3, summary.EntryCount);
        Assert.Equal(2, summary.LevelCounts[LogType.Error]);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(1), summary.FirstTimestampUtc);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(3), summary.LastTimestampUtc);
        Assert.Equal("boom", summary.TopMessages[0].Message);
        Assert.Equal(2, summary.TopMessages[0].Count);
        Assert.Equal(["corr-a", "corr-b"], summary.Correlations);
    }

    [Fact]
    public void Merge_WritesEntriesSortedByTimestamp()
    {
        var first = Path.Combine(Path.GetTempPath(), $"ql_merge_a_{Guid.NewGuid():N}.qlog");
        var second = Path.Combine(Path.GetTempPath(), $"ql_merge_b_{Guid.NewGuid():N}.qlog");
        var merged = Path.Combine(Path.GetTempPath(), $"ql_merge_out_{Guid.NewGuid():N}.qlog");

        try
        {
            using (var sink = new BinaryLogSink(first))
            {
                sink.Write(new LogEntry(DateTime.UnixEpoch.AddSeconds(3), LogType.Info, "third", "test", null, "m", "", 0, 1, ThreadRole.Main));
            }

            using (var sink = new BinaryLogSink(second))
            {
                sink.Write(new LogEntry(DateTime.UnixEpoch.AddSeconds(1), LogType.Info, "first", "test", null, "m", "", 0, 1, ThreadRole.Main));
            }

            BinaryLogMerge.Merge([first, second], merged);

            var messages = BinaryLogReader.Read(merged).Select(e => e.Message).ToArray();
            Assert.Equal(["first", "third"], messages);
        }
        finally
        {
            TryDelete(first);
            TryDelete(second);
            TryDelete(merged);
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
