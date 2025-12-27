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
            this.pnlTop = new Krypton.Toolkit.KryptonPanel();
            this.pnlTopLeft = new Krypton.Toolkit.KryptonPanel();
            this.lblStatus = new Krypton.Toolkit.KryptonLabel();
            this.lblConnectionRetries = new Krypton.Toolkit.KryptonLabel();
            this.lblCnc12Status = new Krypton.Toolkit.KryptonLabel();
            this.lblApiUrl = new Krypton.Toolkit.KryptonLabel();
            this.lblAdmin = new Krypton.Toolkit.KryptonLinkLabel();
            this.pnlTopRight = new Krypton.Toolkit.KryptonPanel();
            this.pnlBottom = new Krypton.Toolkit.KryptonPanel();
            this.btnReset = new Krypton.Toolkit.KryptonButton();
            this.btnStop = new Krypton.Toolkit.KryptonButton();
            this.btnStart = new Krypton.Toolkit.KryptonButton();
            this.btnShowUI = new Krypton.Toolkit.KryptonButton();
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
            this.contextMenuLogs = new System.Windows.Forms.ContextMenuStrip();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlTop.SuspendLayout();
            this.pnlTopLeft.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.adminContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.ForeColor = System.Drawing.Color.Green;
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(12, 9, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(400, 29);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "API Server Status: Starting...";
            // 
            // lblConnectionRetries
            // 
            this.lblConnectionRetries.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConnectionRetries.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblConnectionRetries.Location = new System.Drawing.Point(0, 29);
            this.lblConnectionRetries.Name = "lblConnectionRetries";
            this.lblConnectionRetries.Padding = new System.Windows.Forms.Padding(12, 3, 0, 0);
            this.lblConnectionRetries.Size = new System.Drawing.Size(400, 20);
            this.lblConnectionRetries.TabIndex = 20;
            this.lblConnectionRetries.Text = "";
            // 
            // lblCnc12Status
            // 
            this.lblCnc12Status.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCnc12Status.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblCnc12Status.ForeColor = System.Drawing.Color.Red;
            this.lblCnc12Status.Location = new System.Drawing.Point(0, 66);
            this.lblCnc12Status.Name = "lblCnc12Status";
            this.lblCnc12Status.Padding = new System.Windows.Forms.Padding(12, 4, 0, 0);
            this.lblCnc12Status.Size = new System.Drawing.Size(400, 21);
            this.lblCnc12Status.TabIndex = 21;
            this.lblCnc12Status.Text = "";
            // 
            // lblApiUrl
            // 
            this.lblApiUrl.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblApiUrl.Location = new System.Drawing.Point(0, 49);
            this.lblApiUrl.Name = "lblApiUrl";
            this.lblApiUrl.Padding = new System.Windows.Forms.Padding(12, 2, 0, 0);
            this.lblApiUrl.Size = new System.Drawing.Size(400, 17);
            this.lblApiUrl.TabIndex = 1;
            this.lblApiUrl.Text = "API URL: http://localhost:5000";
            // 
            // lblAdmin
            // 
            this.lblAdmin.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblAdmin.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular);
            this.lblAdmin.Location = new System.Drawing.Point(0, 140);
            this.lblAdmin.Name = "lblAdmin";
            this.lblAdmin.Padding = new System.Windows.Forms.Padding(0, 0, 12, 3);
            this.lblAdmin.Size = new System.Drawing.Size(250, 20);
            this.lblAdmin.TabIndex = 30;
            this.lblAdmin.TabStop = true;
            this.lblAdmin.Text = "Admin ▼";
            this.lblAdmin.Click += new System.EventHandler(this.lblAdmin_LinkClicked);
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
            this.btnShowUI.BackColor = System.Drawing.Color.LimeGreen;
            this.btnShowUI.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Bold);
            this.btnShowUI.Location = new System.Drawing.Point(12, 90);
            this.btnShowUI.Name = "btnShowUI";
            this.btnShowUI.Size = new System.Drawing.Size(380, 290);
            this.btnShowUI.TabIndex = 9;
            this.btnShowUI.Text = "Show UI";
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
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.pnlTopRight);
            this.pnlTop.Controls.Add(this.pnlTopLeft);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(650, 160);
            this.pnlTop.TabIndex = 0;
            // 
            // pnlTopLeft
            // 
            this.pnlTopLeft.Controls.Add(this.btnShowUI);
            this.pnlTopLeft.Controls.Add(this.lblCnc12Status);
            this.pnlTopLeft.Controls.Add(this.lblApiUrl);
            this.pnlTopLeft.Controls.Add(this.lblConnectionRetries);
            this.pnlTopLeft.Controls.Add(this.lblStatus);
            this.pnlTopLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlTopLeft.Location = new System.Drawing.Point(0, 0);
            this.pnlTopLeft.Name = "pnlTopLeft";
            this.pnlTopLeft.Size = new System.Drawing.Size(400, 160);
            this.pnlTopLeft.TabIndex = 0;
            // 
            // pnlTopRight
            // 
            this.pnlTopRight.Controls.Add(this.lblAdmin);
            this.pnlTopRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTopRight.Location = new System.Drawing.Point(400, 0);
            this.pnlTopRight.Name = "pnlTopRight";
            this.pnlTopRight.Size = new System.Drawing.Size(250, 160);
            this.pnlTopRight.TabIndex = 1;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.btnStart);
            this.pnlBottom.Controls.Add(this.btnStop);
            this.pnlBottom.Controls.Add(this.btnReset);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - 140);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(650, 140);
            this.pnlBottom.TabIndex = 2;
            // 
            // btnReset
            // 
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnReset.Location = new System.Drawing.Point(0, 0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(210, 140);
            this.btnReset.TabIndex = 20;
            this.btnReset.Values.Text = "RESET";
            this.btnReset.StateCommon.Back.Color1 = System.Drawing.Color.Orange;
            this.btnReset.StateCommon.Back.Color2 = System.Drawing.Color.Orange;
            this.btnReset.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 32F, System.Drawing.FontStyle.Bold);
            this.btnReset.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnReset_MouseDown);
            this.btnReset.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnReset_MouseUp);
            // 
            // btnStop
            // 
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnStop.Location = new System.Drawing.Point(210, 0);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(220, 140);
            this.btnStop.TabIndex = 21;
            this.btnStop.Values.Text = "STOP";
            this.btnStop.StateCommon.Back.Color1 = System.Drawing.Color.Red;
            this.btnStop.StateCommon.Back.Color2 = System.Drawing.Color.Red;
            this.btnStop.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 32F, System.Drawing.FontStyle.Bold);
            this.btnStop.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnStop.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnStop_MouseDown);
            this.btnStop.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnStop_MouseUp);
            // 
            // btnStart
            // 
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.Location = new System.Drawing.Point(430, 0);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(220, 140);
            this.btnStart.TabIndex = 22;
            this.btnStart.Values.Text = "START";
            this.btnStart.StateCommon.Back.Color1 = System.Drawing.Color.LimeGreen;
            this.btnStart.StateCommon.Back.Color2 = System.Drawing.Color.LimeGreen;
            this.btnStart.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 32F, System.Drawing.FontStyle.Bold);
            this.btnStart.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnStart_MouseDown);
            this.btnStart.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnStart_MouseUp);
            // 
            // 
            // btnShowUI
            // 
            this.btnShowUI.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnShowUI.Location = new System.Drawing.Point(0, 87);
            this.btnShowUI.Name = "btnShowUI";
            this.btnShowUI.Padding = new System.Windows.Forms.Padding(10);
            this.btnShowUI.Size = new System.Drawing.Size(400, 50);
            this.btnShowUI.TabIndex = 9;
            this.btnShowUI.Values.Text = "Show UI";
            this.btnShowUI.StateCommon.Back.Color1 = System.Drawing.Color.LimeGreen;
            this.btnShowUI.StateCommon.Back.Color2 = System.Drawing.Color.LimeGreen;
            this.btnShowUI.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.btnShowUI.Click += new System.EventHandler(this.btnShowUI_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.webView);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.TopMost = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 650, 0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.pnlTop.ResumeLayout(false);
            this.pnlTopLeft.ResumeLayout(false);
            this.pnlTopLeft.PerformLayout();
            this.pnlBottom.ResumeLayout(false);
            this.adminContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private Krypton.Toolkit.KryptonPanel pnlTop;
        private Krypton.Toolkit.KryptonPanel pnlTopLeft;
        private Krypton.Toolkit.KryptonPanel pnlTopRight;
        private Krypton.Toolkit.KryptonPanel pnlBottom;
        private Krypton.Toolkit.KryptonLabel lblStatus;
        private Krypton.Toolkit.KryptonLabel lblConnectionRetries;
        private Krypton.Toolkit.KryptonLabel lblCnc12Status;
        private Krypton.Toolkit.KryptonLabel lblApiUrl;
        private Krypton.Toolkit.KryptonLinkLabel lblAdmin;
        private Krypton.Toolkit.KryptonButton btnShowUI;
        private Krypton.Toolkit.KryptonButton btnReset;
        private Krypton.Toolkit.KryptonButton btnStop;
        private Krypton.Toolkit.KryptonButton btnStart;
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
        private System.Windows.Forms.ContextMenuStrip contextMenuLogs;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
    }
}