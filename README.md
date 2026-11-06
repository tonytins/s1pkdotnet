# Sims1Pkg

Sims1Pkg is a format for making compressing and extracting Sims 1 content easier. Inspired by Sims2Pack, this format requires a `metadata.xml` file to be present in the archive and supports all major Sims 1 file types.

It provides a clean command-line interface for working with Sims 1 game files, bringing modern package management to classic Sims games.

## Features

- **Extract**: Unpack `.s1pk` archives to a directory
- **Compress**: Create `.s1pk` archives from source files
- **Metadata support**: Store package name, author, date, version, and description
- **XML metadata**: Versatile metadata format with support for validation, comments, and complex structures
- **Validation**: Ensures `metadata.xml` is present; extracts all supported Sims 1 file types
- **Supported file types**: All major Sims 1 formats (iff, far, cmx, skn, bmp, hdb, house, family, sims, cht, HIT, hot, hsm, XA, tds, pal, dat)

## License

In jurisdictions that recognize copyright waivers, I've [waived all copyright](UNLICENSE) and related or neighboring rights for this project. In areas where these waivers are not recognized, [BSD-3-Clause](COPYING) is enforced.

## Usage

### Build

```sh
dotnet build
```

### Extract Files

```sh
# Basic extraction (extracts to same directory as package)
dotnet run -- extract package.s1pk

# Extract to specific directory
dotnet run -- extract package.s1pk output_directory
```

### Compress Files

```sh
# Basic compression (auto-generates metadata)
dotnet run -- compress source_directory/ package.s1pk

# Compression with custom metadata
dotnet run -- compress source_directory/ package.s1pk --name "My Package" --author "John Doe" --date "2024-01-01"
```

### All Commands

```sh
# Show help
dotnet run -- help

# Extract
dotnet run -- extract <package-path> [output-directory]

# Compress
dotnet run -- compress <source-directory> <package-path> [options]
```

### Compress Options

- `--name, -n` - Package name
- `--author, -a` - Package author
- `--date, -d` - Package date
- `--version` - Package version
- `--description` - Package description

## Package Format

A valid `.s1pk` archive is a standard ZIP file containing:

1. **metadata.xml** - Package metadata in XML format (required)
2. **Supported content files** - Any combination of supported Sims 1 file types

### Supported File Types

Sims1Pkg supports all major Sims 1 file formats:

- **Core Resources**: `.iff`, `.far`
- **Custom Content**: `.cmx`, `.skn`
- **Images**: `.bmp`
- **Game Data**: `.hdb`, `.house`, `.family`, `.sims`, `.cht`, `.dat`
- **Audio**: `.HIT`, `.hot`, `.hsm`, `.XA`
- **Textures**: `.tds`, `.pal`

### Example metadata.xml

```xml
<PackageMetadata>
  <Name>My Sims 1 Package</Name>
  <Author>Jane Doe</Author>
  <Date>2024-01-01</Date>
  <Version>1.0.0</Version>
  <Description>A sample package for Sims 1</Description>
</PackageMetadata>
```

## Project Structure

```
s1pkdotnet/
├── Sims1Pkg.slnx              # Solution file
├── README.md                  # This file
├── UNLICENSE                   # Primary license
├── COPYING                    # Fallback license
├── .gitignore                 # Git ignore rules
└── src/
    └── Sims1Pkg/
        ├── Sims1Pkg.csproj    # .NET 8.0 console application
        ├── Program.cs         # CLI entry point and argument parsing
        ├── ISPackage.cs       # Interfaces: ISPackage, ISMetadata
        ├── PackageMetadata.cs # Metadata DTO class
        └── TS1Package.cs      # Main implementation class
```

## Requirements

- .NET 8.0 SDK or runtime

## Error Handling

The tool provides clear error messages for common issues:

- Missing package file
- Invalid archive structure (missing `metadata.xml` file)
- Missing source directory
- Missing required files in source directory

## Notes

This format is inspired by Sims2Pack, providing a similar open package format for Sims 1 content. Unlike the original Sims 1 `.far` format (which uses a proprietary archive format), Sims1Pkg uses standard ZIP compression for better compatibility and interoperability.
