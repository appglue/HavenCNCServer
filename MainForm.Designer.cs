namespace HavenCNCServer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblConnectionRetries = new System.Windows.Forms.Label();
            this.lblApiUrl = new System.Windows.Forms.Label();
            this.btnOpenSwagger = new System.Windows.Forms.Button();
            this.txtLog = new HavenCNCServer.Components.FlickerFreeLogViewer();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.btnClearMessages = new System.Windows.Forms.Button();
            this.btnGCodeTest = new System.Windows.Forms.Button();
            this.btnAlwaysOnTop = new System.Windows.Forms.Button();
            this.btnShowUI = new System.Windows.Forms.Button();
            this.btnViewLogs = new System.Windows.Forms.Button();
            this.btnOpenDataFolder = new System.Windows.Forms.Button();
            this.contextMenuLogs = new System.Windows.Forms.ContextMenuStrip();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabLogs = new System.Windows.Forms.TabPage();
            this.tabMessages = new System.Windows.Forms.TabPage();
            this.tabGCode = new System.Windows.Forms.TabPage();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.lblCnc12Path = new System.Windows.Forms.Label();
            this.txtCnc12Path = new System.Windows.Forms.TextBox();
            this.btnBrowseCnc12Path = new System.Windows.Forms.Button();
            this.lblUserName = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.lblMachineName = new System.Windows.Forms.Label();
            this.txtMachineName = new System.Windows.Forms.TextBox();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.pnlControls.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabLogs.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.ForeColor = System.Drawing.Color.Green;
            this.lblStatus.Location = new System.Drawing.Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(232, 20);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "API Server Status: Starting...";
            // 
            // lblConnectionRetries
            // 
            this.lblConnectionRetries.AutoSize = true;
            this.lblConnectionRetries.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblConnectionRetries.Location = new System.Drawing.Point(250, 12);
            this.lblConnectionRetries.Name = "lblConnectionRetries";
            this.lblConnectionRetries.Size = new System.Drawing.Size(120, 17);
            this.lblConnectionRetries.TabIndex = 20;
            this.lblConnectionRetries.Text = "";
            // 
            // lblApiUrl
            // 
            this.lblApiUrl.AutoSize = true;
            this.lblApiUrl.Location = new System.Drawing.Point(12, 40);
            this.lblApiUrl.Name = "lblApiUrl";
            this.lblApiUrl.Size = new System.Drawing.Size(159, 15);
            this.lblApiUrl.TabIndex = 1;
            this.lblApiUrl.Text = "API URL: http://localhost:5000";
            // 
            // btnOpenSwagger
            // 
            this.btnOpenSwagger.Location = new System.Drawing.Point(12, 70);
            this.btnOpenSwagger.Name = "btnOpenSwagger";
            this.btnOpenSwagger.Size = new System.Drawing.Size(120, 30);
            this.btnOpenSwagger.TabIndex = 2;
            this.btnOpenSwagger.Text = "Open Swagger UI";
            this.btnOpenSwagger.UseVisualStyleBackColor = true;
            this.btnOpenSwagger.Click += new System.EventHandler(this.btnOpenSwagger_Click);
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLogs.Location = new System.Drawing.Point(1230, 6);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(110, 30);
            this.btnClearLogs.TabIndex = 1;
            this.btnClearLogs.Text = "Clear Logs";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.btnClearLogs_Click);
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(3, 3);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1340, 486);
            this.txtLog.TabIndex = 3;
            this.txtLog.WordWrap = true;





            // 
            // btnGCodeTest
            // 
            this.btnGCodeTest.Location = new System.Drawing.Point(150, 70);
            this.btnGCodeTest.Name = "btnGCodeTest";
            this.btnGCodeTest.Size = new System.Drawing.Size(100, 30);
            this.btnGCodeTest.TabIndex = 9;
            this.btnGCodeTest.Text = "G-Code Test";
            this.btnGCodeTest.UseVisualStyleBackColor = true;
            this.btnGCodeTest.Click += new System.EventHandler(this.btnGCodeTest_Click);
            // 
            // btnAlwaysOnTop
            // 
            this.btnAlwaysOnTop.Location = new System.Drawing.Point(270, 70);
            this.btnAlwaysOnTop.Name = "btnAlwaysOnTop";
            this.btnAlwaysOnTop.Size = new System.Drawing.Size(120, 30);
            this.btnAlwaysOnTop.TabIndex = 11;
            this.btnAlwaysOnTop.Text = "Always on Top: OFF";
            this.btnAlwaysOnTop.UseVisualStyleBackColor = true;
            this.btnAlwaysOnTop.Click += new System.EventHandler(this.btnAlwaysOnTop_Click);
            // 
            // btnShowUI
            // 
            this.btnShowUI.Location = new System.Drawing.Point(410, 70);
            this.btnShowUI.Name = "btnShowUI";
            this.btnShowUI.Size = new System.Drawing.Size(100, 30);
            this.btnShowUI.TabIndex = 12;
            this.btnShowUI.Text = "Show UI";
            this.btnShowUI.UseVisualStyleBackColor = true;
            this.btnShowUI.Click += new System.EventHandler(this.btnShowUI_Click);
            // 
            // btnViewLogs
            // 
            this.btnViewLogs.Location = new System.Drawing.Point(530, 70);
            this.btnViewLogs.Name = "btnViewLogs";
            this.btnViewLogs.Size = new System.Drawing.Size(100, 30);
            this.btnViewLogs.TabIndex = 13;
            this.btnViewLogs.Text = "View Logs ▼";
            this.btnViewLogs.UseVisualStyleBackColor = true;
            this.btnViewLogs.Click += new System.EventHandler(this.btnViewLogs_Click);
            // 
            // btnOpenDataFolder
            // 
            this.btnOpenDataFolder.Location = new System.Drawing.Point(650, 70);
            this.btnOpenDataFolder.Name = "btnOpenDataFolder";
            this.btnOpenDataFolder.Size = new System.Drawing.Size(120, 30);
            this.btnOpenDataFolder.TabIndex = 14;
            this.btnOpenDataFolder.Text = "Open Data Folder";
            this.btnOpenDataFolder.UseVisualStyleBackColor = true;
            this.btnOpenDataFolder.Click += new System.EventHandler(this.btnOpenDataFolder_Click);
            // 
            // contextMenuLogs
            // 
            this.contextMenuLogs.Name = "contextMenuLogs";
            this.contextMenuLogs.Size = new System.Drawing.Size(181, 26);
            // 
            // webView
            // 
            this.webView.AllowExternalDrop = true;
            this.webView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webView.CreationProperties = null;
            this.webView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView.Location = new System.Drawing.Point(12, 170);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(880, 500);
            this.webView.TabIndex = 9;
            this.webView.Visible = false;
            this.webView.ZoomFactor = 1D;
            // 
            // pnlControls
            // 
            this.pnlControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlControls.Controls.Add(this.lblStatus);
            this.pnlControls.Controls.Add(this.lblConnectionRetries);
            this.pnlControls.Controls.Add(this.lblApiUrl);
            this.pnlControls.Controls.Add(this.btnOpenSwagger);
            this.pnlControls.Controls.Add(this.btnGCodeTest);
            this.pnlControls.Controls.Add(this.btnAlwaysOnTop);
            this.pnlControls.Controls.Add(this.btnShowUI);
            this.pnlControls.Controls.Add(this.btnViewLogs);
            this.pnlControls.Controls.Add(this.btnOpenDataFolder);
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(1380, 120);
            this.pnlControls.TabIndex = 10;
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabLogs);
            this.tabControl.Controls.Add(this.tabMessages);
            this.tabControl.Controls.Add(this.tabGCode);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Location = new System.Drawing.Point(12, 130);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1356, 938);
            this.tabControl.TabIndex = 11;
            // 
            // tabLogs
            // 
            this.tabLogs.Controls.Add(this.btnClearLogs);
            this.tabLogs.Controls.Add(this.txtLog);
            this.tabLogs.Location = new System.Drawing.Point(4, 24);
            this.tabLogs.Name = "tabLogs";
            this.tabLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogs.Size = new System.Drawing.Size(1348, 910);
            this.tabLogs.TabIndex = 0;
            this.tabLogs.Text = "Logs";
            this.tabLogs.UseVisualStyleBackColor = true;
            // 
            // tabMessages
            // 
            this.tabMessages.Controls.Add(this.btnClearMessages);
            this.tabMessages.Location = new System.Drawing.Point(4, 24);
            this.tabMessages.Name = "tabMessages";
            this.tabMessages.Padding = new System.Windows.Forms.Padding(3);
            this.tabMessages.Size = new System.Drawing.Size(1348, 910);
            this.tabMessages.TabIndex = 1;
            this.tabMessages.Text = "Messages";
            this.tabMessages.UseVisualStyleBackColor = true;
            // 
            // btnClearMessages
            // 
            this.btnClearMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearMessages.Location = new System.Drawing.Point(1230, 6);
            this.btnClearMessages.Name = "btnClearMessages";
            this.btnClearMessages.Size = new System.Drawing.Size(110, 30);
            this.btnClearMessages.TabIndex = 1;
            this.btnClearMessages.Text = "Clear Messages";
            this.btnClearMessages.UseVisualStyleBackColor = true;
            this.btnClearMessages.Click += new System.EventHandler(this.btnClearMessages_Click);
            // 
            // tabGCode
            // 
            this.tabGCode.Location = new System.Drawing.Point(4, 24);
            this.tabGCode.Name = "tabGCode";
            this.tabGCode.Padding = new System.Windows.Forms.Padding(3);
            this.tabGCode.Size = new System.Drawing.Size(1348, 910);
            this.tabGCode.TabIndex = 2;
            this.tabGCode.Text = "G-Code";
            this.tabGCode.UseVisualStyleBackColor = true;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.btnSaveSettings);
            this.tabSettings.Controls.Add(this.lblMachineName);
            this.tabSettings.Controls.Add(this.txtMachineName);
            this.tabSettings.Controls.Add(this.lblUserName);
            this.tabSettings.Controls.Add(this.txtUserName);
            this.tabSettings.Controls.Add(this.lblCnc12Path);
            this.tabSettings.Controls.Add(this.txtCnc12Path);
            this.tabSettings.Controls.Add(this.btnBrowseCnc12Path);
            this.tabSettings.Location = new System.Drawing.Point(4, 24);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(1348, 522);
            this.tabSettings.TabIndex = 3;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // lblCnc12Path
            // 
            this.lblCnc12Path.AutoSize = true;
            this.lblCnc12Path.Location = new System.Drawing.Point(20, 20);
            this.lblCnc12Path.Name = "lblCnc12Path";
            this.lblCnc12Path.Size = new System.Drawing.Size(122, 15);
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
            this.btnBrowseCnc12Path.Click += new System.EventHandler(this.btnBrowseCnc12Path_Click);
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Location = new System.Drawing.Point(20, 80);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(68, 15);
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
            this.lblMachineName.Size = new System.Drawing.Size(90, 15);
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
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1380, 1080);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.webView);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.TopMost = false;  // Default to false, controlled by checkbox
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(0, 0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabLogs.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblConnectionRetries;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnOpenSwagger;
        private HavenCNCServer.Components.FlickerFreeLogViewer txtLog;
        private System.Windows.Forms.Button btnGCodeTest;
        private System.Windows.Forms.Button btnAlwaysOnTop;
        private System.Windows.Forms.Button btnShowUI;
        private System.Windows.Forms.Button btnViewLogs;
        private System.Windows.Forms.Button btnOpenDataFolder;
        private System.Windows.Forms.Button btnClearLogs;
        private System.Windows.Forms.Button btnClearMessages;
        private System.Windows.Forms.ContextMenuStrip contextMenuLogs;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabLogs;
        private System.Windows.Forms.TabPage tabMessages;
        private System.Windows.Forms.TabPage tabGCode;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.Label lblCnc12Path;
        private System.Windows.Forms.TextBox txtCnc12Path;
        private System.Windows.Forms.Button btnBrowseCnc12Path;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Label lblMachineName;
        private System.Windows.Forms.TextBox txtMachineName;
        private System.Windows.Forms.Button btnSaveSettings;
    }
}
