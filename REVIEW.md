# Sims1Pkg Review Checklist

Use this checklist when reviewing changes to Sims1Pkg. The focus is on correctness, robustness, and clarity appropriate for a small utility tool.

## Before Reviewing

- Read the change description and any linked issues or discussions
- Understand what user problem or feature the change addresses
- Skim the modified files to understand the scope of changes

## Review Checklist

### Correctness
- [ ] Does the change implement the stated functionality correctly?
- [ ] Are edge cases handled: empty archives, missing metadata.json, missing .ts1 files?
- [ ] Does extraction work with valid archives containing the required files?
- [ ] Are error messages clear and actionable for end users?

### Error Handling
- [ ] Are all file operations wrapped in appropriate try-catch blocks or using statements?
- [ ] Are invalid inputs detected and handled gracefully?
- [ ] Do error messages explain what went wrong and how to fix it?
- [ ] Are file handles, streams, and other disposable resources properly cleaned up?

### Code Quality
- [ ] Is the code readable and well-structured?
- [ ] Are method and variable names clear and descriptive?
- [ ] Are there any obvious performance issues (e.g., loading entire files into memory unnecessarily)?
- [ ] Is the code consistent with existing style and conventions?

### Interface Design
- [ ] If `ISPackage` or `ISMetadata` interfaces were modified, do the changes make sense?
- [ ] Are interface methods well-defined with clear contracts?
- [ ] Does the implementation fully satisfy the interface requirements?

### CLI Experience
- [ ] If CLI arguments or output changed, is the new behavior intuitive?
- [ ] Are usage instructions clear (in README.md or help text)?
- [ ] Does the tool provide appropriate feedback during operations?

### Compatibility
- [ ] Does the change maintain backward compatibility with existing archives?
- [ ] If new file formats are supported, are they clearly documented?
- [ ] Does the project still build and run on .NET 6.0?

### Documentation
- [ ] Is README.md updated if the CLI interface or behavior changed?
- [ ] Are new capabilities or requirements documented?
- [ ] Are there any comments explaining non-obvious logic?

### Testing
- [ ] Can the change be tested manually?
- [ ] Have you verified the change works with a test archive?
- [ ] Have you tested error cases (missing files, invalid formats)?

## Writing Review Comments

- **Be specific**: Point to exact lines and explain the issue
- **Be constructive**: Suggest improvements, not just point out problems
- **Prioritize by impact**:
  - **Blocking**: Prevents compilation, causes crashes, or breaks existing functionality
  - **High**: Major bugs, data corruption risks, or security issues
  - **Medium**: Behavior issues, unclear code, or missing error handling
  - **Low**: Style improvements, minor edge cases, or documentation tweaks

- If no issues remain, confirm and note any testing limitations

## Quick Test Commands

```sh
# Build the project
dotnet build

# Test with a valid archive (if available)
dotnet run -- archive.s1pk

# Test error handling with missing files
dotnet run -- nonexistent.s1pk
```
