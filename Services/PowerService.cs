using HuLoopBOT.Models;
using HuLoopBOT.Utilities;
using System.Diagnostics;

namespace HuLoopBOT.Services;

public class PowerService
{
    public OperationResult DisableSleep()
    {
        try
        {
            Logger.LogVerbose("Starting DisableSleep operation");
            Logger.LogVerbose("Executing: powercfg /change standby-timeout-ac 0");
            Logger.LogVerbose("Executing: powercfg /change standby-timeout-dc 0");

            // Disable sleep on AC power
            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change standby-timeout-ac 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo)?.WaitForExit();

            // Disable sleep on battery/DC power
            var startInfoDc = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change standby-timeout-dc 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfoDc)?.WaitForExit();

            // Disable hibernate timeout
            var startInfoHibernate = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change hibernate-timeout-ac 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfoHibernate)?.WaitForExit();

            Logger.LogInformation("Sleep and hibernate disabled successfully");
            Logger.LogVerbose("DisableSleep operation completed");

            return OperationResult.Ok("Sleep and hibernate disabled");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to disable sleep", ex);
            return OperationResult.Fail($"Failed to disable sleep: {ex.Message}");
        }
    }

    public OperationResult DisableScreenTimeout()
    {
        try
        {
            Logger.LogVerbose("Starting DisableScreenTimeout operation");
            Logger.LogVerbose("Executing: powercfg /change monitor-timeout-ac 0");
            Logger.LogVerbose("Executing: powercfg /change monitor-timeout-dc 0");

            // Disable screen timeout on AC power
            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change monitor-timeout-ac 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo)?.WaitForExit();

            // Disable screen timeout on battery/DC power
            var startInfoDc = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change monitor-timeout-dc 0",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfoDc)?.WaitForExit();

            Logger.LogInformation("Screen timeout disabled successfully");
            Logger.LogVerbose("DisableScreenTimeout operation completed");

            return OperationResult.Ok("Screen timeout disabled");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to disable screen timeout", ex);
            return OperationResult.Fail($"Failed to disable screen timeout: {ex.Message}");
        }
    }
}
