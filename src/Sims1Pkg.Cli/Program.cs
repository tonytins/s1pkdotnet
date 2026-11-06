using System;
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
    /// Usage: extract <package-path> [output-directory]
    /// </summary>
    private static int HandleExtract(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing package path.");
            Console.WriteLine("Usage: extract <package-path> [output-directory]");
            return 1;
        }

        string packagePath = args[1];
        string? outputDirectory = args.Length > 2 ? args[2] : null;

        var package = new TS1Package();
        package.Progress += (sender, e) => 
        {
            if (e.CurrentFile != null)
                Console.WriteLine("  " + e.Message + ": " + e.CurrentFile);
            else
                Console.WriteLine("  " + e.Message);
        };
        
        package.Extract(packagePath, outputDirectory);
        
        return 0;
    }

    /// <summary>
    /// Handles the compress command.
    /// Usage: compress <source-directory> <package-path> [--name value] [--author value] [--date value]
    /// </summary>
    private static int HandleCompress(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Missing required arguments.");
            Console.WriteLine("Usage: compress <source-directory> <package-path> [--name value] [--author value] [--date value]");
            return 1;
        }

        string sourceDirectory = args[1];
        string packagePath = args[2];
        
        var metadata = ParseMetadata(args, 3);

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
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  extract <package-path> [output-directory]");
        Console.WriteLine("  compress <source-directory> <package-path> [--name value] [--author value] [--date value]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  extract, e    Extract files from a .s1pk archive");
        Console.WriteLine("  compress, c   Compress files into a .s1pk archive");
        Console.WriteLine("  help, ?       Show this help message");
        Console.WriteLine();
        Console.WriteLine("Metadata Options (for compress):");
        Console.WriteLine("  --name, -n    Package name");
        Console.WriteLine("  --author, -a  Package author");
        Console.WriteLine("  --date, -d    Package date");
        Console.WriteLine("  --version     Package version");
        Console.WriteLine("  --description Package description");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  - Archives must contain 'metadata.xml' file");
        Console.WriteLine("  - Supported file types: .iff, .far, .cmx, .skn, .bmp, .hdb, .house, .family, .sims, .cht, .HIT, .hot, .hsm, .XA, .tds, .pal, .dat");
        Console.WriteLine("  - Format inspired by Sims2Pack");
    }
}
