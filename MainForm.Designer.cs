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
            this.lblApiUrl = new System.Windows.Forms.Label();
            this.btnOpenSwagger = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.btnGCodeTest = new System.Windows.Forms.Button();
            this.btnAlwaysOnTop = new System.Windows.Forms.Button();
            this.btnShowUI = new System.Windows.Forms.Button();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabLogs = new System.Windows.Forms.TabPage();
            this.tabMessages = new System.Windows.Forms.TabPage();
            this.tabGCode = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.pnlControls.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabLogs.SuspendLayout();
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
            this.pnlControls.Controls.Add(this.lblApiUrl);
            this.pnlControls.Controls.Add(this.btnOpenSwagger);
            this.pnlControls.Controls.Add(this.btnGCodeTest);
            this.pnlControls.Controls.Add(this.btnAlwaysOnTop);
            this.pnlControls.Controls.Add(this.btnShowUI);
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
            this.tabControl.Location = new System.Drawing.Point(12, 130);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1356, 550);
            this.tabControl.TabIndex = 11;
            // 
            // tabLogs
            // 
            this.tabLogs.Controls.Add(this.txtLog);
            this.tabLogs.Location = new System.Drawing.Point(4, 24);
            this.tabLogs.Name = "tabLogs";
            this.tabLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogs.Size = new System.Drawing.Size(1348, 522);
            this.tabLogs.TabIndex = 0;
            this.tabLogs.Text = "Logs";
            this.tabLogs.UseVisualStyleBackColor = true;
            // 
            // tabMessages
            // 
            this.tabMessages.Location = new System.Drawing.Point(4, 24);
            this.tabMessages.Name = "tabMessages";
            this.tabMessages.Padding = new System.Windows.Forms.Padding(3);
            this.tabMessages.Size = new System.Drawing.Size(1348, 522);
            this.tabMessages.TabIndex = 1;
            this.tabMessages.Text = "Messages";
            this.tabMessages.UseVisualStyleBackColor = true;
            // 
            // tabGCode
            // 
            this.tabGCode.Location = new System.Drawing.Point(4, 24);
            this.tabGCode.Name = "tabGCode";
            this.tabGCode.Padding = new System.Windows.Forms.Padding(3);
            this.tabGCode.Size = new System.Drawing.Size(1348, 522);
            this.tabGCode.TabIndex = 2;
            this.tabGCode.Text = "G-Code";
            this.tabGCode.UseVisualStyleBackColor = true;
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
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnOpenSwagger;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnGCodeTest;
        private System.Windows.Forms.Button btnAlwaysOnTop;
        private System.Windows.Forms.Button btnShowUI;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabLogs;
        private System.Windows.Forms.TabPage tabMessages;
        private System.Windows.Forms.TabPage tabGCode;
    }
}
