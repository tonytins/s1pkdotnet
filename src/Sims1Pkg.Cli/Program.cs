using System.Reflection;

namespace Sims1Pkg.Cli;

internal static class Program
{
    private const int Success = 0;
    private const int Failure = 1;
    private const int UsageError = 2;

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage(Console.Error);
            return UsageError;
        }

        string command = args[0];
        return command switch
        {
            "pack" or "inspect" or "install" or "uninstall" or "list" => NotImplemented(command),
            "help" or "--help" or "-h" => PrintHelp(),
            "version" or "--version" => PrintVersion(),
            _ => UnknownCommand(command),
        };
    }

    private static int NotImplemented(string command)
    {
        Console.Error.WriteLine($"s1pk: '{command}' is not implemented yet.");
        return Failure;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"s1pk: unknown command '{command}'.");
        PrintUsage(Console.Error);
        return UsageError;
    }

    private static int PrintHelp()
    {
        PrintUsage(Console.Out);
        return Success;
    }

    private static int PrintVersion()
    {
        string version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";
        Console.WriteLine($"s1pk {version} (format {PackageFormat.CurrentVersion})");
        return Success;
    }

    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("""
            Usage: s1pk <command> [arguments]

            Commands:
              pack <folder> <package.s1pk>   Build a package from a folder of content
              inspect <package.s1pk>         Show a package's manifest and files
              install <package.s1pk>         Install a package into the game
              uninstall <name>               Remove an installed package
              list                           List installed packages
              help                           Show this help
              version                        Show the tool and format version
            """);
    }
}
