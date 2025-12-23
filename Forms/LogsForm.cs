using System;
using System.Windows.Forms;
using HavenCNCServer.Components;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Forms
{
    /// <summary>
    /// Dedicated form for displaying application logs
    /// </summary>
    public partial class LogsForm : Form
    {
        private FlickerFreeLogViewer txtLog = null!;
        private Button btnClearLogs = null!;

        /// <summary>
        /// Initializes a new instance of the LogsForm
        /// </summary>
        public LogsForm()
        {
            InitializeComponent();
            SetupLogging();
        }

        private void InitializeComponent()
        {
            this.txtLog = new FlickerFreeLogViewer();
            this.btnClearLogs = new Button();
            this.SuspendLayout();

            // 
            // txtLog
            // 
            this.txtLog.Dock = DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(0, 40);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1000, 560);
            this.txtLog.TabIndex = 0;
            this.txtLog.WordWrap = true;

            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnClearLogs.Location = new System.Drawing.Point(880, 5);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(110, 30);
            this.btnClearLogs.TabIndex = 1;
            this.btnClearLogs.Text = "Clear Logs";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new EventHandler(this.btnClearLogs_Click);

            // 
            // LogsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.btnClearLogs);
            this.Controls.Add(this.txtLog);
            this.Name = "LogsForm";
            this.Text = "Application Logs";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }

        private void SetupLogging()
        {
            // Create and register a log target for this form's log viewer
            var logTarget = new LoggingService.FlickerFreeLogTarget(txtLog, this);
            LoggingService.AddTarget(logTarget);
            LogInfo("Logs form initialized", "LogsForm");
        }

        private void btnClearLogs_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all logs?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                txtLog.Clear();
                LogInfo("Logs cleared by user", "LogsForm");
            }
        }

        /// <summary>
        /// Handles form closing event to hide instead of disposing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of close to keep the log target active
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
