using System;
using System.IO;
using Sims1Pkg;

namespace Sims1Pkg.Cli;

/// <summary>
/// Entry point for the Sims1Pkg command-line utility.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "extract":
                case "e":
                    return HandleExtract(args);

                case "compress":
                case "c":
                    return HandleCompress(args);

                case "info":
                case "i":
                    return HandleInfo(args);

                case "help":
                case "?":
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;

                default:
                    Console.WriteLine("Unknown command: " + command);
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("Details: " + ex.InnerException.Message);
            }
            return 1;
        }
    }

    /// <summary>
    /// Handles the extract command.
    /// Usage: extract <package-path> [output-directory] [--downloads] [--force]
    /// </summary>
    private static int HandleExtract(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing package path.");
            Console.WriteLine("Usage: extract <package-path> [output-directory] [--downloads] [--force]");
            return 1;
        }

        string packagePath = args[1];
        string? outputDirectory = null;
        bool extractToDownloads = false;
        bool forceOverwrite = false;

        // Parse optional arguments
        for (int i = 2; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();
            switch (arg)
            {
                case "--downloads":
                case "-dl":
                    extractToDownloads = true;
                    break;

                case "--force":
                case "-f":
                    forceOverwrite = true;
                    break;

                default:
                    // If it's not a flag, treat it as output directory
                    if (!arg.StartsWith("-"))
                    {
                        if (outputDirectory != null)
                        {
                            Console.WriteLine("Error: Multiple output directories specified.");
                            return 1;
                        }
                        outputDirectory = arg;
                    }
                    else
                    {
                        Console.WriteLine("Warning: Unknown option: " + arg);
                    }
                    break;
            }
        }

        // If --downloads is specified, use Downloads directory
        if (extractToDownloads)
        {
            if (outputDirectory != null)
            {
                Console.WriteLine("Error: Cannot specify both --downloads and an output directory.");
                return 1;
            }

            string downloadsDir = GameDirectories.DownloadsDirectory;
            if (string.IsNullOrEmpty(downloadsDir))
            {
                Console.WriteLine("Error: Could not determine Downloads directory.");
                Console.WriteLine("Please set TS1_GAME_DIR environment variable or install The Sims 1.");
                return 1;
            }

            outputDirectory = downloadsDir;
            Console.WriteLine("Extracting to Downloads directory: " + downloadsDir);
        }
        // Default to Downloads directory if no output specified
        else if (outputDirectory == null)
        {
            string downloadsDir = GameDirectories.DownloadsDirectory;
            if (!string.IsNullOrEmpty(downloadsDir))
            {
                outputDirectory = downloadsDir;
                Console.WriteLine("Defaulting to Downloads directory: " + downloadsDir);
            }
        }
        
        // Determine if we're extracting to Downloads
        bool isDownloads = outputDirectory != null && 
                          outputDirectory.Equals(GameDirectories.DownloadsDirectory, StringComparison.OrdinalIgnoreCase);
        
        // If extracting to Downloads and not forcing, check for conflicts first
        if (outputDirectory != null && !forceOverwrite)
        {
            
            if (isDownloads || Directory.Exists(outputDirectory))
            {
                using var tempZip = System.IO.Compression.ZipFile.OpenRead(packagePath);
                foreach (var entry in tempZip.Entries)
                {
                    if (TS1Package.IsSupportedFile(entry.FullName))
                    {
                        // Skip metadata for Downloads
                        if (isDownloads && entry.FullName.Equals("metadata.xml", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        string targetPath = isDownloads 
                            ? Path.Combine(outputDirectory, Path.GetFileName(entry.FullName))
                            : Path.Combine(outputDirectory, entry.FullName);
                        
                        if (File.Exists(targetPath))
                        {
                            Console.Write("File '" + Path.GetFileName(entry.FullName) + "' already exists. Overwrite? (y/n): ");
                            var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                            if (response != "y" && response != "yes")
                            {
                                Console.WriteLine("Extraction cancelled.");
                                return 0;
                            }
                            forceOverwrite = true;
                            break;
                        }
                    }
                }
            }
        }

        var packageHandler = new TS1Package();
        packageHandler.Progress += (sender, e) => 
        {
            if (e.CurrentFile != null)
                Console.WriteLine("  " + e.Message + ": " + e.CurrentFile);
            else
                Console.WriteLine("  " + e.Message);
        };
        
        try
        {
            packageHandler.ExtractWithOptions(packagePath, outputDirectory, isDownloads, forceOverwrite);
        }
        catch (IOException ex) when (ex.Message.Contains("already exists"))
        {
            // This shouldn't happen since we already checked, but handle it just in case
            Console.WriteLine("Error: " + ex.Message);
            return 1;
        }
        
        return 0;
    }

    /// <summary>
    /// Handles the compress command.
    /// Usage: compress <source-directory> <package-path> [--name value] [--author value] [--date value] [--license value] [--sourceurl value]
    /// </summary>
    private static int HandleCompress(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Missing required arguments.");
            Console.WriteLine("Usage: compress <source-directory> <package-path> [--name value] [--author value] [--date value] [--license value] [--sourceurl value]");
            return 1;
        }

        string sourceDirectory = args[1];
        string packagePath = args[2];
        
        var metadata = ParseMetadata(args, 3);

        // Validate required metadata
        if (metadata != null)
        {
            if (string.IsNullOrEmpty(metadata.Name))
            {
                Console.WriteLine("Error: Package name is required.");
                return 1;
            }
            if (string.IsNullOrEmpty(metadata.Author))
            {
                Console.WriteLine("Error: Package author is required.");
                return 1;
            }
            if (string.IsNullOrEmpty(metadata.Date))
            {
                Console.WriteLine("Error: Package date is required.");
                return 1;
            }
            if (string.IsNullOrEmpty(metadata.License))
            {
                Console.WriteLine("Error: Package license is required.");
                return 1;
            }
            if (string.IsNullOrEmpty(metadata.SourceURL))
            {
                Console.WriteLine("Error: Package source URL is required.");
                return 1;
            }
        }

        var package = new TS1Package();
        package.Progress += (sender, e) => 
        {
            if (e.CurrentFile != null)
                Console.WriteLine("  " + e.Message + ": " + e.CurrentFile);
            else
                Console.WriteLine("  " + e.Message);
        };
        
        package.Compress(sourceDirectory, packagePath, metadata);
        
        return 0;
    }

    /// <summary>
    /// Handles the info command to display game directory information.
    /// </summary>
    private static int HandleInfo(string[] args)
    {
        Console.WriteLine("Sims 1 Directory Information:");
        Console.WriteLine("----------------------------");
        
        string gameDir = GameDirectories.GameDirectory;
        if (string.IsNullOrEmpty(gameDir))
        {
            Console.WriteLine("Game Directory: Not found");
            Console.WriteLine("Set TS1_GAME_DIR environment variable or install The Sims 1.");
        }
        else
        {
            Console.WriteLine("Game Directory: " + gameDir);
            
            // Validate by checking for Sims.exe
            string simsExe = Path.Combine(gameDir, "Sims.exe");
            if (File.Exists(simsExe))
            {
                Console.WriteLine("Status: Valid (Sims.exe found)");
            }
            else
            {
                Console.WriteLine("Status: Warning (Sims.exe not found)");
            }
        }
        
        string downloadsDir = GameDirectories.DownloadsDirectory;
        if (string.IsNullOrEmpty(downloadsDir))
        {
            Console.WriteLine("Downloads Directory: Not found");
        }
        else
        {
            Console.WriteLine("Downloads Directory: " + downloadsDir);
            if (Directory.Exists(downloadsDir))
            {
                Console.WriteLine("Status: Exists");
            }
            else
            {
                Console.WriteLine("Status: Will be created on first use");
            }
        }
        
        string gameDataDir = GameDirectories.GameDataDirectory;
        if (!string.IsNullOrEmpty(gameDataDir))
        {
            Console.WriteLine("GameData Directory: " + gameDataDir);
        }
        
        return 0;
    }

    /// <summary>
    /// Parses metadata from command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="startIndex">Index to start parsing from.</param>
    /// <returns>PackageMetadata object with parsed values.</returns>
    private static PackageMetadata? ParseMetadata(string[] args, int startIndex)
    {
        var metadata = new PackageMetadata();
        bool hasMetadata = false;

        for (int i = startIndex; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length)
            {
                Console.WriteLine("Warning: Metadata argument " + args[i] + " has no value.");
                break;
            }

            string arg = args[i].ToLowerInvariant();
            string value = args[i + 1];

            switch (arg)
            {
                case "--name":
                case "-n":
                    metadata.Name = value;
                    hasMetadata = true;
                    break;

                case "--author":
                case "-a":
                    metadata.Author = value;
                    hasMetadata = true;
                    break;

                case "--date":
                case "-d":
                    metadata.Date = value;
                    hasMetadata = true;
                    break;

                case "--description":
                    metadata.Description = value;
                    hasMetadata = true;
                    break;

                case "--version":
                    metadata.Version = value;
                    hasMetadata = true;
                    break;

                case "--license":
                    metadata.License = value;
                    hasMetadata = true;
                    break;

                case "--sourceurl":
                    metadata.SourceURL = value;
                    hasMetadata = true;
                    break;

                default:
                    Console.WriteLine("Warning: Unknown metadata argument: " + args[i]);
                    break;
            }
        }

        return hasMetadata ? metadata : null;
    }

    /// <summary>
    /// Prints usage information.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine("Sims1Pkg - Sims 1 Package File Utility");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  extract <package-path> [output-directory] [--downloads] [--force]");
        Console.WriteLine("  compress <source-directory> <package-path> [--name value] [--author value] [--date value] [--license value] [--sourceurl value]");
        Console.WriteLine("  info");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  extract, e    Extract files from a .s1pk archive");
        Console.WriteLine("                 Defaults to Downloads directory if not specified");
        Console.WriteLine("  compress, c   Compress files into a .s1pk archive");
        Console.WriteLine("  info, i       Show detected Sims 1 game directories");
        Console.WriteLine("  help, ?       Show this help message");
        Console.WriteLine();
        Console.WriteLine("Extract Options:");
        Console.WriteLine("  --downloads, -dl  Extract to Downloads directory");
        Console.WriteLine("  --force, -f      Force overwrite without prompting");
        Console.WriteLine();
        Console.WriteLine("Metadata Options (for compress):");
        Console.WriteLine("  --name, -n        Package name (required)");
        Console.WriteLine("  --author, -a      Package author (required)");
        Console.WriteLine("  --date, -d        Package date (required, format: YYYY-MM-DD)");
        Console.WriteLine("  --license         Package license (required, e.g., UNLICENSE, CC-BY-NC-SA)");
        Console.WriteLine("  --sourceurl      Source URL (required)");
        Console.WriteLine("  --version         Package version");
        Console.WriteLine("  --description     Package description");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  - Archives must contain 'metadata.xml' file");
        Console.WriteLine("  - Default extraction target: Downloads directory");
        Console.WriteLine("  - Extraction prompts before overwriting existing files");
        Console.WriteLine("  - Supported file types: .iff, .far, .cmx, .skn, .bmp, .hdb, .house, .family, .sims, .cht, .HIT, .hot, .hsm, .XA, .tds, .pal, .dat");
        Console.WriteLine("  - Format inspired by Sims2Pack");
    }
}
