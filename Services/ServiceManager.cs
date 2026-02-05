using System.Diagnostics;
using System.ServiceProcess;
using System.Security.Principal;
using Microsoft.Win32;

namespace HuLoopBOT.Services;

/// <summary>
/// Unified Service Manager - Handles all service operations from C# code
/// No need for separate PowerShell scripts!
/// </summary>
public static class ServiceManager
{
    private const string SERVICE_NAME = "HuLoopBOT_RDP_Monitor";
    private const string SERVICE_DISPLAY_NAME = "HuLoop BOT - RDP Session Monitor";
    private const string SERVICE_DESCRIPTION = "Monitors RDP sessions and automatically transfers them to console on disconnect";

    #region Admin Check

    /// <summary>
    /// Check if running with administrator privileges
    /// </summary>
    public static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restart application with administrator privileges
    /// </summary>
    public static bool RestartAsAdministrator(string arguments = "")
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "",
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas" // Request elevation
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Service Installation

    /// <summary>
    /// Install the Windows Service
    /// </summary>
    public static ServiceOperationResult InstallService(string executablePath = "")
    {
        if (!IsRunningAsAdministrator())
        {
            return ServiceOperationResult.CreateFailure("Administrator privileges required for service installation");
        }

        try
        {
            // Determine executable path
            if (string.IsNullOrEmpty(executablePath))
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                executablePath = Path.Combine(appDir, "HuLoopBOT_Service.exe");
            }

            // Verify executable exists
            if (!File.Exists(executablePath))
            {
                return ServiceOperationResult.CreateFailure($"Service executable not found: {executablePath}");
            }

            // Check if service already exists
            if (IsServiceInstalled())
            {
                var uninstallResult = UninstallService();
                if (!uninstallResult.Success)
                {
                    return ServiceOperationResult.CreateFailure("Could not uninstall existing service: " + uninstallResult.Message);
                }
                Thread.Sleep(2000); // Wait for service to be removed
            }

            // Install service using sc.exe
            var result = ExecuteCommand("sc.exe", $"create \"{SERVICE_NAME}\" binPath= \"\\\"{executablePath}\\\"\" start= auto DisplayName= \"{SERVICE_DISPLAY_NAME}\"");

            if (!result.Success)
            {
                return result;
            }

            // Set description
            ExecuteCommand("sc.exe", $"description \"{SERVICE_NAME}\" \"{SERVICE_DESCRIPTION}\"");

            // Configure failure recovery
            ExecuteCommand("sc.exe", $"failure \"{SERVICE_NAME}\" reset= 86400 actions= restart/60000/restart/60000/restart/60000");

            // Ensure registry key is set
            EnsureRegistryConfiguration();

            Thread.Sleep(2000); // Wait for service registration

            return ServiceOperationResult.CreateSuccess("Service installed successfully");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Installation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Uninstall the Windows Service
    /// </summary>
    public static ServiceOperationResult UninstallService()
    {
        if (!IsRunningAsAdministrator())
        {
            return ServiceOperationResult.CreateFailure("Administrator privileges required for service uninstallation");
        }

        try
        {
            if (!IsServiceInstalled())
            {
                return ServiceOperationResult.CreateSuccess("Service not installed");
            }

            // Stop service first
            var stopResult = StopService();
            if (!stopResult.Success && stopResult.Message != "Service already stopped")
            {
                // Continue anyway
            }

            // Delete service
            var result = ExecuteCommand("sc.exe", $"delete \"{SERVICE_NAME}\"");

            Thread.Sleep(2000);

            return result.Success
                ? ServiceOperationResult.CreateSuccess("Service uninstalled successfully")
          : ServiceOperationResult.CreateFailure("Service uninstallation failed: " + result.Message);
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Uninstallation failed: {ex.Message}");
        }
    }

    #endregion

    #region Service Control

    /// <summary>
    /// Start the Windows Service
    /// </summary>
    public static ServiceOperationResult StartService()
    {
        if (!IsRunningAsAdministrator())
        {
            return ServiceOperationResult.CreateFailure("Administrator privileges required to start service");
        }

        try
        {
            if (!IsServiceInstalled())
            {
                return ServiceOperationResult.CreateFailure("Service not installed");
            }

            using var service = new ServiceController(SERVICE_NAME);
            service.Refresh();

            // Check current status
            switch (service.Status)
            {
                case ServiceControllerStatus.Running:
                    return ServiceOperationResult.CreateSuccess("Service is already running");

                case ServiceControllerStatus.StartPending:
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    return ServiceOperationResult.CreateSuccess("Service started");

                case ServiceControllerStatus.StopPending:
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    break;

                case ServiceControllerStatus.Paused:
                    service.Continue();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    return ServiceOperationResult.CreateSuccess("Service continued and running");
            }

            // Start the service
            var stopwatch = Stopwatch.StartNew();
            service.Start();

            // Wait for service to start
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            stopwatch.Stop();

            return ServiceOperationResult.CreateSuccess($"Service started successfully in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Failed to start service: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the Windows Service
    /// </summary>
    public static ServiceOperationResult StopService()
    {
        if (!IsRunningAsAdministrator())
        {
            return ServiceOperationResult.CreateFailure("Administrator privileges required to stop service");
        }

        try
        {
            if (!IsServiceInstalled())
            {
                return ServiceOperationResult.CreateFailure("Service not installed");
            }

            using var service = new ServiceController(SERVICE_NAME);
            service.Refresh();

            if (service.Status == ServiceControllerStatus.Stopped)
            {
                return ServiceOperationResult.CreateSuccess("Service already stopped");
            }

            if (service.Status == ServiceControllerStatus.StopPending)
            {
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                return ServiceOperationResult.CreateSuccess("Service stopped");
            }

            // Stop the service
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

            return ServiceOperationResult.CreateSuccess("Service stopped successfully");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Failed to stop service: {ex.Message}");
        }
    }

    /// <summary>
    /// Restart the Windows Service
    /// </summary>
    public static ServiceOperationResult RestartService()
    {
        var stopResult = StopService();
        if (!stopResult.Success)
        {
            return stopResult;
        }

        Thread.Sleep(1000);

        return StartService();
    }

    #endregion

    #region Service Status

    /// <summary>
    /// Check if service is installed
    /// </summary>
    public static bool IsServiceInstalled()
    {
        try
        {
            using var service = new ServiceController(SERVICE_NAME);
            _ = service.Status; // This will throw if service doesn't exist
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if service is running
    /// </summary>
    public static bool IsServiceRunning()
    {
        try
        {
            if (!IsServiceInstalled())
                return false;

            using var service = new ServiceController(SERVICE_NAME);
            service.Refresh();
            return service.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get service status
    /// </summary>
    public static ServiceStatus GetServiceStatus()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return new ServiceStatus
                {
                    IsInstalled = false,
                    Status = "Not Installed",
                    StatusCode = ServiceControllerStatus.Stopped
                };
            }

            using var service = new ServiceController(SERVICE_NAME);
            service.Refresh();

            return new ServiceStatus
            {
                IsInstalled = true,
                Status = service.Status.ToString(),
                StatusCode = service.Status,
                DisplayName = service.DisplayName,
                ServiceName = service.ServiceName
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus
            {
                IsInstalled = false,
                Status = "Error",
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Registry Configuration

    /// <summary>
    /// Ensure registry configuration exists
    /// </summary>
    public static ServiceOperationResult EnsureRegistryConfiguration()
    {
        try
        {
            var registryPath = @"SOFTWARE\HuLoopBOT";
            using var key = Registry.LocalMachine.CreateSubKey(registryPath);

            if (key == null)
            {
                return ServiceOperationResult.CreateFailure("Could not create/open registry key");
            }

            key.SetValue("RdpMonitoringEnabled", 1, RegistryValueKind.DWord);

            return ServiceOperationResult.CreateSuccess("Registry configured");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Registry configuration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Enable service
    /// </summary>
    public static ServiceOperationResult EnableService()
    {
        try
        {
            var registryPath = @"SOFTWARE\HuLoopBOT";
            using var key = Registry.LocalMachine.CreateSubKey(registryPath);

            if (key == null)
            {
                return ServiceOperationResult.CreateFailure("Could not create/open registry key");
            }

            key.SetValue("RdpMonitoringEnabled", 1, RegistryValueKind.DWord);

            return ServiceOperationResult.CreateSuccess("Service enabled in registry");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Failed to enable service: {ex.Message}");
        }
    }

    /// <summary>
    /// Disable service
    /// </summary>
    public static ServiceOperationResult DisableService()
    {
        try
        {
            var registryPath = @"SOFTWARE\HuLoopBOT";
            using var key = Registry.LocalMachine.OpenSubKey(registryPath, true);

            if (key == null)
            {
                return ServiceOperationResult.CreateSuccess("Service already disabled (registry key not found)");
            }

            key.SetValue("RdpMonitoringEnabled", 0, RegistryValueKind.DWord);

            return ServiceOperationResult.CreateSuccess("Service disabled in registry");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Failed to disable service: {ex.Message}");
        }
    }
    #endregion

    #region Self-Contained Deployment

    /// <summary>
    /// Build self-contained deployment package
    /// </summary>
    public static ServiceOperationResult BuildSelfContained(string outputDirectory = "publish-self-contained")
    {
        try
        {
            var projectRoot = FindProjectRoot();
            if (string.IsNullOrEmpty(projectRoot))
            {
                return ServiceOperationResult.CreateFailure("Could not find project root");
            }

            var outputPath = Path.Combine(projectRoot, outputDirectory);

            // Clean output directory
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            // Publish service
            var serviceResult = ExecuteCommand("dotnet",
         $"publish ServiceHost\\HuLoopBOT_Service.csproj -c Release -r win-x64 --self-contained true -o \"{Path.Combine(outputPath, "Service")}\"",
            projectRoot);

            if (!serviceResult.Success)
            {
                return ServiceOperationResult.CreateFailure("Service build failed: " + serviceResult.Message);
            }

            // Publish main app
            var mainResult = ExecuteCommand("dotnet",
               $"publish HuLoopBOT.csproj -c Release -r win-x64 --self-contained true -o \"{Path.Combine(outputPath, "MainApp")}\"",
        projectRoot);

            if (!mainResult.Success)
            {
                return ServiceOperationResult.CreateFailure("Main app build failed: " + mainResult.Message);
            }

            return ServiceOperationResult.CreateSuccess($"Self-contained package built: {outputPath}");
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Build failed: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private static ServiceOperationResult ExecuteCommand(string fileName, string arguments, string workingDirectory = "")
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ServiceOperationResult.CreateFailure("Could not start process");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 || process.ExitCode == 1072) // 1072 = service marked for deletion
            {
                return ServiceOperationResult.CreateSuccess(output);
            }
            else
            {
                return ServiceOperationResult.CreateFailure($"Exit code {process.ExitCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            return ServiceOperationResult.CreateFailure($"Command execution failed: {ex.Message}");
        }
    }

    private static string FindProjectRoot()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir, "HuLoopBOT.csproj")))
            {
                return currentDir;
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return "";
    }

    #endregion
}

#region Result Classes

/// <summary>
/// Result of a service operation
/// </summary>
public class ServiceOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? ErrorDetails { get; set; }

    public static ServiceOperationResult CreateSuccess(string message)
    {
        return new ServiceOperationResult { Success = true, Message = message };
    }

    public static ServiceOperationResult CreateFailure(string message, string? errorDetails = null)
    {
        return new ServiceOperationResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails
        };
    }
}

/// <summary>
/// Service status information
/// </summary>
public class ServiceStatus
{
    public bool IsInstalled { get; set; }
    public string Status { get; set; } = "";
    public ServiceControllerStatus StatusCode { get; set; }
    public string? DisplayName { get; set; }
    public string? ServiceName { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
