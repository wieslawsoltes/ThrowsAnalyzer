using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using ThrowsAnalyzer.Cli.Models;

namespace ThrowsAnalyzer.Cli.Analysis;

/// <summary>
/// Analyzes workspaces (solutions/projects) using Roslyn.
/// </summary>
public class WorkspaceAnalyzer
{
    private readonly AnalysisOptions _options;
    private static bool _msbuildLocated;

    public WorkspaceAnalyzer(AnalysisOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        EnsureMSBuildLocated();
    }

    /// <summary>
    /// Ensure MSBuild is located (only once per process).
    /// </summary>
    private static void EnsureMSBuildLocated()
    {
        if (_msbuildLocated) return;

        if (!MSBuildLocator.IsRegistered)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            if (instances.Count > 0)
            {
                // Use the first (usually latest) Visual Studio instance
                MSBuildLocator.RegisterInstance(instances.OrderByDescending(i => i.Version).First());
            }
            else
            {
                // Try to register default instance
                MSBuildLocator.RegisterDefaults();
            }
        }

        _msbuildLocated = true;
    }

    /// <summary>
    /// Analyze a solution, project, or directory.
    /// </summary>
    public async Task<AnalysisResult> AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var result = new AnalysisResult
        {
            TargetPath = Path.GetFullPath(_options.TargetPath),
            TargetType = "Unknown", // Will be updated based on actual target
            Configuration = _options.Configuration
        };

        try
        {
            // Determine target type and load workspace
            if (_options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                result.TargetType = "Solution";
                await AnalyzeSolutionAsync(result, cancellationToken);
            }
            else if (_options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                result.TargetType = "Project";
                await AnalyzeProjectAsync(result, cancellationToken);
            }
            else if (Directory.Exists(_options.TargetPath))
            {
                result.TargetType = "Directory";
                await AnalyzeDirectoryAsync(result, cancellationToken);
            }
            else
            {
                throw new ArgumentException($"Invalid target path: {_options.TargetPath}");
            }

            // Aggregate statistics
            AggregateStatistics(result);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.AnalysisMessages.Add($"Analysis failed: {ex.Message}");

            if (_options.Verbose)
            {
                result.AnalysisMessages.Add($"Stack trace: {ex.StackTrace}");
            }
        }
        finally
        {
            result.Duration = DateTime.Now - startTime;
        }

        return result;
    }

    private async Task AnalyzeSolutionAsync(AnalysisResult result, CancellationToken cancellationToken)
    {
        if (_options.Verbose)
        {
            Console.WriteLine($"Loading solution: {result.TargetPath}");
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (sender, args) =>
        {
            if (_options.Verbose)
            {
                Console.WriteLine($"Workspace warning: {args.Diagnostic.Message}");
            }
        };

        var solution = await workspace.OpenSolutionAsync(result.TargetPath, cancellationToken: cancellationToken);

        if (_options.Verbose)
        {
            Console.WriteLine($"Loaded {solution.Projects.Count()} projects");
        }

        foreach (var project in solution.Projects)
        {
            if (ShouldAnalyzeProject(project))
            {
                await AnalyzeProjectInternalAsync(result, project, cancellationToken);
            }
        }
    }

    private async Task AnalyzeProjectAsync(AnalysisResult result, CancellationToken cancellationToken)
    {
        if (_options.Verbose)
        {
            Console.WriteLine($"Loading project: {result.TargetPath}");
        }

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(result.TargetPath, cancellationToken: cancellationToken);

        await AnalyzeProjectInternalAsync(result, project, cancellationToken);
    }

    private async Task AnalyzeDirectoryAsync(AnalysisResult result, CancellationToken cancellationToken)
    {
        // Find all .sln files in directory
        var solutionFiles = Directory.GetFiles(_options.TargetPath, "*.sln", SearchOption.TopDirectoryOnly);

        if (solutionFiles.Length > 0)
        {
            // Analyze first solution found
            _options.TargetPath = solutionFiles[0];
            result.TargetPath = solutionFiles[0];
            result.TargetType = "Solution";
            await AnalyzeSolutionAsync(result, cancellationToken);
            return;
        }

        // Find all .csproj files
        var projectFiles = Directory.GetFiles(_options.TargetPath, "*.csproj", SearchOption.AllDirectories);

        if (projectFiles.Length == 0)
        {
            throw new ArgumentException($"No solution or project files found in: {_options.TargetPath}");
        }

        // Analyze each project
        using var workspace = MSBuildWorkspace.Create();
        foreach (var projectFile in projectFiles)
        {
            try
            {
                var project = await workspace.OpenProjectAsync(projectFile, cancellationToken: cancellationToken);
                if (ShouldAnalyzeProject(project))
                {
                    await AnalyzeProjectInternalAsync(result, project, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                result.AnalysisMessages.Add($"Failed to load project {projectFile}: {ex.Message}");
            }
        }
    }

    private async Task AnalyzeProjectInternalAsync(AnalysisResult result, Project project, CancellationToken cancellationToken)
    {
        if (_options.Verbose)
        {
            Console.WriteLine($"Analyzing project: {project.Name}");
        }

        var projectAnalysis = new ProjectAnalysis
        {
            ProjectName = project.Name,
            ProjectPath = project.FilePath ?? string.Empty,
            FileCount = project.DocumentIds.Count
        };

        try
        {
            // Get compilation
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
            {
                projectAnalysis.CompiledSuccessfully = false;
                projectAnalysis.CompilationErrors.Add("Failed to get compilation");
                result.Projects.Add(projectAnalysis);
                return;
            }

            // Collect diagnostics using DiagnosticCollector
            var collector = new DiagnosticCollector(_options);
            var diagnostics = await collector.CollectDiagnosticsAsync(compilation, project, cancellationToken);

            projectAnalysis.Diagnostics.AddRange(diagnostics);

            // Calculate statistics for this project
            CalculateProjectStatistics(projectAnalysis);

            if (_options.Verbose)
            {
                Console.WriteLine($"  Found {projectAnalysis.Diagnostics.Count} diagnostics");
            }
        }
        catch (Exception ex)
        {
            projectAnalysis.CompiledSuccessfully = false;
            projectAnalysis.CompilationErrors.Add(ex.Message);

            if (_options.Verbose)
            {
                Console.WriteLine($"  Error analyzing project: {ex.Message}");
            }
        }

        result.Projects.Add(projectAnalysis);
    }

    private bool ShouldAnalyzeProject(Project project)
    {
        // Filter by project name if specified
        if (_options.ProjectNames.Count > 0)
        {
            return _options.ProjectNames.Contains(project.Name, StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }

    private void CalculateProjectStatistics(ProjectAnalysis projectAnalysis)
    {
        var stats = projectAnalysis.Statistics;
        stats.TotalCount = projectAnalysis.Diagnostics.Count;
        stats.ErrorCount = projectAnalysis.Diagnostics.Count(d => d.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        stats.WarningCount = projectAnalysis.Diagnostics.Count(d => d.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
        stats.InfoCount = projectAnalysis.Diagnostics.Count(d => d.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase) || d.Severity.Equals("Hidden", StringComparison.OrdinalIgnoreCase));

        // Count by diagnostic ID
        foreach (var diagnostic in projectAnalysis.Diagnostics)
        {
            stats.CountByDiagnosticId.TryGetValue(diagnostic.Id, out var count);
            stats.CountByDiagnosticId[diagnostic.Id] = count + 1;

            stats.CountByFile.TryGetValue(diagnostic.FilePath, out count);
            stats.CountByFile[diagnostic.FilePath] = count + 1;

            stats.CountBySeverity.TryGetValue(diagnostic.Severity, out count);
            stats.CountBySeverity[diagnostic.Severity] = count + 1;
        }

        stats.FilesAnalyzed = stats.CountByFile.Count;
        stats.ProjectsAnalyzed = 1;
    }

    private void AggregateStatistics(AnalysisResult result)
    {
        result.AllDiagnostics.Clear();

        foreach (var project in result.Projects)
        {
            result.AllDiagnostics.AddRange(project.Diagnostics);
        }

        var stats = result.Statistics;
        stats.TotalCount = result.AllDiagnostics.Count;
        stats.ErrorCount = result.AllDiagnostics.Count(d => d.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        stats.WarningCount = result.AllDiagnostics.Count(d => d.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
        stats.InfoCount = result.AllDiagnostics.Count(d => d.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase) || d.Severity.Equals("Hidden", StringComparison.OrdinalIgnoreCase));

        // Aggregate counts
        foreach (var diagnostic in result.AllDiagnostics)
        {
            stats.CountByDiagnosticId.TryGetValue(diagnostic.Id, out var count);
            stats.CountByDiagnosticId[diagnostic.Id] = count + 1;

            stats.CountByProject.TryGetValue(diagnostic.ProjectName, out count);
            stats.CountByProject[diagnostic.ProjectName] = count + 1;

            stats.CountByFile.TryGetValue(diagnostic.FilePath, out count);
            stats.CountByFile[diagnostic.FilePath] = count + 1;

            stats.CountBySeverity.TryGetValue(diagnostic.Severity, out count);
            stats.CountBySeverity[diagnostic.Severity] = count + 1;
        }

        stats.ProjectsAnalyzed = result.Projects.Count;
        stats.FilesAnalyzed = stats.CountByFile.Count;
    }
}
