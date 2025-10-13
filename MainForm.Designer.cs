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
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblLog = new System.Windows.Forms.Label();
            this.btnStopServer = new System.Windows.Forms.Button();
            this.btnStartServer = new System.Windows.Forms.Button();
            this.btnOpenReactApp = new System.Windows.Forms.Button();
            this.btnGenerateOpenApi = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnGCodeTest = new System.Windows.Forms.Button();
            this.btnCNCServer = new System.Windows.Forms.Button();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.grpCoordinates = new System.Windows.Forms.GroupBox();
            this.lblXPos = new System.Windows.Forms.Label();
            this.lblYPos = new System.Windows.Forms.Label();
            this.lblZPos = new System.Windows.Forms.Label();
            this.lblXValue = new System.Windows.Forms.Label();
            this.lblYValue = new System.Windows.Forms.Label();
            this.lblZValue = new System.Windows.Forms.Label();
            this.txtMessages = new System.Windows.Forms.TextBox();
            this.lblMessages = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.pnlControls.SuspendLayout();
            this.grpCoordinates.SuspendLayout();
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
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtLog.Location = new System.Drawing.Point(12, 170);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(440, 500);
            this.txtLog.TabIndex = 3;
            // 
            // lblLog
            // 
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(12, 152);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(69, 15);
            this.lblLog.TabIndex = 4;
            this.lblLog.Text = "Server Logs:";
            // 
            // txtMessages
            // 
            this.txtMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right))));
            this.txtMessages.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtMessages.Location = new System.Drawing.Point(462, 170);
            this.txtMessages.Multiline = true;
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ReadOnly = true;
            this.txtMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessages.Size = new System.Drawing.Size(430, 500);
            this.txtMessages.TabIndex = 13;
            // 
            // lblMessages
            // 
            this.lblMessages.AutoSize = true;
            this.lblMessages.Location = new System.Drawing.Point(462, 152);
            this.lblMessages.Name = "lblMessages";
            this.lblMessages.Size = new System.Drawing.Size(84, 15);
            this.lblMessages.TabIndex = 14;
            this.lblMessages.Text = "CNC Messages:";
            // 
            // btnStopServer
            // 
            this.btnStopServer.Location = new System.Drawing.Point(270, 70);
            this.btnStopServer.Name = "btnStopServer";
            this.btnStopServer.Size = new System.Drawing.Size(100, 30);
            this.btnStopServer.TabIndex = 5;
            this.btnStopServer.Text = "Stop Server";
            this.btnStopServer.UseVisualStyleBackColor = true;
            this.btnStopServer.Click += new System.EventHandler(this.btnStopServer_Click);
            // 
            // btnStartServer
            // 
            this.btnStartServer.Location = new System.Drawing.Point(150, 70);
            this.btnStartServer.Name = "btnStartServer";
            this.btnStartServer.Size = new System.Drawing.Size(100, 30);
            this.btnStartServer.TabIndex = 6;
            this.btnStartServer.Text = "Start Server";
            this.btnStartServer.UseVisualStyleBackColor = true;
            this.btnStartServer.Click += new System.EventHandler(this.btnStartServer_Click);
            // 
            // btnOpenReactApp
            // 
            this.btnOpenReactApp.Location = new System.Drawing.Point(390, 70);
            this.btnOpenReactApp.Name = "btnOpenReactApp";
            this.btnOpenReactApp.Size = new System.Drawing.Size(120, 30);
            this.btnOpenReactApp.TabIndex = 7;
            this.btnOpenReactApp.Text = "Open React App";
            this.btnOpenReactApp.UseVisualStyleBackColor = true;
            this.btnOpenReactApp.Click += new System.EventHandler(this.btnOpenReactApp_Click);
            // 
            // btnGenerateOpenApi
            // 
            this.btnGenerateOpenApi.Location = new System.Drawing.Point(530, 70);
            this.btnGenerateOpenApi.Name = "btnGenerateOpenApi";
            this.btnGenerateOpenApi.Size = new System.Drawing.Size(120, 30);
            this.btnGenerateOpenApi.TabIndex = 8;
            this.btnGenerateOpenApi.Text = "Generate OpenAPI";
            this.btnGenerateOpenApi.UseVisualStyleBackColor = true;
            this.btnGenerateOpenApi.Click += new System.EventHandler(this.btnGenerateOpenApi_Click);
            // 
            // btnGCodeTest
            // 
            this.btnGCodeTest.Location = new System.Drawing.Point(670, 70);
            this.btnGCodeTest.Name = "btnGCodeTest";
            this.btnGCodeTest.Size = new System.Drawing.Size(100, 30);
            this.btnGCodeTest.TabIndex = 9;
            this.btnGCodeTest.Text = "G-Code Test";
            this.btnGCodeTest.UseVisualStyleBackColor = true;
            this.btnGCodeTest.Click += new System.EventHandler(this.btnGCodeTest_Click);
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(790, 70);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(100, 30);
            this.btnTest.TabIndex = 10;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // btnCNCServer
            // 
            this.btnCNCServer.Location = new System.Drawing.Point(12, 110);
            this.btnCNCServer.Name = "btnCNCServer";
            this.btnCNCServer.Size = new System.Drawing.Size(120, 30);
            this.btnCNCServer.TabIndex = 11;
            this.btnCNCServer.Text = "Start CNC Server";
            this.btnCNCServer.UseVisualStyleBackColor = true;
            this.btnCNCServer.Click += new System.EventHandler(this.btnCNCServer_Click);
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
            this.pnlControls.Controls.Add(this.btnStartServer);
            this.pnlControls.Controls.Add(this.btnStopServer);
            this.pnlControls.Controls.Add(this.btnOpenReactApp);
            this.pnlControls.Controls.Add(this.btnGenerateOpenApi);
            this.pnlControls.Controls.Add(this.btnGCodeTest);
            this.pnlControls.Controls.Add(this.btnTest);
            this.pnlControls.Controls.Add(this.btnCNCServer);
            this.pnlControls.Controls.Add(this.grpCoordinates);
            this.pnlControls.Controls.Add(this.lblLog);
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(904, 160);
            this.pnlControls.TabIndex = 10;
            // 
            // grpCoordinates
            // 
            this.grpCoordinates.Controls.Add(this.lblXPos);
            this.grpCoordinates.Controls.Add(this.lblYPos);
            this.grpCoordinates.Controls.Add(this.lblZPos);
            this.grpCoordinates.Controls.Add(this.lblXValue);
            this.grpCoordinates.Controls.Add(this.lblYValue);
            this.grpCoordinates.Controls.Add(this.lblZValue);
            this.grpCoordinates.Location = new System.Drawing.Point(150, 110);
            this.grpCoordinates.Name = "grpCoordinates";
            this.grpCoordinates.Size = new System.Drawing.Size(300, 45);
            this.grpCoordinates.TabIndex = 12;
            this.grpCoordinates.TabStop = false;
            this.grpCoordinates.Text = "Machine Coordinates";
            // 
            // lblXPos
            // 
            this.lblXPos.AutoSize = true;
            this.lblXPos.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblXPos.Location = new System.Drawing.Point(6, 20);
            this.lblXPos.Name = "lblXPos";
            this.lblXPos.Size = new System.Drawing.Size(17, 15);
            this.lblXPos.TabIndex = 0;
            this.lblXPos.Text = "X:";
            // 
            // lblYPos
            // 
            this.lblYPos.AutoSize = true;
            this.lblYPos.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblYPos.Location = new System.Drawing.Point(100, 20);
            this.lblYPos.Name = "lblYPos";
            this.lblYPos.Size = new System.Drawing.Size(17, 15);
            this.lblYPos.TabIndex = 1;
            this.lblYPos.Text = "Y:";
            // 
            // lblZPos
            // 
            this.lblZPos.AutoSize = true;
            this.lblZPos.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblZPos.Location = new System.Drawing.Point(200, 20);
            this.lblZPos.Name = "lblZPos";
            this.lblZPos.Size = new System.Drawing.Size(17, 15);
            this.lblZPos.TabIndex = 2;
            this.lblZPos.Text = "Z:";
            // 
            // lblXValue
            // 
            this.lblXValue.AutoSize = true;
            this.lblXValue.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblXValue.ForeColor = System.Drawing.Color.Blue;
            this.lblXValue.Location = new System.Drawing.Point(25, 20);
            this.lblXValue.Name = "lblXValue";
            this.lblXValue.Size = new System.Drawing.Size(56, 15);
            this.lblXValue.TabIndex = 3;
            this.lblXValue.Text = "0.0000";
            // 
            // lblYValue
            // 
            this.lblYValue.AutoSize = true;
            this.lblYValue.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblYValue.ForeColor = System.Drawing.Color.Blue;
            this.lblYValue.Location = new System.Drawing.Point(119, 20);
            this.lblYValue.Name = "lblYValue";
            this.lblYValue.Size = new System.Drawing.Size(56, 15);
            this.lblYValue.TabIndex = 4;
            this.lblYValue.Text = "0.0000";
            // 
            // lblZValue
            // 
            this.lblZValue.AutoSize = true;
            this.lblZValue.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblZValue.ForeColor = System.Drawing.Color.Blue;
            this.lblZValue.Location = new System.Drawing.Point(219, 20);
            this.lblZValue.Name = "lblZValue";
            this.lblZValue.Size = new System.Drawing.Size(56, 15);
            this.lblZValue.TabIndex = 5;
            this.lblZValue.Text = "0.0000";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 692);
            this.Controls.Add(this.lblMessages);
            this.Controls.Add(this.txtMessages);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.webView);
            this.Name = "MainForm";
            this.Text = "HavenCNC Server";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.grpCoordinates.ResumeLayout(false);
            this.grpCoordinates.PerformLayout();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblApiUrl;
        private System.Windows.Forms.Button btnOpenSwagger;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.Button btnStopServer;
        private System.Windows.Forms.Button btnStartServer;
        private System.Windows.Forms.Button btnOpenReactApp;
        private System.Windows.Forms.Button btnGenerateOpenApi;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnGCodeTest;
        private System.Windows.Forms.Button btnCNCServer;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.GroupBox grpCoordinates;
        private System.Windows.Forms.Label lblXPos;
        private System.Windows.Forms.Label lblYPos;
        private System.Windows.Forms.Label lblZPos;
        private System.Windows.Forms.Label lblXValue;
        private System.Windows.Forms.Label lblYValue;
        private System.Windows.Forms.Label lblZValue;
        private System.Windows.Forms.TextBox txtMessages;
        private System.Windows.Forms.Label lblMessages;
    }
}
