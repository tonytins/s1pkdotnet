# Sims1Pkg Review Checklist

Use this checklist when reviewing changes to Sims1Pkg. The focus is on correctness, robustness, and maintaining alignment with the project's vision of being an open, comprehensive package format for Sims 1 content.

## Before Reviewing

- Read the change description and any linked issues or discussions
- Understand what user problem or feature the change addresses
- Review the project vision in AGENTS.md
- Skim the modified files to understand the scope of changes

## Review Checklist

### Correctness
- [ ] Does the change implement the stated functionality correctly?
- [ ] Are edge cases handled: empty archives, missing metadata.xml, unsupported file types?
- [ ] Does extraction work with valid archives containing various Sims 1 file types?
- [ ] Does compression work with various combinations of supported file types?
- [ ] Are error messages clear and actionable for end users?

### File Type Support
- [ ] If adding new file types, are they added to `SupportedExtensions` in TS1Package.cs?
- [ ] Are new file types documented in README.md?
- [ ] Are new file types mentioned in help text (Program.cs)?
- [ ] Do new file types follow the existing pattern (case-insensitive matching)?

### Error Handling
- [ ] Are all file operations wrapped in appropriate try-catch blocks or using statements?
- [ ] Are invalid inputs detected and handled gracefully?
- [ ] Do error messages explain what went wrong and how to fix it?
- [ ] Are file handles, streams, and other disposable resources properly cleaned up?
- [ ] Are error messages consistent across the codebase?

### Code Quality
- [ ] Is the code readable and well-structured?
- [ ] Are method and variable names clear and descriptive?
- [ ] Are there any obvious performance issues (e.g., loading entire files into memory unnecessarily)?
- [ ] Is the code consistent with existing style and conventions?
- [ ] Are there unnecessary dependencies between components?

### Interface Design
- [ ] If `ISPackage` or `ISMetadata` interfaces were modified, do the changes make sense?
- [ ] Are interface methods well-defined with clear contracts?
- [ ] Does the implementation fully satisfy the interface requirements?
- [ ] Are interfaces minimal and focused (Single Responsibility Principle)?

### XML Metadata
- [ ] Is XML serialization/deserialization working correctly?
- [ ] Are all metadata fields properly mapped with `[XmlElement]` attributes?
- [ ] Are `ToXml()` and `FromXml()` helper methods maintained?
- [ ] Does metadata handling preserve all fields?

### CLI Experience
- [ ] If CLI arguments or output changed, is the new behavior intuitive?
- [ ] Are usage instructions clear (in README.md or help text)?
- [ ] Does the tool provide appropriate feedback during operations?
- [ ] Are command names and aliases consistent?

### Documentation
- [ ] Is README.md updated if the CLI interface or behavior changed?
- [ ] Are new capabilities or requirements documented?
- [ ] Are there XML documentation comments for public methods?
- [ ] Is AGENTS.md updated for architectural changes?
- [ ] Are there any comments explaining non-obvious logic?

### Architecture (For Library Split)
- [ ] Are core logic and CLI separated (no CLI-specific code in core)?
- [ ] Are data models (PackageMetadata) independent of presentation?
- [ ] Can core functionality be reused without the CLI?
- [ ] Are dependencies minimal and well-managed?

### Compatibility
- [ ] Does the change maintain backward compatibility with existing archives?
- [ ] If new file formats are supported, are they clearly documented?
- [ ] Does the project still build and run on .NET 8.0?
- [ ] Are all supported file types still working?

### Testing
- [ ] Can the change be tested manually?
- [ ] Have you verified the change works with test archives?
- [ ] Have you tested error cases (missing files, invalid formats, unsupported types)?
- [ ] Have you tested with multiple file types in a single archive?
- [ ] Have you tested both compression and extraction?

## Writing Review Comments

- **Be specific**: Point to exact lines and explain the issue
- **Be constructive**: Suggest improvements, not just point out problems
- **Prioritize by impact**:
  - **Blocking**: Prevents compilation, causes crashes, or breaks existing functionality
  - **High**: Major bugs, data corruption risks, or security issues
  - **Medium**: Behavior issues, unclear code, or missing error handling
  - **Low**: Style improvements, minor edge cases, or documentation tweaks

- If no issues remain, confirm and note any testing limitations
- For architectural changes, consider the impact on future library split

## Test Scenarios

### Basic Functionality
```sh
# Build the project
dotnet build

# Test extraction with a valid archive
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj extract test.s1pk

# Test compression
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj compress source/ output.s1pk

# Test help
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj help
```

### Error Cases
```sh
# Missing package file
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj extract nonexistent.s1pk

# Missing metadata.xml
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj extract invalid.s1pk

# No supported files in source
dotnet run --project src/Sims1Pkg/Sims1Pkg.csproj compress empty/ output.s1pk
```

### File Type Tests
Create test archives with various combinations of:
- `.iff`, `.far`, `.cmx`, `.skn`, `.bmp`, `.hdb`, `.house`, `.family`, `.sims`, `.cht`, `.dat`
- `.HIT`, `.hot`, `.hsm`, `.XA`
- `.tds`, `.pal`

Verify all file types are correctly compressed and extracted.

## Avalonia GUI Considerations (Future)

When reviewing code that might affect the GUI:
- [ ] Is the core logic separated from CLI-specific code?
- [ ] Can the functionality be accessed through clean interfaces?
- [ ] Are there any dependencies on Console operations in core logic?
- [ ] Is error handling suitable for both CLI and GUI?
- [ ] Are progress reporting mechanisms abstracted (not tied to Console.WriteLine)?
