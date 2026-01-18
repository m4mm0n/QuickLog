/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : AsyncDropPolicy.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:32:27 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 29F5C515
 *  
 *  Description    :
 *                   Specifies the policy used to handle entries when an asynchronous queue reaches its capacity.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 29F5C515

namespace QuickLog.Core;

/// <summary>
/// Specifies the policy used to handle entries when an asynchronous queue reaches its capacity.
/// </summary>
/// <remarks>Use this enumeration to control how entries are managed when the queue is full. The selected policy
/// determines whether new entries are blocked, dropped, or if existing entries are removed to make room. This is
/// commonly used in logging, event processing, or other asynchronous systems where queue overflow handling is
/// required.</remarks>
public enum AsyncDropPolicy
{
    /// <summary>No dropping; block or fail when queue is full.</summary>
    None,

    /// <summary>Never drop; TryAdd failure discards entry.</summary>
    DropNewest,

    /// <summary>Drop oldest entry to make room.</summary>
    DropOldest,

    /// <summary>Drop entries below a severity threshold.</summary>
    DropBelowLevel,

    /// <summary>Drop entries based on the role of the producing thread.</summary>
    DropByThreadRole
}