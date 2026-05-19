using QuickLog.Exceptions;
using QuickLog.Godot;
using QuickLog.Loggers;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests
{
    [Collection("Sequential")]
    public sealed class GodotIntegrationTests : IDisposable
    {
        private readonly MemoryQuickLogger _logger = new();

        public void Dispose()
        {
            GodotLogInterceptor.Detach();
            ExceptionHookManager.Detach();
            _logger.Dispose();
            global::Godot.OS.Reset();
        }

        [Fact]
        public void GodotUserPathResolver_FindsLoadedGodotProjectSettings()
        {
            var root = Path.Combine(Path.GetTempPath(), "quicklog-godot-tests", Guid.NewGuid().ToString("N"));
            global::Godot.ProjectSettings.UserPath = root;

            Assert.Equal(root, GodotUserPathResolver.GetUserDir());
        }

        [Fact]
        public void Attach_CanRegisterDynamicLoggerAfterEarlierDisabledAttach()
        {
            GodotLogInterceptor.Attach(_logger, new GodotLogOptions
            {
                HijackExceptions = false,
                TryDynamicLoggerRegistration = false
            });

            Assert.False(GodotLogInterceptor.IsDynamicSinkRegistered);

            GodotLogInterceptor.Attach(_logger, new GodotLogOptions
            {
                HijackExceptions = false,
                TryDynamicLoggerRegistration = true
            });

            Assert.True(GodotLogInterceptor.IsDynamicSinkRegistered);
            Assert.NotNull(global::Godot.OS.LastLogger);
        }

        [Fact]
        public void DynamicLogger_RoutesMessageAndErrorCallbacks()
        {
            GodotLogInterceptor.Attach(_logger, new GodotLogOptions
            {
                HijackExceptions = false,
                TryDynamicLoggerRegistration = true
            });

            Assert.NotNull(global::Godot.OS.LastLogger);

            global::Godot.OS.LastLogger!._LogMessage("hello from godot", error: false);
            global::Godot.OS.LastLogger!._LogError(
                "Tick",
                "res://player.gd",
                42,
                "ERR_TEST",
                "simulated warning",
                editorNotify: false,
                global::Godot.Logger.ErrorType.Warning,
                new global::Godot.Collections.Array<global::Godot.ScriptBacktrace>());

            var entries = _logger.Snapshot();
            Assert.Contains(entries, e => e.LoggingType == LogType.Info && e.Message?.Contains("[Godot] hello from godot") == true);
            Assert.Contains(entries, e => e.LoggingType == LogType.Warn && e.Message?.Contains("[Godot Warning] @ res://player.gd:42 (Tick)") == true);
        }

        [Fact]
        public void Reattach_WithHijackDisabled_DetachesGodotOwnedExceptionHooks()
        {
            GodotLogInterceptor.Attach(_logger, new GodotLogOptions
            {
                TryDynamicLoggerRegistration = false,
                HijackExceptions = true,
                ExceptionOptions = new ExceptionHookOptions
                {
                    ShowPopup = false,
                    CrashDump = new CrashDumpOptions { Enabled = false }
                }
            });

            Assert.True(ExceptionHookManager.IsAttached);

            GodotLogInterceptor.Attach(_logger, new GodotLogOptions
            {
                TryDynamicLoggerRegistration = false,
                HijackExceptions = false
            });

            Assert.False(ExceptionHookManager.IsAttached);
        }

        [Fact]
        public void GodotFileLogger_UsesSafeDefaultName_WhenSanitizedNameIsEmpty()
        {
            var root = Path.Combine(Path.GetTempPath(), "quicklog-godot-tests", Guid.NewGuid().ToString("N"));
            global::Godot.ProjectSettings.UserPath = root;

            using var logger = new GodotFileLogger("<>:\"/\\|?*", subfolder: "");

            Assert.Equal(Path.Combine(root, "game.log"), logger.FullPath);
            logger.Log(LogType.Info, "safe file name");
            Assert.True(File.Exists(logger.FullPath));
        }
    }
}

namespace Godot
{
    public abstract class Logger
    {
        public enum ErrorType
        {
            Error = 0,
            Warning = 1,
            Script = 2,
            Shader = 3
        }

        public virtual void _LogMessage(string message, bool error)
        {
        }

        public virtual void _LogError(
            string function,
            string file,
            int line,
            string code,
            string rationale,
            bool editorNotify,
            ErrorType type,
            Collections.Array<ScriptBacktrace> scriptBacktraces)
        {
        }
    }

    public static class OS
    {
        public static Logger? LastLogger { get; private set; }
        public static string? LastAlertText { get; private set; }
        public static string? LastAlertTitle { get; private set; }

        public static void AddLogger(Logger logger) => LastLogger = logger;

        public static void RemoveLogger(Logger logger)
        {
            if (ReferenceEquals(LastLogger, logger))
                LastLogger = null;
        }

        public static void Alert(string text, string title = "Alert!")
        {
            LastAlertText = text;
            LastAlertTitle = title;
        }

        public static void Reset()
        {
            LastLogger = null;
            LastAlertText = null;
            LastAlertTitle = null;
            ProjectSettings.UserPath = Path.Combine(Path.GetTempPath(), "quicklog-fake-godot-user");
        }
    }

    public static class ProjectSettings
    {
        public static string UserPath { get; set; } = Path.Combine(Path.GetTempPath(), "quicklog-fake-godot-user");

        public static string GlobalizePath(string path) => path == "user://" ? UserPath : path;
    }

    public sealed class ScriptBacktrace
    {
    }

    namespace Collections
    {
        public sealed class Array<T>
        {
        }
    }
}
