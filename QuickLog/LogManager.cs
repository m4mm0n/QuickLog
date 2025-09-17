using QuickLog.Loggers;
using QuickLog.Utilities;
using System.Collections.Concurrent;

namespace QuickLog
{
    /// <summary>
    /// Manages the creation, configuration, and access of loggers throughout the application.
    /// Provides thread-safe access to named loggers and a default logger for centralized logging management.
    /// </summary>
    public static class LogManager
    {
        // Thread-safe dictionary to store loggers by name.
        private static readonly ConcurrentDictionary<string, IQuickLog> _loggers = new();
        private static bool _configured = false;
        private static QuickLogger? _defaultLogger;

        /// <summary>
        /// Gets or creates a logger by name. 
        /// If a logger with the specified name does not exist, it will be created with default settings.
        /// </summary>
        /// <param name="name">The unique name of the logger. This can be used to identify loggers in different parts of the application.</param>
        /// <param name="useLogFile">If <see langword="true"/>, the logger will log to a file with the same name as the logger.</param>
        /// <returns>An instance of <see cref="IQuickLog"/> that corresponds to the specified name.</returns>
        public static IQuickLog GetLogger(string name, bool useLogFile = false)
        {
            if (!_configured)
                if(useLogFile)
                    ConfigureDefault(name);
                else
                    ConfigureDefault();

            return _loggers.GetOrAdd(name, key => CreateLogger(name));
        }

        /// <summary>
        /// Gets or creates a logger by type.
        /// If a logger with the specified type does not exist, it will be created with default settings.
        /// </summary>
        /// <param name="type">The Type to create a logger for.</param>
        /// <param name="useLogFile">If <see langword="true"/>, the logger will log to a file with the same name as the logger.</param>
        /// <returns>An instance of <see cref="IQuickLog"/> that corresponds to the specified type</returns>
        public static IQuickLog GetLogger(Type type, bool useLogFile = false) => GetLogger(type.EnsureNotNull().FullName!, useLogFile);

        /// <summary>
        /// Configures the default logger used by the application. 
        /// If no specific logger is requested, the default logger is returned by the <see cref="GetDefaultLogger"/> method.
        /// </summary>
        /// <param name="defaultLogger">An optional instance of <see cref="QuickLogger"/> to be used as the default logger. 
        /// If null, a new default logger with console logging enabled is created.</param>
        public static void ConfigureDefault(QuickLogger? defaultLogger = null)
        {
            _defaultLogger = defaultLogger != null
                ? (QuickLogger)defaultLogger.Clone()
                : new QuickLogger
                {
                    EnableConsoleLogging = true,
                    EnableFileLogging = false,
                    EnableEventLogging = false,
                    EnableTraceLogging = false
                };

            _configured = true;
        }

        /// <summary>
        /// Configures the default logger used by the application to log to a specific file.
        /// If no logger has been configured, the default logger is returned by the <see cref="GetDefaultLogger"/> method.
        /// </summary>
        /// <param name="fileName">The filename of the log-file</param>
        /// <param name="defaultLogger">An optional instance of <see cref="QuickLogger"/> to be used as the default logger.
        /// If null, a new default logger with console logging enabled is created.</param>
        public static void ConfigureDefault(string fileName, QuickLogger? defaultLogger = null)
        {
            _defaultLogger = defaultLogger != null
                ? defaultLogger.CloneDeep(fileName)
                : new QuickLogger(fileName)
                {
                    EnableConsoleLogging = true,
                    EnableFileLogging = true,
                    EnableEventLogging = false,
                    EnableTraceLogging = false
                };
            _configured = true;
        }

        /// <summary>
        /// Configures the default logger to log to a specific file path.
        /// The logger will log both to the console and the specified file, making it suitable for general use.
        /// </summary>
        /// <param name="logFilePath">The file path where logs will be written. If the file or directory does not exist, it will be created.</param>
        public static void ConfigureDefaultFileLogger(string logFilePath)
        {
            _defaultLogger = new QuickLogger(logFilePath)
            {
                EnableConsoleLogging = true,
                EnableFileLogging = true,
                EnableEventLogging = false,
                EnableTraceLogging = false
            };

            _configured = true;
        }

        /// <summary>
        /// Retrieves the default logger used by the application.
        /// If no logger has been configured, the default logger will have console logging enabled.
        /// </summary>
        /// <returns>An instance of <see cref="IQuickLog"/> representing the default logger.</returns>
        public static IQuickLog GetDefaultLogger()
        {
            if (!_configured) ConfigureDefault();

            return _defaultLogger!;
        }

        /// <summary>
        /// Creates a new logger with default settings for the specified name. 
        /// This logger can be configured to log to multiple destinations such as the console, file, or trace.
        /// </summary>
        /// <param name="name">The unique name for the logger. This name helps identify the logger instance within the application.</param>
        /// <returns>A new instance of <see cref="IQuickLog"/> with default logging settings (console, event, and trace enabled).</returns>
        private static IQuickLog CreateLogger(string name)
        {
            // Check if a custom logger configuration is required for the name.
            if (_configured && _defaultLogger != null)
            {
                // For example, create a logger that logs to a specific file based on the name
                return new QuickLogger($"{_defaultLogger.LogPath}/{name}.log")
                {
                    EnableConsoleLogging = _defaultLogger.EnableConsoleLogging,
                    EnableFileLogging = _defaultLogger.EnableFileLogging,
                    EnableEventLogging = _defaultLogger.EnableEventLogging,
                    EnableTraceLogging = _defaultLogger.EnableTraceLogging
                };
            }

            // Fallback: create a new logger with generic default settings
            return new QuickLogger
            {
                EnableConsoleLogging = true,
                EnableFileLogging = false,
                EnableEventLogging = true,
                EnableTraceLogging = true
            };
        }

        /// <summary>
        /// Configures the default logger to write its file output into Godot's <c>user://</c> directory
        /// when the Godot runtime is present, or into a standard local folder otherwise.
        /// <para>
        /// No compile-time dependency on Godot is required. Resolution of <c>user://</c> is done at runtime
        /// via reflection; if it fails, a safe fallback directory is used.
        /// </para>
        /// </summary>
        /// <param name="fileName">
        /// Log file name only (e.g. <c>application.log</c>). Invalid characters are sanitized.
        /// </param>
        /// <param name="subfolder">
        /// Optional subfolder under the base directory (default: <c>"logs"</c>). Use <c>""</c> for the root.
        /// </param>
        /// <param name="fallbackRoot">
        /// Optional custom fallback when not in Godot. If <see langword="null"/>, defaults to a platform-specific
        /// local app-data folder (e.g. <c>%LOCALAPPDATA%\GodotUser</c>).
        /// </param>
        public static void ConfigureDefaultGodotLogger(
            string fileName = "application.log",
            string subfolder = "logs",
            string? fallbackRoot = null)
        {
            fileName = fileName.ReplaceInvalidChars(); // uses your Extensions helper

            var baseDir = GodotUserPathResolver.GetUserDir(fallbackRoot);
            var targetDir = string.IsNullOrWhiteSpace(subfolder) ? baseDir : Path.Combine(baseDir, subfolder);
            Directory.CreateDirectory(targetDir);

            var fullPath = Path.Combine(targetDir, fileName);

            // Create a QuickLogger that writes to the resolved path.
            _defaultLogger = new Loggers.QuickLogger(fullPath)
            {
                EnableConsoleLogging = true,
                EnableFileLogging = true,
                EnableEventLogging = false,
                EnableTraceLogging = false
            };

            _configured = true;
        }

        /// <summary>
        /// Retrieves or creates a named logger whose file output is directed to Godot's <c>user://</c> directory
        /// when available (or a safe fallback otherwise). The logger is cached under <paramref name="name"/> just like
        /// <see cref="GetLogger(string, bool)"/>, but its file path is resolved using the Godot-aware logic.
        /// </summary>
        /// <param name="name">Unique logical name for the logger (also used for the default file name if none is supplied).</param>
        /// <param name="fileName">
        /// Optional explicit file name (e.g. <c>"netcode.log"</c>). If <see langword="null"/>, uses <c>{name}.log</c>.
        /// </param>
        /// <param name="subfolder">Optional subfolder under the base directory (default: <c>"logs"</c>).</param>
        /// <param name="fallbackRoot">Optional custom fallback root when not in Godot.</param>
        /// <returns>An <see cref="IQuickLog"/> instance configured for Godot-style storage.</returns>
        public static IQuickLog GetGodotLogger(
            string name,
            string? fileName = null,
            string subfolder = "logs",
            string? fallbackRoot = null)
        {
            if (!_configured) ConfigureDefault(); // ensure base config exists

            return _loggers.GetOrAdd(name, _ =>
            {
                var baseDir = GodotUserPathResolver.GetUserDir(fallbackRoot);
                var targetDir = string.IsNullOrWhiteSpace(subfolder) ? baseDir : Path.Combine(baseDir, subfolder);
                Directory.CreateDirectory(targetDir);

                var fn = (fileName ?? $"{name}.log").ReplaceInvalidChars();
                var fullPath = Path.Combine(targetDir, fn);

                return new Loggers.QuickLogger(fullPath)
                {
                    EnableConsoleLogging = _defaultLogger?.EnableConsoleLogging ?? true,
                    EnableFileLogging = true, // force file logging for a “file” logger
                    EnableEventLogging = _defaultLogger?.EnableEventLogging ?? false,
                    EnableTraceLogging = _defaultLogger?.EnableTraceLogging ?? false
                };
            });
        }

        /// <summary>
        /// Clears all the loggers that have been created and managed by the <see cref="LogManager"/>.
        /// This method is useful for resetting the log configuration during testing or re-initializing loggers in the application.
        /// </summary>
        public static void ClearLoggers() => _loggers.Clear();
    }

}
