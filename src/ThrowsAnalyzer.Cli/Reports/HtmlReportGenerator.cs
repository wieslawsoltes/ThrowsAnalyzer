using System.Text;
using System.Web;
using ThrowsAnalyzer.Cli.Models;

namespace ThrowsAnalyzer.Cli.Reports;

/// <summary>
/// Generates HTML format reports with interactive tables and charts.
/// </summary>
public class HtmlReportGenerator : IReportGenerator
{
    public string FileExtension => ".html";

    public async Task<string> GenerateReportAsync(
        AnalysisResult result,
        ReportOptions options,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // HTML header
        GenerateHeader(sb, result, options);

        // Body start
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");

        // Title
        sb.AppendLine($"    <h1>ThrowsAnalyzer Report: {Escape(result.GetTargetName())}</h1>");

        // Metadata
        GenerateMetadata(sb, result);

        // Summary
        GenerateSummarySection(sb, result, options);

        // Charts
        if (options.IncludeCharts)
        {
            GenerateChartsSection(sb, result);
        }

        // Statistics tables
        GenerateDiagnosticIdSection(sb, result, options);

        if (result.Projects.Count > 1)
        {
            GenerateProjectSection(sb, result, options);
        }

        GenerateSeveritySection(sb, result, options);
        GenerateTopFilesSection(sb, result, options);

        // Detailed diagnostics
        if (options.MaxDetailedDiagnostics == 0 || result.AllDiagnostics.Count <= options.MaxDetailedDiagnostics)
        {
            GenerateDetailedDiagnosticsSection(sb, result, options);
        }
        else
        {
            sb.AppendLine($"    <h2>Detailed Diagnostics</h2>");
            sb.AppendLine($"    <p class=\"info\">Showing top {options.MaxDetailedDiagnostics} of {result.AllDiagnostics.Count} diagnostics.</p>");
            GenerateDetailedDiagnosticsSection(sb, result, options, options.MaxDetailedDiagnostics);
        }

        // Analysis messages
        if (result.AnalysisMessages.Count > 0)
        {
            GenerateMessagesSection(sb, result);
        }

        // Footer
        GenerateFooter(sb);

        // Body end
        sb.AppendLine("  </div>");

        // Initialize sortable tables
        if (options.MakeSortable)
        {
            sb.AppendLine("  <script>initSortableTables();</script>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        // Write to file
        var outputPath = GetOutputPath(options, result);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), cancellationToken);

        return outputPath;
    }

    private void GenerateHeader(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>ThrowsAnalyzer Report - {Escape(result.GetTargetName())}</title>");

        // Embedded CSS
        GenerateStyles(sb, options);

        // Embedded JavaScript
        if (options.MakeSortable || options.IncludeCharts)
        {
            GenerateScripts(sb, options);
        }

        sb.AppendLine("</head>");
    }

    private void GenerateStyles(StringBuilder sb, ReportOptions options)
    {
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
      line-height: 1.6;
      color: #333;
      max-width: 1400px;
      margin: 0 auto;
      padding: 20px;
      background: #f5f5f5;
    }
    .container {
      background: white;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    h1 {
      color: #2c3e50;
      border-bottom: 3px solid #3498db;
      padding-bottom: 10px;
    }
    h2 {
      color: #34495e;
      margin-top: 30px;
      border-bottom: 2px solid #ecf0f1;
      padding-bottom: 8px;
    }
    h3 {
      color: #7f8c8d;
      margin-top: 20px;
    }
    .metadata {
      background: #ecf0f1;
      padding: 15px;
      border-radius: 5px;
      margin: 20px 0;
    }
    .metadata p {
      margin: 5px 0;
    }
    .status-success {
      color: #27ae60;
      font-weight: bold;
    }
    .status-failed {
      color: #e74c3c;
      font-weight: bold;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin: 20px 0;
      background: white;
    }
    th, td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }
    th {
      background: #3498db;
      color: white;
      font-weight: 600;
      cursor: pointer;
      user-select: none;
      position: relative;
    }
    th:hover {
      background: #2980b9;
    }
    th.sortable::after {
      content: ' ⇅';
      opacity: 0.3;
    }
    th.sorted-asc::after {
      content: ' ▲';
      opacity: 1;
    }
    th.sorted-desc::after {
      content: ' ▼';
      opacity: 1;
    }
    tr:hover {
      background: #f8f9fa;
    }
    td:last-child, th:last-child {
      text-align: right;
    }
    .diagnostic-card {
      background: #f8f9fa;
      border-left: 4px solid #3498db;
      padding: 15px;
      margin: 15px 0;
      border-radius: 4px;
    }
    .diagnostic-error {
      border-left-color: #e74c3c;
    }
    .diagnostic-warning {
      border-left-color: #f39c12;
    }
    .diagnostic-info {
      border-left-color: #3498db;
    }
    .diagnostic-hidden {
      border-left-color: #95a5a6;
    }
    .diagnostic-card h4 {
      margin-top: 0;
      color: #2c3e50;
    }
    .diagnostic-meta {
      color: #7f8c8d;
      font-size: 0.9em;
      margin: 10px 0;
    }
    .code-snippet {
      background: #2c3e50;
      color: #ecf0f1;
      padding: 15px;
      border-radius: 4px;
      overflow-x: auto;
      font-family: 'Courier New', monospace;
      font-size: 0.9em;
      line-height: 1.4;
      margin: 10px 0;
    }
    .severity-error {
      color: #e74c3c;
      font-weight: bold;
    }
    .severity-warning {
      color: #f39c12;
      font-weight: bold;
    }
    .severity-info {
      color: #3498db;
      font-weight: bold;
    }
    .chart-container {
      margin: 30px 0;
      padding: 20px;
      background: #f8f9fa;
      border-radius: 5px;
    }
    canvas {
      max-height: 400px;
    }
    .info {
      background: #d6eaf8;
      padding: 10px;
      border-radius: 4px;
      border-left: 4px solid #3498db;
    }
    .messages {
      background: #fef5e7;
      padding: 15px;
      border-radius: 4px;
      border-left: 4px solid #f39c12;
    }
    .footer {
      margin-top: 40px;
      padding-top: 20px;
      border-top: 2px solid #ecf0f1;
      text-align: center;
      color: #7f8c8d;
      font-size: 0.9em;
    }
    code {
      background: #ecf0f1;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      font-size: 0.9em;
    }
    a {
      color: #3498db;
      text-decoration: none;
    }
    a:hover {
      text-decoration: underline;
    }
  ");
        sb.AppendLine("  </style>");
    }

    private void GenerateScripts(StringBuilder sb, ReportOptions options)
    {
        sb.AppendLine("  <script>");

        if (options.MakeSortable)
        {
            sb.AppendLine(@"
    function initSortableTables() {
      const tables = document.querySelectorAll('table.sortable');
      tables.forEach(table => {
        const headers = table.querySelectorAll('th');
        headers.forEach((header, index) => {
          header.classList.add('sortable');
          header.addEventListener('click', () => {
            sortTable(table, index);
          });
        });
      });
    }

    function sortTable(table, column) {
      const tbody = table.querySelector('tbody');
      const rows = Array.from(tbody.querySelectorAll('tr'));
      const header = table.querySelectorAll('th')[column];
      const isAsc = header.classList.contains('sorted-asc');

      // Remove all sorting classes
      table.querySelectorAll('th').forEach(h => {
        h.classList.remove('sorted-asc', 'sorted-desc');
      });

      // Add new sorting class
      header.classList.add(isAsc ? 'sorted-desc' : 'sorted-asc');

      rows.sort((a, b) => {
        const aText = a.cells[column].textContent.trim();
        const bText = b.cells[column].textContent.trim();

        // Try to parse as numbers
        const aNum = parseFloat(aText.replace(/[^0-9.-]/g, ''));
        const bNum = parseFloat(bText.replace(/[^0-9.-]/g, ''));

        if (!isNaN(aNum) && !isNaN(bNum)) {
          return isAsc ? bNum - aNum : aNum - bNum;
        }

        // Compare as strings
        return isAsc ? bText.localeCompare(aText) : aText.localeCompare(bText);
      });

      rows.forEach(row => tbody.appendChild(row));
    }
  ");
        }

        sb.AppendLine("  </script>");
    }

    private void GenerateMetadata(StringBuilder sb, AnalysisResult result)
    {
        var statusClass = result.Success ? "status-success" : "status-failed";
        var statusText = result.Success ? "✓ Success" : "✗ Failed";

        sb.AppendLine("    <div class=\"metadata\">");
        sb.AppendLine($"      <p><strong>Analysis Date:</strong> {result.AnalysisDate:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"      <p><strong>Target:</strong> {result.TargetType} - <code>{Escape(result.TargetPath)}</code></p>");
        sb.AppendLine($"      <p><strong>Configuration:</strong> {result.Configuration}</p>");
        sb.AppendLine($"      <p><strong>Duration:</strong> {result.Duration.TotalSeconds:F2}s</p>");
        sb.AppendLine($"      <p><strong>Status:</strong> <span class=\"{statusClass}\">{statusText}</span></p>");
        sb.AppendLine("    </div>");
    }

    private void GenerateSummarySection(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        sb.AppendLine("    <h2>Summary</h2>");
        sb.AppendLine($"    <table class=\"{(options.MakeSortable ? "sortable" : "")}\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>Metric</th><th>Count</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");
        sb.AppendLine($"        <tr><td>Total Diagnostics</td><td>{result.Statistics.TotalCount}</td></tr>");
        sb.AppendLine($"        <tr><td>Errors</td><td class=\"severity-error\">{result.Statistics.ErrorCount}</td></tr>");
        sb.AppendLine($"        <tr><td>Warnings</td><td class=\"severity-warning\">{result.Statistics.WarningCount}</td></tr>");
        sb.AppendLine($"        <tr><td>Info/Hidden</td><td>{result.Statistics.InfoCount}</td></tr>");
        sb.AppendLine($"        <tr><td>Projects Analyzed</td><td>{result.Statistics.ProjectsAnalyzed}</td></tr>");
        sb.AppendLine($"        <tr><td>Files Analyzed</td><td>{result.Statistics.FilesAnalyzed}</td></tr>");
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
    }

    private void GenerateChartsSection(StringBuilder sb, AnalysisResult result)
    {
        // Note: This would require Chart.js or similar library
        // For now, we'll skip actual chart generation to keep dependencies minimal
        // Users can add Chart.js CDN and implement charts if needed
    }

    private void GenerateDiagnosticIdSection(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        if (result.Statistics.CountByDiagnosticId.Count == 0) return;

        sb.AppendLine("    <h2>Diagnostics by ID</h2>");
        sb.AppendLine($"    <table class=\"{(options.MakeSortable ? "sortable" : "")}\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>Diagnostic ID</th><th>Count</th><th>Percentage</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        foreach (var kvp in result.Statistics.CountByDiagnosticId.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            sb.AppendLine($"        <tr><td>{Escape(kvp.Key)}</td><td>{kvp.Value}</td><td>{percentage:F1}%</td></tr>");
        }

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
    }

    private void GenerateProjectSection(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        if (result.Statistics.CountByProject.Count == 0) return;

        sb.AppendLine("    <h2>Diagnostics by Project</h2>");
        sb.AppendLine($"    <table class=\"{(options.MakeSortable ? "sortable" : "")}\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>Project</th><th>Count</th><th>Percentage</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        foreach (var kvp in result.Statistics.CountByProject.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            sb.AppendLine($"        <tr><td>{Escape(kvp.Key)}</td><td>{kvp.Value}</td><td>{percentage:F1}%</td></tr>");
        }

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
    }

    private void GenerateSeveritySection(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        if (result.Statistics.CountBySeverity.Count == 0) return;

        sb.AppendLine("    <h2>Diagnostics by Severity</h2>");
        sb.AppendLine($"    <table class=\"{(options.MakeSortable ? "sortable" : "")}\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>Severity</th><th>Count</th><th>Percentage</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        foreach (var kvp in result.Statistics.CountBySeverity.OrderByDescending(x => x.Value))
        {
            var percentage = result.Statistics.GetPercentage(kvp.Value);
            var severityClass = GetSeverityClass(kvp.Key);
            sb.AppendLine($"        <tr><td class=\"{severityClass}\">{Escape(kvp.Key)}</td><td>{kvp.Value}</td><td>{percentage:F1}%</td></tr>");
        }

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
    }

    private void GenerateTopFilesSection(StringBuilder sb, AnalysisResult result, ReportOptions options)
    {
        if (result.Statistics.CountByFile.Count == 0) return;

        sb.AppendLine("    <h2>Top 20 Files with Most Diagnostics</h2>");
        sb.AppendLine($"    <table class=\"{(options.MakeSortable ? "sortable" : "")}\">");
        sb.AppendLine("      <thead>");
        sb.AppendLine("        <tr><th>File</th><th>Count</th></tr>");
        sb.AppendLine("      </thead>");
        sb.AppendLine("      <tbody>");

        var baseDir = result.GetBaseDirectory();
        foreach (var kvp in result.Statistics.CountByFile.OrderByDescending(x => x.Value).Take(20))
        {
            var relativePath = GetRelativePath(kvp.Key, baseDir);
            sb.AppendLine($"        <tr><td><code>{Escape(relativePath)}</code></td><td>{kvp.Value}</td></tr>");
        }

        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");
    }

    private void GenerateDetailedDiagnosticsSection(StringBuilder sb, AnalysisResult result, ReportOptions options, int? maxCount = null)
    {
        if (result.AllDiagnostics.Count == 0) return;

        if (maxCount == null)
        {
            sb.AppendLine("    <h2>Detailed Diagnostics</h2>");
        }

        var diagnostics = maxCount.HasValue
            ? result.AllDiagnostics.Take(maxCount.Value)
            : result.AllDiagnostics;

        var baseDir = result.GetBaseDirectory();
        var groupedByFile = diagnostics.GroupBy(d => d.FilePath).OrderBy(g => g.Key);

        foreach (var fileGroup in groupedByFile)
        {
            var relativePath = GetRelativePath(fileGroup.Key, baseDir);
            sb.AppendLine($"    <h3>{Escape(relativePath)}</h3>");

            foreach (var diagnostic in fileGroup.OrderBy(d => d.Line))
            {
                var severityClass = GetSeverityClass(diagnostic.Severity);
                var cardClass = $"diagnostic-card diagnostic-{diagnostic.Severity.ToLower()}";

                sb.AppendLine($"    <div class=\"{cardClass}\">");
                sb.AppendLine($"      <h4>{diagnostic.Id}: {Escape(diagnostic.Title)}</h4>");
                sb.AppendLine($"      <div class=\"diagnostic-meta\">");
                sb.AppendLine($"        <span class=\"{severityClass}\">Severity: {diagnostic.Severity}</span> | ");
                sb.AppendLine($"        Location: Line {diagnostic.Line}, Column {diagnostic.Column} | ");
                sb.AppendLine($"        Project: {Escape(diagnostic.ProjectName)}");
                sb.AppendLine($"      </div>");
                sb.AppendLine($"      <p><strong>Message:</strong> {Escape(diagnostic.Message)}</p>");

                if (options.IncludeCodeSnippets && !string.IsNullOrEmpty(diagnostic.CodeSnippet))
                {
                    sb.AppendLine("      <div class=\"code-snippet\">");
                    sb.AppendLine($"<pre>{Escape(diagnostic.CodeSnippet)}</pre>");
                    sb.AppendLine("      </div>");
                }

                if (!string.IsNullOrEmpty(diagnostic.HelpLink))
                {
                    sb.AppendLine($"      <p><a href=\"{diagnostic.HelpLink}\" target=\"_blank\">More information →</a></p>");
                }

                sb.AppendLine("    </div>");
            }
        }
    }

    private void GenerateMessagesSection(StringBuilder sb, AnalysisResult result)
    {
        sb.AppendLine("    <h2>Analysis Messages</h2>");
        sb.AppendLine("    <div class=\"messages\">");
        sb.AppendLine("      <ul>");

        foreach (var message in result.AnalysisMessages)
        {
            sb.AppendLine($"        <li>{Escape(message)}</li>");
        }

        sb.AppendLine("      </ul>");
        sb.AppendLine("    </div>");
    }

    private void GenerateFooter(StringBuilder sb)
    {
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"      <p>Generated by ThrowsAnalyzer CLI on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("      <p><a href=\"https://github.com/wieslawsoltes/ThrowsAnalyzer\" target=\"_blank\">ThrowsAnalyzer on GitHub</a></p>");
        sb.AppendLine("    </div>");
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

    private static string GetSeverityClass(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "error" => "severity-error",
            "warning" => "severity-warning",
            "info" => "severity-info",
            "hidden" => "severity-info",
            _ => ""
        };
    }

    private static string Escape(string text)
    {
        return HttpUtility.HtmlEncode(text);
    }
}
