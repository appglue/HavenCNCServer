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
            this.btnOpenSwagger = new System.Windows.Forms.Button();
            this.btnGCodeTest = new System.Windows.Forms.Button();
            this.btnAlwaysOnTop = new System.Windows.Forms.Button();
            this.btnShowUI = new System.Windows.Forms.Button();
            this.btnOpenDataFolder = new System.Windows.Forms.Button();
            this.btnShowLogs = new System.Windows.Forms.Button();
            this.btnShowMessages = new System.Windows.Forms.Button();
            this.btnShowGCode = new System.Windows.Forms.Button();
            this.btnShowSettings = new System.Windows.Forms.Button();
            this.contextMenuLogs = new System.Windows.Forms.ContextMenuStrip();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlControls = new System.Windows.Forms.Panel();
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
            // btnShowLogs
            // 
            this.btnShowLogs.Location = new System.Drawing.Point(12, 70);
            this.btnShowLogs.Name = "btnShowLogs";
            this.btnShowLogs.Size = new System.Drawing.Size(90, 30);
            this.btnShowLogs.TabIndex = 2;
            this.btnShowLogs.Text = "Show Logs";
            this.btnShowLogs.UseVisualStyleBackColor = true;
            this.btnShowLogs.Click += new System.EventHandler(this.btnShowLogs_Click);
            // 
            // btnShowMessages
            // 
            this.btnShowMessages.Location = new System.Drawing.Point(110, 70);
            this.btnShowMessages.Name = "btnShowMessages";
            this.btnShowMessages.Size = new System.Drawing.Size(110, 30);
            this.btnShowMessages.TabIndex = 3;
            this.btnShowMessages.Text = "Show Messages";
            this.btnShowMessages.UseVisualStyleBackColor = true;
            this.btnShowMessages.Click += new System.EventHandler(this.btnShowMessages_Click);
            // 
            // btnShowGCode
            // 
            this.btnShowGCode.Location = new System.Drawing.Point(230, 70);
            this.btnShowGCode.Name = "btnShowGCode";
            this.btnShowGCode.Size = new System.Drawing.Size(100, 30);
            this.btnShowGCode.TabIndex = 4;
            this.btnShowGCode.Text = "Show G-Code";
            this.btnShowGCode.UseVisualStyleBackColor = true;
            this.btnShowGCode.Click += new System.EventHandler(this.btnShowGCode_Click);
            // 
            // btnShowSettings
            // 
            this.btnShowSettings.Location = new System.Drawing.Point(340, 70);
            this.btnShowSettings.Name = "btnShowSettings";
            this.btnShowSettings.Size = new System.Drawing.Size(100, 30);
            this.btnShowSettings.TabIndex = 5;
            this.btnShowSettings.Text = "Settings";
            this.btnShowSettings.UseVisualStyleBackColor = true;
            this.btnShowSettings.Click += new System.EventHandler(this.btnShowSettings_Click);
            // 
            // btnOpenSwagger
            // 
            this.btnOpenSwagger.Location = new System.Drawing.Point(450, 70);
            this.btnOpenSwagger.Name = "btnOpenSwagger";
            this.btnOpenSwagger.Size = new System.Drawing.Size(120, 30);
            this.btnOpenSwagger.TabIndex = 6;
            this.btnOpenSwagger.Text = "Open Swagger UI";
            this.btnOpenSwagger.UseVisualStyleBackColor = true;
            this.btnOpenSwagger.Click += new System.EventHandler(this.btnOpenSwagger_Click);
            // 
            // btnGCodeTest
            // 
            this.btnGCodeTest.Location = new System.Drawing.Point(580, 70);
            this.btnGCodeTest.Name = "btnGCodeTest";
            this.btnGCodeTest.Size = new System.Drawing.Size(100, 30);
            this.btnGCodeTest.TabIndex = 7;
            this.btnGCodeTest.Text = "G-Code Test";
            this.btnGCodeTest.UseVisualStyleBackColor = true;
            this.btnGCodeTest.Click += new System.EventHandler(this.btnGCodeTest_Click);
            // 
            // btnAlwaysOnTop
            // 
            this.btnAlwaysOnTop.Location = new System.Drawing.Point(690, 70);
            this.btnAlwaysOnTop.Name = "btnAlwaysOnTop";
            this.btnAlwaysOnTop.Size = new System.Drawing.Size(120, 30);
            this.btnAlwaysOnTop.TabIndex = 8;
            this.btnAlwaysOnTop.Text = "Always on Top: OFF";
            this.btnAlwaysOnTop.UseVisualStyleBackColor = true;
            this.btnAlwaysOnTop.Click += new System.EventHandler(this.btnAlwaysOnTop_Click);
            // 
            // btnShowUI
            // 
            this.btnShowUI.Location = new System.Drawing.Point(820, 70);
            this.btnShowUI.Name = "btnShowUI";
            this.btnShowUI.Size = new System.Drawing.Size(100, 30);
            this.btnShowUI.TabIndex = 9;
            this.btnShowUI.Text = "Show UI";
            this.btnShowUI.UseVisualStyleBackColor = true;
            this.btnShowUI.Click += new System.EventHandler(this.btnShowUI_Click);
            // 
            // btnOpenDataFolder
            // 
            this.btnOpenDataFolder.Location = new System.Drawing.Point(930, 70);
            this.btnOpenDataFolder.Name = "btnOpenDataFolder";
            this.btnOpenDataFolder.Size = new System.Drawing.Size(120, 30);
            this.btnOpenDataFolder.TabIndex = 10;
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
            this.pnlControls.Controls.Add(this.lblCnc12Status);
            this.pnlControls.Controls.Add(this.lblApiUrl);
            this.pnlControls.Controls.Add(this.btnShowLogs);
            this.pnlControls.Controls.Add(this.btnShowMessages);
            this.pnlControls.Controls.Add(this.btnShowGCode);
            this.pnlControls.Controls.Add(this.btnShowSettings);
            this.pnlControls.Controls.Add(this.btnOpenSwagger);
            this.pnlControls.Controls.Add(this.btnGCodeTest);
            this.pnlControls.Controls.Add(this.btnAlwaysOnTop);
            this.pnlControls.Controls.Add(this.btnShowUI);
            this.pnlControls.Controls.Add(this.btnOpenDataFolder);
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(1380, 120);
            this.pnlControls.TabIndex = 10;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.webView);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.TopMost = false;  // Default to false, controlled by checkbox
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 650, 0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblConnectionRetries;
        private System.Windows.Forms.Label lblCnc12Status;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnOpenSwagger;
        private System.Windows.Forms.Button btnGCodeTest;
        private System.Windows.Forms.Button btnAlwaysOnTop;
        private System.Windows.Forms.Button btnShowUI;
        private System.Windows.Forms.Button btnOpenDataFolder;
        private System.Windows.Forms.Button btnShowLogs;
        private System.Windows.Forms.Button btnShowMessages;
        private System.Windows.Forms.Button btnShowGCode;
        private System.Windows.Forms.Button btnShowSettings;
        private System.Windows.Forms.ContextMenuStrip contextMenuLogs;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
    }
}