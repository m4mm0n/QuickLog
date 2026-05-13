using QuickLog.Core;
using CoreLogScope = QuickLog.Core.LogScope;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogScopeTests
{
    [Fact]
    public void Current_IsNull_WhenNoScopeIsActive()
    {
        Assert.Null(CoreLogScope.Current);
    }

    [Fact]
    public void Push_SetsCurrent_DisposeRestoresNull()
    {
        using (CoreLogScope.Push("MyScope"))
            Assert.Equal("MyScope", CoreLogScope.Current);

        Assert.Null(CoreLogScope.Current);
    }

    [Fact]
    public void NestedPush_ReturnsInnermost_ThenRestoresOuter()
    {
        using (CoreLogScope.Push("Outer"))
        {
            Assert.Equal("Outer", CoreLogScope.Current);

            using (CoreLogScope.Push("Inner"))
                Assert.Equal("Inner", CoreLogScope.Current);

            Assert.Equal("Outer", CoreLogScope.Current);
        }

        Assert.Null(CoreLogScope.Current);
    }

    [Fact]
    public void Scopes_FlowWithExecutionContext()
    {
        string? otherThreadScope = "not-set";

        using (CoreLogScope.Push("MainScope"))
        {
            var t = new Thread(() => otherThreadScope = CoreLogScope.Current);
            t.Start();
            t.Join();
        }

        Assert.Equal("MainScope", otherThreadScope);
    }

    [Fact]
    public async Task PublicLogScope_FlowsAcrossAwait()
    {
        using (QuickLog.LogScope.Begin("Frame", 42))
        {
            await Task.Yield();
            Assert.Equal("Frame:42", QuickLog.LogScope.Current);
        }

        Assert.Null(QuickLog.LogScope.Current);
    }
}
