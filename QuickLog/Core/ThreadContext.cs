/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ThreadContext.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:39:14 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : BA0E2081
 *  
 *  Description    :
 *                   Provides per-thread role tagging for logging.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: BA0E2081

namespace QuickLog.Core;

/// <summary>
/// Provides per-thread role tagging for logging.
/// </summary>
public static class ThreadContext
{
    [ThreadStatic]
    private static ThreadRole _role;

    /// <summary>
    /// Gets or sets the current role assigned to the thread.
    /// </summary>
    /// <remarks>Changing the thread role may affect how the thread is managed or scheduled within the
    /// application. Ensure that the assigned value is appropriate for the intended thread behavior.</remarks>
    public static ThreadRole Role
    {
        get => _role;
        set => _role = value;
    }
    /// <summary>
    /// Sets the current thread role to the specified value.
    /// </summary>
    /// <param name="role">The thread role to assign. This value determines the behavior or permissions associated with the current thread.</param>
    public static void Set(ThreadRole role) => _role = role;
}