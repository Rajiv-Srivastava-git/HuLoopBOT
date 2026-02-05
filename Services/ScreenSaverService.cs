using HuLoopBOT.Models;
using HuLoopBOT.Utilities;
using Microsoft.Win32;

namespace HuLoopBOT.Services;

/// <summary>
/// Service for managing screen saver settings
/// </summary>
public class ScreenSaverService
{
    public OperationResult DisableScreenSaver()
    {
        try
        {
            Logger.LogVerbose("Starting DisableScreenSaver operation");

            // Disable screen saver for all users (system-wide)
            var registryPath = @"HKEY_USERS\.DEFAULT\Control Panel\Desktop";

            Logger.LogVerbose($"Registry path: {registryPath}");
            Logger.LogVerbose("Setting ScreenSaveActive = 0");

            // Disable screen saver
            Registry.SetValue(registryPath, "ScreenSaveActive", "0", RegistryValueKind.String);

            // Set screen saver timeout to 0
            Registry.SetValue(registryPath, "ScreenSaveTimeout", "0", RegistryValueKind.String);

            // Also set for current user
            var currentUserPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
            Logger.LogVerbose($"Also configuring for current user: {currentUserPath}");

            Registry.SetValue(currentUserPath, "ScreenSaveActive", "0", RegistryValueKind.String);
            Registry.SetValue(currentUserPath, "ScreenSaveTimeout", "0", RegistryValueKind.String);

            Logger.LogInformation("Screen saver disabled successfully");
            Logger.LogVerbose("DisableScreenSaver operation completed");

            return OperationResult.Ok("Screen saver disabled");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to disable screen saver", ex);
            return OperationResult.Fail($"Failed to disable screen saver: {ex.Message}");
        }
    }

    public OperationResult DisableScreenSaverForUser(string username)
    {
        try
        {
            Logger.LogVerbose($"Starting DisableScreenSaver operation for user: {username}");

            // Try to find the user's SID and configure their registry
            var userSid = GetUserSid(username);

            if (string.IsNullOrEmpty(userSid))
            {
                Logger.LogWarning($"Could not find SID for user: {username}, using system-wide settings only");
                return DisableScreenSaver();
            }

            var registryPath = $@"HKEY_USERS\{userSid}\Control Panel\Desktop";
            Logger.LogVerbose($"User-specific registry path: {registryPath}");

            try
            {
                Registry.SetValue(registryPath, "ScreenSaveActive", "0", RegistryValueKind.String);
                Registry.SetValue(registryPath, "ScreenSaveTimeout", "0", RegistryValueKind.String);
                Logger.LogInformation($"Screen saver disabled for user: {username}");
            }
            catch
            {
                // If user-specific fails, fall back to system-wide
                Logger.LogVerbose($"Could not set user-specific settings, using system-wide");
                return DisableScreenSaver();
            }

            // Also set system-wide as backup
            DisableScreenSaver();

            return OperationResult.Ok($"Screen saver disabled for {username}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to disable screen saver for user {username}", ex);
            return OperationResult.Fail($"Failed to disable screen saver: {ex.Message}");
        }
    }

    private string? GetUserSid(string username)
    {
        try
        {
            // Remove domain prefix if present
            var cleanUsername = username.Contains('\\') ? username.Split('\\')[1] : username;

            var profileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
            using var profileList = Registry.LocalMachine.OpenSubKey(profileListPath);

            if (profileList != null)
            {
                foreach (var sidString in profileList.GetSubKeyNames())
                {
                    using var profileKey = profileList.OpenSubKey(sidString);
                    if (profileKey != null)
                    {
                        var profilePath = profileKey.GetValue("ProfileImagePath") as string;
                        if (profilePath != null && profilePath.EndsWith(cleanUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            return sidString;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogVerbose($"Error finding SID for user {username}: {ex.Message}");
        }

        return null;
    }
}
