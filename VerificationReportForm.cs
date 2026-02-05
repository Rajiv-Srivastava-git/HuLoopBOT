using HuLoopBOT.Services;

namespace HuLoopBOT;

/// <summary>
/// Enhanced form to display system verification report with better formatting
/// </summary>
public class VerificationReportForm : Form
{
    private Panel pnlHeader;
    private Label lblTitle;
    private Label lblSummary;
    private ListView listResults;
    private Panel pnlFooter;
    private Button btnClose;
    private Button btnCopyToClipboard;
    private Button btnExportReport;
    private ProgressBar progressBar;
    private Label lblProgress;

    private readonly List<SystemVerificationService.VerificationResult> _results;
    private readonly string? _username;

    public VerificationReportForm(List<SystemVerificationService.VerificationResult> results, string? username = null)
    {
        _results = results;
        _username = username;
        InitializeComponents();
        PopulateResults();
    }

    private void InitializeComponents()
    {
        // Form settings
        Text = "System Configuration Verification Report";
        Size = new Size(900, 650);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(800, 500);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(240, 240, 240);

        // Header Panel
        pnlHeader = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(900, 120),
            BackColor = Color.FromArgb(41, 128, 185),
            Dock = DockStyle.Top
        };

        // Title Label
        lblTitle = new Label
        {
            Text = "? System Configuration Verification",
            Location = new Point(20, 15),
            Size = new Size(850, 40),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false
        };

        // Summary Label
        lblSummary = new Label
        {
            Location = new Point(20, 60),
            Size = new Size(850, 50),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.White,
            AutoSize = false
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSummary });

        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(20, 130),
            Size = new Size(840, 25),
            Style = ProgressBarStyle.Continuous,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Progress Label
        lblProgress = new Label
        {
            Location = new Point(20, 160),
            Size = new Size(840, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Results ListView
        listResults = new ListView
        {
            Location = new Point(20, 195),
            Size = new Size(840, 330),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9F),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Add columns
        listResults.Columns.Add("Status", 60);
        listResults.Columns.Add("Setting", 200);
        listResults.Columns.Add("Configuration", 150);
        listResults.Columns.Add("Details", 400);

        // Footer Panel
        pnlFooter = new Panel
        {
            Size = new Size(900, 70),
            BackColor = Color.FromArgb(236, 240, 241),
            Dock = DockStyle.Bottom
        };

        // Export Button
        btnExportReport = new Button
        {
            Text = "Export Report",
            Location = new Point(20, 20),
            Size = new Size(130, 35),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExportReport.FlatAppearance.BorderSize = 0;
        btnExportReport.Click += BtnExportReport_Click;

        // Copy Button
        btnCopyToClipboard = new Button
        {
            Text = "Copy to Clipboard",
            Location = new Point(160, 20),
            Size = new Size(150, 35),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyToClipboard.FlatAppearance.BorderSize = 0;
        btnCopyToClipboard.Click += BtnCopyToClipboard_Click;

        // Close Button
        btnClose = new Button
        {
            Text = "Close",
            Location = new Point(760, 20),
            Size = new Size(100, 35),
            Font = new Font("Segoe UI", 9F),
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        btnClose.FlatAppearance.BorderSize = 0;

        pnlFooter.Controls.AddRange(new Control[] { btnExportReport, btnCopyToClipboard, btnClose });

        // Add controls to form
        Controls.AddRange(new Control[]
        {
     pnlHeader,
            progressBar,
            lblProgress,
   listResults,
            pnlFooter
    });

        AcceptButton = btnClose;
    }

    private void PopulateResults()
    {
        listResults.Items.Clear();

        int configuredCount = 0;
        int totalCount = _results.Count;

        // Update summary
        string userInfo = string.IsNullOrEmpty(_username)
        ? "System-wide configuration check"
                : $"User: {_username}";

        foreach (var result in _results)
        {
            if (result.IsConfigured)
                configuredCount++;

            // Determine icon and color - using ASCII-compatible symbols
            string icon = result.IsConfigured ? "[OK]" : "[X]";
            Color backColor = result.IsConfigured ? Color.FromArgb(232, 245, 233) : Color.FromArgb(255, 235, 238);
            Color foreColor = result.IsConfigured ? Color.FromArgb(27, 94, 32) : Color.FromArgb(183, 28, 28);

            var item = new ListViewItem(new[]
            {
                icon,
                result.Setting,
                result.Status,
                result.Details
            })
            {
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 9F, result.IsConfigured ? FontStyle.Regular : FontStyle.Bold)
            };

            listResults.Items.Add(item);
        }

        // Auto-resize columns
        foreach (ColumnHeader column in listResults.Columns)
        {
            column.Width = -2; // Auto-size to content
        }

        // Update progress bar
        int percentage = totalCount > 0 ? (configuredCount * 100 / totalCount) : 0;
        progressBar.Value = percentage;
        progressBar.ForeColor = percentage == 100 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(230, 126, 34);

        // Update labels
        lblSummary.Text = $"{userInfo}\nVerified: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        lblProgress.Text = $"Configuration Status: {configuredCount} of {totalCount} items configured ({percentage}%)";
        lblProgress.ForeColor = percentage == 100 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(230, 126, 34);

        // Update title based on results - using ASCII-compatible symbols
        if (percentage == 100)
        {
            lblTitle.Text = "[OK] All Configurations Verified - System Ready!";
            pnlHeader.BackColor = Color.FromArgb(39, 174, 96);
        }
        else if (percentage >= 50)
        {
            lblTitle.Text = "[!] Partial Configuration - Attention Required";
            pnlHeader.BackColor = Color.FromArgb(230, 126, 34);
        }
        else
        {
            lblTitle.Text = "[X] Configuration Incomplete - Action Needed";
            pnlHeader.BackColor = Color.FromArgb(231, 76, 60);
        }
    }

    private void BtnCopyToClipboard_Click(object? sender, EventArgs e)
    {
        try
        {
            string report = GenerateTextReport();
            Clipboard.SetText(report);

            MessageBox.Show(
                    "Configuration report copied to clipboard successfully!",
                "Copy Successful",
               MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                    $"Failed to copy to clipboard:\n\n{ex.Message}",
              "Copy Failed",
                   MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
        }
    }

    private void BtnExportReport_Click(object? sender, EventArgs e)
    {
        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"HuLoopBOT_Verification_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Export Verification Report"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string report = GenerateTextReport();
                File.WriteAllText(saveDialog.FileName, report);

                MessageBox.Show(
                    $"Report exported successfully to:\n\n{saveDialog.FileName}",
                 "Export Successful",
           MessageBoxButtons.OK,
                      MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
            $"Failed to export report:\n\n{ex.Message}",
           "Export Failed",
                        MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        }
    }

    private string GenerateTextReport()
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("===========================================================================");
        report.AppendLine("       HULOOPBOT - SYSTEM CONFIGURATION VERIFICATION REPORT        ");
        report.AppendLine("===========================================================================");
        report.AppendLine();

        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Machine: {Environment.MachineName}");

        if (!string.IsNullOrEmpty(_username))
        {
            report.AppendLine($"Target User: {_username}");
        }
        else
        {
            report.AppendLine("Target: System-wide configuration");
        }

        report.AppendLine();
        report.AppendLine("===========================================================================");
        report.AppendLine("  CONFIGURATION ITEMS");
        report.AppendLine("===========================================================================");
        report.AppendLine();

        int index = 1;
        int configuredCount = 0;

        foreach (var result in _results)
        {
            if (result.IsConfigured)
                configuredCount++;

            string icon = result.IsConfigured ? "[OK]" : "[X]";
            string status = result.IsConfigured ? "PASS" : "FAIL";

            report.AppendLine($"{index}. {icon} {result.Setting}");
            report.AppendLine($"   Status: {status} - {result.Status}");
            report.AppendLine($"   Details: {result.Details}");
            report.AppendLine();

            index++;
        }

        report.AppendLine("===========================================================================");
        report.AppendLine("  SUMMARY");
        report.AppendLine("===========================================================================");
        report.AppendLine();

        int totalCount = _results.Count;
        int percentage = totalCount > 0 ? (configuredCount * 100 / totalCount) : 0;

        report.AppendLine($"Total Items Checked: {totalCount}");
        report.AppendLine($"Items Configured: {configuredCount}");
        report.AppendLine($"Items Requiring Attention: {totalCount - configuredCount}");
        report.AppendLine($"Completion Percentage: {percentage}%");
        report.AppendLine();

        if (percentage == 100)
        {
            report.AppendLine("[OK] STATUS: ALL SYSTEMS GO - READY FOR OPERATION");
            report.AppendLine();
            report.AppendLine("All configuration items have been verified and are properly set.");
            report.AppendLine("Your system is ready for unattended remote operation.");
        }
        else
        {
            report.AppendLine($"[!] STATUS: CONFIGURATION INCOMPLETE ({percentage}%)");
            report.AppendLine();
            report.AppendLine("Action Required:");

            foreach (var result in _results.Where(r => !r.IsConfigured))
            {
                report.AppendLine($"  * {result.Setting} - {result.Status}");
            }

            report.AppendLine();
            report.AppendLine("Please use the 'Configure All Settings' button to complete setup.");
        }

        report.AppendLine();
        report.AppendLine("===========================================================================");
        report.AppendLine("  RECOMMENDATIONS");
        report.AppendLine("===========================================================================");
        report.AppendLine();
        report.AppendLine("1. Ensure all items show '[OK] PASS' status for optimal operation");
        report.AppendLine("2. Enable 'Transfer Session' service for RDP session monitoring");
        report.AppendLine("3. Run verification after any system configuration changes");
        report.AppendLine("4. Keep this report for compliance and troubleshooting purposes");
        report.AppendLine();
        report.AppendLine("===========================================================================");
        report.AppendLine($"End of Report - HuLoopBOT v1.0");
        report.AppendLine("===========================================================================");

        return report.ToString();
    }
}
