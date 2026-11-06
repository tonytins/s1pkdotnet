# Sims1Pkg Documentation

This directory holds Sims1Pkg's design notes and roadmap.

- [Roadmap](roadmap.md) tracks the project milestones and future plans.
- [AGENTS.md](../AGENTS.md) describes the project guidelines and coding standards.
- [REVIEW.md](../REVIEW.md) provides the code review checklist.

## What Sims1Pkg is

Sims1Pkg is an open package format for Sims 1 content, inspired by Sims2Pack.
It provides a standardized way to package Sims 1 files using ZIP compression and
XML metadata, making it easier to share, distribute, and manage custom content.

## Where Sims1Pkg came from

Sims1Pkg started as a simple command-line utility for compressing and extracting
Sims 1 files. The original implementation required both `metadata.json` and `.ts1`
files, with limited file type support.

Through refactoring and analysis of actual Sims 1 game files (particularly the
`Downloads` directory), the project evolved to support all major Sims 1 file types
and migrated to XML metadata for better versatility. The `.ts1` requirement was
removed as it was a temporary workaround, and the format now supports 17 different
Sims 1 file types including `.iff`, `.far`, `.cmx`, `.skn`, `.bmp`, and many others.

The project is now evolving into a comprehensive ecosystem with:

1. **Sims1Pkg.Core** - A reusable library for package operations
2. **Sims1Pkg.Cli** - Command-line interface for scripting and automation
3. **Sims1Pkg.Gui** - User-friendly GUI using Avalonia

This separation allows the core functionality to be reused by other tools while
providing both command-line and graphical interfaces for end users.

## Design influences

Sims1Pkg takes inspiration from:

- **Sims2Pack** - The package format used by Sims 2, which inspired the overall
  structure and goal of creating an open package format
- **ZIP format** - Standard, widely-supported compression format
- **XML metadata** - Versatile, human-readable, supports validation and comments
- **Avalonia** - Cross-platform UI framework for the GUI implementation
- **Modular architecture** - Separation of concerns between core, CLI, and GUI

## Documentation structure

This documentation directory contains high-level design and planning documents.
Technical details and usage instructions are in the main [README.md](../README.md).

## See also

- [Main README](../README.md) - Usage and technical details
- [AGENTS.md](../AGENTS.md) - Project guidelines and coding standards
- [REVIEW.md](../REVIEW.md) - Code review checklist
