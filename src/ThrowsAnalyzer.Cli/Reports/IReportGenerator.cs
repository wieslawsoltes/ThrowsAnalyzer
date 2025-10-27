using ThrowsAnalyzer.Cli.Models;

namespace ThrowsAnalyzer.Cli.Reports;

/// <summary>
/// Interface for generating analysis reports.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generate a report from analysis results.
    /// </summary>
    /// <param name="result">The analysis result to report on.</param>
    /// <param name="options">Report generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the generated report file.</returns>
    Task<string> GenerateReportAsync(
        AnalysisResult result,
        ReportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the file extension for this report format (e.g., ".html", ".md").
    /// </summary>
    string FileExtension { get; }
}
