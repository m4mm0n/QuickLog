/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogTimelineViewer.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:48:35 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 6120CBB0
 *  
 *  Description    :
 *                   Provides a console-based interactive viewer for navigating and inspecting entries in a binary log file.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 6120CBB0

using QuickLog.Core;

namespace QuickLog.Utilities;

/// <summary>
/// Provides a console-based interactive viewer for navigating and inspecting entries in a binary log file.
/// </summary>
/// <remarks>The viewer displays log entry details and allows navigation using the arrow keys and page up/down
/// keys. Pressing the Escape key exits the viewer. This class is intended for use as a diagnostic or debugging tool and
/// is not thread-safe.</remarks>
public static class BinaryLogTimelineViewer
{
    /// <summary>
    /// Displays an interactive console viewer for the specified binary log file, allowing navigation through log
    /// entries using keyboard input.
    /// </summary>
    /// <remarks>Use the arrow keys and page up/down to navigate through the log entries. Press the Escape key
    /// to exit the viewer. The method clears and modifies the console display during execution.</remarks>
    /// <param name="binaryLogPath">The path to the binary log file to be read and displayed. Cannot be null or empty.</param>
    public static void Run(string binaryLogPath)
    {
        var allEntries = BinaryLogReader.Read(binaryLogPath).ToList();
        if (allEntries.Count == 0)
        {
            Console.WriteLine("No log entries.");
            return;
        }

        // ---------------- FILTER STATE ----------------
        string? search = null;
        var levelMask = (LogType)(-1);            // all
        var roleMask = ThreadRole.Unknown;     // Unknown = all

        // ---------------- VIEW STATE ------------------
        var entries = allEntries;
        var groups = GroupByTime(entries, TimeSpan.FromMilliseconds(5));

        var index = 0;
        var groupIndex = 0;
        var grouped = false;

        // ---------------- PRESET PATH -----------------
        var presetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "quicklog.filters");

        // ---------------- HELPERS ---------------------
        void RebuildView()
        {
            entries = allEntries
                .Where(e =>
                    (search == null || e.Message.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                    ((levelMask & e.Level) != 0) &&
                    (roleMask == ThreadRole.Unknown || e.ThreadRole == roleMask))
                .ToList();

            index = entries.Count == 0 ? 0 : Math.Clamp(index, 0, entries.Count - 1);
            groups = GroupByTime(entries, TimeSpan.FromMilliseconds(5));
            groupIndex = 0;

            if (grouped && groups.Count > 0)
                index = groups[0].Start;
        }

        void DrawStatusBar()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.WindowHeight - 1);

            Console.Write($" / Search: {(search ?? "OFF")} ");
            Console.Write($"| L Level: {levelMask} ");
            Console.Write($"| R Role: {(roleMask == ThreadRole.Unknown ? "ALL" : roleMask)} ");
            Console.Write($"| G Group: {(grouped ? "ON" : "OFF")} ");
            Console.Write("| F5 Save | F9 Load | Esc Exit ");

            Console.ResetColor();
        }

        void SavePreset()
        {
            File.WriteAllText(presetPath,
                $"{search ?? ""}\n{(int)levelMask}\n{(int)roleMask}\n{grouped}");
        }

        void LoadPreset()
        {
            if (!File.Exists(presetPath))
                return;

            var lines = File.ReadAllLines(presetPath);
            if (lines.Length < 4)
                return;

            search = string.IsNullOrWhiteSpace(lines[0]) ? null : lines[0];
            levelMask = (LogType)int.Parse(lines[1]);
            roleMask = (ThreadRole)int.Parse(lines[2]);
            grouped = bool.Parse(lines[3]);

            RebuildView();
        }

        // ---------------- UI LOOP ---------------------
        Console.Clear();
        Console.CursorVisible = false;

        ConsoleKey key;
        do
        {
            Console.SetCursorPosition(0, 0);

            if (entries.Count == 0)
                Console.WriteLine("No entries match current filters.");
            else
                Render(entries, index, search);

            DrawStatusBar();

            key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.DownArrow:
                    if (grouped && groupIndex < groups.Count - 1)
                        index = groups[++groupIndex].Start;
                    else if (!grouped && index < entries.Count - 1)
                        index++;
                    break;

                case ConsoleKey.UpArrow:
                    if (grouped && groupIndex > 0)
                        index = groups[--groupIndex].Start;
                    else if (!grouped && index > 0)
                        index--;
                    break;

                case ConsoleKey.PageDown:
                    index = Math.Min(index + 10, entries.Count - 1);
                    break;

                case ConsoleKey.PageUp:
                    index = Math.Max(index - 10, 0);
                    break;

                case ConsoleKey.G:
                    grouped = !grouped;
                    groupIndex = 0;
                    if (grouped && groups.Count > 0)
                        index = groups[0].Start;
                    break;

                case ConsoleKey.Oem2: // '/'
                    Console.CursorVisible = true;
                    Console.SetCursorPosition(0, Console.WindowHeight - 2);
                    Console.Write("Search: ");
                    search = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(search))
                        search = null;
                    RebuildView();
                    Console.CursorVisible = false;
                    break;

                case ConsoleKey.L:
                    levelMask = levelMask switch
                    {
                        _ when levelMask == (LogType)(-1)
                            => LogType.Error | LogType.Crit | LogType.Exception,
                        _ when levelMask == (LogType.Error | LogType.Crit | LogType.Exception)
                            => LogType.Warn | LogType.Error | LogType.Crit | LogType.Exception,
                        _ => (LogType)(-1)
                    };
                    RebuildView();
                    break;

                case ConsoleKey.R:
                    roleMask = roleMask switch
                    {
                        ThreadRole.Unknown => ThreadRole.Render,
                        ThreadRole.Render => ThreadRole.Audio,
                        ThreadRole.Audio => ThreadRole.Network,
                        ThreadRole.Network => ThreadRole.IO,
                        ThreadRole.IO => ThreadRole.Worker,
                        ThreadRole.Worker => ThreadRole.Main,
                        _ => ThreadRole.Unknown
                    };
                    RebuildView();
                    break;

                case ConsoleKey.F5:
                    SavePreset();
                    break;

                case ConsoleKey.F9:
                    LoadPreset();
                    break;
            }

        } while (key != ConsoleKey.Escape);

        Console.CursorVisible = true;
        Console.Clear();
    }

    private static void Render(List<LogEntry> entries, int index, string? search)
    {
        var e = entries[index];
        Console.ResetColor();

        Console.WriteLine($"[{index + 1}/{entries.Count}] {e.Timestamp:O}");

        Console.ForegroundColor = ColorForLevel(e.Level);
        Console.WriteLine($"Level      : {e.Level}");

        Console.ForegroundColor = ColorForRole(e.ThreadRole);
        Console.WriteLine($"Thread     : {e.ThreadRole} ({e.ThreadId})");

        Console.ResetColor();
        Console.WriteLine($"Member     : {e.MemberName}");
        Console.WriteLine($"File       : {e.FilePath}:{e.LineNumber}");

        if (!string.IsNullOrWhiteSpace(e.Category))
            Console.WriteLine($"Category   : {e.Category}");

        Console.WriteLine();

        if (search == null)
        {
            Console.ForegroundColor = ColorForLevel(e.Level);
            Console.WriteLine(e.Message);
        }
        else
            WriteHighlighted(e.Message, search, ColorForLevel(e.Level));

        Console.ResetColor();
    }

    private static void WriteHighlighted(string text, string search, ConsoleColor baseColor)
    {
        var idx = 0;
        int hit;

        while ((hit = text.IndexOf(search, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            Console.ForegroundColor = baseColor;
            Console.Write(text.Substring(idx, hit - idx));

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.Write(text.Substring(hit, search.Length));

            Console.ResetColor();
            idx = hit + search.Length;
        }

        Console.ForegroundColor = baseColor;
        Console.Write(text.Substring(idx));
    }

    private static ConsoleColor ColorForLevel(LogType level) => level switch
    {
        LogType.Trace => ConsoleColor.DarkGray,
        LogType.Debug => ConsoleColor.Gray,
        LogType.Info => ConsoleColor.White,
        LogType.Warn => ConsoleColor.Yellow,
        LogType.Error => ConsoleColor.Red,
        LogType.Crit => ConsoleColor.Magenta,
        LogType.Exception => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };

    private static ConsoleColor ColorForRole(ThreadRole role) => role switch
    {
        ThreadRole.Render => ConsoleColor.Cyan,
        ThreadRole.Audio => ConsoleColor.Green,
        ThreadRole.Network => ConsoleColor.Blue,
        ThreadRole.IO => ConsoleColor.DarkYellow,
        ThreadRole.Worker => ConsoleColor.DarkCyan,
        ThreadRole.Main => ConsoleColor.White,
        _ => ConsoleColor.DarkGray
    };

    private sealed record Group(int Start, int Count, DateTime From, DateTime To);

    private static List<Group> GroupByTime(
        List<LogEntry> entries,
        TimeSpan slice)
    {
        var groups = new List<Group>();
        if (entries.Count == 0)
            return groups;

        var start = 0;
        var from = entries[0].Timestamp;
        var last = from;

        for (var i = 1; i < entries.Count; i++)
        {
            var t = entries[i].Timestamp;
            if (t - last > slice)
            {
                groups.Add(new Group(
                    start,
                    i - start,
                    from,
                    last));

                start = i;
                from = t;
            }

            last = t;
        }

        groups.Add(new Group(
            start,
            entries.Count - start,
            from,
            last));

        return groups;
    }
}