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
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 170);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(440, 500);
            this.txtLog.TabIndex = 3;





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
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(904, 160);
            this.pnlControls.TabIndex = 10;

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1344, 692);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.webView);
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnOpenSwagger;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnGCodeTest;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
    }
}
