using System.Text;
using ThrowsAnalyzer.Cli.Models;

namespace ThrowsAnalyzer.Cli.Reports;

/// <summary>
/// Generates Markdown format reports.
/// </summary>
public class MarkdownReportGenerator : IReportGenerator
{
    public string FileExtension => ".md";

    public async Task<string> GenerateReportAsync(
        AnalysisResult result,
        ReportOptions options,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Title and metadata
        sb.AppendLine($"# ThrowsAnalyzer Report: {result.GetTargetName()}");
        sb.AppendLine();
        sb.AppendLine($"**Analysis Date:** {result.AnalysisDate:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Target:** {result.TargetType} - `{result.TargetPath}`");
        sb.AppendLine($"**Configuration:** {result.Configuration}");
        sb.AppendLine($"**Duration:** {result.Duration.TotalSeconds:F2}s");
        sb.AppendLine($"**Status:** {(result.Success ? "✓ Success" : "✗ Failed")}");
        sb.AppendLine();

        // Summary statistics
        GenerateSummarySection(sb, result);

        // Statistics by diagnostic ID
        GenerateDiagnosticIdSection(sb, result);

        // Statistics by project
        if (result.Projects.Count > 1)
        {
            GenerateProjectSection(sb, result);
        }

        // Statistics by severity
        GenerateSeveritySection(sb, result);

        // Top files with most diagnostics
        GenerateTopFilesSection(sb, result);

        // Detailed diagnostics
        if (options.MaxDetailedDiagnostics == 0 || result.AllDiagnostics.Count <= options.MaxDetailedDiagnostics)
        {
            GenerateDetailedDiagnosticsSection(sb, result, options);
        }
        else
        {
            sb.AppendLine($"## Detailed Diagnostics");
            sb.AppendLine();
            sb.AppendLine($"_Showing top {options.MaxDetailedDiagnostics} of {result.AllDiagnostics.Count} diagnostics._");
            sb.AppendLine();
            GenerateDetailedDiagnosticsSection(sb, result, options, options.MaxDetailedDiagnostics);
        }

        // Analysis messages
        if (result.AnalysisMessages.Count > 0)
        {
            GenerateMessagesSection(sb, result);
        }

        // Write to file
        var outputPath = GetOutputPath(options, result);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), cancellationToken);

        return outputPath;
    }

    private void GenerateSummarySection(StringBuilder sb, AnalysisResult result)
    {
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|------:|");
        sb.AppendLine($"| Total Diagnostics | {result.Statistics.TotalCount} |");
        sb.AppendLine($"| Errors | {result.Statistics.ErrorCount} |");
        sb.AppendLine($"| Warnings | {result.Statistics.WarningCount} |");
        sb.AppendLine($"| Info/Hidden | {result.Statistics.InfoCount} |");
        sb.AppendLine($"| Projects Analyzed | {result.Statistics.ProjectsAnalyzed} |");
        sb.AppendLine($"| Files Analyzed | {result.Statistics.FilesAnalyzed} |");
        sb.AppendLine();
    }

    private void GenerateDiagnosticIdSection(StringBuilder sb, AnalysisResult result)
    {
        if (result.Statistics.CountByDiagnosticId.Count == 0) return;

        sb.AppendLine("## Diagnostics by ID");
        sb.AppendLine();
        sb.AppendLine("| Diagnostic ID | Count | Percentage |");
        sb.AppendLine("|--------------|------:|-----------:|");

        foreach (var kvp in result.Statistics.CountByDiagnosticId.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            sb.AppendLine($"| {kvp.Key} | {kvp.Value} | {percentage:F1}% |");
        }
        sb.AppendLine();
    }

    private void GenerateProjectSection(StringBuilder sb, AnalysisResult result)
    {
        if (result.Statistics.CountByProject.Count == 0) return;

        sb.AppendLine("## Diagnostics by Project");
        sb.AppendLine();
        sb.AppendLine("| Project | Count | Percentage |");
        sb.AppendLine("|---------|------:|-----------:|");

        foreach (var kvp in result.Statistics.CountByProject.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            sb.AppendLine($"| {kvp.Key} | {kvp.Value} | {percentage:F1}% |");
        }
        sb.AppendLine();
    }

    private void GenerateSeveritySection(StringBuilder sb, AnalysisResult result)
    {
        if (result.Statistics.CountBySeverity.Count == 0) return;

        sb.AppendLine("## Diagnostics by Severity");
        sb.AppendLine();
        sb.AppendLine("| Severity | Count | Percentage |");
        sb.AppendLine("|----------|------:|-----------:|");

        foreach (var kvp in result.Statistics.CountBySeverity.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            sb.AppendLine($"| {kvp.Key} | {kvp.Value} | {percentage:F1}% |");
        }
        sb.AppendLine();
    }

    private void GenerateTopFilesSection(StringBuilder sb, AnalysisResult result)
    {
        if (result.Statistics.CountByFile.Count == 0) return;

        sb.AppendLine("## Top 20 Files with Most Diagnostics");
        sb.AppendLine();
        sb.AppendLine("| File | Count |");
        sb.AppendLine("|------|------:|");

        var baseDir = result.GetBaseDirectory();
        foreach (var kvp in result.Statistics.CountByFile.OrderByDescending(x => x.Value).Take(20))
        {
            var relativePath = GetRelativePath(kvp.Key, baseDir);
            sb.AppendLine($"| `{EscapeMarkdown(relativePath)}` | {kvp.Value} |");
        }
        sb.AppendLine();
    }

    private void GenerateDetailedDiagnosticsSection(StringBuilder sb, AnalysisResult result, ReportOptions options, int? maxCount = null)
    {
        if (result.AllDiagnostics.Count == 0) return;

        if (maxCount == null)
        {
            sb.AppendLine("## Detailed Diagnostics");
            sb.AppendLine();
        }

        var diagnostics = maxCount.HasValue
            ? result.AllDiagnostics.Take(maxCount.Value)
            : result.AllDiagnostics;

        var baseDir = result.GetBaseDirectory();
        var groupedByFile = diagnostics.GroupBy(d => d.FilePath).OrderBy(g => g.Key);

        foreach (var fileGroup in groupedByFile)
        {
            var relativePath = GetRelativePath(fileGroup.Key, baseDir);
            sb.AppendLine($"### {EscapeMarkdown(relativePath)}");
            sb.AppendLine();

            foreach (var diagnostic in fileGroup.OrderBy(d => d.Line))
            {
                sb.AppendLine($"#### {diagnostic.Id}: {EscapeMarkdown(diagnostic.Title)}");
                sb.AppendLine();
                sb.AppendLine($"**Severity:** {diagnostic.Severity}  ");
                sb.AppendLine($"**Location:** Line {diagnostic.Line}, Column {diagnostic.Column}  ");
                sb.AppendLine($"**Project:** {diagnostic.ProjectName}");
                sb.AppendLine();
                sb.AppendLine($"**Message:** {EscapeMarkdown(diagnostic.Message)}");
                sb.AppendLine();

                if (options.IncludeCodeSnippets && !string.IsNullOrEmpty(diagnostic.CodeSnippet))
                {
                    sb.AppendLine("**Code:**");
                    sb.AppendLine();
                    sb.AppendLine("```csharp");
                    sb.AppendLine(diagnostic.CodeSnippet);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(diagnostic.HelpLink))
                {
                    sb.AppendLine($"[More information]({diagnostic.HelpLink})");
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
    }

    private void GenerateMessagesSection(StringBuilder sb, AnalysisResult result)
    {
        sb.AppendLine("## Analysis Messages");
        sb.AppendLine();

        foreach (var message in result.AnalysisMessages)
        {
            sb.AppendLine($"- {EscapeMarkdown(message)}");
        }
        sb.AppendLine();
    }

    private string GetOutputPath(ReportOptions options, AnalysisResult result)
    {
        var fileName = $"{options.BaseFileName}{FileExtension}";
        return Path.Combine(options.OutputDirectory, fileName);
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
        {
            return fullPath;
        }

        try
        {
            var fullUri = new Uri(Path.GetFullPath(fullPath));
            var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return fullPath;
        }
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("[", "\\[")
            .Replace("]", "\\]");
    }
}
