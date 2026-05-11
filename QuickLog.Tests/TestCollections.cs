using Xunit;

namespace QuickLog.Tests;

// All tests that mutate process-wide static state (ExceptionHookManager, LogManager)
// belong in this single collection so they run serially and cannot interfere.
[CollectionDefinition("Sequential")]
public sealed class SequentialCollection { }
