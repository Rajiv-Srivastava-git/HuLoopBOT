using HuLoopBOT.Utilities;
using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceProcess;

namespace HuLoopBOT.Services;

/// <summary>
/// Windows Service for persistent RDP session monitoring
/// Runs independently of the configuration application
/// </summary>
public class RdpMonitoringService : ServiceBase
{
    private RdpSessionMonitor? _monitor;
    private ServiceMessageWindow? _messageWindow;
    private Thread? _messageThread;
    private volatile bool _isStarting = false;
    private volatile bool _isRunning = false;
    private readonly ManualResetEventSlim _initializationComplete = new ManualResetEventSlim(false);
    private const string SERVICE_NAME = "HuLoopBOT_RDP_Monitor";
    private const string ServiceDisplayName = "HuLoop BOT - RDP Session Monitor";
    private const string ServiceDescription = "Monitors RDP sessions and automatically transfers them to console on disconnect";

    public RdpMonitoringService()
    {
        base.ServiceName = SERVICE_NAME;
        this.CanStop = true;
        this.CanShutdown = true;
        this.AutoLog = true;

        // CRITICAL: Set these BEFORE OnStart to prevent Error 1052
        this.CanPauseAndContinue = false;
        this.CanHandlePowerEvent = false;
        this.CanHandleSessionChangeEvent = true;
    }

    protected override void OnStart(string[] args)
    {
        try
        {
            LogRdpInfo("========================================");
            LogRdpInfo("RDP Monitoring Service - OnStart Called");
            LogRdpInfo("========================================");
            LogRdpInfo($"Start requested at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            LogRdpInfo($"Process ID: {Process.GetCurrentProcess().Id}");
            LogRdpInfo($"Command line args: {(args?.Length > 0 ? string.Join(", ", args) : "None")}");

            // CRITICAL: Return immediately to avoid Error 1053
            // Do ALL initialization asynchronously
            _isStarting = true;
            _initializationComplete.Reset();

            LogRdpInfo("Starting asynchronous initialization...");

            // Start initialization on a background thread
            Task.Run(() => InitializeService());

            // CRITICAL: Return immediately from OnStart
            // Do NOT wait for initialization to complete
            LogRdpInfo("OnStart returning immediately - initialization will continue in background");
            LogRdpInfo("This prevents Error 1053 timeout issues");
        }
        catch (Exception ex)
        {
            LogRdpError("CRITICAL: OnStart failed", ex);
            // Don't throw - let initialization continue in background
        }
    }

    private void InitializeService()
    {
        try
        {
            LogRdpInfo("========================================");
            LogRdpInfo("Service Initialization Started");
            LogRdpInfo("========================================");

            // Add a small delay to ensure OnStart has returned
            Thread.Sleep(100);
            LogRdpInfo("Starting initialization after OnStart return...");

            // Step 1: Check registry
            LogRdpInfo("Step 1: Checking registry configuration...");
            if (!IsServiceEnabled())
            {
                LogRdpWarning("Service is disabled in registry (RdpMonitoringEnabled = 0)");
                LogRdpInfo("Service will remain installed but not monitor sessions");
                LogRdpInfo("To enable: Set HKLM\\SOFTWARE\\HuLoopBOT\\RdpMonitoringEnabled = 1");
                _isStarting = false;
                _initializationComplete.Set();
                LogRdpInfo("Initialization completed - Service disabled");
                return;
            }
            LogRdpInfo("✓ Registry check passed - Service is enabled");

            // Step 2: Create RDP Session Monitor
            LogRdpInfo("Step 2: Creating RDP Session Monitor...");
            try
            {
                _monitor = new RdpSessionMonitor();
                _monitor.AutoTransferOnDisconnect = true;
                LogRdpInfo("✓ RDP Session Monitor created successfully");
                LogRdpInfo($"   Auto-transfer on disconnect: {_monitor.AutoTransferOnDisconnect}");

                // Start the monitor
                LogRdpInfo("Starting RDP monitor...");
                _monitor.Start();
                LogRdpInfo("✓ RDP monitor started");
            }
            catch (Exception ex)
            {
                LogRdpError("Failed to create or start RDP Session Monitor", ex);
                _isStarting = false;
                _isRunning = false;
                _initializationComplete.Set();
                return;
            }

            // Mark as running - no message window needed since we're using OnSessionChange
            _isRunning = true;
            _isStarting = false;

            LogRdpInfo("========================================");
            LogRdpInfo("✓✓✓ RDP Monitoring Service Started Successfully ✓✓✓");
            LogRdpInfo("========================================");
            LogRdpInfo($"Service status: Running");
            LogRdpInfo($"Monitoring: Active");
            LogRdpInfo($"Auto-transfer: {_monitor?.AutoTransferOnDisconnect ?? false}");
            LogRdpInfo($"Using OnSessionChange for session notifications");
            LogRdpInfo($"Ready to monitor RDP sessions");
            LogRdpInfo("========================================");

            // Signal initialization complete
            _initializationComplete.Set();
        }
        catch (Exception ex)
        {
            LogRdpError("CRITICAL: Unhandled exception during service initialization", ex);
            _isStarting = false;
            _isRunning = false;
            _initializationComplete.Set();
        }
    }

    protected override void OnStop()
    {
        try
        {
            LogRdpInfo("========================================");
            LogRdpInfo("RDP Monitoring Service - OnStop Called");
            LogRdpInfo("========================================");
            LogRdpInfo($"Stop requested at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
      _isRunning = false;

   // Stop and dispose the monitor
       if (_monitor != null)
       {
        LogRdpInfo("Stopping RDP monitor...");
  try
        {
         _monitor.Stop();
      _monitor.Dispose();
 LogRdpInfo("✓ RDP monitor stopped and disposed");
     }
  catch (Exception ex)
    {
   LogRdpError("Error stopping monitor", ex);
    }
       _monitor = null;
   }

       LogRdpInfo("========================================");
     LogRdpInfo("✓ RDP Monitoring Service Stopped Successfully");
         LogRdpInfo("========================================");
      }
        catch (Exception ex)
   {
       LogRdpError("CRITICAL: Error during service stop", ex);
  }
    }

    protected override void OnSessionChange(SessionChangeDescription change)
    {
        Logger.LogRdpInfo($"Session change detected: {change.Reason} (Session {change.SessionId})");

        if (change.Reason == SessionChangeReason.RemoteDisconnect)
        {
            _monitor?.HandleDisconnect(change.SessionId);
        }
    }

    protected override void OnShutdown()
    {
        try
        {
            LogRdpWarning("========================================");
            LogRdpWarning("System Shutdown Detected");
            LogRdpWarning("========================================");
            OnStop();
        }
        catch (Exception ex)
        {
            LogRdpError("Error during shutdown", ex);
        }
    }

    /// <summary>
    /// Check if the service is enabled in registry
    /// </summary>
    private bool IsServiceEnabled()
    {
        try
        {
            LogRdpInfo("Checking registry: HKLM\\SOFTWARE\\HuLoopBOT");
            var registryPath = @"SOFTWARE\HuLoopBOT";

            using var key = Registry.LocalMachine.OpenSubKey(registryPath);

            if (key == null)
            {
                LogRdpWarning("Registry key not found - service will be disabled");
                return false;
            }

            var enabled = key.GetValue("RdpMonitoringEnabled");

            if (enabled == null)
            {
                LogRdpWarning("RdpMonitoringEnabled value not found - service will be disabled");
                return false;
            }

            bool isEnabled = enabled.ToString() == "1";
            LogRdpInfo($"RdpMonitoringEnabled = {enabled} (Enabled: {isEnabled})");

            return isEnabled;
        }
        catch (Exception ex)
        {
            LogRdpError("Error checking service enabled status", ex);
            return false;
        }
    }

    // Add helper method for RDP logging
    private static void LogRdpInfo(string message) => Logger.LogRdpInfo(message);
    private static void LogRdpWarning(string message) => Logger.LogRdpWarning(message);
    private static void LogRdpError(string message, Exception? ex) => Logger.LogRdpError(message, ex);

    #region Service Installation and Management

    /// <summary>
    /// Install the Windows Service
    /// </summary>
    public static bool InstallService()
    {
        try
        {
            Logger.LogInformation("========================================", "ServiceInstall");
            Logger.LogInformation("Installing RDP Monitoring Service", "ServiceInstall");
            Logger.LogInformation("========================================", "ServiceInstall");

            // Step 1: Locate service executable
            Logger.LogInformation("Step 1: Locating service executable", "ServiceInstall");
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var serviceExePath = Path.Combine(appDirectory, "HuLoopBOT_Service.exe");

            Logger.LogInformation($"Application directory: {appDirectory}", "ServiceInstall");
            Logger.LogInformation($"Looking for: {serviceExePath}", "ServiceInstall");

            // List all .exe files in the directory for debugging
            try
            {
                var exeFiles = Directory.GetFiles(appDirectory, "*.exe");
                if (exeFiles.Length > 0)
                {
                    Logger.LogVerbose($"EXE files in directory ({exeFiles.Length}):", "ServiceInstall");
                    foreach (var exe in exeFiles)
                    {
                        var fileInfo = new FileInfo(exe);
                        Logger.LogVerbose($"   - {Path.GetFileName(exe)} ({fileInfo.Length:N0} bytes)", "ServiceInstall");
                    }
                }
                else
                {
                    Logger.LogWarning("No EXE files found in directory", "ServiceInstall");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not list directory files: {ex.Message}", "ServiceInstall");
            }

            // Step 2: Verify executable exists
            Logger.LogInformation("Step 2: Verifying executable exists", "ServiceInstall");
            if (!File.Exists(serviceExePath))
            {
                Logger.LogError("Service executable not found!", null, "ServiceInstall");
                Logger.LogError($"Expected path: {serviceExePath}", null, "ServiceInstall");
                Logger.LogError("The service must be built and deployed correctly", null, "ServiceInstall");

                // Check alternate locations
                Logger.LogInformation("Checking alternate locations...", "ServiceInstall");
                var possiblePaths = new[]
                {
                    Path.Combine(appDirectory, "ServiceHost", "HuLoopBOT_Service.exe"),
                    Path.Combine(appDirectory, "win-x64", "HuLoopBOT_Service.exe"),
                    Path.Combine(Path.GetDirectoryName(appDirectory) ?? "", "ServiceHost", "bin", "Debug", "net8.0-windows", "HuLoopBOT_Service.exe"),
                    Path.Combine(Path.GetDirectoryName(appDirectory) ?? "", "ServiceHost", "bin", "Release", "net8.0-windows", "HuLoopBOT_Service.exe")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Logger.LogError($"Found at alternate location: {path}", null, "ServiceInstall");
                        Logger.LogError("Build configuration needs adjustment", null, "ServiceInstall");
                        break;
                    }
                }

                return false;
            }

            var fileSize = new FileInfo(serviceExePath).Length;
            Logger.LogInformation($"? Service executable found ({fileSize:N0} bytes)", "ServiceInstall");

            // Step 3: Check if service already exists
            Logger.LogInformation("Step 3: Checking if service already exists", "ServiceInstall");
            if (IsServiceInstalled())
            {
                Logger.LogInformation("Service already installed - uninstalling first", "ServiceInstall");

                if (!UninstallService())
                {
                    Logger.LogError("Failed to uninstall existing service", null, "ServiceInstall");
                    return false;
                }

                Logger.LogInformation("Waiting for service to be removed...", "ServiceInstall");
                System.Threading.Thread.Sleep(2000);
            }
            else
            {
                Logger.LogInformation("Service not currently installed", "ServiceInstall");
            }

            // Step 4: Install the service
            Logger.LogInformation("Step 4: Installing service using sc.exe", "ServiceInstall");
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create \"{SERVICE_NAME}\" binPath= \"\\\"{serviceExePath}\\\"\" start= auto DisplayName= \"{ServiceDisplayName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.LogVerbose($"Command: sc.exe {startInfo.Arguments}", "ServiceInstall");

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Logger.LogError("Failed to start sc.exe process", null, "ServiceInstall");
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Logger.LogVerbose($"sc.exe exit code: {process.ExitCode}", "ServiceInstall");

                if (process.ExitCode == 0)
                {
                    Logger.LogInformation("? Service created successfully", "ServiceInstall");

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Logger.LogVerbose($"Output: {output.Trim()}", "ServiceInstall");
                    }

                    // Step 5: Set service description
                    Logger.LogInformation("Step 5: Setting service description", "ServiceInstall");
                    if (SetServiceDescription())
                    {
                        Logger.LogInformation("? Service description set", "ServiceInstall");
                    }

                    // Step 6: Configure service failure actions
                    Logger.LogInformation("Step 6: Configuring failure recovery", "ServiceInstall");
                    ConfigureServiceFailureRecovery();

                    // Step 7: Wait for Windows to fully register the service
                    Logger.LogInformation("Step 7: Waiting for service registration to complete...", "ServiceInstall");
                    System.Threading.Thread.Sleep(2000); // Give Windows time to fully register
                    Logger.LogInformation("? Service registration wait complete", "ServiceInstall");

                    // Verify service is accessible
                    Logger.LogInformation("Verifying service is accessible...", "ServiceInstall");
                    try
                    {
                        using var service = new ServiceController(SERVICE_NAME);
                        service.Refresh();
                        Logger.LogInformation($"? Service accessible (Status: {service.Status})", "ServiceInstall");
                    }
                    catch (Exception verifyEx)
                    {
                        Logger.LogWarning($"Could not verify service status: {verifyEx.Message}", "ServiceInstall");
                        Logger.LogWarning("Service may need additional time to initialize", "ServiceInstall");
                    }

                    Logger.LogInformation("========================================", "ServiceInstall");
                    Logger.LogInformation("??? Service installed successfully ???", "ServiceInstall");
                    Logger.LogInformation("========================================", "ServiceInstall");
                    return true;
                }
                else
                {
                    Logger.LogError($"Service installation failed with exit code {process.ExitCode}", null, "ServiceInstall");

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Logger.LogError($"Error output: {error.Trim()}", null, "ServiceInstall");
                    }

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Logger.LogError($"Standard output: {output.Trim()}", null, "ServiceInstall");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception while running sc.exe", ex, "ServiceInstall");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("CRITICAL: Unhandled exception during service installation", ex, "ServiceInstall");
            return false;
        }
    }

    /// <summary>
    /// Uninstall the Windows Service
    /// </summary>
    public static bool UninstallService()
    {
        try
        {
            Logger.LogInformation("========================================", "ServiceUninstall");
            Logger.LogInformation("Uninstalling RDP Monitoring Service", "ServiceUninstall");
            Logger.LogInformation("========================================", "ServiceUninstall");

            // Step 1: Stop service first
            Logger.LogInformation("Step 1: Stopping service if running", "ServiceUninstall");
            if (!StopService())
            {
                Logger.LogWarning("Service could not be stopped (may not be running)", "ServiceUninstall");
            }

            // Step 2: Delete service
            Logger.LogInformation("Step 2: Deleting service", "ServiceUninstall");
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"delete \"{SERVICE_NAME}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.LogVerbose($"Command: sc.exe {startInfo.Arguments}", "ServiceUninstall");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.LogError("Failed to start sc.exe process", null, "ServiceUninstall");
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Logger.LogVerbose($"sc.exe exit code: {process.ExitCode}", "ServiceUninstall");

            if (process.ExitCode == 0)
            {
                Logger.LogInformation("? Service uninstalled successfully", "ServiceUninstall");
                Logger.LogInformation("========================================", "ServiceUninstall");
                return true;
            }
            else
            {
                Logger.LogWarning($"Service deletion returned code {process.ExitCode}", "ServiceUninstall");

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Logger.LogWarning($"Error output: {error.Trim()}", "ServiceUninstall");
                }

                return process.ExitCode == 1072; // Service marked for deletion
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error uninstalling service", ex, "ServiceUninstall");
            return false;
        }
    }

    /// <summary>
    /// Start the Windows Service
    /// </summary>
    public static bool StartService()
    {
        try
        {
            Logger.LogInformation("========================================", "ServiceStart");
            Logger.LogInformation("Starting RDP Monitoring Service", "ServiceStart");
            Logger.LogInformation("========================================", "ServiceStart");

            // Give Windows time to fully register the service if it was just installed
            Logger.LogInformation("Checking service registration status...", "ServiceStart");
            System.Threading.Thread.Sleep(1000);

            using var service = new ServiceController(SERVICE_NAME);

            // Refresh to get current status
            service.Refresh();
            Logger.LogInformation($"Current status: {service.Status}", "ServiceStart");

            // Handle different service states
            switch (service.Status)
            {
                case ServiceControllerStatus.Running:
                    Logger.LogInformation("Service is already running", "ServiceStart");
                    Logger.LogInformation("========================================", "ServiceStart");
                    return true;

                case ServiceControllerStatus.StartPending:
                    Logger.LogInformation("Service is already starting", "ServiceStart");
                    Logger.LogInformation("Waiting for service to start...", "ServiceStart");

                    try
                    {
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        Logger.LogInformation("? Service started", "ServiceStart");
                        Logger.LogInformation("========================================", "ServiceStart");
                        return true;
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        Logger.LogWarning("Service start is taking longer than expected", "ServiceStart");

                        // Check if it eventually started
                        service.Refresh();
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            Logger.LogInformation("? Service is now running", "ServiceStart");
                            Logger.LogInformation("========================================", "ServiceStart");
                            return true;
                        }

                        throw;
                    }

                case ServiceControllerStatus.Stopped:
                    Logger.LogInformation("Service is stopped - will attempt to start", "ServiceStart");
                    break;

                case ServiceControllerStatus.StopPending:
                    Logger.LogInformation("Service is currently stopping - waiting...", "ServiceStart");

                    try
                    {
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        Logger.LogInformation("Service stopped - will now start", "ServiceStart");
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        Logger.LogWarning("Service stop is taking longer than expected", "ServiceStart");
                        Logger.LogWarning("Proceeding with start attempt anyway", "ServiceStart");
                    }
                    break;

                case ServiceControllerStatus.Paused:
                    Logger.LogInformation("Service is paused - attempting to continue", "ServiceStart");

                    try
                    {
                        service.Continue();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        Logger.LogInformation("? Service continued and running", "ServiceStart");
                        Logger.LogInformation("========================================", "ServiceStart");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to continue paused service: {ex.Message}", "ServiceStart");
                        Logger.LogInformation("Will attempt full start instead", "ServiceStart");
                    }
                    break;

                default:
                    Logger.LogWarning($"Service in unexpected state: {service.Status}", "ServiceStart");
                    Logger.LogInformation("Attempting to start anyway", "ServiceStart");
                    break;
            }

            // Attempt to start the service
            Logger.LogInformation("Sending start command to service...", "ServiceStart");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                service.Start();
                Logger.LogInformation("Start command sent successfully", "ServiceStart");
            }
            catch (InvalidOperationException ex) when (ex.InnerException is System.ComponentModel.Win32Exception win32Ex)
            {
                Logger.LogError($"Failed to send start command: {win32Ex.Message} (Error code: {win32Ex.NativeErrorCode})", ex, "ServiceStart");

                // Check if service actually started despite the error
                System.Threading.Thread.Sleep(2000);
                service.Refresh();

                if (service.Status == ServiceControllerStatus.Running)
                {
                    Logger.LogWarning("Service appears to be running despite start error", "ServiceStart");
                    Logger.LogInformation("? Service is running", "ServiceStart");
                    Logger.LogInformation("========================================", "ServiceStart");
                    return true;
                }

                // If it's Error 1053, provide specific guidance
                if (win32Ex.NativeErrorCode == 1053)
                {
                    Logger.LogError("Error 1053: Service did not respond in time", null, "ServiceStart");
                    Logger.LogError("This usually means:", null, "ServiceStart");
                    Logger.LogError("1. Service OnStart() is taking too long", null, "ServiceStart");
                    Logger.LogError("2. Service has a blocking operation in startup", null, "ServiceStart");
                    Logger.LogError("3. Dependencies are not yet ready", null, "ServiceStart");
                    Logger.LogError("", null, "ServiceStart");
                    Logger.LogError("Check service log file for details:", null, "ServiceStart");
                    Logger.LogError("C:\\ProgramData\\HuLoopBOT\\Logs\\HuLoopBOT_Service_*.log", null, "ServiceStart");
                }

                throw;
            }

            // Wait for service to reach running state
            Logger.LogInformation("Waiting for service to reach Running state (timeout: 60s)...", "ServiceStart");

      int checkCount = 0;
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(60))
            {
           service.Refresh();
         checkCount++;

    Logger.LogVerbose($"Status check #{checkCount}: {service.Status}", "ServiceStart");

  if (service.Status == ServiceControllerStatus.Running)
    {
  stopwatch.Stop();
          Logger.LogInformation($"? Service started successfully in {stopwatch.ElapsedMilliseconds}ms", "ServiceStart");
             Logger.LogInformation($"Status checks performed: {checkCount}", "ServiceStart");
     
  // Give the service a bit more time to fully initialize in background
          Logger.LogInformation("Waiting additional 2 seconds for background initialization...", "ServiceStart");
              System.Threading.Thread.Sleep(2000);
    Logger.LogInformation("? Service should now be fully initialized", "ServiceStart");
            
Logger.LogInformation("========================================", "ServiceStart");
        return true;
       }

       if (service.Status == ServiceControllerStatus.Stopped)
      {
       Logger.LogError("Service stopped unexpectedly during startup", null, "ServiceStart");
     Logger.LogError("Check Event Viewer and log files for crash details", null, "ServiceStart");
     throw new InvalidOperationException("Service stopped unexpectedly during startup");
         }

                // Log progress every 5 seconds
           if (checkCount % 10 == 0)
                {
        Logger.LogInformation($"Still waiting... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed, Status: {service.Status})", "ServiceStart");
                }

         System.Threading.Thread.Sleep(500);
            }

            // Timeout reached
    service.Refresh();
  Logger.LogError($"Service failed to start within 60 seconds (Final status: {service.Status})", null, "ServiceStart");
    Logger.LogError("", null, "ServiceStart");
    Logger.LogError("Troubleshooting steps:", null, "ServiceStart");
    Logger.LogError("1. Check Event Viewer (eventvwr.msc)", null, "ServiceStart");
    Logger.LogError("   - Windows Logs ? Application", null, "ServiceStart");
    Logger.LogError("   - Filter by Source: HuLoopBOT_RDP_Monitor", null, "ServiceStart");
    Logger.LogError("", null, "ServiceStart");
    Logger.LogError("2. Check service log file:", null, "ServiceStart");
    Logger.LogError("   C:\\ProgramData\\HuLoopBOT\\Logs\\HuLoopBOT_Service_*.log", null, "ServiceStart");
    Logger.LogError("", null, "ServiceStart");
    Logger.LogError("3. Try manual start:", null, "ServiceStart");
    Logger.LogError("   sc start HuLoopBOT_RDP_Monitor", null, "ServiceStart");
    Logger.LogError("", null, "ServiceStart");
    Logger.LogError("4. Check registry setting:", null, "ServiceStart");
    Logger.LogError("   HKLM\\SOFTWARE\\HuLoopBOT\\RdpMonitoringEnabled should be 1", null, "ServiceStart");

            throw new System.ServiceProcess.TimeoutException($"Service failed to start within 60 seconds (Status: {service.Status})");
        }
        catch (System.ServiceProcess.TimeoutException ex)
        {
            Logger.LogError("Service start timeout", ex, "ServiceStart");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError("Cannot start service", ex, "ServiceStart");
            Logger.LogError("Check that service is properly installed and not marked for deletion", null, "ServiceStart");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Unexpected error starting service", ex, "ServiceStart");
            return false;
        }
    }

    /// <summary>
    /// Stop the Windows Service
    /// </summary>
    public static bool StopService()
    {
        try
        {
            Logger.LogInformation("Stopping RDP Monitoring Service", "ServiceStop");

            using var service = new ServiceController(SERVICE_NAME);

            service.Refresh();
            Logger.LogInformation($"Current status: {service.Status}", "ServiceStop");

            switch (service.Status)
            {
                case ServiceControllerStatus.Stopped:
                    Logger.LogInformation("Service is already stopped", "ServiceStop");
                    return true;

                case ServiceControllerStatus.StopPending:
                    Logger.LogInformation("Service is already stopping - waiting...", "ServiceStop");

                    try
                    {
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        Logger.LogInformation("? Service stopped", "ServiceStop");
                        return true;
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        Logger.LogWarning("Service stop is taking longer than expected", "ServiceStop");

                        // Check if it eventually stopped
                        service.Refresh();
                        if (service.Status == ServiceControllerStatus.Stopped)
                        {
                            Logger.LogInformation("? Service is now stopped", "ServiceStop");
                            return true;
                        }

                        throw;
                    }

                case ServiceControllerStatus.StartPending:
                    Logger.LogInformation("Service is currently starting - waiting for it to start first", "ServiceStop");

                    try
                    {
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        Logger.LogInformation("Service started - now stopping", "ServiceStop");
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        Logger.LogWarning("Service did not start in time - attempting stop anyway", "ServiceStop");
                    }
                    break;

                case ServiceControllerStatus.Paused:
                    Logger.LogInformation("Service is paused - will stop from paused state", "ServiceStop");
                    break;

                case ServiceControllerStatus.Running:
                    Logger.LogInformation("Service is running - will stop", "ServiceStop");
                    break;

                default:
                    Logger.LogWarning($"Service in unexpected state: {service.Status}", "ServiceStop");
                    break;
            }

            // Attempt to stop the service
            Logger.LogInformation("Sending stop command...", "ServiceStop");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                service.Stop();
                Logger.LogInformation("Stop command sent successfully", "ServiceStop");
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning($"Failed to send stop command: {ex.Message}", "ServiceStop");

                // Check if service already stopped
                service.Refresh();
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    Logger.LogInformation("Service is already stopped", "ServiceStop");
                    return true;
                }

                throw;
            }

            // Wait for service to stop
            Logger.LogInformation("Waiting for service to stop (timeout: 30s)...", "ServiceStop");

            int checkCount = 0;
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                service.Refresh();
                checkCount++;

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    stopwatch.Stop();
                    Logger.LogInformation($"? Service stopped in {stopwatch.ElapsedMilliseconds}ms", "ServiceStop");
                    return true;
                }

                // Log progress every 5 seconds
                if (checkCount % 10 == 0)
                {
                    Logger.LogInformation($"Still waiting... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed, Status: {service.Status})", "ServiceStop");
                }

                System.Threading.Thread.Sleep(500);
            }

            // Timeout
            service.Refresh();
            Logger.LogError($"Service failed to stop within 30 seconds (Status: {service.Status})", null, "ServiceStop");
            return false;
        }
        catch (System.ServiceProcess.TimeoutException ex)
        {
            Logger.LogError("Service stop timeout", ex, "ServiceStop");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning($"Service not found or not accessible: {ex.Message}", "ServiceStop");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Error stopping service", ex, "ServiceStop");
            return false;
        }
    }

    /// <summary>
    /// Check if the Windows Service is installed
    /// </summary>
    public static bool IsServiceInstalled()
    {
        try
        {
            using var service = new ServiceController(SERVICE_NAME);
            // Try to access a property - if service doesn't exist, this will throw
            var status = service.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Check if the Windows Service is running
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
    /// Enable the service (set registry key)
    /// </summary>
    public static bool EnableService()
    {
        try
        {
            Logger.LogInformation("Enabling RDP Monitoring Service", "ServiceConfig");

            var registryPath = @"SOFTWARE\HuLoopBOT";
            using var key = Registry.LocalMachine.CreateSubKey(registryPath);

            if (key == null)
            {
                Logger.LogError("Failed to create/open registry key", null, "ServiceConfig");
                return false;
            }

            key.SetValue("RdpMonitoringEnabled", 1, RegistryValueKind.DWord);
            Logger.LogInformation("? Service enabled in registry", "ServiceConfig");

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to enable service", ex, "ServiceConfig");
            return false;
        }
    }

    /// <summary>
    /// Disable the service (set registry key)
    /// </summary>
    public static bool DisableService()
    {
        try
        {
            Logger.LogInformation("Disabling RDP Monitoring Service", "ServiceConfig");

            var registryPath = @"SOFTWARE\HuLoopBOT";
            using var key = Registry.LocalMachine.OpenSubKey(registryPath, true);

            if (key == null)
            {
                Logger.LogWarning("Registry key not found - service already disabled", "ServiceConfig");
                return true;
            }

            key.SetValue("RdpMonitoringEnabled", 0, RegistryValueKind.DWord);
            Logger.LogInformation("? Service disabled in registry", "ServiceConfig");

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to disable service", ex, "ServiceConfig");
            return false;
        }
    }

    /// <summary>
    /// Set the service description
    /// </summary>
    private static bool SetServiceDescription()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"description \"{SERVICE_NAME}\" \"{ServiceDescription}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.LogWarning("Failed to start sc.exe for description", "ServiceInstall");
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to set service description: {ex.Message}", "ServiceInstall");
            return false;
        }
    }

    /// <summary>
    /// Configure service failure recovery actions
    /// </summary>
    private static void ConfigureServiceFailureRecovery()
    {
        try
        {
            // Configure service to restart on failure
            // reset= 86400 means reset failure count after 24 hours
            // actions= restart/60000/restart/60000/restart/60000 means restart after 60 seconds, 3 times
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"failure \"{SERVICE_NAME}\" reset= 86400 actions= restart/60000/restart/60000/restart/60000",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.LogVerbose($"Command: sc.exe {startInfo.Arguments}", "ServiceInstall");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.LogWarning("Failed to start sc.exe for failure config", "ServiceInstall");
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Logger.LogInformation("✓ Failure recovery configured", "ServiceInstall");
            }
            else
            {
                Logger.LogWarning($"Failed to configure failure recovery (exit code: {process.ExitCode})", "ServiceInstall");
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Logger.LogWarning($"Error: {error.Trim()}", "ServiceInstall");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Exception configuring failure recovery: {ex.Message}", "ServiceInstall");
        }
    }

    #endregion
}
