using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CentroidAPI;
using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying CNC messages
    /// </summary>
    public class MessageDisplayComponent : UserControl, ICNCEventListener
    {
        private RichTextBox txtMessages = null!;
        private Label lblMessages = null!;
        private int _maxMessages = 1000;
        private int _currentMessageCount = 0;

        /// <summary>
        /// Initializes a new instance of the MessageDisplayComponent
        /// </summary>
        public MessageDisplayComponent()
        {
            InitializeComponent();
            SetupMessageDisplay();
        }

        private void InitializeComponent()
        {
            this.lblMessages = new Label();
            this.txtMessages = new RichTextBox();
            this.SuspendLayout();

            // lblMessages
            this.lblMessages.AutoSize = true;
            this.lblMessages.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblMessages.ForeColor = Color.DarkBlue;
            this.lblMessages.Location = new Point(3, 0);
            this.lblMessages.Name = "lblMessages";
            this.lblMessages.Size = new Size(102, 13);
            this.lblMessages.TabIndex = 0;
            this.lblMessages.Text = "CNC Messages";

            // txtMessages
            this.txtMessages.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.txtMessages.Location = new Point(3, 16);
            this.txtMessages.Name = "txtMessages";
            this.txtMessages.ReadOnly = true;
            this.txtMessages.Size = new Size(394, 381);
            this.txtMessages.TabIndex = 1;
            this.txtMessages.Text = "";

            // MessageDisplayComponent
            this.Controls.Add(this.txtMessages);
            this.Controls.Add(this.lblMessages);
            this.Name = "MessageDisplayComponent";
            this.Size = new Size(400, 400);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupMessageDisplay()
        {
            txtMessages.Text = "=== CNC Message Monitor ===\r\nWaiting for CNC messages...\r\n\r\n";
            
            // Register as event listener with CNCJobInfoListener
            CNCJobInfoListener.AddListener(this);
        }

        /// <summary>
        /// Receives and processes CNC events for message display
        /// </summary>
        /// <param name="centroidEvent">The CNC event to process</param>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            // Process multiple event types for comprehensive display
            if (centroidEvent is MessageEvent messageEvent)
            {
                // Update UI on the main thread using Invoke
                if (InvokeRequired)
                {
                    Invoke(new Action(() => AddMessage(messageEvent)));
                }
                else
                {
                    AddMessage(messageEvent);
                }
            }
            else if (centroidEvent is JobInfoEvent jobEvent)
            {
                // Show job info events (line numbers, program changes, etc.)
                if (InvokeRequired)
                {
                    Invoke(new Action(() => AddJobInfoMessage(jobEvent)));
                }
                else
                {
                    AddJobInfoMessage(jobEvent);
                }
            }
            else if (centroidEvent is StepExecutionEvent stepEvent)
            {
                // Only show step execution events for significant status changes, not every executing step
                if (stepEvent.Status == StepExecutionStatus.Failed || 
                    stepEvent.Status == StepExecutionStatus.Completed ||
                    stepEvent.IsLastStep)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => AddStepExecutionMessage(stepEvent)));
                    }
                    else
                    {
                        AddStepExecutionMessage(stepEvent);
                    }
                }
            }
            else if (centroidEvent is DROEvent droEvent)
            {
                // Show DRO position updates
                if (InvokeRequired)
                {
                    Invoke(new Action(() => AddDroMessage(droEvent)));
                }
                else
                {
                    AddDroMessage(droEvent);
                }
            }
        }

        private void AddMessage(MessageEvent messageEvent)
        {
            try
            {
                var severity = GetMessageSeverity(messageEvent.EventType);
                var timestamp = messageEvent.Timestamp.ToString("HH:mm:ss.fff");
                var eventCode = messageEvent.EventCode > 0 ? $"[{messageEvent.EventCode}]" : "";
                var messageText = $"[{timestamp}] {eventCode} ({messageEvent.EventType}) {messageEvent.Message}";

                // Add message with color coding
                AddColoredMessage(messageText, GetColorForSeverity(severity));

                _currentMessageCount++;

                // Trim old messages if we exceed the limit
                if (_currentMessageCount > _maxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding CNC message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddJobInfoMessage(JobInfoEvent jobEvent)
        {
            try
            {
                var timestamp = jobEvent.Timestamp.ToString("HH:mm:ss.fff");
                var levelInfo = $" (Level {jobEvent.StackLevel})"; // Always show level, including 0 for main program
                var messageText = $"[{timestamp}] JOB: Line {jobEvent.LineNumber}{levelInfo} - {jobEvent.Message}";

                // Job info messages are shown in blue
                AddColoredMessage(messageText, Color.Blue);

                _currentMessageCount++;

                // Trim old messages if we exceed the limit
                if (_currentMessageCount > _maxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding job info message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddStepExecutionMessage(StepExecutionEvent stepEvent)
        {
            try
            {
                var timestamp = stepEvent.Timestamp.ToString("HH:mm:ss.fff");
                var progressInfo = stepEvent.TotalLines > 0 ? $" ({stepEvent.LineNumber}/{stepEvent.TotalLines})" : "";
                var statusInfo = $" [{stepEvent.Status}]";
                var messageText = $"[{timestamp}] STEP{progressInfo}{statusInfo}: {stepEvent.CurrentLine}";

                // Step execution messages are shown in dark green
                AddColoredMessage(messageText, Color.DarkGreen);

                _currentMessageCount++;

                // Trim old messages if we exceed the limit
                if (_currentMessageCount > _maxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding step execution message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddDroMessage(DROEvent droEvent)
        {
            try
            {
                var timestamp = droEvent.Timestamp.ToString("HH:mm:ss.fff");
                var messageText = $"[{timestamp}] DRO: X:{droEvent.Axis1:F4} Y:{droEvent.Axis2:F4} Z:{droEvent.Axis3:F4}";

                // DRO messages are shown in purple/magenta
                AddColoredMessage(messageText, Color.Purple);

                _currentMessageCount++;

                // Trim old messages if we exceed the limit
                if (_currentMessageCount > _maxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding DRO message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddColoredMessage(string message, Color color)
        {
            try
            {
                // Save current selection
                var originalStart = txtMessages.SelectionStart;
                var originalLength = txtMessages.SelectionLength;

                // Move to end and add text
                txtMessages.SelectionStart = txtMessages.Text.Length;
                txtMessages.SelectionLength = 0;
                txtMessages.SelectionColor = color;
                txtMessages.AppendText(message + Environment.NewLine);

                // Reset color to default
                txtMessages.SelectionColor = Color.Black;

                // Scroll to bottom to show latest messages
                txtMessages.ScrollToCaret();

                // Restore original selection (if any)
                if (originalLength > 0)
                {
                    txtMessages.SelectionStart = originalStart;
                    txtMessages.SelectionLength = originalLength;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding colored message: {ex.Message}", "MessageDisplay");
            }
        }

        private Color GetColorForSeverity(MessageSeverity severity)
        {
            return severity switch
            {
                MessageSeverity.Error => Color.Red,
                MessageSeverity.Warning => Color.Orange,
                MessageSeverity.Success => Color.Green,
                MessageSeverity.Info => Color.Blue,
                MessageSeverity.Normal => Color.Black,
                _ => Color.Black
            };
        }

        private MessageSeverity GetMessageSeverity(MessageEventType eventType)
        {
            return eventType switch
            {
                MessageEventType.SystemFault or
                MessageEventType.AxisFault or
                MessageEventType.LimitError or
                MessageEventType.ProbeError or
                MessageEventType.CommunicationError or
                MessageEventType.StartupError or
                MessageEventType.MiscellaneousError => MessageSeverity.Error,

                MessageEventType.SyntaxError or
                MessageEventType.GCodeError or
                MessageEventType.ParameterError or
                MessageEventType.CutterCompensationError or
                MessageEventType.ParameterSettingError or
                MessageEventType.CannedCycleError or
                MessageEventType.ScalingError => MessageSeverity.Warning,

                MessageEventType.JobCompleted => MessageSeverity.Success,
                MessageEventType.JobStarted or
                MessageEventType.StatusMessage => MessageSeverity.Info,

                _ => MessageSeverity.Normal
            };
        }

        private void TrimOldMessages()
        {
            try
            {
                var lines = txtMessages.Lines;
                if (lines.Length > _maxMessages)
                {
                    // Keep only the most recent messages
                    var keepLines = lines.Skip(lines.Length - (_maxMessages * 3 / 4)).ToArray();
                    txtMessages.Lines = keepLines;
                    _currentMessageCount = keepLines.Length;
                    
                    // Add separator to show where trimming occurred
                    AddColoredMessage("--- [Previous messages trimmed] ---", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error trimming old messages: {ex.Message}", "MessageDisplay");
            }
        }

        /// <summary>
        /// Disposes of the component resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unregister from event listener
                CNCJobInfoListener.RemoveListener(this);
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Message severity levels for color coding
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>
        /// Normal message level
        /// </summary>
        Normal,
        /// <summary>
        /// Informational message level
        /// </summary>
        Info,
        /// <summary>
        /// Success message level
        /// </summary>
        Success,
        /// <summary>
        /// Warning message level
        /// </summary>
        Warning,
        /// <summary>
        /// Error message level
        /// </summary>
        Error
    }
}