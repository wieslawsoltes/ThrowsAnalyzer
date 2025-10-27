using System;

namespace ThrowsAnalyzer;

/// <summary>
/// Suppresses ThrowsAnalyzer diagnostics for the attributed member.
/// </summary>
/// <example>
/// <code>
/// [SuppressThrowsAnalysis("THROWS001", "THROWS002", Justification = "Exception handling is intentional")]
/// public void MethodWithIntentionalThrow()
/// {
///     throw new InvalidOperationException();
/// }
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Method |
    AttributeTargets.Constructor |
    AttributeTargets.Property |
    AttributeTargets.Event |
    AttributeTargets.Class |
    AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public sealed class SuppressThrowsAnalysisAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuppressThrowsAnalysisAttribute"/> class.
    /// </summary>
    /// <param name="rules">
    /// The diagnostic rules to suppress (e.g., "THROWS001", "THROWS002").
    /// Use "THROWS*" to suppress all ThrowsAnalyzer diagnostics.
    /// </param>
    public SuppressThrowsAnalysisAttribute(params string[] rules)
    {
        Rules = rules ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the diagnostic rules to suppress.
    /// </summary>
    public string[] Rules { get; }

    /// <summary>
    /// Gets or sets the justification for suppressing the diagnostics.
    /// </summary>
    public string Justification { get; set; }
}
