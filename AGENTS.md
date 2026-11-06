# Sims1Pkg Project Guidelines

## Overview

**Sims1Pkg** is an open package format for Sims 1 content, inspired by Sims2Pack. It provides command-line utilities and a GUI for compressing and extracting Sims 1 files into a standardized, interoperable package format using ZIP compression and XML metadata.

The project aims to create a modern, open alternative to the proprietary `.far` format used by The Sims 1, making it easier to share, distribute, and manage Sims 1 custom content across different tools and platforms.

## Vision

Sims1Pkg is a comprehensive ecosystem:

1. **Core Library** (`Sims1Pkg.Core`) - Reusable package handling logic
2. **CLI Tool** (`Sims1Pkg.Cli`) - Command-line interface for scripting and automation
3. **GUI Application** (`Sims1Pkg.Gui`) - User-friendly interface using Avalonia

This modular approach allows the core functionality to be used by other tools while providing both CLI and GUI options for end users.

## Project Structure

```
s1pkdotnet/
├── Sims1Pkg.slnx              # Solution file
├── README.md                  # Project documentation
├── AGENTS.md                  # This file - project guidelines
├── REVIEW.md                 # Code review checklist
├── .gitignore                 # Git ignore rules
├── UNLICENSE                  # Primary license (public domain)
├── COPYING                   # Fallback license (BSD-3-Clause)
├── doc/
│   ├── README.md             # Documentation overview
│   └── roadmap.md            # Project roadmap
└── src/
    ├── Sims1Pkg.Core/
    │   ├── Sims1Pkg.Core.csproj    # .NET 8.0 library
    │   ├── ISPackage.cs            # Interface definitions
    │   ├── ISMetadata.cs           # Metadata interface
    │   ├── PackageMetadata.cs      # XML metadata DTO
    │   ├── PackageProgressEventArgs.cs  # Progress event args
    │   └── TS1Package.cs           # Main package implementation
    ├── Sims1Pkg.Cli/
    │   ├── Sims1Pkg.Cli.csproj     # .NET 8.0 console application
    │   └── Program.cs              # CLI entry point and argument parsing
    └── Sims1Pkg.Gui/
        ├── Sims1Pkg.Gui.csproj     # .NET 8.0 Avalonia application
        ├── Program.cs              # GUI entry point
        ├── App.axaml + App.axaml.cs # Application definition
        └── MainWindow.axaml + MainWindow.axaml.cs  # Main window
```

## Supported File Types

Sims1Pkg supports all major Sims 1 file formats:

- **Core Resources**: `.iff`, `.far`
- **Custom Content**: `.cmx`, `.skn`
- **Images**: `.bmp`
- **Game Data**: `.hdb`, `.house`, `.family`, `.sims`, `.cht`, `.dat`
- **Audio**: `.HIT`, `.hot`, `.hsm`, `.XA`
- **Textures**: `.tds`, `.pal`

## Build and Run

```sh
# Build the entire solution
dotnet build

# Build specific project
dotnet build src/Sims1Pkg.Core/Sims1Pkg.Core.csproj
dotnet build src/Sims1Pkg.Cli/Sims1Pkg.Cli.csproj
dotnet build src/Sims1Pkg.Gui/Sims1Pkg.Gui.csproj

# Run CLI
dotnet run --project src/Sims1Pkg.Cli/Sims1Pkg.Cli.csproj extract package.s1pk
dotnet run --project src/Sims1Pkg.Cli/Sims1Pkg.Cli.csproj compress source/ package.s1pk

# Show help
dotnet run --project src/Sims1Pkg.Cli/Sims1Pkg.Cli.csproj help
```

## Package Format Specification

A valid `.s1pk` archive:

1. **Container**: Standard ZIP file format
2. **Required**: `metadata.xml` file in root with package metadata
3. **Content**: Any combination of supported Sims 1 file types
4. **Compression**: Standard ZIP compression (no proprietary formats)

### metadata.xml Structure

```xml
<PackageMetadata>
  <Name>Package Name</Name>
  <Author>Author Name</Author>
  <Date>YYYY-MM-DD</Date>
  <Version>1.0.0</Version>
  <Description>Optional description</Description>
</PackageMetadata>
```

## Coding Standards

### General
- Follow standard C# conventions and .NET naming guidelines
- Use nullable reference types (enabled in csproj)
- Prefer immutable data where practical
- Keep methods short and focused on a single responsibility
- Use XML documentation comments for public APIs

### Architecture
- **Separation of Concerns**: Keep CLI, core logic, and data models separate
- **Interface Design**: Interfaces should be minimal and focused
- **Dependency Flow**: Core library should have no dependencies on CLI or GUI
- **Testability**: Design for easy unit testing (coming soon)

### Error Handling
- Validate all inputs before processing
- Provide clear, actionable error messages to users
- Use appropriate exception types for different failure modes
- Document expected failure conditions in XML comments

### File Handling
- Always dispose of file handles properly (use `using` statements)
- Validate file paths and existence before operations
- Handle edge cases: missing files, corrupt archives, invalid metadata
- Preserve original file paths within the archive

### XML Metadata
- Use `System.Xml.Serialization` for metadata handling
- Support both serialization and deserialization
- Provide helper methods (`ToXml()`, `FromXml()`) for convenience
- Consider adding schema validation (XSD) in the future

## Contribution Guidelines

### Before Submitting Changes
1. Run `dotnet build` - ensure the project compiles without warnings
2. Test extraction and compression with various file types
3. Test error handling with invalid/missing files
4. Update documentation (README.md, AGENTS.md) if interfaces or behavior changes
5. Follow the code review checklist in REVIEW.md

### Adding Features
- For new file types: Add to `SupportedExtensions` array in TS1Package.cs
- For core functionality: Consider if it belongs in the library or CLI
- For GUI features: Wait for library split, then add to GUI project
- Add comprehensive error handling for new scenarios
- Document new capabilities in README.md
- Consider backward compatibility for existing archives

### Library Split
The library split is now complete:
- `Sims1Pkg.Core` contains all core package handling logic
- `Sims1Pkg.Cli` provides the command-line interface
- `Sims1Pkg.Gui` provides the Avalonia-based GUI

The three projects are independent and can be built/used separately.

### Testing
- Manual testing is currently sufficient for this small-scale project
- Test with various zip archive structures
- Verify error messages are clear and helpful
- Test on different platforms (.NET 8.0 is cross-platform)
- Future: Add unit tests for core library

## License

This project uses a dual-license approach:
- **Primary**: [UNLICENSE](UNLICENSE) - Public domain dedication
- **Fallback**: [BSD-3-Clause](COPYING) - For jurisdictions that don't recognize public domain waivers

All contributions should maintain this licensing approach.

## Personal Configuration

Keep personal preferences and machine-specific guidance in git-ignored
`*.local` files or directories (such as `AGENTS.local.md`), rather than in
shared project instructions. Check for relevant local guidance before
environment-specific work.

## See Also

- [Roadmap](doc/roadmap.md) - Project milestones and future plans
- [Documentation](doc/README.md) - Overview of project documentation
