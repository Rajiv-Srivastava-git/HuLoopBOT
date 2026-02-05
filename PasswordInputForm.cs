using System.Security;

namespace HuLoopBOT;

/// <summary>
/// Form for securely collecting user password for Auto Login configuration
/// </summary>
public class PasswordInputForm : Form
{
    private Label lblUsername;
    private Label lblPassword;
    private Label lblConfirmPassword;
    private TextBox txtPassword;
    private TextBox txtConfirmPassword;
    private Button btnOk;
    private Button btnCancel;
    private Label lblInfo;
    private CheckBox chkShowPassword;

    public SecureString Password { get; private set; }

    public PasswordInputForm(string username)
    {
      Password = new SecureString();
        InitializeComponents(username);
    }

    private void InitializeComponents(string username)
    {
        // Form settings
        Text = "Enter Password for Auto Login";
  Size = new Size(450, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
   MaximizeBox = false;
        MinimizeBox = false;
Font = new Font("Segoe UI", 9F);

        // lblUsername
        lblUsername = new Label
        {
   Text = $"User: {username}",
    Location = new Point(20, 20),
       Size = new Size(400, 25),
  Font = new Font("Segoe UI", 10F, FontStyle.Bold)
    };

        // lblInfo
        lblInfo = new Label
        {
 Text = "Enter the password for this user account.\nThis password will be stored encrypted by Windows for auto login.",
    Location = new Point(20, 50),
            Size = new Size(400, 40),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.Gray
        };

        // lblPassword
    lblPassword = new Label
        {
          Text = "Password:",
            Location = new Point(20, 100),
   Size = new Size(100, 23),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // txtPassword
   txtPassword = new TextBox
 {
            Location = new Point(120, 100),
    Size = new Size(300, 23),
    UseSystemPasswordChar = true,
Font = new Font("Segoe UI", 9F)
        };
 txtPassword.TextChanged += (s, e) => ValidatePasswords();

        // lblConfirmPassword
        lblConfirmPassword = new Label
      {
            Text = "Confirm Password:",
       Location = new Point(20, 135),
  Size = new Size(100, 23),
            TextAlign = ContentAlignment.MiddleLeft
        };

 // txtConfirmPassword
        txtConfirmPassword = new TextBox
        {
    Location = new Point(120, 135),
            Size = new Size(300, 23),
            UseSystemPasswordChar = true,
          Font = new Font("Segoe UI", 9F)
      };
     txtConfirmPassword.TextChanged += (s, e) => ValidatePasswords();

        // chkShowPassword
     chkShowPassword = new CheckBox
   {
            Text = "Show password",
      Location = new Point(120, 165),
            Size = new Size(150, 23),
Font = new Font("Segoe UI", 8.5F)
        };
        chkShowPassword.CheckedChanged += (s, e) =>
        {
txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
 txtConfirmPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        };

        // btnCancel
      btnCancel = new Button
        {
        Text = "Cancel",
            Location = new Point(240, 200),
     Size = new Size(90, 30),
 DialogResult = DialogResult.Cancel,
            Font = new Font("Segoe UI", 9F)
   };

        // btnOk
        btnOk = new Button
      {
        Text = "OK",
       Location = new Point(340, 200),
         Size = new Size(90, 30),
     Enabled = false,
     Font = new Font("Segoe UI", 9F, FontStyle.Bold),
          BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
       FlatStyle = FlatStyle.Flat
        };
    btnOk.FlatAppearance.BorderSize = 0;
  btnOk.Click += BtnOk_Click;

        // Add controls
        Controls.AddRange(new Control[]
        {
         lblUsername,
            lblInfo,
     lblPassword,
            txtPassword,
   lblConfirmPassword,
            txtConfirmPassword,
            chkShowPassword,
     btnOk,
  btnCancel
        });

   AcceptButton = btnOk;
      CancelButton = btnCancel;
    }

    private void ValidatePasswords()
    {
        bool isValid = !string.IsNullOrEmpty(txtPassword.Text) &&
         txtPassword.Text == txtConfirmPassword.Text &&
    txtPassword.Text.Length >= 1; // At least 1 character

        btnOk.Enabled = isValid;

        // Visual feedback
        if (!string.IsNullOrEmpty(txtConfirmPassword.Text))
        {
            if (txtPassword.Text == txtConfirmPassword.Text)
            {
  txtConfirmPassword.BackColor = Color.LightGreen;
   }
 else
      {
    txtConfirmPassword.BackColor = Color.LightPink;
    }
        }
    else
        {
        txtConfirmPassword.BackColor = SystemColors.Window;
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        // Convert password to SecureString
      Password = new SecureString();
        foreach (char c in txtPassword.Text)
{
            Password.AppendChar(c);
  }
        Password.MakeReadOnly();

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
  // Clear password fields for security
            if (txtPassword != null)
    {
         txtPassword.Text = "";
        }
   if (txtConfirmPassword != null)
       {
   txtConfirmPassword.Text = "";
 }
        }
        base.Dispose(disposing);
    }
}
