namespace QuickLog.Utilities
{
    /// <summary>
    /// Attribute to tell Roslyn-Analyzers that a parameter will be checked for <see langword="null"/>
    /// </summary>
    // https://github.com/dotnet/roslyn-analyzers/issues/2215
    // https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/UnitTests/Microsoft.CodeQuality.Analyzers/QualityGuidelines/ValidateArgumentsOfPublicMethodsTests.cs

    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ValidatedNotNullAttribute : Attribute { }
}
