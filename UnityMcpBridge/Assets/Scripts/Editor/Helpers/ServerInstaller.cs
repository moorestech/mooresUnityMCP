using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace UnityMcpBridge.Editor.Helpers
{
    public static class ServerInstaller
    {
        private const string RootFolder = "mooresUnityMCP";
        private const string ServerFolder = "UnityMcpServer";
        private const string BranchName = "master";
        private const string GitUrl = "https://github.com/moorestech/mooresUnityMCP";
        private const string PyprojectUrl =
            "https://raw.githubusercontent.com/moorestech/mooresUnityMCP/refs/heads/"
            + BranchName
            + "/UnityMcpServer/src/pyproject.toml";

        /// <summary>
        /// Ensures the unity-mcp-server is installed and up to date.
        /// </summary>
        public static void EnsureServerInstalled()
        {
            try
            {
                string saveLocation = GetSaveLocation();

                if (!IsServerInstalled(saveLocation))
                {
                    InstallServer(saveLocation);
                }
                else
                {
                    string installedVersion = GetInstalledVersion();
                    string latestVersion = GetLatestVersion();

                    if (IsNewerVersion(latestVersion, installedVersion))
                    {
                        UpdateServer(saveLocation);
                    }
                    else { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to ensure server installation: {ex.Message}");
            }
        }

        public static string GetServerPath()
        {
            return Path.Combine(GetSaveLocation(), ServerFolder, "src");
        }

        /// <summary>
        /// Gets the platform-specific save location for the server.
        /// </summary>
        private static string GetSaveLocation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData",
                    "Local",
                    "Programs",
                    RootFolder
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "bin",
                    RootFolder
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string path = "/usr/local/bin";
                return !Directory.Exists(path) || !IsDirectoryWritable(path)
                    ? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Applications",
                        RootFolder
                    )
                    : Path.Combine(path, RootFolder);
            }
            throw new Exception("Unsupported operating system.");
        }

        private static bool IsDirectoryWritable(string path)
        {
            try
            {
                File.Create(Path.Combine(path, "test.txt")).Dispose();
                File.Delete(Path.Combine(path, "test.txt"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the server is installed at the specified location.
        /// </summary>
        private static bool IsServerInstalled(string location)
        {
            return Directory.Exists(location)
                && File.Exists(Path.Combine(location, ServerFolder, "src", "pyproject.toml"));
        }

        /// <summary>
        /// Installs the server by cloning only the UnityMcpServer folder from the repository and setting up dependencies.
        /// </summary>
        private static void InstallServer(string location)
        {
            // Create the src directory where the server code will reside
            Directory.CreateDirectory(location);

            // Initialize git repo in the src directory
            RunCommand("git", $"init", workingDirectory: location);

            // Add remote
            RunCommand("git", $"remote add origin {GitUrl}", workingDirectory: location);

            // Configure sparse checkout
            RunCommand("git", "config core.sparseCheckout true", workingDirectory: location);

            // Set sparse checkout path to only include UnityMcpServer folder
            string sparseCheckoutPath = Path.Combine(location, ".git", "info", "sparse-checkout");
            File.WriteAllText(sparseCheckoutPath, $"{ServerFolder}/");

            // Fetch and checkout the branch
            RunCommand("git", $"fetch --depth=1 origin {BranchName}", workingDirectory: location);
            RunCommand("git", $"checkout {BranchName}", workingDirectory: location);
        }

        /// <summary>
        /// Fetches the currently installed version from the local pyproject.toml file.
        /// </summary>
        public static string GetInstalledVersion()
        {
            string pyprojectPath = Path.Combine(
                GetSaveLocation(),
                ServerFolder,
                "src",
                "pyproject.toml"
            );
            return ParseVersionFromPyproject(File.ReadAllText(pyprojectPath));
        }

        /// <summary>
        /// Fetches the latest version from the GitHub pyproject.toml file.
        /// </summary>
        public static string GetLatestVersion()
        {
            using WebClient webClient = new();
            string pyprojectContent = webClient.DownloadString(PyprojectUrl);
            return ParseVersionFromPyproject(pyprojectContent);
        }

        /// <summary>
        /// Updates the server by pulling the latest changes for the UnityMcpServer folder only.
        /// </summary>
        private static void UpdateServer(string location)
        {
            RunCommand("git", $"pull origin {BranchName}", workingDirectory: location);
        }
        
        /// <summary>
        /// Forces a git pull to update the server regardless of version check.
        /// </summary>
        public static void ForceUpdateServer()
        {
            try
            {
                string saveLocation = GetSaveLocation();
                
                if (!IsServerInstalled(saveLocation))
                {
                    Debug.Log("[ServerInstaller] Server not installed. Installing...");
                    InstallServer(saveLocation);
                }
                else
                {
                    Debug.Log("[ServerInstaller] Forcing git pull to update server...");
                    
                    // Reset any local changes to avoid conflicts
                    try
                    {
                        RunCommand("git", "reset --hard HEAD", workingDirectory: saveLocation);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ServerInstaller] Failed to reset: {e.Message}");
                    }
                    
                    // Pull latest changes
                    UpdateServer(saveLocation);
                    Debug.Log("[ServerInstaller] Server updated successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerInstaller] Failed to force update server: {e.Message}");
            }
        }
        
        /// <summary>
        /// Syncs the local UnityMcpServer directory to the Applications folder using rsync
        /// </summary>
        public static void SyncLocalServerToApplications()
        {
            try
            {
                // Get the actual project root (not UnityMcpBridge subdirectory)
                string unityProjectRoot = Directory.GetParent(Application.dataPath).FullName;
                string projectRoot = Directory.GetParent(unityProjectRoot).FullName;
                string localServerPath = Path.Combine(projectRoot, ServerFolder);
                
                Debug.Log($"[ServerInstaller] Application.dataPath: {Application.dataPath}");
                Debug.Log($"[ServerInstaller] unityProjectRoot: {unityProjectRoot}");
                Debug.Log($"[ServerInstaller] projectRoot: {projectRoot}");
                Debug.Log($"[ServerInstaller] localServerPath: {localServerPath}");
                
                if (!Directory.Exists(localServerPath))
                {
                    Debug.LogError($"[ServerInstaller] Local server not found at: {localServerPath}");
                    return;
                }
                
                string saveLocation = GetSaveLocation();
                string destinationPath = Path.Combine(saveLocation, ServerFolder);
                
                Debug.Log($"[ServerInstaller] Syncing local server from {localServerPath} to {destinationPath}");
                
                // Create parent directory if it doesn't exist
                if (!Directory.Exists(saveLocation))
                {
                    Directory.CreateDirectory(saveLocation);
                }
                
                // Use rsync for efficient synchronization (macOS/Linux)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // rsync with delete to ensure destination matches source exactly
                    // Exclude __pycache__, .git, .uv, .venv, and other unnecessary files
                    string rsyncArgs = $"-av --delete --exclude='__pycache__' --exclude='.git' --exclude='.uv' --exclude='.venv' --exclude='*.pyc' \"{localServerPath}/\" \"{destinationPath}/\"";
                    RunCommand("rsync", rsyncArgs);
                    Debug.Log("[ServerInstaller] Successfully synced local server using rsync");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // For Windows, use robocopy
                    string robocopyArgs = $"\"{localServerPath}\" \"{destinationPath}\" /MIR /XD __pycache__ .git .uv .venv /XF *.pyc";
                    try
                    {
                        RunCommand("robocopy", robocopyArgs);
                    }
                    catch (Exception e)
                    {
                        // Robocopy returns non-zero exit codes for success with warnings
                        if (!e.Message.Contains("exit code 1") && !e.Message.Contains("exit code 2"))
                        {
                            throw;
                        }
                    }
                    Debug.Log("[ServerInstaller] Successfully synced local server using robocopy");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerInstaller] Failed to sync local server: {e.Message}");
            }
        }

        /// <summary>
        /// Parses the version number from pyproject.toml content.
        /// </summary>
        private static string ParseVersionFromPyproject(string content)
        {
            foreach (string line in content.Split('\n'))
            {
                if (line.Trim().StartsWith("version ="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        return parts[1].Trim().Trim('"');
                    }
                }
            }
            throw new Exception("Version not found in pyproject.toml");
        }

        /// <summary>
        /// Compares two version strings to determine if the latest is newer.
        /// </summary>
        public static bool IsNewerVersion(string latest, string installed)
        {
            int[] latestParts = latest.Split('.').Select(int.Parse).ToArray();
            int[] installedParts = installed.Split('.').Select(int.Parse).ToArray();
            for (int i = 0; i < Math.Min(latestParts.Length, installedParts.Length); i++)
            {
                if (latestParts[i] > installedParts[i])
                {
                    return true;
                }

                if (latestParts[i] < installedParts[i])
                {
                    return false;
                }
            }
            return latestParts.Length > installedParts.Length;
        }

        /// <summary>
        /// Runs a command-line process and handles output/errors.
        /// </summary>
        private static void RunCommand(
            string command,
            string arguments,
            string workingDirectory = null
        )
        {
            System.Diagnostics.Process process = new()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory ?? string.Empty,
                },
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception(
                    $"Command failed: {command} {arguments}\nOutput: {output}\nError: {error}"
                );
            }
        }
    }
}
