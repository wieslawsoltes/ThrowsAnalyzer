namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Options for running the analysis.
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Path to solution, project, or directory to analyze.
    /// </summary>
    public required string TargetPath { get; set; }

    /// <summary>
    /// Minimum severity level to report (Error, Warning, Info, Hidden).
    /// </summary>
    public string MinimumSeverity { get; set; } = "Info";

    /// <summary>
    /// Build configuration (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; set; } = "Debug";

    /// <summary>
    /// Filter to specific diagnostic IDs (e.g., "THROWS004,THROWS021").
    /// </summary>
    public List<string> DiagnosticIds { get; set; } = new();

    /// <summary>
    /// Filter to specific project names.
    /// </summary>
    public List<string> ProjectNames { get; set; } = new();

    /// <summary>
    /// File patterns to exclude (glob patterns).
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Whether to skip NuGet restore before analysis.
    /// </summary>
    public bool SkipRestore { get; set; }

    /// <summary>
    /// Whether to show verbose output.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Maximum number of diagnostics to collect (0 = unlimited).
    /// </summary>
    public int MaxDiagnostics { get; set; }

    /// <summary>
    /// Whether to include code snippets in results.
    /// </summary>
    public bool IncludeCodeSnippets { get; set; } = true;

    /// <summary>
    /// Number of lines to include in code snippets (before and after).
    /// </summary>
    public int CodeSnippetLines { get; set; } = 3;
}
