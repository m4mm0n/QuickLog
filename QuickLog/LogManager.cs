using QuickLog.Loggers;
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
        private static string _logPath = "logs";

        /// <summary>
        /// Gets or creates a logger by name. 
        /// If a logger with the specified name does not exist, it will be created with default settings.
        /// </summary>
        /// <param name="name">The unique name of the logger. This can be used to identify loggers in different parts of the application.</param>
        /// <returns>An instance of <see cref="IQuickLog"/> that corresponds to the specified name.</returns>
        public static IQuickLog GetLogger(string name)
        {
            if (!_configured) ConfigureDefault();

            return _loggers.GetOrAdd(name, key => CreateLogger(name));
        }

        /// <summary>
        /// Configures the default logger used by the application. 
        /// If no specific logger is requested, the default logger is returned by the <see cref="GetDefaultLogger"/> method.
        /// </summary>
        /// <param name="defaultLogger">An optional instance of <see cref="QuickLogger"/> to be used as the default logger. 
        /// If null, a new default logger with console logging enabled is created.</param>
        public static void ConfigureDefault(QuickLogger? defaultLogger = null)
        {
            _defaultLogger = defaultLogger ?? new QuickLogger
            {
                EnableConsoleLogging = true,
                EnableFileLogging = false,
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
        /// Clears all the loggers that have been created and managed by the <see cref="LogManager"/>.
        /// This method is useful for resetting the log configuration during testing or re-initializing loggers in the application.
        /// </summary>
        public static void ClearLoggers() => _loggers.Clear();
    }

}
