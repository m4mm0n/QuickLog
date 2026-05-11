using QuickLog.Core;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogScopeTests
{
    [Fact]
    public void Current_IsNull_WhenNoScopeIsActive()
    {
        Assert.Null(LogScope.Current);
    }

    [Fact]
    public void Push_SetsCurrent_DisposeRestoresNull()
    {
        using (LogScope.Push("MyScope"))
            Assert.Equal("MyScope", LogScope.Current);

        Assert.Null(LogScope.Current);
    }

    [Fact]
    public void NestedPush_ReturnsInnermost_ThenRestoresOuter()
    {
        using (LogScope.Push("Outer"))
        {
            Assert.Equal("Outer", LogScope.Current);

            using (LogScope.Push("Inner"))
                Assert.Equal("Inner", LogScope.Current);

            Assert.Equal("Outer", LogScope.Current);
        }

        Assert.Null(LogScope.Current);
    }

    [Fact]
    public void Scopes_AreThreadLocal_DoNotLeakAcrossThreads()
    {
        string? otherThreadScope = "not-set";

        using (LogScope.Push("MainScope"))
        {
            var t = new Thread(() => otherThreadScope = LogScope.Current);
            t.Start();
            t.Join();
        }

        Assert.Null(otherThreadScope);
    }
}
