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
            this.lblCnc12Status = new System.Windows.Forms.Label();
            this.lblApiUrl = new System.Windows.Forms.Label();
            this.btnAdmin = new System.Windows.Forms.Button();
            this.adminContextMenu = new System.Windows.Forms.ContextMenuStrip();
            this.showLogsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showMessagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showGCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.openSwaggerUIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gCodeTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alwaysOnTopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDataFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnShowUI = new System.Windows.Forms.Button();
            this.contextMenuLogs = new System.Windows.Forms.ContextMenuStrip();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.adminContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.pnlControls.SuspendLayout();
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
            // lblCnc12Status
            // 
            this.lblCnc12Status.AutoSize = true;
            this.lblCnc12Status.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblCnc12Status.ForeColor = System.Drawing.Color.Red;
            this.lblCnc12Status.Location = new System.Drawing.Point(450, 12);
            this.lblCnc12Status.Name = "lblCnc12Status";
            this.lblCnc12Status.Size = new System.Drawing.Size(180, 17);
            this.lblCnc12Status.TabIndex = 21;
            this.lblCnc12Status.Text = "";
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
            // btnAdmin
            // 
            this.btnAdmin.Location = new System.Drawing.Point(120, 70);
            this.btnAdmin.Name = "btnAdmin";
            this.btnAdmin.Size = new System.Drawing.Size(100, 30);
            this.btnAdmin.TabIndex = 10;
            this.btnAdmin.Text = "Admin ▼";
            this.btnAdmin.UseVisualStyleBackColor = true;
            this.btnAdmin.Click += new System.EventHandler(this.btnAdmin_Click);
            // 
            // adminContextMenu
            // 
            this.adminContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showLogsToolStripMenuItem,
            this.showMessagesToolStripMenuItem,
            this.showGCodeToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.toolStripSeparator1,
            this.openSwaggerUIToolStripMenuItem,
            this.gCodeTestToolStripMenuItem,
            this.alwaysOnTopToolStripMenuItem,
            this.openDataFolderToolStripMenuItem});
            this.adminContextMenu.Name = "adminContextMenu";
            this.adminContextMenu.Size = new System.Drawing.Size(181, 186);
            // 
            // showLogsToolStripMenuItem
            // 
            this.showLogsToolStripMenuItem.Name = "showLogsToolStripMenuItem";
            this.showLogsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.showLogsToolStripMenuItem.Text = "Show Logs";
            this.showLogsToolStripMenuItem.Click += new System.EventHandler(this.btnShowLogs_Click);
            // 
            // showMessagesToolStripMenuItem
            // 
            this.showMessagesToolStripMenuItem.Name = "showMessagesToolStripMenuItem";
            this.showMessagesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.showMessagesToolStripMenuItem.Text = "Show Messages";
            this.showMessagesToolStripMenuItem.Click += new System.EventHandler(this.btnShowMessages_Click);
            // 
            // showGCodeToolStripMenuItem
            // 
            this.showGCodeToolStripMenuItem.Name = "showGCodeToolStripMenuItem";
            this.showGCodeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.showGCodeToolStripMenuItem.Text = "Show G-Code";
            this.showGCodeToolStripMenuItem.Click += new System.EventHandler(this.btnShowGCode_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.btnShowSettings_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // openSwaggerUIToolStripMenuItem
            // 
            this.openSwaggerUIToolStripMenuItem.Name = "openSwaggerUIToolStripMenuItem";
            this.openSwaggerUIToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openSwaggerUIToolStripMenuItem.Text = "Open Swagger UI";
            this.openSwaggerUIToolStripMenuItem.Click += new System.EventHandler(this.btnOpenSwagger_Click);
            // 
            // gCodeTestToolStripMenuItem
            // 
            this.gCodeTestToolStripMenuItem.Name = "gCodeTestToolStripMenuItem";
            this.gCodeTestToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.gCodeTestToolStripMenuItem.Text = "G-Code Test";
            this.gCodeTestToolStripMenuItem.Click += new System.EventHandler(this.btnGCodeTest_Click);
            // 
            // alwaysOnTopToolStripMenuItem
            // 
            this.alwaysOnTopToolStripMenuItem.Name = "alwaysOnTopToolStripMenuItem";
            this.alwaysOnTopToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.alwaysOnTopToolStripMenuItem.Text = "Always on Top: OFF";
            this.alwaysOnTopToolStripMenuItem.Click += new System.EventHandler(this.btnAlwaysOnTop_Click);
            // 
            // openDataFolderToolStripMenuItem
            // 
            this.openDataFolderToolStripMenuItem.Name = "openDataFolderToolStripMenuItem";
            this.openDataFolderToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openDataFolderToolStripMenuItem.Text = "Open Data Folder";
            this.openDataFolderToolStripMenuItem.Click += new System.EventHandler(this.btnOpenDataFolder_Click);
            // 
            // btnShowUI
            // 
            this.btnShowUI.Location = new System.Drawing.Point(12, 70);
            this.btnShowUI.Name = "btnShowUI";
            this.btnShowUI.Size = new System.Drawing.Size(100, 30);
            this.btnShowUI.TabIndex = 9;
            this.btnShowUI.Text = "Show UI";
            this.btnShowUI.UseVisualStyleBackColor = true;
            this.btnShowUI.Click += new System.EventHandler(this.btnShowUI_Click);
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
            this.pnlControls.Controls.Add(this.lblCnc12Status);
            this.pnlControls.Controls.Add(this.lblApiUrl);
            this.pnlControls.Controls.Add(this.btnShowUI);
            this.pnlControls.Controls.Add(this.btnAdmin);
            this.pnlControls.Location = new System.Drawing.Point(0, 24);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(1380, 120);
            this.pnlControls.TabIndex = 10;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
            this.Controls.Add(this.webView);
            this.Controls.Add(this.pnlControls);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.TopMost = false;  // Default to false, controlled by checkbox
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 650, 0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.adminContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblConnectionRetries;
        private System.Windows.Forms.Label lblCnc12Status;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnAdmin;
        private System.Windows.Forms.ContextMenuStrip adminContextMenu;
        private System.Windows.Forms.ToolStripMenuItem showLogsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMessagesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showGCodeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem openSwaggerUIToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gCodeTestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDataFolderToolStripMenuItem;
        private System.Windows.Forms.Button btnShowUI;
        private System.Windows.Forms.ContextMenuStrip contextMenuLogs;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
    }
}