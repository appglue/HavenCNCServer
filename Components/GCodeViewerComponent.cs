using System;
using System.Drawing;
using System.Windows.Forms;
using CentroidAPI;
using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying G-code with line highlighting
    /// </summary>
    public class GCodeViewerComponent : UserControl, ICNCEventListener
    {
        private RichTextBox txtGCode = null!;
        private Label lblGCode = null!;
        private Label lblCurrentJob = null!;
        private string[] _currentGCode = Array.Empty<string>();
        private int _currentLineNumber = 0;

        /// <summary>
        /// Initializes a new instance of the GCodeViewerComponent
        /// </summary>
        public GCodeViewerComponent()
        {
            InitializeComponent();
            SetupGCodeViewer();
        }

        private void InitializeComponent()
        {
            this.lblGCode = new Label();
            this.lblCurrentJob = new Label();
            this.txtGCode = new RichTextBox();
            this.SuspendLayout();

            // lblGCode
            this.lblGCode.AutoSize = true;
            this.lblGCode.Location = new Point(3, 0);
            this.lblGCode.Name = "lblGCode";
            this.lblGCode.Size = new Size(79, 13);
            this.lblGCode.TabIndex = 0;
            this.lblGCode.Text = "G-Code Viewer";

            // lblCurrentJob
            this.lblCurrentJob.AutoSize = true;
            this.lblCurrentJob.Location = new Point(3, 384);
            this.lblCurrentJob.Name = "lblCurrentJob";
            this.lblCurrentJob.Size = new Size(73, 13);
            this.lblCurrentJob.TabIndex = 2;
            this.lblCurrentJob.Text = "No active job";

            // txtGCode
            this.txtGCode.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.txtGCode.Location = new Point(3, 16);
            this.txtGCode.Name = "txtGCode";
            this.txtGCode.ReadOnly = true;
            this.txtGCode.Size = new Size(394, 365);
            this.txtGCode.TabIndex = 1;
            this.txtGCode.Text = "";
            this.txtGCode.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

            // GCodeViewerComponent
            this.Controls.Add(this.txtGCode);
            this.Controls.Add(this.lblCurrentJob);
            this.Controls.Add(this.lblGCode);
            this.Name = "GCodeViewerComponent";
            this.Size = new Size(400, 400);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupGCodeViewer()
        {
            txtGCode.Clear();
            txtGCode.ReadOnly = true;
            lblCurrentJob.Text = "No active job";
            
            // Register as event listener with CNCJobInfoListener
            CNCJobInfoListener.AddListener(this);
        }

        /// <summary>
        /// Receives and processes CNC events for G-code display updates
        /// </summary>
        /// <param name="centroidEvent">The CNC event to process</param>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            // Prioritize our custom StepExecutionEvent for accurate line tracking
            if (centroidEvent is StepExecutionEvent stepEvent)
            {
                // Handle step execution event (most accurate for G-code display)
                if (InvokeRequired)
                {
                    Invoke(new Action(() => HandleStepExecution(stepEvent)));
                }
                else
                {
                    HandleStepExecution(stepEvent);
                }
            }
            else if (centroidEvent is JobStartedEvent jobStartedEvent)
            {
                // Handle job started event
                if (InvokeRequired)
                {
                    Invoke(new Action(() => HandleJobStarted(jobStartedEvent)));
                }
                else
                {
                    HandleJobStarted(jobStartedEvent);
                }
            }
            else if (centroidEvent is JobCompletedEvent jobCompletedEvent)
            {
                // Handle job completed event
                if (InvokeRequired)
                {
                    Invoke(new Action(() => HandleJobCompleted(jobCompletedEvent)));
                }
                else
                {
                    HandleJobCompleted(jobCompletedEvent);
                }
            }
            else if (centroidEvent is JobInfoEvent jobEvent)
            {
                // Fallback to JobInfoEvent only if no StepExecutionEvent is available
                // This ensures compatibility with existing CNC system events
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateGCodeDisplayFallback(jobEvent)));
                }
                else
                {
                    UpdateGCodeDisplayFallback(jobEvent);
                }
            }
        }

        private void UpdateGCodeDisplayFallback(JobInfoEvent jobEvent)
        {
            try
            {
                // Update current line number
                _currentLineNumber = jobEvent.LineNumber;

                // Update current job label
                lblCurrentJob.Text = $"Current Job: Line {_currentLineNumber} - {jobEvent.JobName ?? "Unknown"}";

                // If we have G-code loaded, highlight the current line
                if (_currentGCode.Length > 0)
                {
                    DisplayGCodeWithHighlight();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating G-code display (fallback): {ex.Message}", "GCodeDisplay");
            }
        }

        private void HandleStepExecution(StepExecutionEvent stepEvent)
        {
            try
            {
                // Update current line number
                _currentLineNumber = stepEvent.LineNumber;

                // Update job status with step information
                var statusText = stepEvent.Status switch
                {
                    StepExecutionStatus.AboutToExecute => "About to execute",
                    StepExecutionStatus.Executing => "Executing",
                    StepExecutionStatus.Completed => "Completed",
                    StepExecutionStatus.Failed => "Failed",
                    StepExecutionStatus.Skipped => "Skipped",
                    _ => "Unknown"
                };

                lblCurrentJob.Text = $"Step Run: {stepEvent.JobId} - Line {stepEvent.LineNumber}/{stepEvent.TotalLines} ({statusText})";

                // If we have G-code loaded, highlight the current line
                if (_currentGCode.Length > 0)
                {
                    DisplayGCodeWithHighlight();
                }

                LogDebug($"Step execution event: Line {stepEvent.LineNumber} - {statusText} - {stepEvent.CurrentLine}", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error handling step execution event: {ex.Message}", "GCodeDisplay");
            }
        }

        private void HandleJobStarted(JobStartedEvent jobStartedEvent)
        {
            try
            {
                // Load the G-code into the display
                LoadGCodeForDisplay(jobStartedEvent.GCodeLines);
                
                // Update job info
                lblCurrentJob.Text = $"Job Started: {jobStartedEvent.JobId} ({jobStartedEvent.TotalLines} lines)";
                
                // Reset current line to start
                _currentLineNumber = 1;
                
                DisplayGCodeWithHighlight();
                
                LogInfo($"Job started event handled: {jobStartedEvent.JobId}", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error handling job started event: {ex.Message}", "GCodeDisplay");
            }
        }

        private void HandleJobCompleted(JobCompletedEvent jobCompletedEvent)
        {
            try
            {
                // Update job status
                var status = jobCompletedEvent.Success ? "COMPLETED" : "FAILED";
                var duration = jobCompletedEvent.Duration.TotalSeconds.ToString("F1");
                
                lblCurrentJob.Text = $"Job {status}: {jobCompletedEvent.JobId} ({duration}s, {jobCompletedEvent.LinesExecuted} lines)";
                
                if (!jobCompletedEvent.Success && !string.IsNullOrEmpty(jobCompletedEvent.ErrorMessage))
                {
                    lblCurrentJob.Text += $" - Error: {jobCompletedEvent.ErrorMessage}";
                }
                
                LogInfo($"Job completed event handled: {jobCompletedEvent.JobId} - Success: {jobCompletedEvent.Success}", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error handling job completed event: {ex.Message}", "GCodeDisplay");
            }
        }

        /// <summary>
        /// Loads G-code lines for display in the viewer
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to display</param>
        public void LoadGCodeForDisplay(string[] gCodeLines)
        {
            try
            {
                _currentGCode = gCodeLines ?? Array.Empty<string>();
                _currentLineNumber = 0;

                // Display the G-code
                DisplayGCodeWithHighlight();
            }
            catch (Exception ex)
            {
                LogError($"Error loading G-code: {ex.Message}", "GCodeDisplay");
            }
        }

        /// <summary>
        /// Clears the G-code display
        /// </summary>
        public void ClearGCode()
        {
            try
            {
                _currentGCode = Array.Empty<string>();
                _currentLineNumber = 0;
                txtGCode.Clear();
                lblCurrentJob.Text = "No active job";
            }
            catch (Exception ex)
            {
                LogError($"Error clearing G-code: {ex.Message}", "GCodeDisplay");
            }
        }

        private void DisplayGCodeWithHighlight()
        {
            try
            {
                if (_currentGCode.Length == 0)
                {
                    txtGCode.Clear();
                    return;
                }

                // Clear existing text
                txtGCode.Clear();

                // Add each line with appropriate highlighting
                for (int i = 0; i < _currentGCode.Length; i++)
                {
                    var lineNumber = i + 1;
                    var line = _currentGCode[i];
                    var displayLine = $"{lineNumber:D4}: {line}";

                    // Set color based on whether this is the current line
                    Color lineColor = lineNumber == _currentLineNumber ? Color.Red : Color.Black;
                    Color backgroundColor = lineNumber == _currentLineNumber ? Color.Yellow : Color.White;

                    // Add the line with color
                    AddColoredGCodeLine(displayLine, lineColor, backgroundColor);
                }

                // Scroll to the current line
                ScrollToCurrentLine();
            }
            catch (Exception ex)
            {
                LogError($"Error displaying G-code with highlight: {ex.Message}", "GCodeDisplay");
            }
        }

        private void AddColoredGCodeLine(string line, Color textColor, Color backgroundColor)
        {
            try
            {
                // Move to end and add text
                txtGCode.SelectionStart = txtGCode.Text.Length;
                txtGCode.SelectionLength = 0;
                txtGCode.SelectionColor = textColor;
                txtGCode.SelectionBackColor = backgroundColor;
                txtGCode.AppendText(line + Environment.NewLine);

                // Reset colors to default
                txtGCode.SelectionColor = Color.Black;
                txtGCode.SelectionBackColor = Color.White;
            }
            catch (Exception ex)
            {
                LogError($"Error adding colored G-code line: {ex.Message}", "GCodeDisplay");
            }
        }

        private void ScrollToCurrentLine()
        {
            try
            {
                if (_currentLineNumber > 0 && _currentGCode.Length > 0)
                {
                    // Calculate the character position of the current line
                    int charPosition = 0;
                    for (int i = 0; i < Math.Min(_currentLineNumber - 1, _currentGCode.Length); i++)
                    {
                        charPosition += $"{i + 1:D4}: {_currentGCode[i]}".Length + Environment.NewLine.Length;
                    }

                    // Set selection to the current line
                    txtGCode.SelectionStart = charPosition;
                    txtGCode.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error scrolling to current line: {ex.Message}", "GCodeDisplay");
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
}