namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Complete analysis result for a solution or project.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Path to the analyzed solution or project.
    /// </summary>
    public required string TargetPath { get; set; }

    /// <summary>
    /// Type of target (Solution or Project).
    /// </summary>
    public required string TargetType { get; set; }

    /// <summary>
    /// When the analysis was performed.
    /// </summary>
    public DateTime AnalysisDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Duration of the analysis.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// All projects analyzed.
    /// </summary>
    public List<ProjectAnalysis> Projects { get; set; } = new();

    /// <summary>
    /// Overall statistics across all projects.
    /// </summary>
    public DiagnosticStatistics Statistics { get; set; } = new();

    /// <summary>
    /// All diagnostics from all projects.
    /// </summary>
    public List<DiagnosticInfo> AllDiagnostics { get; set; } = new();

    /// <summary>
    /// Build configuration used (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; set; } = "Debug";

    /// <summary>
    /// ThrowsAnalyzer version used.
    /// </summary>
    public string? AnalyzerVersion { get; set; }

    /// <summary>
    /// Whether the analysis completed successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Any errors or warnings during analysis.
    /// </summary>
    public List<string> AnalysisMessages { get; set; } = new();

    /// <summary>
    /// Get name of the target (solution or project name).
    /// </summary>
    public string GetTargetName()
    {
        return Path.GetFileNameWithoutExtension(TargetPath);
    }

    /// <summary>
    /// Get base directory for relative paths.
    /// </summary>
    public string GetBaseDirectory()
    {
        return Path.GetDirectoryName(TargetPath) ?? string.Empty;
    }
}
