# ThrowsAnalyzer CLI Tool

Command-line tool to analyze C# projects and solutions for exception handling diagnostics using ThrowsAnalyzer, and generate comprehensive HTML and Markdown reports.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Options](#options)
- [Examples](#examples)
- [Reports](#reports)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)

## Installation

### Install as Global Tool

```bash
# Install from NuGet (once published)
dotnet tool install --global ThrowsAnalyzer.Cli

# Or install from local build
dotnet pack src/ThrowsAnalyzer.Cli/ThrowsAnalyzer.Cli.csproj
dotnet tool install --global --add-source src/ThrowsAnalyzer.Cli/nupkg ThrowsAnalyzer.Cli
```

### Build from Source

```bash
git clone https://github.com/wieslawsoltes/ThrowsAnalyzer.git
cd ThrowsAnalyzer
dotnet build src/ThrowsAnalyzer.Cli/ThrowsAnalyzer.Cli.csproj
```

### Run without Installation

```bash
dotnet run --project src/ThrowsAnalyzer.Cli/ThrowsAnalyzer.Cli.csproj -- [arguments]
```

## Quick Start

Analyze a project and generate reports:

```bash
# Analyze a project
throws-analyzer analyze MyProject.csproj

# Analyze a solution
throws-analyzer analyze MySolution.sln

# Analyze a directory (finds .sln or .csproj files)
throws-analyzer analyze ./src
```

Reports are generated in `./reports/` directory by default:
- `analysis-report.html` - Interactive HTML report with sortable tables
- `analysis-report.md` - Markdown report for documentation

## Usage

```
throws-analyzer analyze <path> [options]
```

### Arguments

- `<path>` - Path to solution (.sln), project (.csproj), or directory to analyze

## Options

### Analysis Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--configuration <config>` | `-c` | Build configuration (Debug, Release) | `Debug` |
| `--min-severity <level>` | `-s` | Minimum severity (Error, Warning, Info, Hidden) | `Info` |
| `--diagnostics <ids...>` | `-d` | Filter specific diagnostic IDs | All |
| `--projects <names...>` | `-p` | Filter specific projects | All |
| `--exclude <patterns...>` | `-e` | Exclude file patterns | None |
| `--verbose` | `-v` | Show verbose output | false |
| `--skip-restore` | | Skip NuGet restore | false |
| `--max-diagnostics <n>` | | Maximum diagnostics (0 = unlimited) | `0` |
| `--no-code-snippets` | | Exclude code snippets | false |
| `--snippet-lines <n>` | | Context lines in snippets | `3` |

### Report Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--output <dir>` | `-o` | Output directory | `./reports` |
| `--format <format>` | `-f` | Format: html, markdown, both | `both` |
| `--filename <name>` | | Base filename (no extension) | `analysis-report` |
| `--open` | | Open report after generation | false |
| `--no-charts` | | Exclude charts from HTML | false |
| `--no-sortable` | | Disable sortable tables | false |
| `--max-detailed <n>` | | Max detailed diagnostics (0 = all) | `0` |

## Examples

### Basic Analysis

```bash
# Analyze project with defaults
throws-analyzer analyze MyProject.csproj

# Analyze solution in Release configuration
throws-analyzer analyze MySolution.sln -c Release

# Verbose output
throws-analyzer analyze MyProject.csproj --verbose
```

### Filtering Diagnostics

```bash
# Only show errors and warnings
throws-analyzer analyze MyProject.csproj --min-severity Warning

# Only specific diagnostic IDs
throws-analyzer analyze MyProject.csproj -d THROWS004 THROWS021 THROWS026

# Exclude certain files
throws-analyzer analyze MySolution.sln -e "**/obj/**" "**/bin/**" "**/Migrations/**"
```

### Report Customization

```bash
# HTML report only
throws-analyzer analyze MyProject.csproj -f html

# Markdown only
throws-analyzer analyze MyProject.csproj -f markdown

# Custom output directory and filename
throws-analyzer analyze MyProject.csproj -o ./build/reports --filename throws-analysis

# Open report in browser after generation
throws-analyzer analyze MyProject.csproj --open

# Limit detailed diagnostics in report
throws-analyzer analyze MySolution.sln --max-detailed 50
```

### Advanced Filtering

```bash
# Analyze specific projects in solution
throws-analyzer analyze MySolution.sln -p MyProject.Core MyProject.Api

# Multiple filters combined
throws-analyzer analyze MySolution.sln \
  -c Release \
  --min-severity Warning \
  -d THROWS004 THROWS021 THROWS026 \
  -e "**/obj/**" "**/bin/**" \
  -o ./quality-reports \
  --filename critical-issues
```

### Production Use Cases

```bash
# Strict analysis for CI/CD (critical issues only)
throws-analyzer analyze MySolution.sln \
  -c Release \
  --min-severity Error \
  -d THROWS004 THROWS021 THROWS026 \
  --no-code-snippets \
  -f html \
  -o ./build/reports

# Comprehensive analysis for code review
throws-analyzer analyze MySolution.sln \
  --verbose \
  --snippet-lines 5 \
  --open \
  -o ./docs/code-quality

# Quick analysis without restore
throws-analyzer analyze MyProject.csproj --skip-restore -f markdown
```

## Reports

### HTML Report Features

The HTML report includes:

- **Interactive Dashboard**
  - Summary statistics
  - Diagnostics by ID, project, severity, and file
  - Top 20 files with most diagnostics

- **Sortable Tables**
  - Click column headers to sort
  - Numeric and alphabetic sorting

- **Detailed Diagnostics**
  - Grouped by file
  - Color-coded by severity
  - Code snippets with syntax highlighting
  - Links to documentation

- **Professional Styling**
  - Responsive design
  - Dark code snippets
  - Color-coded severity indicators

### Markdown Report Features

The Markdown report includes:

- Summary tables
- Statistics by diagnostic ID, project, severity
- Top files with most diagnostics
- Detailed diagnostics with code snippets
- Compatible with GitHub, GitLab, documentation sites

### Report Structure

```
./reports/
├── analysis-report.html    # Interactive HTML report
└── analysis-report.md      # Markdown report
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Code Quality

on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Install ThrowsAnalyzer CLI
        run: dotnet tool install --global ThrowsAnalyzer.Cli

      - name: Run Analysis
        run: |
          throws-analyzer analyze MySolution.sln \
            -c Release \
            --min-severity Warning \
            -o ./reports

      - name: Upload Reports
        uses: actions/upload-artifact@v3
        with:
          name: throws-analyzer-reports
          path: ./reports/

      - name: Comment PR
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v6
        with:
          script: |
            const fs = require('fs');
            const report = fs.readFileSync('./reports/analysis-report.md', 'utf8');
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: report
            });
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- script: |
    dotnet tool install --global ThrowsAnalyzer.Cli
  displayName: 'Install ThrowsAnalyzer CLI'

- script: |
    throws-analyzer analyze $(Build.SourcesDirectory)/MySolution.sln \
      -c Release \
      --min-severity Warning \
      -o $(Build.ArtifactStagingDirectory)/reports
  displayName: 'Run ThrowsAnalyzer'

- task: PublishBuildArtifacts@1
  inputs:
    pathToPublish: '$(Build.ArtifactStagingDirectory)/reports'
    artifactName: 'ThrowsAnalyzer Reports'
```

### GitLab CI

```yaml
stages:
  - analyze

code-quality:
  stage: analyze
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet tool install --global ThrowsAnalyzer.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - throws-analyzer analyze MySolution.sln -c Release -o ./reports
  artifacts:
    paths:
      - reports/
    expire_in: 30 days
```

### Local Pre-commit Hook

Create `.git/hooks/pre-commit`:

```bash
#!/bin/bash

echo "Running ThrowsAnalyzer..."

# Get list of changed .cs files
CHANGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$')

if [ -z "$CHANGED_FILES" ]; then
  echo "No C# files changed"
  exit 0
fi

# Run analyzer on solution
throws-analyzer analyze MySolution.sln \
  --min-severity Error \
  -d THROWS004 THROWS021 THROWS026 \
  --no-code-snippets \
  -f markdown \
  -o /tmp/throws-analyzer

# Check if critical issues found
if grep -q "Errors: [1-9]" /tmp/throws-analyzer/analysis-report.md; then
  echo "❌ Critical exception handling issues found!"
  cat /tmp/throws-analyzer/analysis-report.md
  exit 1
fi

echo "✓ No critical issues found"
exit 0
```

Make executable:
```bash
chmod +x .git/hooks/pre-commit
```

## Troubleshooting

### MSBuild Not Found

**Error:** `Could not locate MSBuild instance`

**Solution:**
```bash
# Install Visual Studio Build Tools or
# Ensure dotnet SDK is installed
dotnet --version

# On macOS, may need to install:
brew install mono
```

### No Diagnostics Found

**Issue:** Analysis completes but finds 0 diagnostics

**Possible causes:**
1. ThrowsAnalyzer not referenced in project
2. All diagnostics configured as `none` in `.editorconfig`
3. Project doesn't compile

**Solution:**
```bash
# Check project references
dotnet list reference

# Try verbose mode
throws-analyzer analyze MyProject.csproj --verbose

# Verify project builds
dotnet build MyProject.csproj
```

### Out of Memory

**Error:** `OutOfMemoryException` when analyzing large solutions

**Solution:**
```bash
# Limit diagnostics collected
throws-analyzer analyze MySolution.sln --max-diagnostics 10000

# Disable code snippets
throws-analyzer analyze MySolution.sln --no-code-snippets

# Analyze specific projects
throws-analyzer analyze MySolution.sln -p ProjectName
```

### Report Not Opening

**Issue:** `--open` flag doesn't open report

**Solution:**
- Manually open report from `./reports/` directory
- Check file path in console output
- Verify default browser/application is set

### Package Vulnerabilities Warning

**Warning:** `Package 'X' has a known high severity vulnerability`

These are transitive dependencies from Roslyn packages. They're used only for compile-time analysis and don't pose runtime risks. You can:
1. Ignore warnings (they don't affect functionality)
2. Wait for upstream Roslyn packages to update dependencies

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Analysis failed |
| 2 | Invalid arguments |

## Performance Tips

1. **Use `--skip-restore`** if packages are already restored
2. **Filter by projects** with `-p` for faster analysis of specific areas
3. **Disable code snippets** with `--no-code-snippets` for large codebases
4. **Use `--max-diagnostics`** to cap collection for quick checks
5. **Analyze incrementally** by targeting changed projects only

## Support

- **GitHub Issues:** https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- **Documentation:** https://github.com/wieslawsoltes/ThrowsAnalyzer/tree/main/docs
- **Configuration Guide:** [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md)

## Version History

### 1.0.0 (Current)
- Initial release
- Support for .sln, .csproj, and directory analysis
- HTML and Markdown report generation
- Interactive sortable tables
- Comprehensive filtering options
- CI/CD integration examples
