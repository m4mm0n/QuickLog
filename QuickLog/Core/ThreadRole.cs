/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ThreadRole.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:41:09 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 84DBDB51
 *  
 *  Description    :
 *                   Specifies the functional role assigned to a thread within an application, such as rendering, audio processing, I/O
 *                   operations, or general work.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 84DBDB51

namespace QuickLog.Core;

/// <summary>
/// Specifies the functional role assigned to a thread within an application, such as rendering, audio processing, I/O
/// operations, or general work.
/// </summary>
/// <remarks>Use this enumeration to identify or categorize threads based on their primary responsibility. This
/// can assist with diagnostics, logging, or thread management strategies. The values are intended to be descriptive and
/// may be extended to support additional roles as needed.</remarks>
public enum ThreadRole : byte
{
    /// <summary>
    /// Indicates that the value is unknown or has not been specified.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents rendering-related functionality or data within the application.
    /// </summary>
    Render,
    /// <summary>
    /// Represents audio-related functionality or data within the application.
    /// </summary>
    Audio,
    /// <summary>
    /// Represents input/output (I/O) operations or functionality within the application.
    /// </summary>
    IO,
    /// <summary>
    /// Represents general-purpose work or tasks that do not fall into other specific categories.
    /// </summary>
    Worker,
    /// <summary>
    /// Represents network-related functionality or data within the application.
    /// </summary>
    Network,
    /// <summary>
    /// Represents the main thread of the application, typically responsible for coordinating overall program flow.
    /// </summary>
    Main
}