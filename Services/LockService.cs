using HuLoopBOT.Models;
using Microsoft.Win32;
using HuLoopBOT.Utilities;

namespace HuLoopBOT.Services;

public class LockService
{
    public OperationResult DisableLockScreen()
    {
        try
        {
            Logger.LogVerbose("Starting DisableLockScreen operation");
            var errors = new List<string>();
            var successes = new List<string>();

            // 1. Disable Lock Screen UI
            try
            {
                var lockScreenPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Personalization";
                Logger.LogVerbose($"Setting NoLockScreen in: {lockScreenPath}");
                Registry.SetValue(lockScreenPath, "NoLockScreen", 1, RegistryValueKind.DWord);
                successes.Add("Lock Screen UI disabled");
                Logger.LogVerbose("NoLockScreen = 1 set successfully");
            }
            catch (Exception ex)
            {
                errors.Add($"Lock Screen UI: {ex.Message}");
                Logger.LogError("Failed to set NoLockScreen", ex);
            }

            // 2. Disable Workstation Lock (Ctrl+Alt+Del -> Lock)
            try
            {
                var systemPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
                Logger.LogVerbose($"Disabling workstation lock in: {systemPath}");
                Registry.SetValue(systemPath, "DisableLockWorkstation", 1, RegistryValueKind.DWord);
                successes.Add("Workstation Lock disabled");
                Logger.LogVerbose("DisableLockWorkstation = 1 set successfully");
            }
            catch (Exception ex)
            {
                errors.Add($"Workstation Lock: {ex.Message}");
                Logger.LogError("Failed to set DisableLockWorkstation", ex);
            }

            // 3. Disable Lock Screen on Resume (Windows 10/11)
            try
            {
                var personalizationPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
                using (var key = Registry.LocalMachine.CreateSubKey(personalizationPath))
                {
                    if (key != null)
                    {
                        Logger.LogVerbose("Disabling lock on resume");
                        key.SetValue("NoLockScreen", 1, RegistryValueKind.DWord);
                        successes.Add("Lock on Resume disabled");
                        Logger.LogVerbose("Lock on Resume disabled successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Lock on Resume: {ex.Message}");
                Logger.LogError("Failed to disable lock on resume", ex);
            }

            // 4. Disable automatic lock after inactivity
            try
            {
                var screenSaverPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                Logger.LogVerbose("Disabling screen saver lock");
                Registry.SetValue(screenSaverPath, "ScreenSaverIsSecure", "0", RegistryValueKind.String);
                successes.Add("Screen Saver Lock disabled");
                Logger.LogVerbose("ScreenSaverIsSecure = 0 set successfully");
            }
            catch (Exception ex)
            {
                errors.Add($"Screen Saver Lock: {ex.Message}");
                Logger.LogError("Failed to disable screen saver lock", ex);
            }

            // Summary
            if (errors.Count == 0)
            {
                Logger.LogInformation("Lock screen fully disabled successfully");
                return OperationResult.Ok("Lock screen and workstation lock disabled successfully");
            }
            else if (successes.Count > 0)
            {
                Logger.LogWarning($"Lock screen partially disabled. Successes: {successes.Count}, Errors: {errors.Count}");
                return OperationResult.Ok($"Lock screen partially disabled:\n? {string.Join("\n? ", successes)}\n\n? Some operations failed:\n? {string.Join("\n? ", errors)}");
            }
            else
            {
                Logger.LogError("Failed to disable lock screen completely", null);
                return OperationResult.Fail($"Failed to disable lock screen:\n{string.Join("\n", errors)}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to disable lock screen", ex);
            return OperationResult.Fail($"Failed to disable lock screen: {ex.Message}");
        }
    }
}
