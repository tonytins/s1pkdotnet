# Sims1Pkg Project Guidelines

## Overview

**Sims1Pkg** is a simple command-line utility written in C# (.NET 6.0) for compressing and extracting Sims 1 package files. The format requires both `metadata.json` and `.ts1` files to be present in a zip archive for extraction to work.

The project is small in scope: a single console application with a clean interface for working with Sims 1 game files. It uses `System.IO.Compression` for zip handling and `System.Text.Json` for metadata parsing.

## Project Structure

```
s1pkdotnet/
├── Sims1Pkg.csproj          # .NET 6.0 console application
├── Program.cs               # Entry point - CLI orchestration
├── ISPackage.cs             # Interfaces: ISPackage, ISMetadata
├── TS1Package.cs            # Main implementation class
└── README.md                # Project description
```

## Build and Run

```sh
dotnet build                 # Build the project
dotnet run -- archive.s1pk   # Extract from archive.s1pk
```

## Coding Standards

### General
- Follow standard C# conventions and .NET naming guidelines
- Use nullable reference types (already enabled in csproj)
- Prefer immutable data where practical
- Keep methods short and focused on a single responsibility

### Error Handling
- Validate all inputs before processing
- Provide clear, actionable error messages to users
- Use appropriate exception types for different failure modes
- Document expected failure conditions

### Code Organization
- Keep the interface definitions in `ISPackage.cs` clean and minimal
- Place all extraction and compression logic in `TS1Package.cs`
- Keep `Program.cs` limited to CLI argument parsing and orchestration
- Consider adding separate classes for compression vs. extraction if complexity grows

### File Handling
- Always dispose of file handles properly (use `using` statements)
- Validate file paths and existence before operations
- Handle edge cases: missing files, corrupt archives, invalid metadata

## Contribution Guidelines

### Before Submitting Changes
1. Run `dotnet build` - ensure the project compiles without warnings
2. Test extraction with a valid archive containing `metadata.json` and `.ts1` files
3. Test error handling with invalid/missing files
4. Update README.md if the CLI interface or behavior changes

### Adding Features
- For new file types or formats, extend the `ISPackage` interface if needed
- Add comprehensive error handling for new scenarios
- Document new capabilities in README.md
- Consider backward compatibility for existing archives

### Testing
- Manual testing is currently sufficient given the project's small scale
- Test with various zip archive structures
- Verify error messages are clear and helpful
- Test on different platforms if possible (.NET 6.0 is cross-platform)

## License

This project uses a dual-license approach:
- **Primary**: [UNLICENSE](UNLICENSE) - Public domain dedication
- **Fallback**: [BSD-3-Clause](COPYING) - For jurisdictions that don't recognize public domain waivers

All contributions should maintain this licensing approach.

## Personal Configuration

Keep personal preferences and machine-specific guidance in git-ignored
`*.local` files or directories (such as `AGENTS.local.md`), rather than in
shared project instructions. Check for relevant local guidance before
environment-specific work. You can create or update these files for details
such as:

- Coding preferences
- Local build and development environment
- Personal shortcuts or workflows
