using System.CommandLine;
using System.CommandLine.Invocation;
using ThrowsAnalyzer.Cli.Analysis;
using ThrowsAnalyzer.Cli.Models;
using ThrowsAnalyzer.Cli.Reports;

namespace ThrowsAnalyzer.Cli.Commands;

/// <summary>
/// CLI command to analyze a solution or project.
/// </summary>
public static class AnalyzeCommand
{
    public static Command Create()
    {
        var command = new Command("analyze", "Analyze a solution, project, or directory for ThrowsAnalyzer diagnostics");

        // Required arguments
        var pathArgument = new Argument<string>(
            name: "path",
            description: "Path to solution (.sln), project (.csproj), or directory to analyze");

        // Options
        var configOption = new Option<string>(
            name: "--configuration",
            description: "Build configuration to use (Debug, Release, etc.)",
            getDefaultValue: () => "Debug");
        configOption.AddAlias("-c");

        var severityOption = new Option<string>(
            name: "--min-severity",
            description: "Minimum severity level to report (Error, Warning, Info, Hidden)",
            getDefaultValue: () => "Info");
        severityOption.AddAlias("-s");

        var diagnosticIdsOption = new Option<string[]>(
            name: "--diagnostics",
            description: "Filter to specific diagnostic IDs (e.g., THROWS004 THROWS021)");
        diagnosticIdsOption.AddAlias("-d");

        var projectsOption = new Option<string[]>(
            name: "--projects",
            description: "Filter to specific project names");
        projectsOption.AddAlias("-p");

        var excludeOption = new Option<string[]>(
            name: "--exclude",
            description: "File patterns to exclude (e.g., **/obj/** **/bin/**)");
        excludeOption.AddAlias("-e");

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Show verbose output");
        verboseOption.AddAlias("-v");

        var skipRestoreOption = new Option<bool>(
            name: "--skip-restore",
            description: "Skip NuGet restore before analysis");

        var maxDiagnosticsOption = new Option<int>(
            name: "--max-diagnostics",
            description: "Maximum number of diagnostics to collect (0 = unlimited)",
            getDefaultValue: () => 0);

        var noCodeSnippetsOption = new Option<bool>(
            name: "--no-code-snippets",
            description: "Exclude code snippets from results");

        var snippetLinesOption = new Option<int>(
            name: "--snippet-lines",
            description: "Number of context lines in code snippets",
            getDefaultValue: () => 3);

        // Report options
        var outputDirOption = new Option<string>(
            name: "--output",
            description: "Output directory for reports",
            getDefaultValue: () => "./reports");
        outputDirOption.AddAlias("-o");

        var formatOption = new Option<string>(
            name: "--format",
            description: "Report format (html, markdown, both)",
            getDefaultValue: () => "both");
        formatOption.AddAlias("-f");

        var fileNameOption = new Option<string>(
            name: "--filename",
            description: "Base file name for reports",
            getDefaultValue: () => "analysis-report");

        var openOption = new Option<bool>(
            name: "--open",
            description: "Open the report after generation");

        var noChartsOption = new Option<bool>(
            name: "--no-charts",
            description: "Exclude charts from HTML report");

        var noSortableOption = new Option<bool>(
            name: "--no-sortable",
            description: "Make HTML tables non-sortable");

        var maxDetailedOption = new Option<int>(
            name: "--max-detailed",
            description: "Maximum detailed diagnostics in report (0 = all)",
            getDefaultValue: () => 0);

        // Add all options
        command.AddArgument(pathArgument);
        command.AddOption(configOption);
        command.AddOption(severityOption);
        command.AddOption(diagnosticIdsOption);
        command.AddOption(projectsOption);
        command.AddOption(excludeOption);
        command.AddOption(verboseOption);
        command.AddOption(skipRestoreOption);
        command.AddOption(maxDiagnosticsOption);
        command.AddOption(noCodeSnippetsOption);
        command.AddOption(snippetLinesOption);
        command.AddOption(outputDirOption);
        command.AddOption(formatOption);
        command.AddOption(fileNameOption);
        command.AddOption(openOption);
        command.AddOption(noChartsOption);
        command.AddOption(noSortableOption);
        command.AddOption(maxDetailedOption);

        command.SetHandler(async (context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            var config = context.ParseResult.GetValueForOption(configOption)!;
            var minSeverity = context.ParseResult.GetValueForOption(severityOption)!;
            var diagnosticIds = context.ParseResult.GetValueForOption(diagnosticIdsOption) ?? Array.Empty<string>();
            var projects = context.ParseResult.GetValueForOption(projectsOption) ?? Array.Empty<string>();
            var exclude = context.ParseResult.GetValueForOption(excludeOption) ?? Array.Empty<string>();
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var skipRestore = context.ParseResult.GetValueForOption(skipRestoreOption);
            var maxDiagnostics = context.ParseResult.GetValueForOption(maxDiagnosticsOption);
            var noCodeSnippets = context.ParseResult.GetValueForOption(noCodeSnippetsOption);
            var snippetLines = context.ParseResult.GetValueForOption(snippetLinesOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption)!;
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var fileName = context.ParseResult.GetValueForOption(fileNameOption)!;
            var open = context.ParseResult.GetValueForOption(openOption);
            var noCharts = context.ParseResult.GetValueForOption(noChartsOption);
            var noSortable = context.ParseResult.GetValueForOption(noSortableOption);
            var maxDetailed = context.ParseResult.GetValueForOption(maxDetailedOption);

            await ExecuteAnalyzeAsync(
                path, config, minSeverity, diagnosticIds, projects, exclude,
                verbose, skipRestore, maxDiagnostics, !noCodeSnippets, snippetLines,
                outputDir, format, fileName, open, !noCharts, !noSortable, maxDetailed,
                context.GetCancellationToken());

            context.ExitCode = 0;
        });

        return command;
    }

    private static async Task ExecuteAnalyzeAsync(
        string path,
        string configuration,
        string minSeverity,
        string[] diagnosticIds,
        string[] projects,
        string[] exclude,
        bool verbose,
        bool skipRestore,
        int maxDiagnostics,
        bool includeCodeSnippets,
        int snippetLines,
        string outputDir,
        string format,
        string fileName,
        bool open,
        bool includeCharts,
        bool makeSortable,
        int maxDetailed,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("ThrowsAnalyzer CLI");
        Console.WriteLine("==================");
        Console.WriteLine();

        // Create analysis options
        var analysisOptions = new AnalysisOptions
        {
            TargetPath = path,
            Configuration = configuration,
            MinimumSeverity = minSeverity,
            DiagnosticIds = new List<string>(diagnosticIds),
            ProjectNames = new List<string>(projects),
            ExcludePatterns = new List<string>(exclude),
            Verbose = verbose,
            SkipRestore = skipRestore,
            MaxDiagnostics = maxDiagnostics,
            IncludeCodeSnippets = includeCodeSnippets,
            CodeSnippetLines = snippetLines
        };

        // Run analysis
        Console.WriteLine($"Analyzing: {path}");
        Console.WriteLine($"Configuration: {configuration}");
        Console.WriteLine();

        var analyzer = new WorkspaceAnalyzer(analysisOptions);
        var result = await analyzer.AnalyzeAsync(cancellationToken);

        // Display summary
        Console.WriteLine();
        Console.WriteLine("Analysis Complete!");
        Console.WriteLine("==================");
        Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"Status: {(result.Success ? "Success" : "Failed")}");
        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Total Diagnostics: {result.Statistics.TotalCount}");
        Console.WriteLine($"  Errors: {result.Statistics.ErrorCount}");
        Console.WriteLine($"  Warnings: {result.Statistics.WarningCount}");
        Console.WriteLine($"  Info/Hidden: {result.Statistics.InfoCount}");
        Console.WriteLine($"  Projects: {result.Statistics.ProjectsAnalyzed}");
        Console.WriteLine($"  Files: {result.Statistics.FilesAnalyzed}");
        Console.WriteLine();

        if (!result.Success)
        {
            Console.WriteLine("Analysis Messages:");
            foreach (var message in result.AnalysisMessages)
            {
                Console.WriteLine($"  - {message}");
            }
            Console.WriteLine();
        }

        // Generate reports
        var reportOptions = new ReportOptions
        {
            OutputDirectory = outputDir,
            Format = ParseReportFormat(format),
            BaseFileName = fileName,
            OpenAfterGeneration = open,
            IncludeCharts = includeCharts,
            MakeSortable = makeSortable,
            IncludeCodeSnippets = includeCodeSnippets,
            MaxDetailedDiagnostics = maxDetailed
        };

        Console.WriteLine("Generating reports...");

        var reportPaths = new List<string>();

        if (reportOptions.Format == ReportFormat.Html || reportOptions.Format == ReportFormat.Both)
        {
            var htmlGenerator = new HtmlReportGenerator();
            var htmlPath = await htmlGenerator.GenerateReportAsync(result, reportOptions, cancellationToken);
            reportPaths.Add(htmlPath);
            Console.WriteLine($"  HTML report: {htmlPath}");
        }

        if (reportOptions.Format == ReportFormat.Markdown || reportOptions.Format == ReportFormat.Both)
        {
            var mdGenerator = new MarkdownReportGenerator();
            var mdPath = await mdGenerator.GenerateReportAsync(result, reportOptions, cancellationToken);
            reportPaths.Add(mdPath);
            Console.WriteLine($"  Markdown report: {mdPath}");
        }

        Console.WriteLine();
        Console.WriteLine("Done!");

        // Open reports if requested
        if (open && reportPaths.Count > 0)
        {
            foreach (var reportPath in reportPaths)
            {
                try
                {
                    OpenFile(reportPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open {reportPath}: {ex.Message}");
                }
            }
        }
    }

    private static ReportFormat ParseReportFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "html" => ReportFormat.Html,
            "markdown" or "md" => ReportFormat.Markdown,
            "both" => ReportFormat.Both,
            _ => ReportFormat.Both
        };
    }

    private static void OpenFile(string filePath)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(processStartInfo);
    }
}
