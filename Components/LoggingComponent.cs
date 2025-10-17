using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying application logs using ListView for better performance
    /// </summary>
    public class LoggingComponent : UserControl
    {
        private ListView lstLog = null!;
        private Label lblLog = null!;
        private readonly List<LogEntry> _logEntries = new();
        private const int MAX_LOG_ENTRIES = 1000;

        /// <summary>
        /// Initializes a new instance of the LoggingComponent
        /// </summary>
        public LoggingComponent()
        {
            InitializeComponent();
            SetupLogging();
        }

        private void SetupLogging()
        {
            // Subscribe to log events
            LoggingService.LogEntryAdded += OnLogEntryAdded;
            
            // Add initial log entry
            var initialEntry = new LogEntry("Logging system initialized", LogLevel.Info, "System");
            _logEntries.Add(initialEntry);
            RefreshListView();
        }

        private void OnLogEntryAdded(LogEntry entry)
        {
            // Update UI on the main thread using Invoke
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnLogEntryAdded(entry)));
                return;
            }

            // Add to our collection
            _logEntries.Add(entry);
            
            // Trim old entries if needed
            if (_logEntries.Count > MAX_LOG_ENTRIES)
            {
                _logEntries.RemoveAt(0);
            }

            // Refresh the ListView
            RefreshListView();
        }

        private void RefreshListView()
        {
            lstLog.VirtualListSize = _logEntries.Count;
            
            // Auto-scroll to bottom to show newest entries
            if (_logEntries.Count > 0)
            {
                lstLog.EnsureVisible(_logEntries.Count - 1);
            }
        }

        private void LstLog_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex >= 0 && e.ItemIndex < _logEntries.Count)
            {
                var entry = _logEntries[e.ItemIndex];
                var item = new ListViewItem(new[]
                {
                    entry.Timestamp.ToString("HH:mm:ss.fff"),
                    entry.Level.ToString(),
                    entry.Source,
                    entry.Message
                });

                // Set color based on log level
                item.ForeColor = entry.Level switch
                {
                    LogLevel.Success => Color.Green,
                    LogLevel.Info => Color.Blue,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Error => Color.Red,
                    _ => Color.Black
                };

                e.Item = item;
            }
        }

        private void InitializeComponent()
        {
            this.lblLog = new Label();
            this.lstLog = new ListView();
            this.SuspendLayout();

            // lblLog
            this.lblLog.AutoSize = true;
            this.lblLog.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblLog.ForeColor = Color.DarkBlue;
            this.lblLog.Location = new Point(3, 0);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new Size(82, 13);
            this.lblLog.TabIndex = 0;
            this.lblLog.Text = "Server Logs";

            // lstLog - Setup ListView for virtual mode
            this.lstLog.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.lstLog.Location = new Point(3, 16);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new Size(394, 381);
            this.lstLog.TabIndex = 1;
            this.lstLog.UseCompatibleStateImageBehavior = false;
            this.lstLog.View = View.Details;
            this.lstLog.VirtualMode = true;
            this.lstLog.FullRowSelect = true;
            this.lstLog.GridLines = true;
            this.lstLog.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            // Add columns
            this.lstLog.Columns.Add("Time", 80);
            this.lstLog.Columns.Add("Level", 60);
            this.lstLog.Columns.Add("Source", 80);
            this.lstLog.Columns.Add("Message", 200);

            // Subscribe to virtual mode events
            this.lstLog.RetrieveVirtualItem += LstLog_RetrieveVirtualItem;

            // LoggingComponent
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.lblLog);
            this.Name = "LoggingComponent";
            this.Size = new Size(400, 400);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// Disposes of the component resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
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