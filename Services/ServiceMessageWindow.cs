using System.Runtime.InteropServices;

namespace HuLoopBOT.Services;

/// <summary>
/// Hidden message-only window that receives Windows Terminal Services session change notifications
/// Used by RdpMonitoringService to monitor RDP session events
/// </summary>
internal class ServiceMessageWindow : Form
{
    private readonly RdpSessionMonitor _monitor;
    private bool _registered = false;

    // Windows Terminal Services API
    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

    // Constants
    private const int NOTIFY_FOR_ALL_SESSIONS = 1;
    private const int WM_WTSSESSION_CHANGE = 0x02B1;

    public ServiceMessageWindow(RdpSessionMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        try
        {
            HuLoopBOT.Utilities.Logger.LogRdpInfo("Creating ServiceMessageWindow...");

            // Create a hidden message-only window
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0;
            this.Width = 0;
            this.Height = 0;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-32000, -32000); // Off-screen

            // CRITICAL: Set to true to ensure window doesn't prevent service from stopping
            this.ControlBox = false;

            // Register for session notifications when handle is created
            this.HandleCreated += OnHandleCreated;
            this.HandleDestroyed += OnHandleDestroyed;
            this.Load += OnLoad;

            HuLoopBOT.Utilities.Logger.LogRdpInfo("? ServiceMessageWindow properties configured");
        }
        catch (Exception ex)
        {
            HuLoopBOT.Utilities.Logger.LogRdpError("CRITICAL: Error in ServiceMessageWindow constructor", ex);
            throw;
        }
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        try
        {
            HuLoopBOT.Utilities.Logger.LogRdpInfo("ServiceMessageWindow Load event");

            // Ensure window is completely hidden
            this.Hide();
            this.Visible = false;

            HuLoopBOT.Utilities.Logger.LogRdpInfo("? ServiceMessageWindow hidden");
        }
        catch (Exception ex)
        {
            HuLoopBOT.Utilities.Logger.LogRdpError("Error in OnLoad", ex);
        }
    }

    private void OnHandleCreated(object? sender, EventArgs e)
    {
        try
        {
            HuLoopBOT.Utilities.Logger.LogRdpInfo($"ServiceMessageWindow handle created: 0x{Handle:X}");

            // Verify handle is valid
            if (Handle == IntPtr.Zero)
            {
                HuLoopBOT.Utilities.Logger.LogRdpError("CRITICAL: Window handle is zero!", null);
                return;
            }

            // Register for all session notifications
            HuLoopBOT.Utilities.Logger.LogRdpInfo("Registering for WTS session notifications...");

            if (WTSRegisterSessionNotification(Handle, NOTIFY_FOR_ALL_SESSIONS))
            {
                _registered = true;
                HuLoopBOT.Utilities.Logger.LogRdpInfo("? Registered for WTS session notifications");

                // Start monitoring with this window handle
                HuLoopBOT.Utilities.Logger.LogRdpInfo("Starting RDP monitoring...");
                if (_monitor.StartMonitoring(Handle))
                {
                    HuLoopBOT.Utilities.Logger.LogRdpInfo("? RDP monitoring started successfully");
                }
                else
                {
                    HuLoopBOT.Utilities.Logger.LogRdpError("Failed to start RDP monitoring", null);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                HuLoopBOT.Utilities.Logger.LogRdpError($"Failed to register for session notifications (Win32 Error: {error})", null);
                HuLoopBOT.Utilities.Logger.LogRdpError("This may indicate insufficient permissions or Terminal Services not available", null);
            }
        }
        catch (Exception ex)
        {
            HuLoopBOT.Utilities.Logger.LogRdpError("CRITICAL: Error in OnHandleCreated", ex);
        }
    }

    private void OnHandleDestroyed(object? sender, EventArgs e)
    {
        try
        {
            if (_registered)
            {
                HuLoopBOT.Utilities.Logger.LogRdpInfo("Unregistering WTS session notifications");

                // Stop monitoring
                _monitor.StopMonitoring(Handle);

                // Unregister from session notifications
                if (WTSUnRegisterSessionNotification(Handle))
                {
                    _registered = false;
                    HuLoopBOT.Utilities.Logger.LogRdpInfo("? Unregistered from WTS session notifications");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    HuLoopBOT.Utilities.Logger.LogRdpWarning($"Failed to unregister session notifications (Error: {error})");
                }
            }
        }
        catch (Exception ex)
        {
            HuLoopBOT.Utilities.Logger.LogRdpError("Error in OnHandleDestroyed", ex);
        }
    }

    /// <summary>
    /// Process Windows messages - specifically looking for WTS session change notifications
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        try
        {
            if (m.Msg == WM_WTSSESSION_CHANGE)
            {
                int eventType = m.WParam.ToInt32();
                int sessionId = m.LParam.ToInt32();

                HuLoopBOT.Utilities.Logger.LogRdpInfo($"WTS Session Change: Event={eventType}, SessionId={sessionId}");

                // Forward to monitor for processing
                _monitor.ProcessSessionChange(eventType, sessionId);
            }
        }
        catch (Exception ex)
        {
            HuLoopBOT.Utilities.Logger.LogRdpError("Error processing WndProc message", ex);
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (_registered && Handle != IntPtr.Zero)
                {
                    _monitor.StopMonitoring(Handle);
                    WTSUnRegisterSessionNotification(Handle);
                    _registered = false;
                }
            }
            catch (Exception ex)
            {
                HuLoopBOT.Utilities.Logger.LogRdpError("Error disposing ServiceMessageWindow", ex);
            }
        }

        base.Dispose(disposing);
    }
}
