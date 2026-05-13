/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : LogScope.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:50:29 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : FC5AD5C8
 *  
 *  Description    :
 *                   Provides a thread-local stack for managing logical logging scopes within the current thread.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: FC5AD5C8

namespace QuickLog.Core;

/// <summary>
/// Provides a thread-local stack for managing logical logging scopes within the current thread.
/// </summary>
/// <remarks>This class is intended for use in logging scenarios where operations are grouped into nested scopes.
/// Each thread maintains its own independent scope stack. The class is static and cannot be instantiated.</remarks>
internal static class LogScope
{
    public static IDisposable Push(string name) => QuickLog.LogScope.Begin(name);

    public static string? Current => QuickLog.LogScope.Current;
}
