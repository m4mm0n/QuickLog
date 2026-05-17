namespace QuickLog;

/// <summary>
/// Runs delegates through QLOG attribute instrumentation without proxies, weaving, or dependencies.
/// </summary>
public static class QLogRunner
{
    /// <summary>
    /// Invokes an action and emits QLOG markers when the target method or declaring type is marked.
    /// </summary>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="action">Action to invoke.</param>
    public static void Invoke(IQuickLog logger, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = QLogScope.Enter(logger, action.Method, action.Method.Name);
        try
        {
            action();
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    /// <summary>
    /// Invokes a function and emits QLOG markers when the target method or declaring type is marked.
    /// </summary>
    /// <typeparam name="T">Return value type.</typeparam>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="func">Function to invoke.</param>
    /// <returns>The function return value.</returns>
    public static T Invoke<T>(IQuickLog logger, Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using var scope = QLogScope.Enter(logger, func.Method, func.Method.Name);
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    /// <summary>
    /// Invokes an asynchronous action and emits QLOG markers when the target method or declaring type is marked.
    /// </summary>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="action">Asynchronous action to invoke.</param>
    public static async Task InvokeAsync(IQuickLog logger, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = QLogScope.Enter(logger, action.Method, action.Method.Name);
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    /// <summary>
    /// Invokes an asynchronous function and emits QLOG markers when the target method or declaring type is marked.
    /// </summary>
    /// <typeparam name="T">Return value type.</typeparam>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="func">Asynchronous function to invoke.</param>
    /// <returns>The function return value.</returns>
    public static async Task<T> InvokeAsync<T>(IQuickLog logger, Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        using var scope = QLogScope.Enter(logger, func.Method, func.Method.Name);
        try
        {
            return await func().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }
}
