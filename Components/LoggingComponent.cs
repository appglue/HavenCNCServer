using System;
using System.Drawing;
using System.Windows.Forms;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying application logs
    /// </summary>
    public class LoggingComponent : UserControl
    {
        private RichTextBox txtLog;
        private Label lblLog;

        public LoggingComponent()
        {
            InitializeComponent();
            SetupLogging();
        }

        private void InitializeComponent()
        {
            this.lblLog = new Label();
            this.txtLog = new RichTextBox();
            this.SuspendLayout();

            // lblLog
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new Point(3, 0);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new Size(66, 13);
            this.lblLog.TabIndex = 0;
            this.lblLog.Text = "Application Logs";

            // txtLog
            this.txtLog.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.txtLog.Location = new Point(3, 16);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new Size(394, 381);
            this.txtLog.TabIndex = 1;
            this.txtLog.Text = "";

            // LoggingComponent
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lblLog);
            this.Name = "LoggingComponent";
            this.Size = new Size(400, 400);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupLogging()
        {
            // Subscribe to logging events
            LoggingService.LogEntryAdded += OnLogEntryAdded;
            
            // Add initial message
            txtLog.Text = "=== Application Log ===\r\nLogging system initialized.\r\n\r\n";
        }

        private void OnLogEntryAdded(LogEntry entry)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => OnLogEntryAdded(entry)));
                return;
            }

            // Determine color based on log level
            Color color = entry.Level switch
            {
                LogLevel.Success => Color.Green,
                LogLevel.Info => Color.Blue,
                LogLevel.Warning => Color.Orange,
                LogLevel.Error => Color.Red,
                _ => Color.Black
            };

            // Preserve current selection
            var originalStart = txtLog.SelectionStart;
            var originalLength = txtLog.SelectionLength;

            // Format the log entry message
            var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] [{entry.Source}] {entry.Message}";

            // Append colored text
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(formattedMessage + Environment.NewLine);
            txtLog.SelectionColor = txtLog.ForeColor; // Reset to default

            // Restore selection
            txtLog.SelectionStart = originalStart;
            txtLog.SelectionLength = originalLength;

            // Auto-scroll to bottom
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from logging events
                LoggingService.LogEntryAdded -= OnLogEntryAdded;
            }
            base.Dispose(disposing);
        }
    }
}