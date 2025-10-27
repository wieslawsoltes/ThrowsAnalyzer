# ThrowsAnalyzer CLI Tool - Design Document

## Overview

Command-line tool to run ThrowsAnalyzer on projects/solutions and generate comprehensive reports.

## Features

### Core Capabilities
1. **Run analyzers** on project/solution using Roslyn Workspaces
2. **Gather diagnostics** from all analyzers
3. **Generate reports** in HTML and Markdown formats
4. **Statistics** aggregation by diagnostic, severity, project, file
5. **Export** raw diagnostic data

### Report Types

#### HTML Report
- Interactive, styled report with navigation
- Sortable/filterable tables
- Visual charts/graphs
- Expandable details
- Copy-to-clipboard functionality

#### Markdown Report
- Plain text, version-control friendly
- GitHub-flavored markdown tables
- Compatible with documentation sites
- Easily convertible to other formats

## Architecture

```
ThrowsAnalyzer.Cli/
├── Program.cs                   # Entry point, CLI parsing
├── Commands/
│   ├── AnalyzeCommand.cs        # Main analyze command
│   └── ReportCommand.cs         # Report generation command
├── Analysis/
│   ├── WorkspaceAnalyzer.cs     # Roslyn workspace loading & analysis
│   ├── DiagnosticCollector.cs   # Collect diagnostics from analyzers
│   └── DiagnosticAggregator.cs  # Aggregate statistics
├── Reporting/
│   ├── IReportGenerator.cs      # Report generator interface
│   ├── HtmlReportGenerator.cs   # HTML report implementation
│   ├── MarkdownReportGenerator.cs # Markdown report implementation
│   └── ReportData.cs            # Report data models
└── Models/
    ├── AnalysisResult.cs        # Analysis results
    ├── DiagnosticInfo.cs        # Diagnostic information
    └── ProjectStatistics.cs     # Statistics models
```

## Command-Line Interface

### Commands

#### `analyze`
Run analysis and generate report in one step.

```bash
throws-analyzer analyze <path> [options]
```

**Arguments**:
- `<path>` - Path to .csproj, .sln, or directory

**Options**:
- `--format, -f <html|markdown|both>` - Report format (default: both)
- `--output, -o <path>` - Output directory (default: ./reports)
- `--severity, -s <level>` - Minimum severity to report (default: suggestion)
- `--project, -p <name>` - Filter specific project
- `--diagnostic, -d <id>` - Filter specific diagnostic IDs
- `--exclude, -e <pattern>` - Exclude files by glob pattern
- `--verbose, -v` - Verbose output
- `--no-restore` - Skip NuGet restore
- `--configuration, -c <config>` - Build configuration (default: Debug)

#### Examples

```bash
# Analyze solution, generate both reports
throws-analyzer analyze MySolution.sln

# Analyze project, HTML only
throws-analyzer analyze MyProject.csproj -f html

# Analyze with filters
throws-analyzer analyze . -s warning -d THROWS004,THROWS021

# Analyze Release configuration
throws-analyzer analyze MySolution.sln -c Release

# Custom output location
throws-analyzer analyze . -o ./analysis-results
```

## Report Structure

### HTML Report

```html
<!DOCTYPE html>
<html>
<head>
    <title>ThrowsAnalyzer Report - [Solution Name]</title>
    <style>/* Embedded CSS */</style>
</head>
<body>
    <!-- Summary Section -->
    <section id="summary">
        <h1>ThrowsAnalyzer Report</h1>
        <div class="stats-grid">
            <div class="stat-card">Total Diagnostics: 127</div>
            <div class="stat-card error">Errors: 3</div>
            <div class="stat-card warning">Warnings: 45</div>
            <div class="stat-card suggestion">Suggestions: 79</div>
        </div>
        <div class="charts">
            <!-- SVG/Chart.js charts -->
        </div>
    </section>

    <!-- Diagnostics by Type -->
    <section id="by-diagnostic">
        <h2>Diagnostics by Type</h2>
        <table class="sortable">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Title</th>
                    <th>Severity</th>
                    <th>Count</th>
                    <th>Files</th>
                </tr>
            </thead>
            <tbody>
                <tr class="error">
                    <td>THROWS004</td>
                    <td>Rethrow anti-pattern</td>
                    <td><span class="badge error">Error</span></td>
                    <td>3</td>
                    <td>2</td>
                </tr>
                <!-- More rows -->
            </tbody>
        </table>
    </section>

    <!-- Diagnostics by Project -->
    <section id="by-project">
        <h2>Diagnostics by Project</h2>
        <!-- Table -->
    </section>

    <!-- Diagnostics by File -->
    <section id="by-file">
        <h2>Diagnostics by File</h2>
        <!-- Table with file paths, line numbers -->
    </section>

    <!-- All Diagnostics -->
    <section id="all-diagnostics">
        <h2>All Diagnostics</h2>
        <table class="detailed">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Severity</th>
                    <th>File</th>
                    <th>Line</th>
                    <th>Column</th>
                    <th>Message</th>
                </tr>
            </thead>
            <tbody>
                <!-- All diagnostics in detail -->
            </tbody>
        </table>
    </section>

    <script>/* JavaScript for sorting, filtering */</script>
</body>
</html>
```

### Markdown Report

```markdown
# ThrowsAnalyzer Report

**Generated**: 2025-10-27 13:00:00
**Solution**: MySolution.sln
**Projects**: 5
**Files Analyzed**: 234

## Summary

| Severity | Count | Percentage |
|----------|-------|------------|
| Error | 3 | 2.4% |
| Warning | 45 | 35.4% |
| Suggestion | 79 | 62.2% |
| **Total** | **127** | **100%** |

## Diagnostics by Type

| ID | Title | Severity | Count | Files |
|----|-------|----------|-------|-------|
| THROWS004 | Rethrow anti-pattern | Error | 3 | 2 |
| THROWS021 | Async void exception | Error | 0 | 0 |
| THROWS002 | Unhandled throw | Warning | 23 | 15 |
| ... | ... | ... | ... | ... |

## Diagnostics by Project

| Project | Errors | Warnings | Suggestions | Total |
|---------|--------|----------|-------------|-------|
| MyProject.Core | 2 | 12 | 34 | 48 |
| MyProject.Web | 1 | 18 | 28 | 47 |
| ... | ... | ... | ... | ... |

## Diagnostics by File

| File | Diagnostics | Errors | Warnings | Suggestions |
|------|-------------|--------|----------|-------------|
| src/Program.cs | 5 | 1 | 2 | 2 |
| src/Service.cs | 8 | 0 | 4 | 4 |
| ... | ... | ... | ... | ... |

## All Diagnostics

### THROWS004: Rethrow anti-pattern

#### src/MyClass.cs:45

**Severity**: Error
**Message**: Rethrow anti-pattern detected - use 'throw;' instead of 'throw ex;'

```csharp
catch (Exception ex)
{
    throw ex; // Line 45
}
```

---

<!-- More diagnostics -->
```

## Data Models

```csharp
public class AnalysisResult
{
    public string SolutionPath { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<ProjectAnalysis> Projects { get; set; }
    public DiagnosticStatistics Statistics { get; set; }
    public List<DiagnosticInfo> AllDiagnostics { get; set; }
}

public class ProjectAnalysis
{
    public string ProjectName { get; set; }
    public string ProjectPath { get; set; }
    public List<DiagnosticInfo> Diagnostics { get; set; }
    public DiagnosticStatistics Statistics { get; set; }
}

public class DiagnosticInfo
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Severity { get; set; }
    public string Message { get; set; }
    public string FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string ProjectName { get; set; }
    public string Code { get; set; } // Source code snippet
}

public class DiagnosticStatistics
{
    public int TotalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int SuggestionCount { get; set; }
    public Dictionary<string, int> CountByDiagnosticId { get; set; }
    public Dictionary<string, int> CountByProject { get; set; }
    public Dictionary<string, int> CountByFile { get; set; }
}
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create CLI project
2. Implement workspace loading
3. Basic diagnostic collection

### Phase 2: Analysis Engine
1. Run analyzers on loaded workspace
2. Collect all diagnostics
3. Aggregate statistics

### Phase 3: Report Generation
1. Markdown report generator
2. HTML report generator
3. Report data serialization

### Phase 4: CLI Experience
1. Argument parsing (System.CommandLine)
2. Progress reporting
3. Error handling
4. Verbose logging

### Phase 5: Polish
1. HTML styling and JavaScript
2. Documentation
3. Testing with real projects
4. Performance optimization

## Technologies

- **Roslyn Workspaces**: Load and analyze solutions/projects
- **Microsoft.CodeAnalysis.Analyzers**: Run ThrowsAnalyzer
- **System.CommandLine**: CLI argument parsing
- **Markdig**: Markdown generation (optional)
- **Razor**: HTML templating (optional)
- **Chart.js**: HTML report charts (embedded)

## Performance Considerations

- **Parallel analysis**: Analyze projects in parallel
- **Incremental compilation**: Use Roslyn's incremental compilation
- **Memory management**: Stream large reports
- **Caching**: Cache workspace compilation

## Error Handling

- Graceful failure if project doesn't compile
- Report partial results
- Clear error messages for missing dependencies
- Validation of input paths

## Future Enhancements

1. **JSON export** for CI/CD integration
2. **Diff reports** - compare two analysis runs
3. **Trend analysis** - track diagnostics over time
4. **Integration with code coverage** tools
5. **Custom report templates**
6. **Multiple analyzer support** (not just ThrowsAnalyzer)
7. **Team dashboards** - aggregate across repositories
8. **Code fix suggestions** in reports

## Example Output

### Terminal Output

```
ThrowsAnalyzer CLI v1.0.0

Loading workspace...
✓ Loaded solution: MySolution.sln (5 projects)

Analyzing projects...
✓ [1/5] MyProject.Core (45 files) - 48 diagnostics
✓ [2/5] MyProject.Web (82 files) - 47 diagnostics
✓ [3/5] MyProject.Tests (63 files) - 32 diagnostics
✓ [4/5] MyProject.Api (34 files) - 0 diagnostics
✓ [5/5] MyProject.Shared (10 files) - 0 diagnostics

Analysis complete! Found 127 diagnostics.

Errors:     3  (2.4%)
Warnings:   45 (35.4%)
Suggestions: 79 (62.2%)

Generating reports...
✓ HTML report: ./reports/analysis-report.html
✓ Markdown report: ./reports/analysis-report.md

Done in 12.3s!
```

## Integration Examples

### GitHub Actions

```yaml
- name: Run ThrowsAnalyzer Report
  run: |
    dotnet tool install --global ThrowsAnalyzer.Cli
    throws-analyzer analyze . -f markdown -o ./reports

- name: Upload Report
  uses: actions/upload-artifact@v3
  with:
    name: analyzer-report
    path: ./reports/*.md
```

### CI/CD Pipeline

```bash
#!/bin/bash
throws-analyzer analyze MySolution.sln -f html -s error
if [ $? -ne 0 ]; then
    echo "Critical errors found!"
    exit 1
fi
```

---

This CLI tool will provide comprehensive, actionable reports for ThrowsAnalyzer diagnostics! 🚀
