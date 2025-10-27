# ThrowsAnalyzer Documentation

Welcome to the ThrowsAnalyzer documentation! This directory contains comprehensive guides, design documents, and historical archives for the ThrowsAnalyzer project.

## Quick Navigation

### User Guides

Essential guides for using ThrowsAnalyzer in your projects:

- **[CLI Tool Guide](guides/CLI_TOOL.md)** - Complete guide for the command-line tool, including installation, usage, CI/CD integration, and troubleshooting
- **[Generic Member Support](guides/GENERIC_MEMBER_SUPPORT.md)** - Understanding generic type analysis in ThrowsAnalyzer
- **[Configuration Guide](CONFIGURATION_GUIDE.md)** - How to configure ThrowsAnalyzer using .editorconfig and project settings
- **[Project Status](PROJECT_STATUS.md)** - Current status, features, and roadmap
- **[Release Checklist](RELEASE_CHECKLIST.md)** - Steps for releasing new versions

### Packaging & Distribution

- **[Packaging Guide](PACKAGING.md)** - NuGet package creation and publishing
- **[NuGet Package Ready](NUGET_PACKAGE_READY.md)** - Package readiness checklist and verification

## Design & Planning Documents

Historical planning documents that guided the development of ThrowsAnalyzer:

### Architecture & Analysis Design

- **[Analysis Design](design/ANALYSIS.md)** - Core analysis architecture and design principles
- **[Exception Type Analysis Plan](design/EXCEPTION_TYPE_ANALYSIS_PLAN.md)** - Plan for comprehensive exception type analysis

### CLI Tool Design

- **[CLI Tool Design](design/CLI_TOOL_DESIGN.md)** - Design document for the command-line tool

### Phase Planning Documents

Development was organized into phases. These documents outline the plans:

- **[Phase 4.6 & 4.7 Plan](design/PHASE4_6_7_PLAN.md)** - Performance optimization and enhanced configuration
- **[Phase 4 Code Fixes Plan](design/PHASE4_CODE_FIXES_PLAN.md)** - Planning for code fix providers
- **[Phase 5 Advanced Analysis Plan](design/PHASE5_ADVANCED_ANALYSIS_PLAN.md)** - Advanced exception flow analysis
- **[Phase 6 IDE Integration Plan](design/PHASE6_IDE_INTEGRATION_PLAN.md)** - Visual Studio and VS Code integration

## Historical Archives

Completion summaries documenting what was accomplished in each development phase:

### Phase 4 Archives

Phase 4 focused on comprehensive exception handling analysis and code fixes:

- **[Phase 4.6 & 4.7 Completion](archive/phase4/PHASE4_6_7_COMPLETION_SUMMARY.md)** - Performance optimization and configuration
- **[Phase 4 Day 2 Completion](archive/phase4/PHASE4_DAY2_COMPLETION_SUMMARY.md)** - Mid-phase progress
- **[Phase 4 Completion](archive/phase4/PHASE4_COMPLETION_SUMMARY.md)** - Final phase 4 summary

### Phase 5 Archives

Phase 5 added advanced exception flow analysis:

- **[Phase 5.1 Completion](archive/phase5/PHASE5_1_COMPLETION_SUMMARY.md)** - Async exception patterns
- **[Phase 5.2 Completion](archive/phase5/PHASE5_2_COMPLETION_SUMMARY.md)** - Iterator exception patterns
- **[Phase 5.3 Completion](archive/phase5/PHASE5_3_COMPLETION_SUMMARY.md)** - Lambda exception patterns
- **[Phase 5.4 Completion](archive/phase5/PHASE5_4_COMPLETION_SUMMARY.md)** - Exception flow analysis
- **[Phase 5 Final Completion](archive/phase5/PHASE5_FINAL_COMPLETION_SUMMARY.md)** - Complete phase 5 summary

### Phase 6 Archives

Phase 6 focused on best practices and IDE integration:

- **[Phase 6.1 Completion](archive/phase6/PHASE6_1_COMPLETION_SUMMARY.md)** - Best practices analyzers
- **[Phase 6 Completion](archive/phase6/PHASE6_COMPLETION_SUMMARY.md)** - Final phase 6 summary

## Documentation Overview

### Current Documentation (Active)

These documents at the root level are actively maintained and relevant for current users:

- Configuration guides
- Status and roadmap
- Release processes
- Packaging information

### Guides Directory

Step-by-step guides for end users:

- CLI tool usage
- Advanced features
- Integration scenarios

### Design Directory

Planning and design documents that explain the "why" behind architectural decisions. These documents are historical and provide context for the codebase structure.

### Archive Directory

Completion summaries organized by development phase. These documents provide a historical record of what was implemented, when, and how. They're valuable for understanding the evolution of the project.

## Additional Resources

- **[Main README](../README.md)** - Project overview, quick start, and feature summary
- **[GitHub Repository](https://github.com/wieslawsoltes/ThrowsAnalyzer)** - Source code and issue tracker
- **[NuGet Package](https://www.nuget.org/packages/ThrowsAnalyzer)** - Download the analyzer
- **[CLI Tool on NuGet](https://www.nuget.org/packages/ThrowsAnalyzer.Cli)** - Download the command-line tool

## Contributing

When adding new documentation:

1. **User guides** → Place in `guides/` directory
2. **Design documents** → Place in `design/` directory
3. **Completion summaries** → Place in `archive/phaseN/` directory
4. **General documentation** → Place at `docs/` root level
5. **Update this index** → Add links to new documents in appropriate sections

## Support

- **Issues**: Report bugs or request features at https://github.com/wieslawsoltes/ThrowsAnalyzer/issues
- **Discussions**: Ask questions at https://github.com/wieslawsoltes/ThrowsAnalyzer/discussions

---

*Last updated: Phase 6 completion - October 2025*
