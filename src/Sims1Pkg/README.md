# Sims1Pkg

Sims1Pkg is a format for making compressing and extracting Sims 1 `*.iff` and other files easier. This format requires both `metadata.json` and `.ts1` files to be present in a zip archive for extraction to work.

It provides a clean command-line interface for working with Sims 1 game files, bringing modern package management to classic Sims games.

## Features

- **Extract**: Unpack `.s1pk` archives to a directory
- **Compress**: Create `.s1pk` archives from source files
- **Metadata support**: Store package name, author, date, version, and description
- **Validation**: Ensures required files (`metadata.json` and `.ts1`) are present
- **Supported file types**: `.iff` and `.ts1`

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

1. **metadata.json** - Package metadata in JSON format
2. **At least one `.ts1` file** - Required for the format
3. **Optional `.iff` files** - Additional Sims 1 files

### Example metadata.json

```json
{
  "name": "My Sims 1 Package",
  "author": "Jane Doe",
  "date": "2024-01-01",
  "version": "1.0.0",
  "description": "A sample package for Sims 1"
}
```

## Project Structure

```
s1pkdotnet/
├── Sims1Pkg.csproj          # .NET 8.0 console application
├── Program.cs               # CLI entry point and argument parsing
├── ISPackage.cs             # Interfaces: ISPackage, ISMetadata
├── PackageMetadata.cs       # Metadata DTO class
└── TS1Package.cs            # Main implementation class
```

## Requirements

- .NET 8.0 SDK or runtime

## Error Handling

The tool provides clear error messages for common issues:

- Missing package file
- Invalid archive structure (missing `metadata.json` or `.ts1` files)
- Missing source directory
- Missing required files in source directory
