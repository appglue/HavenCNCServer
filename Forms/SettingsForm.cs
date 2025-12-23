using System;
using System.Windows.Forms;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Forms
{
    /// <summary>
    /// Dedicated form for application settings
    /// </summary>
    public partial class SettingsForm : Form
    {
        private Label lblCnc12Path = null!;
        private TextBox txtCnc12Path = null!;
        private Button btnBrowseCnc12Path = null!;
        private Label lblUserName = null!;
        private TextBox txtUserName = null!;
        private Label lblMachineName = null!;
        private TextBox txtMachineName = null!;
        private Button btnSaveSettings = null!;

        /// <summary>
        /// Initializes a new instance of the SettingsForm
        /// </summary>
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.lblCnc12Path = new Label();
            this.txtCnc12Path = new TextBox();
            this.btnBrowseCnc12Path = new Button();
            this.lblUserName = new Label();
            this.txtUserName = new TextBox();
            this.lblMachineName = new Label();
            this.txtMachineName = new TextBox();
            this.btnSaveSettings = new Button();
            this.SuspendLayout();

            // 
            // lblCnc12Path
            // 
            this.lblCnc12Path.AutoSize = true;
            this.lblCnc12Path.Location = new System.Drawing.Point(20, 20);
            this.lblCnc12Path.Name = "lblCnc12Path";
            this.lblCnc12Path.Size = new System.Drawing.Size(150, 15);
            this.lblCnc12Path.TabIndex = 0;
            this.lblCnc12Path.Text = "CNC12 Installation Path:";

            // 
            // txtCnc12Path
            // 
            this.txtCnc12Path.Location = new System.Drawing.Point(20, 40);
            this.txtCnc12Path.Name = "txtCnc12Path";
            this.txtCnc12Path.Size = new System.Drawing.Size(400, 23);
            this.txtCnc12Path.TabIndex = 1;

            // 
            // btnBrowseCnc12Path
            // 
            this.btnBrowseCnc12Path.Location = new System.Drawing.Point(430, 38);
            this.btnBrowseCnc12Path.Name = "btnBrowseCnc12Path";
            this.btnBrowseCnc12Path.Size = new System.Drawing.Size(80, 27);
            this.btnBrowseCnc12Path.TabIndex = 2;
            this.btnBrowseCnc12Path.Text = "Browse...";
            this.btnBrowseCnc12Path.UseVisualStyleBackColor = true;
            this.btnBrowseCnc12Path.Click += new EventHandler(this.btnBrowseCnc12Path_Click);

            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Location = new System.Drawing.Point(20, 80);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(70, 15);
            this.lblUserName.TabIndex = 3;
            this.lblUserName.Text = "User Name:";

            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(20, 100);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(300, 23);
            this.txtUserName.TabIndex = 4;

            // 
            // lblMachineName
            // 
            this.lblMachineName.AutoSize = true;
            this.lblMachineName.Location = new System.Drawing.Point(20, 140);
            this.lblMachineName.Name = "lblMachineName";
            this.lblMachineName.Size = new System.Drawing.Size(95, 15);
            this.lblMachineName.TabIndex = 5;
            this.lblMachineName.Text = "Machine Name:";

            // 
            // txtMachineName
            // 
            this.txtMachineName.Location = new System.Drawing.Point(20, 160);
            this.txtMachineName.Name = "txtMachineName";
            this.txtMachineName.Size = new System.Drawing.Size(300, 23);
            this.txtMachineName.TabIndex = 6;

            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(20, 200);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(150, 30);
            this.btnSaveSettings.TabIndex = 7;
            this.btnSaveSettings.Text = "Save Settings";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new EventHandler(this.btnSaveSettings_Click);

            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 300);
            this.Controls.Add(this.btnSaveSettings);
            this.Controls.Add(this.txtMachineName);
            this.Controls.Add(this.lblMachineName);
            this.Controls.Add(this.txtUserName);
            this.Controls.Add(this.lblUserName);
            this.Controls.Add(this.btnBrowseCnc12Path);
            this.Controls.Add(this.txtCnc12Path);
            this.Controls.Add(this.lblCnc12Path);
            this.Name = "SettingsForm";
            this.Text = "Application Settings";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadSettings()
        {
            txtCnc12Path.Text = SettingsManager.Settings.Cnc.Cnc12Path ?? "";
            txtUserName.Text = SettingsManager.Settings.Cnc.UserName ?? "";
            txtMachineName.Text = SettingsManager.Settings.Cnc.MachineName ?? "";
        }

        private void btnBrowseCnc12Path_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select CNC12 Installation Folder";
            folderDialog.SelectedPath = txtCnc12Path.Text;

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtCnc12Path.Text = folderDialog.SelectedPath;
            }
        }

        private void btnSaveSettings_Click(object? sender, EventArgs e)
        {
            try
            {
                SettingsManager.Settings.Cnc.Cnc12Path = txtCnc12Path.Text;
                SettingsManager.Settings.Cnc.UserName = txtUserName.Text;
                SettingsManager.Settings.Cnc.MachineName = txtMachineName.Text;

                SettingsManager.SaveSettings();
                LogSuccess("Settings saved successfully", "Settings");
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"Failed to save settings: {ex.Message}", "Settings");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles form closing event to hide instead of disposing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of close
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
