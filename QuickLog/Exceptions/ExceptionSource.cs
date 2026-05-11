/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ExceptionSource.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

namespace QuickLog.Exceptions;

/// <summary>
/// Identifies which runtime hook captured an unhandled exception.
/// </summary>
public enum ExceptionSource
{
    /// <summary>Raised by <see cref="AppDomain.UnhandledException"/>.</summary>
    AppDomain,

    /// <summary>Raised by <see cref="TaskScheduler.UnobservedTaskException"/>.</summary>
    UnobservedTask
}
