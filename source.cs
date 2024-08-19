// Import necessary namespaces and libraries
using PluginAPI.Core; // For core API functionality
using PluginAPI.Core.Attributes; // For plugin attributes
using PluginAPI.Events; // For event handling
using MEC; // For coroutine management
using UnityEngine; // For Unity engine features
using System.Collections.Generic; // For generic collections
using Respawning; // For respawn-related features
using CommandSystem; // For command handling
using System; // For general .NET features
using System.Net.Http; // For HTTP requests
using System.IO; // For file operations
using System.Threading.Tasks; // For asynchronous tasks

// Namespace declaration for organizing code
namespace Anti_nuke_camp
{
    // Main class for the plugin
    public class Anti_nuke_camp
    {
        // Variable to track the number of times the nuke has been activated
        private int activationCount = 0;
        public static bool isWarheadLocked = false; // Flag to check if the warhead is locked
        private const string PluginVersion = "2.0.2"; // Plugin version

        // Entry point of the plugin, called when the plugin is loaded
        [PluginEntryPoint("Anti_nuke_camp", PluginVersion, "Disables nuke after 3 activations", "Joseph_fallen")]
        public void OnEnable()
        {
            // Log messages indicating the plugin has loaded
            Log.Info("Anti_nuke_camp has been loaded successfully!");
            Log.Info("This Plug-in is under heavy development");

            // Asynchronously check for and update the plugin if necessary
            Task.Run(async () => await CheckAndUpdatePlugin());

            // Register event handlers
            EventManager.RegisterEvents(this);
        }

        // Event handler for the warhead stop event
        [PluginEvent]
        public bool WarheadStopping(WarheadStopEvent args)
        {
            // Increment the activation count each time the warhead stop event is triggered
            activationCount++;

            // If the activation count is less than or equal to 3, allow stopping the warhead
            if (activationCount <= 3)
            {
                isWarheadLocked = false;
                return true; // Allow stopping the warhead
            }

            // If the activation count exceeds 3, lock the warhead
            isWarheadLocked = true;
            Log.Info("Nuke has been disabled.");
            Log.Info("Playing CASSIE Announcement");

            // Play the CASSIE message for the locked nuke
            SubtitledCassie("ALPHA WARHEAD EMERGENCY DETONATION SEQUENCE LOCKED", "Alpha Warhead Emergency Detonation Sequence Locked");

            // Start a coroutine to play the CASSIE message repeatedly every 30 seconds
            Timing.RunCoroutine(PlayCassieMessageRepeatedly());

            Log.Info("Announcements Played");
            return false; // Prevent stopping the warhead after three activations
        }

        // Coroutine to repeatedly play the CASSIE message
        private IEnumerator<float> PlayCassieMessageRepeatedly()
        {
            while (isWarheadLocked)
            {
                // Check if the nuke is detonated, and if so, stop the CASSIE messages
                if (AlphaWarheadController.InProgress && AlphaWarheadController.TimeUntilDetonation == 0f)
                {
                    isWarheadLocked = false;
                    Log.Info("Nuke detonated, stopping CASSIE alerts.");
                    yield break; // Exit the coroutine
                }

                // Play the evacuation message every 30 seconds
                SubtitledCassie("Please evacuate the facility immediately", "Please evacuate the facility immediately");
                yield return Timing.WaitForSeconds(30f); // Wait for 30 seconds without blocking the main thread
            }
        }

        // Method to play a subtitled CASSIE message
        public static void SubtitledCassie(string message, string subtitles)
        {
            string finished = $"{subtitles.Replace(' ', '_')}<size=0>{message}</size>";
            RespawnEffectsController.PlayCassieAnnouncement(finished, true, true, true);
        }

        // Event handler for the round start event
        [PluginEvent]
        public void OnRoundStart(RoundStartEvent args)
        {
            // Reset the activation count and warhead lock status at the start of each round
            activationCount = 0;
            isWarheadLocked = false;
            Log.Info("Round started, activation count reset.");
        }

        // Command to check if the nuke is locked or detonated
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class IsNukeLockedCommand : ICommand
        {
            public string Command => "nuke-status"; // Command name

            public string[] Aliases => null; // No aliases

            public string Description => "Checks if the nuke has been locked or detonated."; // Command description

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                // Determine the status of the nuke and set the response
                if (Anti_nuke_camp.isWarheadLocked)
                {
                    response = "The nuke has been locked.";
                }
                else if (AlphaWarheadController.InProgress && AlphaWarheadController.TimeUntilDetonation == 0f)
                {
                    response = "The nuke has already been detonated.";
                }
                else
                {
                    response = "The nuke is not locked and has not been detonated.";
                }
                return true; // Command executed successfully
            }
        }

        // Command to check and update the plugin if a new version is available
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class UpdatePluginCommand : ICommand
        {
            public string Command => "updateplugin"; // Command name

            public string[] Aliases => null; // No aliases

            public string Description => "Checks for a new version of the plugin and updates it if available."; // Command description

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                // Call the update method and wait for it to complete
                response = Task.Run(async () => await CheckAndUpdatePlugin()).Result;
                return true; // Command executed successfully
            }
        }

        // Command to output the current plugin version
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class PluginVersionCommand : ICommand
        {
            public string Command => "v-camp"; // Command name

            public string[] Aliases => null; // No aliases

            public string Description => "Outputs the current plugin version."; // Command description

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                // Set the response with the current plugin version
                response = $"The current plugin version is {PluginVersion}.";
                return true; // Command executed successfully
            }
        }

        // Command to output the creator and plugin version information
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class UpdatePluginVersionCommand : ICommand
        {
            public string Command => "info"; // Command name

            public string[] Aliases => null; // No aliases

            public string Description => "Outputs information about the plug-in"; // Command description

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                // Set the response with the plugin info
                response = $"Anti-camp plug-in made by Joseph_fallen for The Plague House, Current Version is {PluginVersion}, Thank you for using this Plug-in!";
                return true; // Command executed successfully
            }
        }

        // Static method to check for and update the plugin
        private static async Task<string> CheckAndUpdatePlugin()
        {
            // Define paths for the plugin directory and file
            string pluginDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "PluginAPI", "plugins");
            string pluginPath = Path.Combine(pluginDirectory, "Anti-Camp.dll");

            string downloadUrl = "https://github.com/Josephfallen/Anti-Nuke-Camp/raw/main/Anti-Camp.dll"; // URL for the latest plugin version

            using (HttpClient client = new HttpClient()) // Create HTTP client for downloading the plugin
            {
                try
                {
                    Log.Info("Checking for plugin updates...");
                    byte[] latestPluginData = await client.GetByteArrayAsync(downloadUrl); // Download the latest plugin data

                    if (latestPluginData != null && latestPluginData.Length > 0) // Check if new data is available
                    {
                        Log.Info("New plugin version found. Updating...");

                        // Backup the old plugin if it exists
                        if (File.Exists(pluginPath))
                        {
                            File.Copy(pluginPath, pluginPath + ".bak", true);
                        }

                        // Write the new plugin data to the plugin directory
                        File.WriteAllBytes(pluginPath, latestPluginData);
                        Log.Info("Plugin updated successfully. Please restart the server to apply the update.");

                        return "Plugin updated successfully. Please restart the server to apply the update.";
                    }
                    else
                    {
                        Log.Info("No new updates found.");
                        return "No new updates found.";
                    }
                }
                catch (Exception ex) // Handle any errors that occur during the update
                {
                    Log.Error($"Error updating plugin: {ex.Message}");
                    return $"Error updating plugin: {ex.Message}";
                }
            }
        }
    }
}
