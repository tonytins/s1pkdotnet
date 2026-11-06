using System;
using System.IO;

namespace Sims1Pkg;

/// <summary>
/// Provides methods for discovering Sims 1 game directories.
/// Inspired by Sims2Pack's directory discovery approach.
/// </summary>
public static class GameDirectories
{
    private static string? _gameDirectory;
    private static string? _downloadsDirectory;

    /// <summary>
    /// Gets the Sims 1 game installation directory.
    /// On Windows: Checks registry, then default Program Files path.
    /// On other platforms: Checks TS1_GAME_DIR environment variable.
    /// </summary>
    public static string GameDirectory
    {
        get
        {
            if (_gameDirectory != null)
                return _gameDirectory;

            // Try TS1_GAME_DIR environment variable first (cross-platform)
            string? envDir = Environment.GetEnvironmentVariable("TS1_GAME_DIR");
            if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
            {
                _gameDirectory = envDir;
                return _gameDirectory;
            }

            // Windows-specific registry lookup
            if (OperatingSystem.IsWindows())
            {
                string? regPath = GetWindowsInstallPath();
                if (!string.IsNullOrEmpty(regPath) && Directory.Exists(regPath))
                {
                    _gameDirectory = regPath;
                    return _gameDirectory;
                }

                // Default Windows paths
                string[] defaultPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Maxis", "The Sims"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Maxis", "The Sims"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "The Sims"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "The Sims"),
                };

                foreach (string path in defaultPaths)
                {
                    if (Directory.Exists(path))
                    {
                        _gameDirectory = path;
                        return _gameDirectory;
                    }
                }
            }

            // Last resort: return empty string
            _gameDirectory = string.Empty;
            return _gameDirectory;
        }
        set => _gameDirectory = value;
    }

    /// <summary>
    /// Gets the Sims 1 Downloads directory (where custom content goes).
    /// This is typically GameDirectory/Downloads or UserFolder/EA Games/The Sims/Downloads.
    /// </summary>
    public static string DownloadsDirectory
    {
        get
        {
            if (_downloadsDirectory != null)
                return _downloadsDirectory;

            // If game directory is set, check for Downloads subdirectory
            if (!string.IsNullOrEmpty(GameDirectory))
            {
                string downloadsPath = Path.Combine(GameDirectory, "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    _downloadsDirectory = downloadsPath;
                    return _downloadsDirectory;
                }
            }

            // On Windows, also check user documents folder
            if (OperatingSystem.IsWindows())
            {
                string userDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possiblePaths = {
                    Path.Combine(userDocsPath, "EA Games", "The Sims", "Downloads"),
                    Path.Combine(userDocsPath, "My EA Games", "The Sims", "Downloads"),
                    Path.Combine(userDocsPath, "The Sims", "Downloads"),
                };

                foreach (string path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        _downloadsDirectory = path;
                        return _downloadsDirectory;
                    }
                }
            }

            // If TS1_GAME_DIR is set but Downloads doesn't exist, create it
            if (!string.IsNullOrEmpty(GameDirectory))
            {
                string downloadsPath = Path.Combine(GameDirectory, "Downloads");
                Directory.CreateDirectory(downloadsPath);
                _downloadsDirectory = downloadsPath;
                return _downloadsDirectory;
            }

            // Last resort: return empty string
            _downloadsDirectory = string.Empty;
            return _downloadsDirectory;
        }
        set => _downloadsDirectory = value;
    }

    /// <summary>
    /// Gets the GameData directory (contains core game resources).
    /// </summary>
    public static string GameDataDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(GameDirectory))
                return string.Empty;
            
            string gameDataPath = Path.Combine(GameDirectory, "GameData");
            if (Directory.Exists(gameDataPath))
                return gameDataPath;
            
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if Sims 1 appears to be installed on this system.
    /// </summary>
    public static bool IsInstalled => !string.IsNullOrEmpty(GameDirectory);

    /// <summary>
    /// Attempts to get the Sims 1 install path from Windows registry.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? GetWindowsInstallPath()
    {
        try
        {
            // Try The Sims Complete Collection
            const string completeCollectionKey = "SOFTWARE\\Maxis\\The Sims Complete Collection";
            
            string[] registryPaths = {
                completeCollectionKey,
                "SOFTWARE\\Wow6432Node\\Maxis\\The Sims Complete Collection",
            };

            foreach (string regPath in registryPaths)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    string? installPath = key.GetValue("AppPaths") as string;
                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        return installPath;
                    
                    installPath = key.GetValue("Install_Dir") as string;
                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        return installPath;
                }
            }

            // Try EA Games registry keys
            string[] eaRegistryPaths = {
                "SOFTWARE\\EA GAMES\\The Sims",
                "SOFTWARE\\Wow6432Node\\EA GAMES\\The Sims",
            };

            foreach (string regPath in eaRegistryPaths)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    string? installPath = key.GetValue("Install_Dir") as string;
                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                        return installPath;
                }
            }
        }
        catch
        {
            // Registry access can fail for various reasons; ignore and continue
        }

        return null;
    }

    /// <summary>
    /// Resets the cached directory paths (useful for testing).
    /// </summary>
    public static void Reset()
    {
        _gameDirectory = null;
        _downloadsDirectory = null;
    }
}
