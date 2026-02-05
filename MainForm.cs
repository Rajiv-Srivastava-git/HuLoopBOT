using HuLoopBOT.Services;
using HuLoopBOT.Utilities;
using System.Diagnostics;

namespace HuLoopBOT;

public partial class MainForm : Form
{
    private readonly bool _isAdmin;
    private RdpSessionMonitor? _rdpMonitor;
    private readonly UserManagementService _userService;
    private LocalUser? _selectedUser;

    public MainForm()
    {
        InitializeComponent();

        _isAdmin = AdminService.IsAdmin();
        _userService = new UserManagementService();

        UpdateAdminStatus();
        ConfigureButtonsForPrivilegeLevel();
        ConfigureRestartButton();
        LoadUsers();

        // Initialize RDP session monitor if admin
        if (_isAdmin)
        {
            InitializeRdpMonitor();
        }
    }

    private void cmbUsers_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbUsers.SelectedItem is LocalUser selectedUser)
        {
            _selectedUser = selectedUser;
            Logger.LogInformation($"User selected: {selectedUser.Username} ({selectedUser.FullName})");
            UpdateUserSelectionStatus();
        }
    }

    private void btnRefreshUsers_Click(object sender, EventArgs e)
    {
        RefreshUserList();
    }

    private void LoadUsers()
    {
        try
        {
            Logger.LogVerbose("Loading local users into dropdown");

            cmbUsers.Items.Clear();
            cmbUsers.DisplayMember = "DisplayName";

            var users = _userService.GetLocalUsers();

            if (users.Count == 0)
            {
                Logger.LogWarning("No local users found");
                cmbUsers.Items.Add("No users found");
                cmbUsers.Enabled = false;
                return;
            }

            // Add all enabled users
            foreach (var user in users.Where(u => u.IsEnabled))
            {
                cmbUsers.Items.Add(user);
            }

            Logger.LogInformation($"Loaded {cmbUsers.Items.Count} enabled users into dropdown");

            // Auto-select current user
            var currentUser = _userService.GetCurrentUser();
            if (currentUser != null)
            {
                for (int i = 0; i < cmbUsers.Items.Count; i++)
                {
                    if (cmbUsers.Items[i] is LocalUser user &&
                        user.Username.Equals(currentUser.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbUsers.SelectedIndex = i;
                        Logger.LogVerbose($"Auto-selected current user: {currentUser.Username}");
                        break;
                    }
                }
            }

            // If no selection, select first user
            if (cmbUsers.SelectedIndex == -1 && cmbUsers.Items.Count > 0)
            {
                cmbUsers.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to load users", ex);
            MessageBox.Show(
      $"Failed to load user list:\n\n{ex.Message}",
              "Error Loading Users",
        MessageBoxButtons.OK,
          MessageBoxIcon.Error);
        }
    }

    private void RefreshUserList()
    {
        try
        {
            Logger.LogInformation("Refreshing user list");

            btnRefreshUsers.Enabled = false;
            btnRefreshUsers.Text = "...";

            Application.DoEvents();

            LoadUsers();

            btnRefreshUsers.Text = "\u27F3";
            btnRefreshUsers.Enabled = true;

            Logger.LogInformation("User list refreshed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to refresh user list", ex);
            btnRefreshUsers.Text = "\u27F3";
            btnRefreshUsers.Enabled = true;
        }
    }

    private void UpdateUserSelectionStatus()
    {
        if (_selectedUser != null)
        {
            string currentUserName = Environment.UserName;
            bool isCurrentUser = _selectedUser.Username.Equals(currentUserName, StringComparison.OrdinalIgnoreCase);

            if (!isCurrentUser && _isAdmin)
            {
                Logger.LogVerbose($"Selected user ({_selectedUser.Username}) is different from current user ({currentUserName})");
            }
        }
    }

    private void InitializeRdpMonitor()
    {
        try
        {
            Logger.LogVerbose("Initializing RDP session monitor");

            _rdpMonitor = new RdpSessionMonitor();
            _rdpMonitor.AutoTransferOnDisconnect = true;

            // Start monitoring after form handle is created
            if (Handle != IntPtr.Zero)
            {
                bool started = _rdpMonitor.StartMonitoring(Handle);
                if (started)
                {
                    Logger.LogInformation("RDP session auto-transfer monitoring is active");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to initialize RDP session monitor", ex);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Start RDP monitoring when handle is available
        if (_isAdmin && _rdpMonitor != null && !_rdpMonitor.IsMonitoring)
        {
            _rdpMonitor.StartMonitoring(Handle);
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_WTSSESSION_CHANGE = 0x02B1;

        // Process session change messages
        if (m.Msg == WM_WTSSESSION_CHANGE && _rdpMonitor != null)
        {
            int eventType = m.WParam.ToInt32();
            int sessionId = m.LParam.ToInt32();

            _rdpMonitor.ProcessSessionChange(eventType, sessionId);
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Stop RDP monitoring when form is closing
        if (_rdpMonitor != null && _rdpMonitor.IsMonitoring)
        {
            _rdpMonitor.StopMonitoring(Handle);
            _rdpMonitor.Dispose();
        }

        base.OnFormClosing(e);
    }

    private void UpdateAdminStatus()
    {
        if (_isAdmin)
        {
            lblAdminStatus.Text = "\u2713 Running as Administrator"; // ✓
            lblAdminStatus.ForeColor = Color.Green;
            lblInfo.Text = "All operations are available. RDP auto-transfer is active.";
            lblInfo.ForeColor = Color.Green;
        }
        else
        {
            lblAdminStatus.Text = "\u26A0 Not Administrator - Limited Features"; // ⚠
            lblAdminStatus.ForeColor = Color.Orange;
            lblInfo.Text = "Administrator privileges are required to modify system settings.";
            lblInfo.ForeColor = Color.Gray;
        }

        // Ensure proper font for Unicode characters
        lblAdminStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
    }

    private void ConfigureRestartButton()
    {
        // Only show restart button for non-admin users
        btnRestartAsAdmin.Visible = !_isAdmin;

        if (_isAdmin)
        {
            // Adjust layout when restart button is hidden
            btnConfigureAll.Location = new Point(20, 165);
            lblTransferStatus.Location = new Point(20, 230);
            btnTransferSession.Location = new Point(20, 260);
            btnVerifySettings.Location = new Point(258, 260);
            ClientSize = new Size(500, 325);
        }

        // Update transfer session status
        UpdateTransferSessionStatus();
    }

    private void ConfigureButtonsForPrivilegeLevel()
    {
        if (!_isAdmin)
        {
            // Disable operation buttons for non-admin users
            btnConfigureAll.Enabled = false;
            btnTransferSession.Enabled = false;

            // Update button text to show admin required
            btnConfigureAll.Text = "Configure All Settings (Admin Required)";
            btnTransferSession.Text = "Transfer Session (Admin Required)";

            Logger.LogInformation("UI loaded in non-admin mode - operations disabled");
        }
        else
        {
            Logger.LogInformation("UI loaded in admin mode - all operations enabled");
        }

        // Verify button is always enabled (read-only operation)
        btnVerifySettings.Enabled = true;
    }

    private void btnTransferSession_Click(object sender, EventArgs e)
    {
        if (!_isAdmin)
        {
            ShowAdminRequiredMessage();
            return;
        }

        // Toggle between enable and disable
        bool isServiceRunning = RdpMonitoringService.IsServiceRunning();

        if (isServiceRunning)
        {
            // Stop service
            StopTransferSessionService();
        }
        else
        {
            // Start service
            EnableTransferSessionService();
        }

        UpdateTransferSessionStatus();
    }

    private void EnableTransferSessionService()
    {
        try
        {
            Logger.LogInformation("User requested to enable RDP session transfer service");

            // Check if service is installed
            if (!RdpMonitoringService.IsServiceInstalled())
            {
                var installResult = MessageBox.Show(
                   "RDP Monitoring Service is not installed.\n\n" +
                        "The service needs to be installed first. This is a one-time operation.\n\n" +
                     "The service will run independently in the background and continue monitoring even after you close this application.\n\n" +
                      "Do you want to install and start the service now?",
          "Service Installation Required",
               MessageBoxButtons.YesNo,
                 MessageBoxIcon.Question);

                if (installResult != DialogResult.Yes)
                {
                    Logger.LogInformation("User cancelled service installation");
                    return;
                }

                // Install the service
                Logger.LogInformation("Installing RDP Monitoring Service...");
                if (!RdpMonitoringService.InstallService())
                {
                    MessageBox.Show(
                "Failed to install RDP Monitoring Service.\n\n" +
                    "Possible reasons:\n" +
                "• Service executable (HuLoopBOT_Service.exe) not found\n" +
              "• Insufficient permissions\n" +
                       "• Service already exists\n\n" +
               "Falling back to in-process monitoring.\n\n" +
                  "Note: Monitoring will stop when you close this application.",
            "Service Installation Failed",
                MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

                    // Fall back to in-process monitoring
                    EnableTransferSession();
                    return;
                }

                Logger.LogInformation("Service installed successfully");
                MessageBox.Show(
"RDP Monitoring Service installed successfully!\n\n" +
        "The service will now be started.",
       "Service Installed",
               MessageBoxButtons.OK,
           MessageBoxIcon.Information);
            }

            // Enable in registry
            RdpMonitoringService.EnableService();

            // Start service
            if (RdpMonitoringService.StartService())
            {
                MessageBox.Show(
                 "RDP Session Transfer Service has been enabled successfully!\n\n" +
                       "✓ The service is now running in the background\n" +
                "✓ Sessions will be automatically transferred to console on RDP disconnect\n" +
                 "✓ Monitoring will continue even after you close this application\n\n" +
             "You can verify the service is running by:\n" +
                "• Opening Services (services.msc)\n" +
                   "• Looking for 'HuLoop BOT - RDP Session Monitor'\n\n" +
                  "Status: Active ✓",
                  "Transfer Session Service Enabled",
                MessageBoxButtons.OK,
              MessageBoxIcon.Information);

                Logger.LogInformation("RDP session transfer service enabled and started");
            }
            else
            {
                MessageBox.Show(
           "Failed to start RDP Session Transfer Service.\n\n" +
        "The service is installed but could not be started.\n" +
           "Please check:\n" +
        "• Windows Event Viewer for error details\n" +
        "• That you have Administrator privileges\n\n" +
           "Falling back to in-process monitoring.\n\n" +
          "Note: Monitoring will stop when you close this application.",
           "Service Start Failed",
         MessageBoxButtons.OK,
          MessageBoxIcon.Warning);

                // Fall back to in-process monitoring
                EnableTransferSession();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error enabling RDP session transfer service", ex);
            MessageBox.Show(
               $"An error occurred while enabling transfer session service:\n\n{ex.Message}\n\n" +
                      "Check the Event Viewer for more details.\n\n" +
       "Falling back to in-process monitoring.",
               "Error",
               MessageBoxButtons.OK,
            MessageBoxIcon.Error);

            // Fall back to in-process monitoring
            EnableTransferSession();
        }
    }

    private void EnableTransferSession()
    {
        // In-process monitoring fallback
        if (_rdpMonitor != null && _rdpMonitor.IsMonitoring)
        {
            Logger.LogInformation("In-process RDP monitoring is already active");
            return;
        }

        InitializeRdpMonitor();
        Logger.LogInformation("In-process RDP monitoring started as fallback");
    }

    private void StopTransferSessionService()
    {
        try
        {
            Logger.LogInformation("User requested to stop RDP session transfer service");

            var confirmResult = MessageBox.Show(
            "Are you sure you want to stop RDP Session Transfer Service?\n\n" +
              "⚠️ WARNING: When stopped, RDP sessions will NOT be automatically transferred to console on disconnect.\n\n" +
                  "This means:\n" +
            "• Your session may become inaccessible after disconnect\n" +
          "• You may lose access to running applications\n" +
                    "• Manual intervention may be required to recover the session\n\n" +
                     "Do you want to continue?",
                "Stop Transfer Session Service - Confirmation",
                   MessageBoxButtons.YesNo,
               MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes)
            {
                Logger.LogInformation("User cancelled stopping RDP session transfer service");
                return;
            }

            // Disable in registry
            RdpMonitoringService.DisableService();

            // Stop service
            if (RdpMonitoringService.StopService())
            {
                MessageBox.Show(
               "RDP Session Transfer Service has been stopped.\n\n" +
            "⚠️ Auto-transfer is now DISABLED.\n\n" +
            "RDP sessions will no longer be automatically transferred to console when you disconnect.\n\n" +
         "Status: Inactive\n\n" +
       "Note: You can re-enable it anytime using the 'Enable Transfer Session' button.",
            "Transfer Session Service Stopped",
               MessageBoxButtons.OK,
          MessageBoxIcon.Information);

                Logger.LogWarning("RDP session transfer service stopped by user - auto-transfer disabled");
            }
            else
            {
                MessageBox.Show(
                  "Failed to stop RDP Session Transfer Service.\n\n" +
                  "Please check the Event Viewer logs for more details.",
              "Stop Service Failed",
                        MessageBoxButtons.OK,
               MessageBoxIcon.Error);

                Logger.LogError("Failed to stop RDP session transfer service", null);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error stopping RDP session transfer service", ex);
            MessageBox.Show(
      $"An error occurred while stopping transfer session service:\n\n{ex.Message}",
              "Error",
             MessageBoxButtons.OK,
        MessageBoxIcon.Error);
        }
    }

    private void UpdateTransferSessionStatus()
    {
        // Check if service is running
        bool isServiceRunning = RdpMonitoringService.IsServiceRunning();

        // Fall back to checking in-process monitor
        bool isMonitoring = isServiceRunning || (_rdpMonitor != null && _rdpMonitor.IsMonitoring);

        if (isMonitoring)
        {
            lblTransferStatus.Text = isServiceRunning ? "Status: Active ✓ (Service)" : "Status: Active ✓ (In-Process)";
            lblTransferStatus.ForeColor = Color.Green;
            btnTransferSession.Text = "Stop Transfer Session";
            btnTransferSession.BackColor = Color.FromArgb(231, 76, 60); // Red
        }
        else
        {
            lblTransferStatus.Text = "Status: Inactive";
            lblTransferStatus.ForeColor = Color.Gray;
            btnTransferSession.Text = "Enable Transfer Session";
            btnTransferSession.BackColor = Color.FromArgb(52, 152, 219); // Blue
        }
    }

    private void btnConfigureAll_Click(object sender, EventArgs e)
    {
        if (!_isAdmin)
        {
            ShowAdminRequiredMessage();
            return;
        }

        if (_selectedUser == null)
        {
            ShowNoUserSelectedMessage();
            return;
        }

        var confirmResult = MessageBox.Show(
  $"This will configure ALL settings for user '{_selectedUser.Username}':\n\n" +
        "1. Enable Auto Login (will prompt for password)\n" +
   "2. Disable Sleep & Hibernate\n" +
     "3. Disable Screen Timeout\n" +
     "4. Disable Lock Screen\n" +
            "5. Disable Screen Saver\n" +
        "6. Disable RDP Timeouts (Idle, Disconnected, Connection)\n\n" +
"Do you want to continue?",
         "Configure All Settings",
       MessageBoxButtons.YesNo,
    MessageBoxIcon.Question);

        if (confirmResult != DialogResult.Yes)
            return;

        try
        {
            var results = new List<string>();
            var errors = new List<string>();

            // 1. Enable Auto Login
            using (var passwordForm = new PasswordInputForm(_selectedUser.Username))
            {
                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    var autoLoginService = new AutoLoginService();
                    var result = autoLoginService.EnableAutoLogin(_selectedUser.Username, passwordForm.Password);
                    if (result.Success)
                        results.Add("✓ Auto Login enabled");
                    else
                        errors.Add($"✗ Auto Login: {result.Message}");
                }
                else
                {
                    errors.Add("✗ Auto Login: Password not provided");
                }
            }

            // 2. Disable Sleep & Hibernate
            var powerService = new PowerService();
            var sleepResult = powerService.DisableSleep();
            if (sleepResult.Success)
                results.Add("✓ Sleep & Hibernate disabled");
            else
                errors.Add($"✗ Sleep: {sleepResult.Message}");

            // 3. Disable Screen Timeout
            var timeoutResult = powerService.DisableScreenTimeout();
            if (timeoutResult.Success)
                results.Add("✓ Screen Timeout disabled");
            else
                errors.Add($"✗ Screen Timeout: {timeoutResult.Message}");

            // 4. Disable Lock Screen
            var lockService = new LockService();
            var lockResult = lockService.DisableLockScreen();
            if (lockResult.Success)
                results.Add("✓ Lock Screen disabled");
            else
                errors.Add($"✗ Lock Screen: {lockResult.Message}");

            // 5. Disable Screen Saver
            var screenSaverService = new ScreenSaverService();
            var screenSaverResult = screenSaverService.DisableScreenSaverForUser(_selectedUser.Username);
            if (screenSaverResult.Success)
                results.Add("✓ Screen Saver disabled");
            else
                errors.Add($"✗ Screen Saver: {screenSaverResult.Message}");

            // 6. Disable RDP Timeouts
            var rdpService = new RdpService();
            var rdpResult = rdpService.PreventTimeout();
            if (rdpResult.Success)
                results.Add("✓ RDP Timeouts disabled (Idle, Disconnected, Connection)");
            else
                errors.Add($"✗ RDP Timeouts: {rdpResult.Message}");

            // Show summary
            var summary = "Configuration Complete!\n\n";

            if (results.Count > 0)
            {
                summary += "Successful Operations:\n" + string.Join("\n", results) + "\n\n";
            }

            if (errors.Count > 0)
            {
                summary += "Failed Operations:\n" + string.Join("\n", errors);
            }

            var icon = errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
            MessageBox.Show(summary, "Configuration Summary", MessageBoxButtons.OK, icon);

            Logger.LogInformation($"Configure All completed for user {_selectedUser.Username}. Success: {results.Count}, Failures: {errors.Count}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in btnConfigureAll_Click", ex);
            ShowResult(false, $"Error: {ex.Message}");
        }
    }

    private void btnVerifySettings_Click(object sender, EventArgs e)
    {
        try
        {
            Logger.LogInformation("User requested system settings verification");

            // Show loading message
            btnVerifySettings.Enabled = false;
            btnVerifySettings.Text = "Verifying...";
            Application.DoEvents();

            // Get selected username
            string? selectedUsername = _selectedUser?.Username;

            if (string.IsNullOrEmpty(selectedUsername))
            {
                Logger.LogWarning("No user selected for verification, checking system-wide settings only");
            }
            else
            {
                Logger.LogInformation($"Verifying settings for user: {selectedUsername}");
            }

            // Create verification service and run checks
            var verificationService = new SystemVerificationService();
            var results = verificationService.VerifyAllSettings(selectedUsername);

            // Show the enhanced report form
            using (var reportForm = new VerificationReportForm(results, selectedUsername))
            {
                reportForm.ShowDialog();
            }

            var successCount = results.Count(r => r.IsConfigured);
            var totalCount = results.Count;

            string logMessage = string.IsNullOrEmpty(selectedUsername)
              ? $"System verification completed. Configured: {successCount}/{totalCount}"
                : $"System verification completed for user {selectedUsername}. Configured: {successCount}/{totalCount}";

            Logger.LogInformation(logMessage);

            // Restore button
            btnVerifySettings.Text = "Verify System Settings";
            btnVerifySettings.Enabled = true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in btnVerifySettings_Click", ex);
            MessageBox.Show(
     $"An error occurred while verifying system settings:\n\n{ex.Message}",
     "Verification Error",
         MessageBoxButtons.OK,
         MessageBoxIcon.Error);

            // Restore button
            btnVerifySettings.Text = "Verify System Settings";
            btnVerifySettings.Enabled = true;
        }
    }

    private void ShowResult(bool success, string message)
    {
        var icon = success ? MessageBoxIcon.Information : MessageBoxIcon.Error;
        var title = success ? "Success" : "Error";

        Logger.Log(message);
        MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
    }

    private void ShowAdminRequiredMessage()
    {
        var result = MessageBox.Show(
        "Administrator privileges are required for this operation.\n\n" +
               "Would you like to restart the application as Administrator?",
       "Administrator Required",
           MessageBoxButtons.YesNo,
       MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            RestartAsAdmin();
        }
    }

    private void ShowNoUserSelectedMessage()
    {
        MessageBox.Show(
      "Please select a user from the dropdown list before performing this operation.",
         "No User Selected",
   MessageBoxButtons.OK,
    MessageBoxIcon.Warning);
    }

    private void btnRestartAsAdmin_Click(object sender, EventArgs e)
    {
        RestartAsAdmin();
    }

    private void RestartAsAdmin()
    {
        try
        {
            Logger.LogInformation("User requested to restart application with elevated privileges");

            var psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Logger.LogVerbose($"Starting elevated process: {psi.FileName}");

            Process.Start(psi);

            Logger.LogInformation("Elevated process started, closing current instance");
            Application.Exit();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            if (ex.NativeErrorCode == 1223) // User cancelled UAC
            {
                Logger.LogWarning("User cancelled UAC elevation prompt");
                MessageBox.Show(
                      "Elevation cancelled. The application will continue in limited mode.",
               "Cancelled",
                    MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            }
            else
            {
                Logger.LogError($"Failed to restart as admin. Error Code: {ex.NativeErrorCode}", ex);
                MessageBox.Show(
           $"Failed to restart with elevated privileges.\n\nError: {ex.Message}",
      "Error",
       MessageBoxButtons.OK,
MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Unexpected error during restart as admin", ex);
            MessageBox.Show(
           $"An unexpected error occurred:\n\n{ex.Message}",
          "Error",
        MessageBoxButtons.OK,
          MessageBoxIcon.Error);
        }
    }
}
