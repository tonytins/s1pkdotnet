using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Sims1Pkg;

public class TS1Package : ISPackage
{
    private static readonly string[] SupportedExtensions = { ".iff", ".ts1" };
    private const string MetadataFileName = "metadata.json";

    public PackageMetadata Metadata { get; private set; } = new();

    /// <summary>
    /// Extracts files from a package archive to the current directory.
    /// </summary>
    /// <param name="packagePath">Path to the .s1pk file.</param>
    public void Extract(string packagePath)
    {
        Extract(packagePath, null);
    }

    /// <summary>
    /// Extracts files from a package archive to the specified directory.
    /// </summary>
    /// <param name="packagePath">Path to the .s1pk file.</param>
    /// <param name="outputDirectory">Directory to extract files to. Defaults to package directory.</param>
    public void Extract(string packagePath, string? outputDirectory)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Package file not found: " + packagePath);
        }

        string extractPath = outputDirectory ?? Path.GetDirectoryName(packagePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(extractPath);

        using var zip = ZipFile.OpenRead(packagePath);

        bool metadataFound = false;
        bool ts1Found = false;

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.Equals(MetadataFileName, StringComparison.OrdinalIgnoreCase))
                metadataFound = true;
            if (entry.FullName.EndsWith(".ts1", StringComparison.OrdinalIgnoreCase))
                ts1Found = true;
        }

        if (!metadataFound || !ts1Found)
        {
            throw new InvalidDataException(
                "Cannot extract files: archive must contain both 'metadata.json' and at least one '.ts1' file.");
        }

        var metadataEntry = zip.GetEntry(MetadataFileName);
        if (metadataEntry == null)
        {
            throw new InvalidDataException("Metadata file not found in archive");
        }

        using (var stream = metadataEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Metadata = JsonSerializer.Deserialize<PackageMetadata>(reader.ReadToEnd(), options) ?? new PackageMetadata();
            Console.WriteLine("Package metadata:");
            Console.WriteLine("  Name: " + Metadata.Name);
            Console.WriteLine("  Author: " + Metadata.Author);
            Console.WriteLine("  Date: " + Metadata.Date);
        }

        int extractedCount = 0;
        foreach (var entry in zip.Entries)
        {
            if (IsSupportedFile(entry.FullName))
            {
                string destinationPath = Path.Combine(extractPath, entry.FullName);
                string? directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                entry.ExtractToFile(destinationPath, overwrite: true);
                Console.WriteLine("Extracted: " + entry.FullName);
                extractedCount++;
            }
        }
        Console.WriteLine("Extracted " + extractedCount + " files.");
    }

    /// <summary>
    /// Compresses files from a directory into a package archive.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output .s1pk file.</param>
    public void Compress(string sourceDirectory, string packagePath)
    {
        Compress(sourceDirectory, packagePath, null);
    }

    /// <summary>
    /// Compresses files into a package archive with optional metadata.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output .s1pk file.</param>
    /// <param name="metadata">Package metadata to include. If null, default metadata is created.</param>
    public void Compress(string sourceDirectory, string packagePath, PackageMetadata? metadata)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("Source directory not found: " + sourceDirectory);
        }

        bool hasTs1Files = false;
        foreach (var ext in SupportedExtensions)
        {
            if (Directory.GetFiles(sourceDirectory, "*" + ext, SearchOption.AllDirectories).Length > 0)
            {
                if (ext.Equals(".ts1", StringComparison.OrdinalIgnoreCase))
                    hasTs1Files = true;
            }
        }

        if (!hasTs1Files)
        {
            throw new InvalidDataException(
                "Cannot create package: at least one '.ts1' file is required in the source directory.");
        }

        var packageMetadata = metadata ?? new PackageMetadata
        {
            Name = Path.GetFileNameWithoutExtension(packagePath),
            Author = Environment.UserName,
            Date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        string outputDir = Path.GetDirectoryName(packagePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(outputDir);

        using (var zip = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            var metadataEntry = zip.CreateEntry(MetadataFileName, CompressionLevel.Optimal);
            using (var stream = metadataEntry.Open())
            using (var writer = new StreamWriter(stream))
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(packageMetadata, options);
                writer.Write(json);
            }
            Console.WriteLine("Added metadata.json");

            int addedCount = 0;
            foreach (var ext in SupportedExtensions)
            {
                var files = Directory.GetFiles(sourceDirectory, "*" + ext, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string relativePath = Path.GetRelativePath(sourceDirectory, file);
                    zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                    Console.WriteLine("Added: " + relativePath);
                    addedCount++;
                }
            }
            Console.WriteLine("Added " + addedCount + " files.");
        }
    }

    private static bool IsSupportedFile(string filePath)
    {
        foreach (var ext in SupportedExtensions)
        {
            if (filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
