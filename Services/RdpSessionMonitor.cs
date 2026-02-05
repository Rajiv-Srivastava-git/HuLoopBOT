using HuLoopBOT.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HuLoopBOT.Services;

/// <summary>
/// Monitors RDP session disconnection events and automatically transfers the session to console
/// </summary>
public class RdpSessionMonitor : IDisposable
{
    private bool _isMonitoring;
    private bool _disposed;
    private readonly SessionTransferService _transferService;

    // Windows Terminal Services API
    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQuerySessionInformation(
          IntPtr hServer,
          int sessionId,
      WTS_INFO_CLASS wtsInfoClass,
          out IntPtr ppBuffer,
          out int pBytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("kernel32.dll")]
    private static extern int WTSGetActiveConsoleSessionId();

    // Constants
    private const int NOTIFY_FOR_THIS_SESSION = 0;
    private const int NOTIFY_FOR_ALL_SESSIONS = 1;
    private const int WM_WTSSESSION_CHANGE = 0x02B1;

    // Session change events
    private const int WTS_CONSOLE_CONNECT = 0x1;
    private const int WTS_CONSOLE_DISCONNECT = 0x2;
    private const int WTS_REMOTE_CONNECT = 0x3;
    private const int WTS_REMOTE_DISCONNECT = 0x4;
    private const int WTS_SESSION_LOGON = 0x5;
    private const int WTS_SESSION_LOGOFF = 0x6;
    private const int WTS_SESSION_LOCK = 0x7;
    private const int WTS_SESSION_UNLOCK = 0x8;
    private const int WTS_SESSION_REMOTE_CONTROL = 0x9;

    private enum WTS_INFO_CLASS
    {
        WTSSessionId = 4,
        WTSConnectState = 8
    }

    private enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    public bool IsMonitoring => _isMonitoring;
    private bool _running;
    public bool AutoTransferOnDisconnect { get; set; } = true;

    public RdpSessionMonitor()
    {
        _transferService = new SessionTransferService();
    }

    public void Start()
    {
        Logger.LogRdpInfo("RDP Monitor started.");
        _running = true;
    }

    public void Stop()
    {
        Logger.LogRdpInfo("RDP Monitor stopped.");
        _running = false;
    }

    public void HandleDisconnect(int sessionId)
    {
        if (!_running) return;

        Logger.LogRdpInfo($"Handling disconnect for session {sessionId}");

        try
        {
            TransferSessionToConsole(sessionId);
        }
        catch (Exception ex)
        {
            Logger.LogRdpError("Session transfer failed", ex);
        }
    }

    private void TransferSessionToConsole(int sessionId)
    {
        // Example call — replace with your logic
        Logger.LogRdpInfo($"Transferring session {sessionId} to console...");

        // Call WTS APIs or tscon command
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "tscon",
            Arguments = $"{sessionId} /dest:console",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    /// <summary>
    /// Start monitoring RDP session events
    /// </summary>
    public bool StartMonitoring(IntPtr windowHandle)
    {
        try
        {
            if (_isMonitoring)
            {
                Logger.LogWarning("RDP session monitor is already running");
                return false;
            }

            Logger.LogInformation("Starting RDP session monitoring");
            Logger.LogVerbose($"Window handle: {windowHandle}");

            // Register for all session notifications
            bool success = WTSRegisterSessionNotification(windowHandle, NOTIFY_FOR_ALL_SESSIONS);

            if (success)
            {
                _isMonitoring = true;
                Logger.LogInformation("RDP session monitoring started successfully");
                Logger.LogVerbose("Registered for WTS session change notifications");

                // Log current session info
                LogCurrentSessionInfo();
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Logger.LogError($"Failed to register for session notifications. Win32 Error: {error}", null);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to start RDP session monitoring", ex);
            return false;
        }
    }

    /// <summary>
    /// Stop monitoring RDP session events
    /// </summary>
    public bool StopMonitoring(IntPtr windowHandle)
    {
        try
        {
            if (!_isMonitoring)
            {
                Logger.LogWarning("RDP session monitor is not running");
                return false;
            }

            Logger.LogInformation("Stopping RDP session monitoring");

            bool success = WTSUnRegisterSessionNotification(windowHandle);

            if (success)
            {
                _isMonitoring = false;
                Logger.LogInformation("RDP session monitoring stopped successfully");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Logger.LogError($"Failed to unregister session notifications. Win32 Error: {error}", null);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to stop RDP session monitoring", ex);
            return false;
        }
    }

    /// <summary>
    /// Process Windows session change messages
    /// </summary>
    public void ProcessSessionChange(int eventType, int sessionId)
    {
        try
        {
            string eventName = GetSessionEventName(eventType);
            Logger.LogVerbose($"Session change detected - Event: {eventName} (0x{eventType:X}), Session ID: {sessionId}");

            switch (eventType)
            {
                case WTS_REMOTE_DISCONNECT:
                    OnRdpDisconnected(sessionId);
                    break;

                case WTS_CONSOLE_DISCONNECT:
                    OnConsoleDisconnected(sessionId);
                    break;

                case WTS_REMOTE_CONNECT:
                    OnRdpConnected(sessionId);
                    break;

                case WTS_CONSOLE_CONNECT:
                    OnConsoleConnected(sessionId);
                    break;

                case WTS_SESSION_LOGOFF:
                    OnSessionLogoff(sessionId);
                    break;

                case WTS_SESSION_LOCK:
                    OnSessionLock(sessionId);
                    break;

                case WTS_SESSION_UNLOCK:
                    OnSessionUnlock(sessionId);
                    break;

                default:
                    Logger.LogVerbose($"Unhandled session event: {eventName}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error processing session change event {eventType}", ex);
        }
    }

    private void OnRdpDisconnected(int sessionId)
    {
        Logger.LogInformation($"RDP session disconnected - Session ID: {sessionId}");

        if (AutoTransferOnDisconnect)
        {
            Logger.LogInformation("Auto-transfer enabled - initiating session transfer to console");

            // Small delay to ensure disconnect is complete
            Task.Delay(500).Wait();

            var result = _transferService.Transfer(sessionId);

            if (result.Success)
            {
                Logger.LogInformation($"Successfully transferred session {sessionId} to console after RDP disconnect");
            }
            else
            {
                Logger.LogError($"Failed to transfer session {sessionId} after RDP disconnect: {result.Message}", null);
            }
        }
        else
        {
            Logger.LogInformation("Auto-transfer is disabled - no action taken");
        }
    }

    private void OnConsoleDisconnected(int sessionId)
    {
        Logger.LogInformation($"Console session disconnected - Session ID: {sessionId}");
    }

    private void OnRdpConnected(int sessionId)
    {
        Logger.LogInformation($"RDP session connected - Session ID: {sessionId}");
    }

    private void OnConsoleConnected(int sessionId)
    {
        Logger.LogInformation($"Console session connected - Session ID: {sessionId}");
    }

    private void OnSessionLogoff(int sessionId)
    {
        Logger.LogInformation($"Session logged off - Session ID: {sessionId}");
    }

    private void OnSessionLock(int sessionId)
    {
        Logger.LogVerbose($"Session locked - Session ID: {sessionId}");
    }

    private void OnSessionUnlock(int sessionId)
    {
        Logger.LogVerbose($"Session unlocked - Session ID: {sessionId}");
    }

    private string GetSessionEventName(int eventType)
    {
        return eventType switch
        {
            WTS_CONSOLE_CONNECT => "Console Connect",
            WTS_CONSOLE_DISCONNECT => "Console Disconnect",
            WTS_REMOTE_CONNECT => "Remote Connect (RDP)",
            WTS_REMOTE_DISCONNECT => "Remote Disconnect (RDP)",
            WTS_SESSION_LOGON => "Session Logon",
            WTS_SESSION_LOGOFF => "Session Logoff",
            WTS_SESSION_LOCK => "Session Lock",
            WTS_SESSION_UNLOCK => "Session Unlock",
            WTS_SESSION_REMOTE_CONTROL => "Session Remote Control",
            _ => $"Unknown (0x{eventType:X})"
        };
    }

    private void LogCurrentSessionInfo()
    {
        try
        {
            int currentSessionId = Process.GetCurrentProcess().SessionId;
            int consoleSessionId = WTSGetActiveConsoleSessionId();

            Logger.LogVerbose($"Current process session ID: {currentSessionId}");
            Logger.LogVerbose($"Active console session ID: {consoleSessionId}");

            if (currentSessionId == consoleSessionId)
            {
                Logger.LogVerbose("Running in console session");
            }
            else if (currentSessionId > 0)
            {
                Logger.LogVerbose("Running in RDP session");
            }
        }
        catch (Exception ex)
        {
            Logger.LogVerbose($"Could not retrieve session information: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_isMonitoring)
            {
                Logger.LogWarning("RDP session monitor disposed while still monitoring");
            }

            _disposed = true;
        }
    }
}
