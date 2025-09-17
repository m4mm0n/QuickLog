using QuickLog.Utilities;
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers
{
    /// <summary>
    /// A drop-in logger that writes to Godot's <c>user://</c> directory when the Godot runtime is detected,
    /// and otherwise falls back to a standard local folder. This class has no compile-time dependency on Godot:
    /// it uses reflection at runtime to resolve <c>user://</c>.
    /// <para>
    /// Internally, this is just a thin adapter around <see cref="FileLogger"/> that resolves the correct absolute
    /// path. All file I/O and thread-safety are handled by <see cref="FileLogger"/>.
    /// </para>
    /// </summary>
    public sealed class GodotFileLogger : IQuickLog
    {
        private readonly FileLogger _inner;

        /// <summary>
        /// Fires when a log entry is produced. This event is proxied from the inner <see cref="FileLogger"/>.
        /// </summary>
        public event EventHandler<LogEventArgs>? LogEvent
        {
            add => _inner.LogEvent += value;
            remove => _inner.LogEvent -= value;
        }

        /// <summary>
        /// True when the constructor resolved a path via Godot's <c>user://</c> (i.e., running under Godot),
        /// false when using the non-Godot fallback path.
        /// </summary>
        public bool IsUsingGodotPath { get; }

        /// <summary>
        /// The full, absolute path to the log file used by this instance.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Creates a new <see cref="GodotFileLogger"/>.
        /// </summary>
        /// <param name="fileName">File name only (e.g., <c>game.log</c>). Invalid characters are removed.</param>
        /// <param name="subfolder">Optional subfolder under the base directory. Use empty string to write at the root.</param>
        /// <param name="fallbackRoot">
        /// Optional custom fallback root (used when Godot isn't present). If omitted, defaults to a platform-specific
        /// local app-data path (e.g., <c>%LOCALAPPDATA%\GodotUser</c>).
        /// </param>
        public GodotFileLogger(string fileName = "game.log", string subfolder = "logs", string? fallbackRoot = null)
        {
            // Sanitize filename using your internal Extensions helper.
            fileName = fileName.ReplaceInvalidChars();

            var looksLikeGodot = GodotUserPathResolver.IsGodotRuntime();
            var baseDir = GodotUserPathResolver.GetUserDir(fallbackRoot);
            IsUsingGodotPath = looksLikeGodot && !string.IsNullOrWhiteSpace(baseDir);

            var targetDir = string.IsNullOrWhiteSpace(subfolder) ? baseDir : Path.Combine(baseDir, subfolder);
            Directory.CreateDirectory(targetDir);

            FullPath = Path.Combine(targetDir, fileName);

            // Delegate the heavy lifting to your FileLogger (append=true by default inside it).
            _inner = new FileLogger(FullPath);
        }

        /// <inheritdoc />
        public void Log(LogType logType, string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => _inner.Log(logType, message, callerName, callerFilePath, callerLineNumber);

        /// <inheritdoc />
        public void Log(LogType logType, Exception exception,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => _inner.Log(logType, exception, callerName, callerFilePath, callerLineNumber);

        /// <inheritdoc />
        public void Log(LogType logType, string message, Exception exception,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => _inner.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);

        /// <summary>
        /// Disposes underlying resources held by the inner <see cref="FileLogger"/>.
        /// </summary>
        public void Dispose() => _inner.Dispose();
    }
}
