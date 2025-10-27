namespace ThrowsAnalyzer.Cli.Models;

/// <summary>
/// Detailed information about a single diagnostic.
/// </summary>
public class DiagnosticInfo
{
    /// <summary>
    /// Diagnostic ID (e.g., "THROWS004").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Diagnostic title/category.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Severity level (Error, Warning, Info, Hidden).
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Full diagnostic message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// File path where diagnostic was found.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Project name containing this diagnostic.
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Source code snippet around the diagnostic location.
    /// </summary>
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// Help link URL for more information.
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Relative file path (relative to solution/project root).
    /// </summary>
    public string GetRelativePath(string basePath)
    {
        var fileUri = new Uri(FilePath);
        var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);

        if (baseUri.IsBaseOf(fileUri))
        {
            return baseUri.MakeRelativeUri(fileUri).ToString();
        }

        return FilePath;
    }
}
