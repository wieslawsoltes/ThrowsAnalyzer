# ThrowsAnalyzer CLI Tool

Command-line tool for analyzing C# projects and solutions for exception handling diagnostics using ThrowsAnalyzer.

## Installation

```bash
dotnet tool install --global ThrowsAnalyzer.Cli
```

## Quick Start

```bash
# Analyze a project
throws-analyzer analyze MyProject.csproj

# Analyze a solution
throws-analyzer analyze MySolution.sln

# Generate reports with verbose output
throws-analyzer analyze MyProject.csproj --verbose --open
```

## Features

- **Comprehensive Analysis**: Runs all 30 ThrowsAnalyzer diagnostics on your codebase
- **Dual Report Format**: Generates both HTML and Markdown reports
- **Interactive HTML Reports**: Sortable tables, color-coded severity, code snippets
- **Markdown Reports**: GitHub-compatible documentation format
- **Statistics Dashboard**: Diagnostics by ID, project, severity, and file
- **Flexible Filtering**: Filter by diagnostic IDs, projects, severity levels
- **CI/CD Ready**: Exit codes and report formats suitable for automation

## Usage

```bash
throws-analyzer analyze <path> [options]
```

### Common Options

| Option | Description | Default |
|--------|-------------|---------|
| `-c, --configuration` | Build configuration (Debug/Release) | `Debug` |
| `-s, --min-severity` | Minimum severity (Error/Warning/Info) | `Info` |
| `-o, --output` | Output directory for reports | `./reports` |
| `-f, --format` | Report format (html/markdown/both) | `both` |
| `-v, --verbose` | Show verbose output | `false` |
| `--open` | Open report after generation | `false` |

### Examples

```bash
# Analyze with Release configuration
throws-analyzer analyze MySolution.sln -c Release

# Only show warnings and errors
throws-analyzer analyze MyProject.csproj -s Warning

# Custom output directory
throws-analyzer analyze MySolution.sln -o ./build/reports

# HTML report only
throws-analyzer analyze MyProject.csproj -f html --open

# Filter specific diagnostics
throws-analyzer analyze MySolution.sln -d THROWS004 THROWS021 THROWS026

# Analyze specific projects
throws-analyzer analyze MySolution.sln -p MyProject.Core MyProject.Api
```

## Report Output

The tool generates comprehensive reports in `./reports/` by default:

- **`analysis-report.html`** - Interactive HTML report with sortable tables
- **`analysis-report.md`** - Markdown report for documentation

### Report Contents

- Summary statistics (total diagnostics, by severity)
- Diagnostics breakdown by ID, project, file
- Top files with most diagnostics
- Detailed diagnostics with code snippets
- Color-coded severity indicators

## CI/CD Integration

### GitHub Actions

```yaml
- name: Install ThrowsAnalyzer CLI
  run: dotnet tool install --global ThrowsAnalyzer.Cli

- name: Run Analysis
  run: throws-analyzer analyze MySolution.sln -c Release -o ./reports

- name: Upload Reports
  uses: actions/upload-artifact@v3
  with:
    name: throws-analyzer-reports
    path: ./reports/
```

### Exit Codes

- `0` - Analysis completed successfully
- `1` - Analysis failed
- `2` - Invalid arguments

## Documentation

For complete documentation, see:
- [CLI Tool Guide](https://github.com/wieslawsoltes/ThrowsAnalyzer/blob/main/docs/guides/CLI_TOOL.md)
- [Configuration Guide](https://github.com/wieslawsoltes/ThrowsAnalyzer/blob/main/docs/CONFIGURATION_GUIDE.md)
- [Main Repository](https://github.com/wieslawsoltes/ThrowsAnalyzer)

## ThrowsAnalyzer

The CLI tool uses [ThrowsAnalyzer](https://www.nuget.org/packages/ThrowsAnalyzer), a Roslyn analyzer with **30 diagnostic rules** for exception handling:

- Basic Exception Handling (8 rules)
- Exception Flow Analysis (3 rules)
- Async Exception Patterns (3 rules)
- Iterator Exception Patterns (2 rules)
- Lambda Exception Patterns (2 rules)
- Best Practices (4 rules)

Install the analyzer directly in your projects:

```bash
dotnet add package ThrowsAnalyzer
```

## License

MIT License - Copyright © 2025 Wiesław Šoltés

## Support

- GitHub Issues: https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- Documentation: https://github.com/wieslawsoltes/ThrowsAnalyzer/tree/main/docs
