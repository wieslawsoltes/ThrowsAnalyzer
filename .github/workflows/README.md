# GitHub Actions Workflows

This directory contains GitHub Actions workflows for continuous integration and release automation.

## Workflows

### CI Workflow (`ci.yml`)

**Triggers:**
- Push to `main` branch
- Pull requests to `main` branch
- Ignores changes to markdown files and documentation

**Jobs:**

1. **Build and Test** (Matrix: Ubuntu, Windows, macOS)
   - Restores dependencies
   - Builds in Release configuration
   - Runs all unit tests
   - Uploads test results as artifacts

2. **Pack**
   - Creates NuGet package
   - Uploads package as artifact (7-day retention)

3. **Code Analysis**
   - Performs build analysis
   - Generates workflow summary

**Artifacts:**
- Test results for each platform
- NuGet package (.nupkg)

### Release Workflow (`release.yml`)

**Triggers:**
- Push of version tags (e.g., `v1.0.0`, `v1.0.0-beta.1`)

**Jobs:**

1. **Create Release**
   - Extracts version from git tag
   - Updates `Directory.Build.props` with tag version
   - Builds and tests the project
   - Creates NuGet package
   - Publishes to NuGet.org (requires `NUGET_API_KEY` secret)
   - Creates GitHub release with package attached
   - Marks pre-release for tags with suffixes (e.g., `-beta.1`)

**Required Secrets:**
- `NUGET_API_KEY`: API key for publishing to NuGet.org
- `GITHUB_TOKEN`: Automatically provided by GitHub

**Artifacts:**
- NuGet package attached to GitHub release

## Usage

### Running CI

CI runs automatically on every push to `main` and on pull requests:

```bash
git push origin main
```

### Creating a Release

1. Ensure all changes are committed and pushed to `main`
2. Create and push a version tag:

```bash
# Release version
git tag v1.0.0
git push origin v1.0.0

# Pre-release version
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

3. The release workflow will:
   - Build and test the project
   - Create NuGet package with the tag version
   - Publish to NuGet.org
   - Create GitHub release with release notes

### Setting up NuGet API Key

1. Create a NuGet API key at https://www.nuget.org/account/apikeys
2. Add it to GitHub repository secrets:
   - Go to repository Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet API key

## Version Management

Versions are managed through git tags and `Directory.Build.props`:

- **Development**: Uses version from `Directory.Build.props` (e.g., `1.0.0-beta.1`)
- **Release**: Automatically updated from git tag during release workflow

### Tag Format

- Release: `v1.0.0`, `v2.1.3`
- Pre-release: `v1.0.0-alpha.1`, `v1.0.0-beta.2`, `v1.0.0-rc.1`

The release workflow automatically detects pre-release versions (containing `-`) and marks them appropriately.

## Local Testing

To test the build locally before pushing:

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Create package
dotnet pack src/ThrowsAnalyzer/ThrowsAnalyzer.csproj --configuration Release
```

## Troubleshooting

### NuGet Push Fails

- Verify `NUGET_API_KEY` secret is set correctly
- Check API key permissions on NuGet.org
- Ensure package version doesn't already exist on NuGet.org

### Build Fails on Specific Platform

- Check build logs in GitHub Actions
- Verify local build works on that platform
- Check for platform-specific dependencies or code

### Tests Fail

- Review test results artifacts
- Run tests locally: `dotnet test --verbosity detailed`
- Check for environment-specific test issues
