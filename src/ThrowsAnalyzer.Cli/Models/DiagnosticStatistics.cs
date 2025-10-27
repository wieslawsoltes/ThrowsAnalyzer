namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Aggregated statistics about diagnostics.
/// </summary>
public class DiagnosticStatistics
{
    /// <summary>
    /// Total number of diagnostics.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of error-level diagnostics.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of warning-level diagnostics.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Number of info/suggestion-level diagnostics.
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// Number of hidden diagnostics.
    /// </summary>
    public int HiddenCount { get; set; }

    /// <summary>
    /// Count of diagnostics by diagnostic ID.
    /// </summary>
    public Dictionary<string, int> CountByDiagnosticId { get; set; } = new();

    /// <summary>
    /// Count of diagnostics by project name.
    /// </summary>
    public Dictionary<string, int> CountByProject { get; set; } = new();

    /// <summary>
    /// Count of diagnostics by file path.
    /// </summary>
    public Dictionary<string, int> CountByFile { get; set; } = new();

    /// <summary>
    /// Count of diagnostics by severity level.
    /// </summary>
    public Dictionary<string, int> CountBySeverity { get; set; } = new();

    /// <summary>
    /// Number of files analyzed.
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// Number of projects analyzed.
    /// </summary>
    public int ProjectsAnalyzed { get; set; }

    /// <summary>
    /// Get percentage for a count relative to total.
    /// </summary>
    public double GetPercentage(int count)
    {
        if (TotalCount == 0) return 0;
        return Math.Round((count / (double)TotalCount) * 100, 1);
    }
}
