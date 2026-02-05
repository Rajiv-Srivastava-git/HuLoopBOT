namespace HuLoopBOT;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblAdminStatus;
    private Label lblInfo;
    private Label lblSelectUser;
    private ComboBox cmbUsers;
    private Button btnRefreshUsers;
    private Button btnRestartAsAdmin;
    private Button btnConfigureAll;
    private Button btnTransferSession;
    private Label lblTransferStatus;
    private Button btnVerifySettings;
    private ToolTip toolTip1;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        lblAdminStatus = new Label();
        lblInfo = new Label();
        lblSelectUser = new Label();
        cmbUsers = new ComboBox();
        btnRefreshUsers = new Button();
        btnRestartAsAdmin = new Button();
        btnConfigureAll = new Button();
        btnTransferSession = new Button();
        lblTransferStatus = new Label();
        btnVerifySettings = new Button();
        toolTip1 = new ToolTip(components);
        SuspendLayout();
        // 
        // lblAdminStatus
        // 
        lblAdminStatus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblAdminStatus.Location = new Point(20, 20);
        lblAdminStatus.Name = "lblAdminStatus";
        lblAdminStatus.Size = new Size(460, 35);
        lblAdminStatus.TabIndex = 0;
        lblAdminStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfo
        // 
        lblInfo.Font = new Font("Segoe UI", 9F);
        lblInfo.ForeColor = Color.Gray;
        lblInfo.Location = new Point(20, 60);
        lblInfo.Name = "lblInfo";
        lblInfo.Size = new Size(460, 50);
        lblInfo.TabIndex = 1;
        lblInfo.Text = "Administrator privileges are required to modify system settings.";
        // 
        // lblSelectUser
        // 
        lblSelectUser.Font = new Font("Segoe UI", 10F);
        lblSelectUser.Location = new Point(20, 120);
        lblSelectUser.Name = "lblSelectUser";
        lblSelectUser.Size = new Size(85, 30);
        lblSelectUser.TabIndex = 2;
        lblSelectUser.Text = "Select User:";
        lblSelectUser.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbUsers
        // 
        cmbUsers.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbUsers.Font = new Font("Segoe UI", 10F);
        cmbUsers.Location = new Point(110, 120);
        cmbUsers.Name = "cmbUsers";
        cmbUsers.Size = new Size(310, 31);
        cmbUsers.TabIndex = 3;
        cmbUsers.SelectedIndexChanged += cmbUsers_SelectedIndexChanged;
        // 
        // btnRefreshUsers
        // 
        btnRefreshUsers.BackColor = Color.FromArgb(236, 240, 241);
        btnRefreshUsers.Cursor = Cursors.Hand;
        btnRefreshUsers.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
        btnRefreshUsers.FlatStyle = FlatStyle.Flat;
        btnRefreshUsers.Font = new Font("Segoe UI Symbol", 11F, FontStyle.Bold);
        btnRefreshUsers.ForeColor = Color.FromArgb(52, 73, 94);
        btnRefreshUsers.ImageAlign = ContentAlignment.TopCenter;
        btnRefreshUsers.Location = new Point(430, 120);
        btnRefreshUsers.Name = "btnRefreshUsers";
        btnRefreshUsers.Size = new Size(50, 35);
        btnRefreshUsers.TabIndex = 4;
        btnRefreshUsers.Text = "↻";
        btnRefreshUsers.TextAlign = ContentAlignment.TopCenter;
        btnRefreshUsers.TextImageRelation = TextImageRelation.Overlay;
        btnRefreshUsers.Padding = new Padding(0);
        toolTip1.SetToolTip(btnRefreshUsers, "Refresh user list");
        btnRefreshUsers.UseVisualStyleBackColor = false;
        btnRefreshUsers.Click += btnRefreshUsers_Click;
        // 
        // btnRestartAsAdmin
        // 
        btnRestartAsAdmin.BackColor = Color.FromArgb(0, 120, 215);
        btnRestartAsAdmin.Cursor = Cursors.Hand;
        btnRestartAsAdmin.FlatAppearance.BorderSize = 0;
        btnRestartAsAdmin.FlatStyle = FlatStyle.Flat;
        btnRestartAsAdmin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnRestartAsAdmin.ForeColor = Color.White;
        btnRestartAsAdmin.Location = new Point(20, 165);
        btnRestartAsAdmin.Name = "btnRestartAsAdmin";
        btnRestartAsAdmin.Size = new Size(460, 45);
        btnRestartAsAdmin.TabIndex = 5;
        btnRestartAsAdmin.Text = "🔒 Restart as Administrator";
        btnRestartAsAdmin.UseVisualStyleBackColor = false;
        btnRestartAsAdmin.Click += btnRestartAsAdmin_Click;
        // 
        // btnConfigureAll
        // 
        btnConfigureAll.BackColor = Color.FromArgb(46, 204, 113);
        btnConfigureAll.Cursor = Cursors.Hand;
        btnConfigureAll.FlatAppearance.BorderSize = 0;
        btnConfigureAll.FlatStyle = FlatStyle.Flat;
        btnConfigureAll.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnConfigureAll.ForeColor = Color.White;
        btnConfigureAll.Location = new Point(20, 225);
        btnConfigureAll.Name = "btnConfigureAll";
        btnConfigureAll.Size = new Size(460, 50);
        btnConfigureAll.TabIndex = 6;
        btnConfigureAll.Text = "Configure All Settings";
        btnConfigureAll.UseVisualStyleBackColor = false;
        btnConfigureAll.Click += btnConfigureAll_Click;
        // 
        // btnTransferSession
        // 
        btnTransferSession.BackColor = Color.FromArgb(52, 152, 219);
        btnTransferSession.Cursor = Cursors.Hand;
        btnTransferSession.FlatAppearance.BorderSize = 0;
        btnTransferSession.FlatStyle = FlatStyle.Flat;
        btnTransferSession.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnTransferSession.ForeColor = Color.White;
        btnTransferSession.Location = new Point(20, 320);
        btnTransferSession.Name = "btnTransferSession";
        btnTransferSession.Size = new Size(222, 45);
        btnTransferSession.TabIndex = 8;
        btnTransferSession.Text = "Enable Transfer Session";
        btnTransferSession.UseVisualStyleBackColor = false;
        btnTransferSession.Click += btnTransferSession_Click;
        // 
        // lblTransferStatus
        // 
        lblTransferStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        lblTransferStatus.ForeColor = Color.Gray;
        lblTransferStatus.Location = new Point(20, 290);
        lblTransferStatus.Name = "lblTransferStatus";
        lblTransferStatus.Size = new Size(460, 25);
        lblTransferStatus.TabIndex = 7;
        lblTransferStatus.Text = "Status: Inactive";
        lblTransferStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnVerifySettings
        // 
        btnVerifySettings.BackColor = Color.FromArgb(142, 68, 173);
        btnVerifySettings.Cursor = Cursors.Hand;
        btnVerifySettings.FlatAppearance.BorderSize = 0;
        btnVerifySettings.FlatStyle = FlatStyle.Flat;
        btnVerifySettings.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnVerifySettings.ForeColor = Color.White;
        btnVerifySettings.Location = new Point(258, 320);
        btnVerifySettings.Name = "btnVerifySettings";
        btnVerifySettings.Size = new Size(222, 45);
        btnVerifySettings.TabIndex = 9;
        btnVerifySettings.Text = "Verify System Settings";
        btnVerifySettings.UseVisualStyleBackColor = false;
        btnVerifySettings.Click += btnVerifySettings_Click;
        // 
        // MainForm
        // 
        BackColor = Color.White;
        ClientSize = new Size(500, 385);
        Controls.Add(lblAdminStatus);
        Controls.Add(lblInfo);
        Controls.Add(lblSelectUser);
        Controls.Add(cmbUsers);
        Controls.Add(btnRefreshUsers);
        Controls.Add(btnRestartAsAdmin);
        Controls.Add(btnConfigureAll);
        Controls.Add(lblTransferStatus);
        Controls.Add(btnTransferSession);
        Controls.Add(btnVerifySettings);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HuLoop BOT - Machine Readiness";
        ResumeLayout(false);
    }
}
