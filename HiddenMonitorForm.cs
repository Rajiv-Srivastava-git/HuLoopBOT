using HuLoopBOT.Services;
using HuLoopBOT.Utilities;
using System.Diagnostics;

namespace HuLoopBOT;

/// <summary>
/// Hidden form that runs in background to monitor RDP sessions
/// </summary>
public class HiddenMonitorForm : Form
{
    private RdpSessionMonitor? _monitor;
    private NotifyIcon? _trayIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;

    public HiddenMonitorForm()
    {
        // Make form completely hidden
        this.ShowInTaskbar = false;
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Minimized;
        this.Opacity = 0;
        this.Width = 0;
        this.Height = 0;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(-10000, -10000);

        // Create system tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "HuLoop BOT - RDP Monitor Active",
            Visible = true
        };

        _trayIcon.ContextMenuStrip = CreateContextMenu();
        _trayIcon.DoubleClick += (s, e) => ShowStatus();

        // Status update timer
        _statusTimer = new System.Windows.Forms.Timer();
        _statusTimer.Interval = 5000; // 5 seconds
        _statusTimer.Tick += (s, e) => UpdateTrayIconStatus();
        _statusTimer.Start();

        // Initialize monitoring
        InitializeMonitoring();

        Logger.LogInformation("Hidden monitor form initialized");
    }

    private void InitializeMonitoring()
    {
        try
        {
            _monitor = new RdpSessionMonitor();
            _monitor.AutoTransferOnDisconnect = true;

            if (Handle != IntPtr.Zero)
            {
                bool started = _monitor.StartMonitoring(Handle);
                if (started)
                {
                    Logger.LogInformation("Background RDP monitoring started successfully");
                    ShowBalloonTip("RDP Monitor Started", "RDP session monitoring is now active.", ToolTipIcon.Info);
                }
                else
                {
                    Logger.LogError("Failed to start background RDP monitoring", null);
                    ShowBalloonTip("RDP Monitor Error", "Failed to start RDP monitoring.", ToolTipIcon.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error initializing RDP monitoring", ex);
            ShowBalloonTip("RDP Monitor Error", $"Error: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = menu.Items.Add("? RDP Monitor - Active");
        statusItem.Enabled = false;
        statusItem.Font = new Font(statusItem.Font, FontStyle.Bold);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Show Status", null, (s, e) => ShowStatus());
        menu.Items.Add("Open Configuration", null, (s, e) => OpenConfiguration());

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit Monitor", null, (s, e) => ExitMonitor());

        return menu;
    }

    private void UpdateTrayIconStatus()
    {
        if (_monitor != null && _monitor.IsMonitoring)
        {
            _trayIcon.Text = $"HuLoop BOT - RDP Monitor Active\nMonitoring: Enabled\nStatus: Running";
        }
        else
        {
            _trayIcon.Text = "HuLoop BOT - RDP Monitor\nStatus: Error - Not Monitoring";
        }
    }

    private void ShowStatus()
    {
        var status = _monitor != null && _monitor.IsMonitoring ? "Active" : "Inactive";
        var sessionInfo = $"Current Session: {Environment.UserName}\nMonitoring: {status}";

        MessageBox.Show(
          $"HuLoop BOT - RDP Session Monitor\n\n{sessionInfo}\n\nThe monitor is running in the background and will automatically transfer RDP sessions to console on disconnect.",
       "RDP Monitor Status",
   MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OpenConfiguration()
    {
        try
        {
            var configExe = Application.ExecutablePath;

            // Start configuration tool without /monitor argument
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = configExe,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(configExe)
                }
            };

            process.Start();
            Logger.LogInformation("Opened configuration tool from monitor");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to open configuration tool", ex);
            MessageBox.Show(
        $"Failed to open configuration tool:\n\n{ex.Message}",
           "Error",
   MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitMonitor()
    {
        var result = MessageBox.Show(
           "Are you sure you want to stop RDP session monitoring?\n\n" +
                "?? WARNING: RDP sessions will no longer be automatically transferred to console on disconnect.\n\n" +
                "You can re-enable monitoring from the configuration tool.",
           "Stop RDP Monitor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            Logger.LogInformation("User requested to exit RDP monitor");
            ShowBalloonTip("RDP Monitor Stopped", "RDP session monitoring has been stopped.", ToolTipIcon.Warning);
            Application.Exit();
        }
    }

    private void ShowBalloonTip(string title, string text, ToolTipIcon icon)
    {
        try
        {
            _trayIcon?.ShowBalloonTip(3000, title, text, icon);
        }
        catch
        {
            // Ignore balloon tip errors
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_WTSSESSION_CHANGE = 0x02B1;

        if (m.Msg == WM_WTSSESSION_CHANGE && _monitor != null)
        {
            int eventType = m.WParam.ToInt32();
            int sessionId = m.LParam.ToInt32();
            _monitor.ProcessSessionChange(eventType, sessionId);
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _statusTimer?.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_monitor != null)
            {
                _monitor.StopMonitoring(Handle);
                _monitor.Dispose();
                _monitor = null;
            }

            _statusTimer?.Dispose();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        base.Dispose(disposing);
    }
}
