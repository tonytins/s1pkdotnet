# Sims1Pkg Roadmap

Sims1Pkg is an open package format for Sims 1 content, inspired by Sims2Pack.
It aims to provide a modern, interoperable way to package and share Sims 1 files
using standard ZIP compression and XML metadata.

The project is evolving from a simple CLI tool into a comprehensive ecosystem
with a reusable library, CLI interface, and GUI application.

## Short-term

Complete the current refactoring and establish a solid foundation:

- [x] Fix ISMetdata typo -> ISMetadata
- [x] Migrate metadata from JSON to XML
- [x] Remove .ts1 requirement
- [x] Add comprehensive Sims 1 file type support (17 types)
- [x] Reorganize project structure (src/Sims1Pkg/)
- [x] Update documentation to match new direction
- [x] Remove outdated layout directory
- [x] Verify all file types work correctly in practice
- [x] Test edge cases (empty archives, corrupt files, etc.)

## Medium-term

Split into a reusable library and add GUI support:

- [x] Create Sims1Pkg.Core library project
- [x] Move core logic (ISPackage, PackageMetadata, TS1Package) to Core
- [x] Add progress reporting events/interface to Core
- [x] Remove Console dependencies from Core
- [x] Create Sims1Pkg.Cli project that references Core
- [x] Verify CLI still works with library split
- [x] Create Sims1Pkg.Gui project with Avalonia
- [x] Implement basic GUI: file browser, extract/compress buttons
- [ ] Add package inspector (view contents without extracting)
- [ ] Add metadata editor

## Long-term

Advanced features and polish:

- [ ] Support batch operations (extract/compress multiple packages)
- [ ] Add package verification/validation mode
- [ ] Support package signing for authenticity
- [ ] Create package browser/manager GUI
- [ ] Add integration with Sims 1 game directory
- [ ] Add drag & drop support in GUI
- [ ] Add unit tests for Core library
- [ ] Add schema validation (XSD) for metadata.xml
- [ ] Support package versioning and compatibility checking
- [ ] Add compression level options

## Design Goals

Sims1Pkg should:

1. **Be open**: Standard ZIP format, no proprietary dependencies
2. **Be comprehensive**: Support all major Sims 1 file types
3. **Be flexible**: Work as CLI, library, or GUI
4. **Be reliable**: Validate inputs, clear error messages
5. **Be cross-platform**: .NET 8.0 + Avalonia
6. **Be maintainable**: Clean architecture, good documentation

## Stopping Points

The roadmap reaches its first major milestone when:

1. The Core library is functional and tested
2. The CLI works with the library
3. All 17 file types are properly supported
4. Basic GUI extraction/compression works

At this point, the project can be released as a usable tool.

## Open Questions

- Should we support .far files directly, or treat them as content to be packaged?
- Should we add support for Sims 2 or other Sims versions in the future?
- Should we create a package registry or repository for sharing packages?
- Should we add support for package dependencies?
- Should we create a standard for package icons/thumbnails?

## References

- Sims2Pack format (inspiration for package structure)
- The Sims 1 game files (reference for supported file types)
- Avalonia UI framework (for GUI implementation)
