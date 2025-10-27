namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Analysis results for a single project.
/// </summary>
public class ProjectAnalysis
{
    /// <summary>
    /// Project name.
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Full path to project file.
    /// </summary>
    public required string ProjectPath { get; set; }

    /// <summary>
    /// All diagnostics found in this project.
    /// </summary>
    public List<DiagnosticInfo> Diagnostics { get; set; } = new();

    /// <summary>
    /// Statistics for this project's diagnostics.
    /// </summary>
    public DiagnosticStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Number of files in this project.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Whether the project compiled successfully.
    /// </summary>
    public bool CompiledSuccessfully { get; set; } = true;

    /// <summary>
    /// Compilation errors if any.
    /// </summary>
    public List<string> CompilationErrors { get; set; } = new();
}
