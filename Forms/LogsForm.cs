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
        private Button btnCopyLogs = null!;

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
            this.btnCopyLogs = new Button();
            this.SuspendLayout();

            // Get screen height
            var screenHeight = Screen.PrimaryScreen?.WorkingArea.Height ?? 800;

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
            // btnCopyLogs
            // 
            this.btnCopyLogs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCopyLogs.Location = new System.Drawing.Point(760, 5);
            this.btnCopyLogs.Name = "btnCopyLogs";
            this.btnCopyLogs.Size = new System.Drawing.Size(110, 30);
            this.btnCopyLogs.TabIndex = 1;
            this.btnCopyLogs.Text = "Copy Logs";
            this.btnCopyLogs.UseVisualStyleBackColor = true;
            this.btnCopyLogs.Click += new EventHandler(this.btnCopyLogs_Click);

            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnClearLogs.Location = new System.Drawing.Point(880, 5);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(110, 30);
            this.btnClearLogs.TabIndex = 2;
            this.btnClearLogs.Text = "Clear Logs";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new EventHandler(this.btnClearLogs_Click);

            // 
            // LogsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, screenHeight - 50); // Full screen height minus taskbar
            this.Controls.Add(this.btnCopyLogs);
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

        private void btnCopyLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtLog.Text))
                {
                    Clipboard.SetText(txtLog.Text);
                    LogInfo("Logs copied to clipboard", "LogsForm");
                    MessageBox.Show("Logs copied to clipboard", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No logs to copy", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error copying logs: {ex.Message}", "LogsForm");
                MessageBox.Show($"Failed to copy logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearLogs_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Clear log window display?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                txtLog.Clear();
                txtLog.Reset(); // Reset the internal counter
                LogInfo("Log window display cleared by user", "LogsForm");
            }
        }

        /// <summary>
        /// Handles form shown event to scroll to bottom
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Scroll to bottom when form is shown
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
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
