using HuLoopBOT.Utilities;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

namespace HuLoopBOT.Services;

/// <summary>
/// Service to verify and diagnose system configuration status
/// </summary>
public class SystemVerificationService
{
    public class VerificationResult
    {
        public string Setting { get; set; } = "";
        public bool IsConfigured { get; set; }
        public string Status { get; set; } = "";
        public string Details { get; set; } = "";
    }

    /// <summary>
    /// Verify all machine readiness settings for a specific user
    /// </summary>
    /// <param name="username">Username to verify (can include domain: DOMAIN\Username)</param>
    public List<VerificationResult> VerifyAllSettings(string? username = null)
    {
        var results = new List<VerificationResult>();

        results.Add(VerifyAutoLogin(username));
        results.Add(VerifyLockScreen());
        results.Add(VerifyScreenSaver(username));
        results.Add(VerifySleep());
        results.Add(VerifyRdpTimeouts());
        results.Add(VerifyRdpMonitoringService());

        return results;
    }

    /// <summary>
    /// Verify Auto Login configuration for specific user
    /// </summary>
    /// <param name="expectedUsername">Username that should be configured for auto login</param>
    public VerificationResult VerifyAutoLogin(string? expectedUsername = null)
    {
        try
        {
            var winlogonPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

            using (var key = Registry.LocalMachine.OpenSubKey(winlogonPath))
            {
                if (key == null)
                {
                    return new VerificationResult
                    {
                        Setting = "Auto Login",
                        IsConfigured = false,
                        Status = "ERROR",
                        Details = "Cannot access Winlogon registry key"
                    };
                }

                var autoAdminLogon = key.GetValue("AutoAdminLogon") as string;
                var username = key.GetValue("DefaultUserName") as string;
                var domain = key.GetValue("DefaultDomainName") as string;
                var password = key.GetValue("DefaultPassword") as string;

                Logger.LogVerbose($"Auto Login Check - AutoAdminLogon: '{autoAdminLogon}', Username: '{username}', Domain: '{domain}', HasPassword: {!string.IsNullOrEmpty(password)}");

                bool isEnabled = autoAdminLogon == "1" &&
                     !string.IsNullOrEmpty(username) &&
                      !string.IsNullOrEmpty(password);

                // If a specific user was requested, verify it matches
                if (!string.IsNullOrEmpty(expectedUsername) && isEnabled)
                {
                    string configuredUser = string.IsNullOrEmpty(domain) ? username : $"{domain}\\{username}";
                    string cleanExpectedUser = expectedUsername;

                    // Parse expected username
                    if (expectedUsername.Contains('\\'))
                    {
                        var parts = expectedUsername.Split('\\');
                        cleanExpectedUser = parts[1];
                    }

                    // Compare usernames (case-insensitive)
                    bool userMatches = username?.Equals(cleanExpectedUser, StringComparison.OrdinalIgnoreCase) ?? false;

                    if (!userMatches)
                    {
                        return new VerificationResult
                        {
                            Setting = "Auto Login",
                            IsConfigured = false,
                            Status = "WRONG USER",
                            Details = $"Expected: {expectedUsername}, Configured: {configuredUser}"
                        };
                    }

                    Logger.LogVerbose($"Auto login configured for correct user: {configuredUser}");
                }

                return new VerificationResult
                {
                    Setting = "Auto Login",
                    IsConfigured = isEnabled,
                    Status = isEnabled ? "ENABLED" : "DISABLED",
                    Details = isEnabled ? $"User: {domain}\\{username}" : $"AutoAdminLogon='{autoAdminLogon}', Username='{username}'"
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error verifying auto login", ex);
            return new VerificationResult
            {
                Setting = "Auto Login",
                IsConfigured = false,
                Status = "ERROR",
                Details = ex.Message
            };
        }
    }

    /// <summary>
    /// Verify Lock Screen configuration
    /// </summary>
    public VerificationResult VerifyLockScreen()
    {
        try
        {
            var results = new List<string>();
            bool noLockScreenSet = false;
            bool disableLockSet = false;

            // Check NoLockScreen
            var personalizationPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
            using (var key = Registry.LocalMachine.OpenSubKey(personalizationPath))
            {
                var noLockScreen = key?.GetValue("NoLockScreen");
                noLockScreenSet = noLockScreen?.ToString() == "1";
                results.Add($"NoLockScreen={noLockScreen?.ToString() ?? "not set"}");
            }

            // Check DisableLockWorkstation
            var systemPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
            using (var key = Registry.LocalMachine.OpenSubKey(systemPath))
            {
                var disableLock = key?.GetValue("DisableLockWorkstation");
                disableLockSet = disableLock?.ToString() == "1";
                results.Add($"DisableLockWorkstation={disableLock?.ToString() ?? "not set"}");
            }

            bool isConfigured = noLockScreenSet && disableLockSet;

            return new VerificationResult
            {
                Setting = "Lock Screen",
                IsConfigured = isConfigured,
                Status = isConfigured ? "DISABLED" : "PARTIALLY DISABLED",
                Details = string.Join(", ", results)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Error verifying lock screen", ex);
            return new VerificationResult
            {
                Setting = "Lock Screen",
                IsConfigured = false,
                Status = "ERROR",
                Details = ex.Message
            };
        }
    }

    /// <summary>
    /// Verify Screen Saver configuration for specific user
    /// </summary>
    /// <param name="username">Username to verify (can include domain: DOMAIN\Username)</param>
    public VerificationResult VerifyScreenSaver(string? username = null)
    {
        try
        {
            RegistryKey? key = null;
            string userContext = "Current User";

            if (!string.IsNullOrEmpty(username))
            {
                // Try to get the user's SID and check their registry
                var userSid = GetUserSid(username);
                if (!string.IsNullOrEmpty(userSid))
                {
                    try
                    {
                        var userDesktopPath = $@"{userSid}\Control Panel\Desktop";
                        key = Registry.Users.OpenSubKey(userDesktopPath);
                        userContext = username;
                        Logger.LogVerbose($"Checking screen saver for user: {username} (SID: {userSid})");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogVerbose($"Could not access user-specific registry, falling back to current user: {ex.Message}");
                    }
                }
            }

            // Fall back to current user if user-specific check failed
            if (key == null)
            {
                var desktopPath = @"Control Panel\Desktop";
                key = Registry.CurrentUser.OpenSubKey(desktopPath);
                userContext = "Current User (fallback)";
            }

            using (key)
            {
                if (key == null)
                {
                    return new VerificationResult
                    {
                        Setting = "Screen Saver",
                        IsConfigured = false,
                        Status = "ERROR",
                        Details = $"Cannot access Desktop registry key for {userContext}"
                    };
                }

                var screenSaveActive = key.GetValue("ScreenSaveActive") as string;
                var screenSaveTimeout = key.GetValue("ScreenSaveTimeout") as string;
                var screenSaverIsSecure = key.GetValue("ScreenSaverIsSecure") as string;

                bool isDisabled = screenSaveActive == "0" || screenSaveTimeout == "0";

                return new VerificationResult
                {
                    Setting = "Screen Saver",
                    IsConfigured = isDisabled,
                    Status = isDisabled ? "DISABLED" : "ENABLED",
                    Details = $"User: {userContext}, Active={screenSaveActive}, Timeout={screenSaveTimeout}, Secure={screenSaverIsSecure}"
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error verifying screen saver", ex);
            return new VerificationResult
            {
                Setting = "Screen Saver",
                IsConfigured = false,
                Status = "ERROR",
                Details = ex.Message
            };
        }
    }

    /// <summary>
    /// Verify Sleep and Hibernate configuration
    /// </summary>
    public VerificationResult VerifySleep()
    {
        try
        {
          Logger.LogVerbose("Querying power settings using powercfg /query");

         // Get active power scheme GUID
  var activeSchemeGuid = GetActivePowerScheme();
   if (string.IsNullOrEmpty(activeSchemeGuid))
        {
         return new VerificationResult
    {
        Setting = "Sleep & Hibernate",
       IsConfigured = false,
   Status = "ERROR",
             Details = "Unable to determine active power scheme"
   };
}

            Logger.LogVerbose($"Active power scheme: {activeSchemeGuid}");

       // Query the active scheme for sleep/hibernate settings
  var startInfo = new ProcessStartInfo
 {
     FileName = "powercfg",
        Arguments = $"/query {activeSchemeGuid}",
      RedirectStandardOutput = true,
  UseShellExecute = false,
      CreateNoWindow = true
            };

      using var process = Process.Start(startInfo);
    if (process == null)
            {
       return new VerificationResult
    {
    Setting = "Sleep & Hibernate",
        IsConfigured = false,
  Status = "ERROR",
          Details = "Failed to start powercfg process"
              };
      }

     var output = process.StandardOutput.ReadToEnd();
       process.WaitForExit();

       // Parse the output for sleep and hibernate settings
            var powerSettings = ParsePowerSettings(output);

            // Check if both AC and DC settings are disabled (set to 0)
            bool sleepDisabled = powerSettings.AcSleepTimeout == 0 && powerSettings.DcSleepTimeout == 0;
        bool hibernateDisabled = powerSettings.AcHibernateTimeout == 0 && powerSettings.DcHibernateTimeout == 0;
     bool isConfigured = sleepDisabled && hibernateDisabled;

            // Build detailed status
     var statusParts = new List<string>();
            
         if (sleepDisabled && hibernateDisabled)
            {
  statusParts.Add("DISABLED");
      }
      else if (!sleepDisabled && !hibernateDisabled)
{
    statusParts.Add("ENABLED");
}
            else
  {
     statusParts.Add("PARTIALLY DISABLED");
            }

        // Build details
            var details = new List<string>();
    details.Add($"Sleep: AC={FormatTimeout(powerSettings.AcSleepTimeout)}, DC={FormatTimeout(powerSettings.DcSleepTimeout)}");
            details.Add($"Hibernate: AC={FormatTimeout(powerSettings.AcHibernateTimeout)}, DC={FormatTimeout(powerSettings.DcHibernateTimeout)}");

            Logger.LogVerbose($"Sleep verification - Sleep disabled: {sleepDisabled}, Hibernate disabled: {hibernateDisabled}");

        return new VerificationResult
            {
   Setting = "Sleep & Hibernate",
           IsConfigured = isConfigured,
           Status = string.Join(", ", statusParts),
      Details = string.Join(" | ", details)
       };
        }
        catch (Exception ex)
        {
         Logger.LogError("Error verifying sleep settings", ex);
        return new VerificationResult
    {
        Setting = "Sleep & Hibernate",
          IsConfigured = false,
            Status = "ERROR",
    Details = $"Failed to query power settings: {ex.Message}"
    };
 }
    }

    /// <summary>
    /// Get the GUID of the active power scheme
    /// </summary>
    private string? GetActivePowerScheme()
    {
try
        {
var startInfo = new ProcessStartInfo
        {
     FileName = "powercfg",
          Arguments = "/getactivescheme",
  RedirectStandardOutput = true,
          UseShellExecute = false,
            CreateNoWindow = true
    };

   using var process = Process.Start(startInfo);
       if (process != null)
      {
              var output = process.StandardOutput.ReadToEnd();
       process.WaitForExit();

  // Output format: "Power Scheme GUID: {GUID}  (Name)"
// Extract GUID between the colons and parenthesis
        var match = System.Text.RegularExpressions.Regex.Match(output, @":\s*([a-fA-F0-9\-]+)");
     if (match.Success)
    {
          return match.Groups[1].Value;
    }
 }
      }
        catch (Exception ex)
        {
            Logger.LogVerbose($"Error getting active power scheme: {ex.Message}");
        }

 return null;
    }

    /// <summary>
    /// Parse power settings from powercfg output
    /// </summary>
    private PowerSettings ParsePowerSettings(string output)
    {
   var settings = new PowerSettings();
        var lines = output.Split('\n');
    
        bool inSleepSection = false;
        bool inHibernateSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

         // Detect Sleep After section
         if (trimmedLine.Contains("Sleep after") || trimmedLine.Contains("Subgroup GUID: 238c9fa8-0aad-41ed-83f4-97be242c8f20"))
  {
                inSleepSection = true;
                inHibernateSection = false;
  continue;
            }

            // Detect Hibernate After section
      if (trimmedLine.Contains("Hibernate after") || trimmedLine.Contains("Subgroup GUID: 9d7815a6-7ee4-497e-8888-515a05f02364"))
       {
          inHibernateSection = true;
       inSleepSection = false;
       continue;
  }

        // Reset section flags when we hit a new subgroup
    if (trimmedLine.StartsWith("Subgroup GUID:") && 
                !trimmedLine.Contains("238c9fa8-0aad-41ed-83f4-97be242c8f20") && 
          !trimmedLine.Contains("9d7815a6-7ee4-497e-8888-515a05f02364"))
     {
        inSleepSection = false;
          inHibernateSection = false;
            }

      // Parse AC (plugged in) settings
    if (trimmedLine.Contains("Current AC Power Setting Index:"))
       {
   var value = ExtractHexValue(trimmedLine);
                if (inSleepSection)
  {
    settings.AcSleepTimeout = value;
    Logger.LogVerbose($"Found AC Sleep timeout: {value} seconds");
              }
                else if (inHibernateSection)
    {
         settings.AcHibernateTimeout = value;
        Logger.LogVerbose($"Found AC Hibernate timeout: {value} seconds");
       }
 }

            // Parse DC (battery) settings
            if (trimmedLine.Contains("Current DC Power Setting Index:"))
    {
         var value = ExtractHexValue(trimmedLine);
     if (inSleepSection)
      {
            settings.DcSleepTimeout = value;
           Logger.LogVerbose($"Found DC Sleep timeout: {value} seconds");
}
              else if (inHibernateSection)
   {
 settings.DcHibernateTimeout = value;
      Logger.LogVerbose($"Found DC Hibernate timeout: {value} seconds");
     }
       }
        }

     return settings;
    }

    /// <summary>
    /// Extract hex value from powercfg output line and convert to seconds
    /// </summary>
    private int ExtractHexValue(string line)
    {
    try
      {
            // Format: "Current AC Power Setting Index: 0x00000000" or "0x00000258" etc.
      var parts = line.Split(':');
            if (parts.Length > 1)
    {
     var hexValue = parts[1].Trim();
     // Remove "0x" prefix if present
    hexValue = hexValue.Replace("0x", "").Trim();
   
            // Convert hex to decimal (value is in seconds)
      if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int seconds))
      {
         return seconds;
   }
          }
        }
        catch (Exception ex)
      {
    Logger.LogVerbose($"Error extracting hex value from line '{line}': {ex.Message}");
        }

     return -1; // Return -1 to indicate parsing error
  }

    /// <summary>
    /// Format timeout value for display
    /// </summary>
    private string FormatTimeout(int seconds)
    {
  if (seconds < 0)
        {
       return "Unknown";
        }
        else if (seconds == 0)
        {
    return "Disabled";
        }
    else if (seconds < 60)
   {
            return $"{seconds}s";
        }
        else if (seconds < 3600)
        {
            int minutes = seconds / 60;
            return $"{minutes}m";
        }
    else
        {
         int hours = seconds / 3600;
   int minutes = (seconds % 3600) / 60;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
     }
    }

    /// <summary>
    /// Power settings data structure
    /// </summary>
    private class PowerSettings
    {
        public int AcSleepTimeout { get; set; } = -1;
        public int DcSleepTimeout { get; set; } = -1;
        public int AcHibernateTimeout { get; set; } = -1;
        public int DcHibernateTimeout { get; set; } = -1;
    }

    /// <summary>
    /// Verify RDP Timeout configuration
    /// </summary>
    public VerificationResult VerifyRdpTimeouts()
    {
        try
        {
      var terminalServicesPath = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
            using (var key = Registry.LocalMachine.OpenSubKey(terminalServicesPath))
            {
     if (key == null)
           {
           return new VerificationResult
          {
   Setting = "RDP Timeouts",
               IsConfigured = false,
Status = "NOT CONFIGURED",
      Details = "Terminal Services policy key does not exist"
       };
          }

              var maxIdleTime = key.GetValue("MaxIdleTime");
     var maxDisconnectionTime = key.GetValue("MaxDisconnectionTime");
   var maxConnectionTime = key.GetValue("MaxConnectionTime");

      bool isDisabled = (maxIdleTime?.ToString() == "0") &&
   (maxDisconnectionTime?.ToString() == "0") &&
   (maxConnectionTime?.ToString() == "0");

            return new VerificationResult
                {
      Setting = "RDP Timeouts",
             IsConfigured = isDisabled,
       Status = isDisabled ? "DISABLED" : "ENABLED",
        Details = $"Idle={maxIdleTime}, Disconnected={maxDisconnectionTime}, Connection={maxConnectionTime}"
};
        }
        }
 catch (Exception ex)
        {
       Logger.LogError("Error verifying RDP timeouts", ex);
            return new VerificationResult
            {
           Setting = "RDP Timeouts",
        IsConfigured = false,
    Status = "ERROR",
      Details = ex.Message
            };
   }
    }

    /// <summary>
    /// Verify RDP Monitoring Service configuration and status
  /// </summary>
    public VerificationResult VerifyRdpMonitoringService()
    {
        try
        {
            bool isInstalled = RdpMonitoringService.IsServiceInstalled();
            bool isRunning = RdpMonitoringService.IsServiceRunning();
       bool isEnabled = false;

            // Check registry setting
  try
     {
       var registryPath = @"SOFTWARE\HuLoopBOT";
                using var key = Registry.LocalMachine.OpenSubKey(registryPath);
        if (key != null)
            {
var enabled = key.GetValue("RdpMonitoringEnabled");
   isEnabled = enabled?.ToString() == "1";
        }
            }
 catch
            {
          // Registry key doesn't exist
            }

         string status;
            string details;
            bool isConfigured;

     if (!isInstalled)
 {
                status = "NOT INSTALLED";
                details = "RDP Monitoring Service is not installed. Use 'Enable Transfer Session' button to install.";
              isConfigured = false;
            }
            else if (!isRunning)
            {
     status = "INSTALLED BUT STOPPED";
           details = $"Service is installed but not running. Registry Enabled={isEnabled}. Use 'Enable Transfer Session' to start.";
                isConfigured = false;
            }
            else if (!isEnabled)
            {
           status = "RUNNING BUT DISABLED";
           details = "Service is running but disabled in registry. May not activate on reboot.";
                isConfigured = false;
            }
      else
 {
          status = "ACTIVE";
      details = "Service is installed, running, and enabled. RDP sessions will be auto-transferred on disconnect.";
        isConfigured = true;
            }

            return new VerificationResult
            {
    Setting = "RDP Session Monitor",
     IsConfigured = isConfigured,
       Status = status,
         Details = details
            };
      }
        catch (Exception ex)
        {
    Logger.LogError("Error verifying RDP monitoring service", ex);
            return new VerificationResult
         {
                Setting = "RDP Session Monitor",
       IsConfigured = false,
       Status = "ERROR",
   Details = $"Failed to check service status: {ex.Message}"
          };
        }
    }

    /// <summary>
 /// Get user SID from username
    /// </summary>
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
       Logger.LogVerbose($"Found SID for user {username}: {sidString}");
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

    /// <summary>
    /// Generate detailed report for specific user
 /// </summary>
    /// <param name="username">Username to verify (optional)</param>
    public string GenerateReport(string? username = null)
    {
        var results = VerifyAllSettings(username);
        var report = "===== SYSTEM CONFIGURATION VERIFICATION =====\n";

        if (!string.IsNullOrEmpty(username))
        {
            report += $"User: {username}\n";
   }

        report += "\n";

        foreach (var result in results)
        {
          var icon = result.IsConfigured ? "✓" : "✗";
            var statusColor = result.IsConfigured ? "OK" : "NEEDS ATTENTION";

            report += $"{icon} {result.Setting}\n";
            report += $"   Status: {result.Status}\n";
   report += $"   Details: {result.Details}\n\n";
        }

        var successCount = results.Count(r => r.IsConfigured);
        var totalCount = results.Count;

 report += $"===== SUMMARY =====\n";
        report += $"Configured: {successCount}/{totalCount}\n";
        report += $"Completion: {(successCount * 100 / totalCount)}%\n";

        return report;
    }
}
