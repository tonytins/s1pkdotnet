using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Sims1Pkg;

/// <summary>
/// Main package handler for Sims 1 package files (.s1pk).
/// Implements extraction and compression of package archives.
/// </summary>
public class TS1Package : ISPackage
{
    /// <summary>
    /// All supported Sims 1 file extensions for packaging.
    /// Based on file types found in The Sims 1 GameData and Downloads directories.
    /// Inspired by Sims2Pack's comprehensive format support.
    /// </summary>
    private static readonly string[] SupportedExtensions = {
        // Metadata
        ".xml",    // XML metadata files
        
        // Core resource formats
        ".iff",    // IFF format - various resources
        ".far",    // Fast Access Resource - resource archives
        
        // Custom content
        ".cmx",    // Custom content - clothing, hair, objects
        ".skn",    // Skin files
        
        // Images
        ".bmp",    // Bitmap images
        
        // Game data
        ".hdb",    // House database
        ".house",  // House files
        ".family", // Family files
        ".sims",   // Sims files (alternative format)
        ".cht",    // Character files
        
        // Audio
        ".HIT",    // Sound files
        ".hot",    // Sound files
        ".hsm",    // Sound files
        ".XA",     // Audio files (from expansions)
        
        // Textures and palettes
        ".tds",    // Texture data
        ".pal",    // Palette files
        
        // Data files
        ".dat"     // Generic data files
    };

    private const string MetadataFileName = "metadata.xml";

    /// <summary>
    /// Event raised when progress is made during package operations.
    /// </summary>
    public event PackageProgressEventHandler? Progress;

    /// <summary>
    /// Metadata from the package.
    /// </summary>
    public PackageMetadata Metadata { get; private set; } = new();

    /// <summary>
    /// Gets all supported file extensions.
    /// </summary>
    public static IEnumerable<string> GetSupportedExtensions()
    {
        return SupportedExtensions;
    }

    /// <summary>
    /// Extracts files from a package archive to the current directory.
    /// </summary>
    /// <param name="packagePath">Path to the .s1pk file.</param>
    public void Extract(string packagePath)
    {
        ExtractAsync(packagePath, null).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Extracts files from a package archive to the specified directory.
    /// </summary>
    /// <param name="packagePath">Path to the .s1pk file.</param>
    /// <param name="outputDirectory">Directory to extract files to. Defaults to package directory.</param>
    public void Extract(string packagePath, string? outputDirectory)
    {
        ExtractAsync(packagePath, outputDirectory).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Extracts files from a package archive asynchronously.
    /// </summary>
    /// <param name="packagePath">Path to the .s1pk file.</param>
    /// <param name="outputDirectory">Directory to extract files to. Defaults to package directory.</param>
    public async Task ExtractAsync(string packagePath, string? outputDirectory = null)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Package file not found: " + packagePath);
        }

        string extractPath = outputDirectory ?? Path.GetDirectoryName(packagePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(extractPath);

        using var zip = ZipFile.OpenRead(packagePath);

        // Validate archive
        bool metadataFound = false;
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.Equals(MetadataFileName, StringComparison.OrdinalIgnoreCase))
                metadataFound = true;
        }

        if (!metadataFound)
        {
            throw new InvalidDataException(
                "Cannot extract files: archive must contain 'metadata.xml' file.");
        }

        // Read metadata
        var metadataEntry = zip.GetEntry(MetadataFileName);
        if (metadataEntry == null)
        {
            throw new InvalidDataException("Metadata file not found in archive");
        }

        using (var stream = metadataEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            string xml = await reader.ReadToEndAsync();
            Metadata = PackageMetadata.FromXml(xml) ?? new PackageMetadata();
        }

        Progress?.Invoke(this, new PackageProgressEventArgs(0, "Starting extraction", MetadataFileName));

        // Extract files
        int extractedCount = 0;
        int totalSupported = zip.Entries.Count(e => IsSupportedFile(e.FullName));
        
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
                extractedCount++;
                
                int percent = (int)((float)extractedCount / totalSupported * 100);
                Progress?.Invoke(this, new PackageProgressEventArgs(percent, "Extracting files", entry.FullName));
            }
        }

        Progress?.Invoke(this, new PackageProgressEventArgs(100, "Extraction complete"));
    }

    /// <summary>
    /// Compresses files from a directory into a package archive.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output .s1pk file.</param>
    public void Compress(string sourceDirectory, string packagePath)
    {
        CompressAsync(sourceDirectory, packagePath, null).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Compresses files into a package archive with optional metadata.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output .s1pk file.</param>
    /// <param name="metadata">Package metadata to include. If null, default metadata is created.</param>
    public void Compress(string sourceDirectory, string packagePath, PackageMetadata? metadata)
    {
        CompressAsync(sourceDirectory, packagePath, metadata).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Compresses files into a package archive asynchronously.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output .s1pk file.</param>
    /// <param name="metadata">Package metadata to include. If null, default metadata is created.</param>
    public async Task CompressAsync(string sourceDirectory, string packagePath, PackageMetadata? metadata = null)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("Source directory not found: " + sourceDirectory);
        }

        // Check if there are any supported files to compress
        bool hasFiles = false;
        foreach (var ext in SupportedExtensions)
        {
            if (Directory.GetFiles(sourceDirectory, "*" + ext, SearchOption.AllDirectories).Length > 0)
            {
                hasFiles = true;
                break;
            }
        }

        if (!hasFiles)
        {
            throw new InvalidDataException(
                "Cannot create package: no supported files found in the source directory. " +
                "Supported extensions: " + string.Join(", ", SupportedExtensions));
        }

        // Try to load existing metadata.xml from source if no metadata provided
        var packageMetadata = metadata;
        if (packageMetadata == null)
        {
            string metadataPath = Path.Combine(sourceDirectory, MetadataFileName);
            if (File.Exists(metadataPath))
            {
                using (var reader = new StreamReader(metadataPath))
                {
                    packageMetadata = PackageMetadata.FromXml(await reader.ReadToEndAsync());
                }
            }
            
            // Fallback to default metadata
            packageMetadata ??= new PackageMetadata
            {
                Name = Path.GetFileNameWithoutExtension(packagePath),
                Author = Environment.UserName,
                Date = DateTime.Now.ToString("yyyy-MM-dd")
            };
        }

        string outputDir = Path.GetDirectoryName(packagePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(outputDir);

        Progress?.Invoke(this, new PackageProgressEventArgs(0, "Starting compression"));

        using (var zip = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            // Add metadata.xml
            var metadataEntry = zip.CreateEntry(MetadataFileName, CompressionLevel.Optimal);
            using (var stream = metadataEntry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(packageMetadata.ToXml());
            }
            Progress?.Invoke(this, new PackageProgressEventArgs(5, "Added metadata.xml"));

            // Count total files to compress for progress
            int totalFiles = 0;
            foreach (var ext in SupportedExtensions)
            {
                totalFiles += Directory.GetFiles(sourceDirectory, "*" + ext, SearchOption.AllDirectories).Count();
            }

            // Add all supported files
            int addedCount = 0;
            foreach (var ext in SupportedExtensions)
            {
                var files = Directory.GetFiles(sourceDirectory, "*" + ext, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string relativePath = Path.GetRelativePath(sourceDirectory, file);
                    zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                    addedCount++;
                    
                    int percent = 5 + (int)((float)addedCount / totalFiles * 95);
                    Progress?.Invoke(this, new PackageProgressEventArgs(percent, "Compressing files", relativePath));
                }
            }

            Progress?.Invoke(this, new PackageProgressEventArgs(100, "Compression complete"));
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
