namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Options for report generation.
/// </summary>
public class ReportOptions
{
    /// <summary>
    /// Output directory for reports.
    /// </summary>
    public string OutputDirectory { get; set; } = "./reports";

    /// <summary>
    /// Report format (Html, Markdown, or Both).
    /// </summary>
    public ReportFormat Format { get; set; } = ReportFormat.Both;

    /// <summary>
    /// Base file name for reports (without extension).
    /// </summary>
    public string BaseFileName { get; set; } = "analysis-report";

    /// <summary>
    /// Whether to open the report in browser/editor after generation.
    /// </summary>
    public bool OpenAfterGeneration { get; set; }

    /// <summary>
    /// Whether to include charts/graphs in HTML report.
    /// </summary>
    public bool IncludeCharts { get; set; } = true;

    /// <summary>
    /// Whether to make HTML tables sortable.
    /// </summary>
    public bool MakeSortable { get; set; } = true;

    /// <summary>
    /// Whether to include code snippets in reports.
    /// </summary>
    public bool IncludeCodeSnippets { get; set; } = true;

    /// <summary>
    /// Maximum number of diagnostics to include in detailed section (0 = all).
    /// </summary>
    public int MaxDetailedDiagnostics { get; set; }
}

/// <summary>
/// Report format options.
/// </summary>
public enum ReportFormat
{
    Html,
    Markdown,
    Both
}
