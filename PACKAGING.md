# ThrowsAnalyzer - NuGet Packaging Guide

## Package Information

**Package ID**: ThrowsAnalyzer
**Version**: 1.0.0
**License**: MIT
**Target Framework**: netstandard2.0

## What's Included

### Analyzers & Code Fixes
- 30 diagnostic analyzers (THROWS001-030)
- 16 code fix providers
- Comprehensive exception analysis infrastructure

### Documentation
- NuGet-specific README (displayed on NuGet.org)
- LICENSE file
- Full documentation available on GitHub

### Dependencies
All dependencies are marked as `PrivateAssets="all"` to avoid conflicts:
- Microsoft.CodeAnalysis.CSharp (4.12.0)
- Microsoft.CodeAnalysis.CSharp.Workspaces (4.12.0)
- Microsoft.CodeAnalysis.Analyzers (3.11.0)

## Building the Package

### Prerequisites
- .NET SDK 8.0 or later
- Visual Studio 2022 or JetBrains Rider (optional, for testing)

### Build Steps

1. **Clean the solution**
   ```bash
   dotnet clean
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Build in Release mode**
   ```bash
   dotnet build -c Release
   ```

4. **Run tests** (ensure all pass)
   ```bash
   dotnet test -c Release
   ```
   Expected: 269 tests passing, 0 failures

5. **Create the NuGet package**
   ```bash
   dotnet pack -c Release -o ./nupkg
   ```

   This creates: `./nupkg/ThrowsAnalyzer.1.0.0.nupkg`

## Package Structure

```
ThrowsAnalyzer.1.0.0.nupkg
├── analyzers/
│   └── dotnet/
│       └── cs/
│           └── ThrowsAnalyzer.dll    (Contains all analyzers and code fixes)
├── README.md                          (NuGet-specific README)
├── LICENSE
└── ThrowsAnalyzer.nuspec             (Auto-generated)
```

## Testing the Package Locally

### Option 1: Local NuGet Source

1. **Add local package source**
   ```bash
   dotnet nuget add source /path/to/ThrowsAnalyzer/nupkg -n "Local"
   ```

2. **Create a test project**
   ```bash
   mkdir TestThrowsAnalyzer
   cd TestThrowsAnalyzer
   dotnet new console
   ```

3. **Install the analyzer**
   ```bash
   dotnet add package ThrowsAnalyzer --version 1.0.0 --source Local
   ```

4. **Test the analyzer**
   Add some code with exception issues and build:
   ```bash
   dotnet build
   ```
   
   You should see ThrowsAnalyzer diagnostics in the build output.

### Option 2: Direct Reference

1. **Create test project**
   ```bash
   dotnet new console -n TestProject
   cd TestProject
   ```

2. **Add analyzer to project file**
   ```xml
   <ItemGroup>
     <Analyzer Include="../path/to/ThrowsAnalyzer/nupkg/ThrowsAnalyzer.1.0.0.nupkg" />
   </ItemGroup>
   ```

3. **Build and verify**
   ```bash
   dotnet build
   ```

## Publishing to NuGet.org

### Prerequisites
- NuGet.org account
- API key from https://www.nuget.org/account/apikeys

### Steps

1. **Set API key** (one-time setup)
   ```bash
   dotnet nuget push --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

2. **Push the package**
   ```bash
   dotnet nuget push ./nupkg/ThrowsAnalyzer.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

3. **Verify on NuGet.org**
   - Package should appear at: https://www.nuget.org/packages/ThrowsAnalyzer/
   - Allow 5-10 minutes for indexing

## Version Management

### Semantic Versioning
ThrowsAnalyzer follows [SemVer 2.0.0](https://semver.org/):

- **MAJOR** version (1.x.x): Breaking changes to public API
- **MINOR** version (x.1.x): New features, backwards-compatible
- **PATCH** version (x.x.1): Bug fixes, backwards-compatible

### Current Version: 1.0.0
This is the initial release with all planned features complete.

### Future Versions
- **1.0.x**: Bug fixes and minor improvements
- **1.1.0**: New diagnostic rules or code fixes
- **2.0.0**: Breaking changes (if any)

## Package Metadata Checklist

Before publishing, verify:

- ✅ Version number updated in ThrowsAnalyzer.csproj
- ✅ Release notes updated with changelog
- ✅ All tests passing (269/269)
- ✅ Build succeeds in Release mode
- ✅ README.md is current and accurate
- ✅ LICENSE file is included
- ✅ Package tags are relevant and searchable
- ✅ Description is clear and comprehensive
- ✅ Repository URL is correct
- ✅ Icon is included (if available)

## Package Quality Indicators

### NuGet.org Best Practices

When published, ThrowsAnalyzer will have:

✅ **License**: MIT (OSI-approved)
✅ **Repository**: GitHub link provided
✅ **README**: Comprehensive documentation
✅ **Tags**: Relevant, searchable keywords
✅ **Dependencies**: Properly marked as private
✅ **Target Framework**: netstandard2.0 (broad compatibility)
✅ **Development Dependency**: Marked as `true`
✅ **Icon**: Placeholder (update with custom icon if desired)

### Expected Package Score

NuGet.org assigns quality scores based on:
- Documentation: ⭐⭐⭐⭐⭐ (README, license, repo)
- Metadata: ⭐⭐⭐⭐⭐ (tags, description, release notes)
- Dependencies: ⭐⭐⭐⭐⭐ (properly configured)
- Practices: ⭐⭐⭐⭐⭐ (development dependency, proper packaging)

Expected: **5/5 stars** in all categories

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Publish NuGet

on:
  release:
    types: [published]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release --no-restore
    
    - name: Test
      run: dotnet test -c Release --no-build
    
    - name: Pack
      run: dotnet pack -c Release --no-build -o ./nupkg
    
    - name: Push to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

## Installation for Users

### .NET CLI
```bash
dotnet add package ThrowsAnalyzer
```

### Package Manager Console
```powershell
Install-Package ThrowsAnalyzer
```

### PackageReference (csproj)
```xml
<ItemGroup>
  <PackageReference Include="ThrowsAnalyzer" Version="1.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

## Troubleshooting

### Package doesn't show diagnostics

**Solution**: Ensure the package is referenced as an analyzer:
```xml
<PackageReference Include="ThrowsAnalyzer" Version="1.0.0" />
```

The `DevelopmentDependency=true` setting ensures proper installation as an analyzer.

### Conflicts with other analyzers

**Solution**: ThrowsAnalyzer uses `PrivateAssets="all"` for all dependencies, preventing conflicts.

### Code fixes not appearing

**Solution**: 
1. Restart Visual Studio or VS Code
2. Clean and rebuild solution
3. Check that code fix providers are included in package (they are in analyzers/dotnet/cs/)

## Support

For issues or questions:
- **GitHub Issues**: https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- **Discussions**: https://github.com/wieslawsoltes/ThrowsAnalyzer/discussions

## License

MIT License - See LICENSE file for details
