using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using ThrowsAnalyzer.Cli.Models;

namespace ThrowsAnalyzer.Cli.Analysis;

/// <summary>
/// Collects diagnostics from Roslyn compilation.
/// </summary>
public class DiagnosticCollector
{
    private readonly AnalysisOptions _options;

    public DiagnosticCollector(AnalysisOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Collect all ThrowsAnalyzer diagnostics from the compilation.
    /// </summary>
    public async Task<List<DiagnosticInfo>> CollectDiagnosticsAsync(
        Compilation compilation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DiagnosticInfo>();

        // Get all analyzers from the project
        var analyzers = project.AnalyzerReferences
            .SelectMany(r => r.GetAnalyzers(project.Language))
            .Where(a => a.GetType().Assembly.GetName().Name?.Contains("ThrowsAnalyzer") == true)
            .ToImmutableArray();

        if (analyzers.IsEmpty)
        {
            // No ThrowsAnalyzer found, return empty list
            return result;
        }

        // Create compilation with analyzers
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            analyzers,
            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty)
        );

        // Get all diagnostics
        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken);

        // Filter and convert diagnostics
        foreach (var diagnostic in diagnostics)
        {
            if (ShouldIncludeDiagnostic(diagnostic))
            {
                var diagnosticInfo = await ConvertDiagnosticAsync(diagnostic, project, cancellationToken);
                if (diagnosticInfo != null)
                {
                    result.Add(diagnosticInfo);

                    // Check if we've hit the max diagnostics limit
                    if (_options.MaxDiagnostics > 0 && result.Count >= _options.MaxDiagnostics)
                    {
                        break;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get diagnostic options based on analysis options.
    /// </summary>
    private Dictionary<string, ReportDiagnostic> GetDiagnosticOptions()
    {
        var options = new Dictionary<string, ReportDiagnostic>();

        // If specific diagnostic IDs are requested, enable only those
        if (_options.DiagnosticIds.Count > 0)
        {
            foreach (var id in _options.DiagnosticIds)
            {
                options[id] = ReportDiagnostic.Warn; // Report as warnings to ensure they show up
            }
        }

        return options;
    }

    /// <summary>
    /// Determine if a diagnostic should be included in results.
    /// </summary>
    private bool ShouldIncludeDiagnostic(Diagnostic diagnostic)
    {
        // Only include ThrowsAnalyzer diagnostics
        if (!diagnostic.Id.StartsWith("THROWS", StringComparison.Ordinal))
        {
            return false;
        }

        // Filter by diagnostic IDs if specified
        if (_options.DiagnosticIds.Count > 0 &&
            !_options.DiagnosticIds.Contains(diagnostic.Id, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Filter by severity
        if (!MeetsSeverityThreshold(diagnostic.Severity))
        {
            return false;
        }

        // Filter by file patterns
        if (diagnostic.Location.IsInSource && _options.ExcludePatterns.Count > 0)
        {
            var filePath = diagnostic.Location.SourceTree?.FilePath ?? string.Empty;
            if (IsExcluded(filePath))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if diagnostic severity meets minimum threshold.
    /// </summary>
    private bool MeetsSeverityThreshold(DiagnosticSeverity severity)
    {
        var minimumSeverity = ParseSeverity(_options.MinimumSeverity);
        return severity >= minimumSeverity;
    }

    /// <summary>
    /// Parse severity string to DiagnosticSeverity enum.
    /// </summary>
    private DiagnosticSeverity ParseSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "error" => DiagnosticSeverity.Error,
            "warning" => DiagnosticSeverity.Warning,
            "info" => DiagnosticSeverity.Info,
            "hidden" => DiagnosticSeverity.Hidden,
            _ => DiagnosticSeverity.Info
        };
    }

    /// <summary>
    /// Check if file path matches any exclude patterns.
    /// </summary>
    private bool IsExcluded(string filePath)
    {
        foreach (var pattern in _options.ExcludePatterns)
        {
            // Simple glob matching (can be enhanced with proper glob library)
            if (filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Convert Roslyn Diagnostic to DiagnosticInfo.
    /// </summary>
    private async Task<DiagnosticInfo?> ConvertDiagnosticAsync(
        Diagnostic diagnostic,
        Project project,
        CancellationToken cancellationToken)
    {
        if (!diagnostic.Location.IsInSource)
        {
            return null;
        }

        var location = diagnostic.Location;
        var lineSpan = location.GetLineSpan();
        var filePath = lineSpan.Path;

        // Get code snippet if requested
        string? codeSnippet = null;
        if (_options.IncludeCodeSnippets && location.SourceTree != null)
        {
            codeSnippet = await GetCodeSnippetAsync(
                location.SourceTree,
                lineSpan.StartLinePosition.Line,
                _options.CodeSnippetLines,
                cancellationToken
            );
        }

        return new DiagnosticInfo
        {
            Id = diagnostic.Id,
            Title = diagnostic.Descriptor.Title.ToString(),
            Severity = diagnostic.Severity.ToString(),
            Message = diagnostic.GetMessage(),
            FilePath = filePath,
            Line = lineSpan.StartLinePosition.Line + 1, // Convert to 1-based
            Column = lineSpan.StartLinePosition.Character + 1, // Convert to 1-based
            ProjectName = project.Name,
            CodeSnippet = codeSnippet,
            HelpLink = diagnostic.Descriptor.HelpLinkUri
        };
    }

    /// <summary>
    /// Extract code snippet around the diagnostic location.
    /// </summary>
    private async Task<string> GetCodeSnippetAsync(
        SyntaxTree syntaxTree,
        int lineNumber,
        int contextLines,
        CancellationToken cancellationToken)
    {
        var text = await syntaxTree.GetTextAsync(cancellationToken);
        var lines = text.Lines;

        if (lineNumber < 0 || lineNumber >= lines.Count)
        {
            return string.Empty;
        }

        var startLine = Math.Max(0, lineNumber - contextLines);
        var endLine = Math.Min(lines.Count - 1, lineNumber + contextLines);

        var snippetLines = new List<string>();
        for (int i = startLine; i <= endLine; i++)
        {
            var line = lines[i];
            var lineText = text.ToString(line.Span);
            var marker = i == lineNumber ? ">>> " : "    ";
            snippetLines.Add($"{marker}{i + 1,4}: {lineText}");
        }

        return string.Join(Environment.NewLine, snippetLines);
    }
}
