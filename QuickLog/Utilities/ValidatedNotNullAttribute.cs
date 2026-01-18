/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ValidatedNotNullAttribute.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2024-11-27 06:56:25 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 6F211498
 *  
 *  Description    :
 *                   Attribute to tell Roslyn-Analyzers that a parameter will be checked for <see langword="null"/>
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 6F211498

namespace QuickLog.Utilities;

/// <summary>
/// Attribute to tell Roslyn-Analyzers that a parameter will be checked for <see langword="null"/>
/// </summary>
// https://github.com/dotnet/roslyn-analyzers/issues/2215
// https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/UnitTests/Microsoft.CodeQuality.Analyzers/QualityGuidelines/ValidateArgumentsOfPublicMethodsTests.cs

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class ValidatedNotNullAttribute : Attribute { }
