
# QuickLogger

**QuickLogger** is a lightweight, extensible logging library designed to provide a simple and flexible logging framework for .NET applications. It offers easy-to-use loggers for various destinations, such as the console, files, trace output, and event handlers, all without relying on external dependencies. QuickLogger is designed with simplicity in mind, making it easy to integrate into any .NET project.

## Key Features

- **Multi-Destination Logging**: Log to multiple outputs, including console, files, system trace, and event handlers.
- **Flexible Configuration**: Easily enable or disable different logging destinations (console, file, event, trace) through configurable flags.
- **Structured Log Types**: Supports various log levels/types such as `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Crit`, and `Exception` to categorize and filter log entries.
- **Caller Information**: Automatically captures the calling method, file, and line number for more informative log entries.
- **Thread-Safe Logging**: Designed for use in multi-threaded applications, ensuring loggers are safe to use across different threads and tasks.
- **Event-Driven**: Supports logging through event handlers, enabling custom handling of log entries (e.g., for sending logs to a remote server).
- **Centralized Logger Management**: Includes a `LogManager` for managing and retrieving loggers by name, making it easy to organize logging across large applications.

## Log Types

The `LogType` enum categorizes logs into various levels:

- **Trace**: Detailed logs typically used for tracing application flow and debugging.
- **Debug**: Logs used for debugging during development.
- **Info**: Informational messages that represent normal application behavior.
- **Warn**: Indicates potential issues or situations that require attention.
- **Error**: Represents errors that occur during application execution but are not critical.
- **Crit**: Critical errors, often representing system failures.
- **Exception**: Logs for exceptions, whether handled or unhandled.

## Installation

Simply include the source files in your .NET project. No external dependencies are required.

## Usage

### 1. Configuring a Default Logger

You can configure a default logger to log to a specific file or the console. For example, to log to a file:

```csharp
LogManager.ConfigureDefaultFileLogger("logs/application.log");
```

Alternatively, to configure a default logger that logs to the console:

```csharp
LogManager.ConfigureDefault();
```

### 2. Getting a Logger

You can retrieve a named logger from `LogManager`:

```csharp
var logger = LogManager.GetLogger("AppLogger");
```

Once retrieved, you can log messages or exceptions:

```csharp
logger.Log(LogType.Info, "Application has started.");
logger.Log(LogType.Error, new Exception("Something went wrong."));
```

You can also use the default logger:

```csharp
var defaultLogger = LogManager.GetDefaultLogger();
defaultLogger.Log(LogType.Debug, "Debug message from default logger.");
```

### 3. Logging with Caller Information

The logger automatically captures the method, file, and line number where the log is initiated:

```csharp
logger.Log(LogType.Warn, "This is a warning message.");
```

The log entry will include the calling method, file path, and line number, which makes it easier to trace issues in the code.

### 4. Event-Only Logging

If you want to capture logs through event handlers, you can use the `EventOnlyLogger`:

```csharp
var eventLogger = new EventOnlyLogger();
eventLogger.LogEvent += (sender, args) =>
{
    // Custom log handling
    Console.WriteLine($"Received log: {args.Message}");
};
eventLogger.Log(LogType.Info, "This log will trigger the event.");
```

### 5. Trace Logging

Use the `TraceLogger` to output logs to the system trace:

```csharp
var traceLogger = new TraceLogger();
traceLogger.Log(LogType.Trace, "This is a trace log.");
traceLogger.TraceMethodEntry();
traceLogger.TraceMethodExit(new Stopwatch());
```

### 6. Console Logging

The `ConsoleQuickLogger` outputs logs directly to the console:

```csharp
var consoleLogger = new ConsoleQuickLogger();
consoleLogger.Log(LogType.Info, "Logging to the console.");
```

### 7. File Logging

The `FileQuickLogger` writes log entries to a file:

```csharp
var fileLogger = new FileQuickLogger("logs/app.log");
fileLogger.Log(LogType.Error, "An error occurred.");
```



### 8. Godot Logging

The `GodotFileLogger` integrates seamlessly with **Godot 4+** projects written in C#.  
It automatically detects if it is running inside a Godot runtime using `IsGodotRuntime()`.  

- If **Godot is not present**, the logger falls back gracefully to a safe local folder and does not require the Godot assemblies at build time.  
- If **Godot is present**, log files are written to the standard Godot `user://` path (the game’s writable directory).  

```csharp
// Create a Godot logger (logs will go to user://logs/game.log inside Godot)
var godotLogger = new GodotFileLogger("game.log");

// Example usage
godotLogger.Log(LogType.Info, "Game started successfully.");
godotLogger.Log(LogType.Error, new Exception("Something went wrong inside Godot."));
```

### LogManager Support for Godot

You can configure and retrieve a default Godot logger via `LogManager` helpers:

```csharp
// Configure default logger for Godot (writes to user://logs/game.log)
LogManager.ConfigureDefaultGodotLogger("game.log");

// Retrieve default Godot logger
var godotLogger = LogManager.GetDefaultLogger();

// Log with it
godotLogger.Log(LogType.Debug, "Debug log from inside Godot.");
```

This allows **plug-and-play logging** for Godot projects while keeping QuickLogger compatible with plain .NET projects.


## LogManager Features

The `LogManager` simplifies the management of loggers across your application:

- **GetLogger(name)**: Retrieves or creates a logger with the specified name.
- **ConfigureDefault()**: Configures a default logger that logs to the console.
- **ConfigureDefaultFileLogger(logFilePath)**: Configures a default logger that logs to the specified file.
- **GetDefaultLogger()**: Retrieves the default logger.

## Example

Here’s a simple example of how to use `QuickLogger`:

```csharp
public class Program
{
    public static void Main()
    {
        // Configure default logger to log to both console and file
        LogManager.ConfigureDefaultFileLogger("logs/application.log");

        // Retrieve a named logger
        var logger = LogManager.GetLogger("MainApp");

        // Log messages of various types
        logger.Log(LogType.Info, "Application has started.");
        logger.Log(LogType.Warn, "This is a warning message.");
        logger.Log(LogType.Error, new Exception("A critical error occurred."));

        // Use the default logger
        var defaultLogger = LogManager.GetDefaultLogger();
        defaultLogger.Log(LogType.Debug, "This is a debug message from the default logger.");
    }
}
```

## License

QuickLogger is licensed under the MIT License. See `LICENSE` for more details.