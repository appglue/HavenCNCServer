using CentroidAPI;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// G-Code Test Dialog for testing Centroid API G-code execution capabilities
    /// </summary>
    public partial class GCodeTestDialog : Form
    {
        private readonly MainForm? _mainForm;
        
        // Step run mode fields
        private string _currentJobId = string.Empty;
        private bool _isStepRunActive = false;
        private int _currentStepLine = 0;
        private string[] _stepRunGCodeLines = Array.Empty<string>();

        /// <summary>
        /// Initializes a new instance of the GCodeTestDialog
        /// </summary>
        public GCodeTestDialog(MainForm? mainForm = null)
        {
            InitializeComponent();
            _mainForm = mainForm;
            
            // Subscribe to G-code text changes to enable/disable single command button
            txtGCode.TextChanged += OnGCodeTextChanged;
            
            // Update button states initially
            UpdateSingleCommandButtonState();
            UpdateStepRunControls();
        }



        /// <summary>
        /// Handle G-code text changes to update button states
        /// </summary>
        private void OnGCodeTextChanged(object? sender, EventArgs e)
        {
            UpdateSingleCommandButtonState();
        }

        /// <summary>
        /// Update the single command button state based on G-code content
        /// </summary>
        private void UpdateSingleCommandButtonState()
        {
            try
            {
                var gCodeText = txtGCode.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(gCodeText))
                {
                    btnRunSingleCommand.Enabled = false;
                    btnRunSingleCommand.Text = "Run Single Command";
                    return;
                }

                // Count valid G-code lines (excluding comments and empty lines)
                var validLines = gCodeText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line.Trim()) && 
                                   !line.Trim().StartsWith(";") && 
                                   !line.Trim().StartsWith("("))
                    .Count();

                if (validLines == 1)
                {
                    btnRunSingleCommand.Enabled = true;
                    btnRunSingleCommand.Text = "Run Single Command";
                    btnRunSingleCommand.BackColor = Color.LightGreen;
                }
                else if (validLines == 0)
                {
                    btnRunSingleCommand.Enabled = false;
                    btnRunSingleCommand.Text = "Run Single Command";
                    btnRunSingleCommand.BackColor = Color.LightGray;
                }
                else
                {
                    btnRunSingleCommand.Enabled = false;
                    btnRunSingleCommand.Text = $"Multiple Commands ({validLines})";
                    btnRunSingleCommand.BackColor = Color.LightYellow;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error updating single command button state: {ex.Message}", "UI");
                btnRunSingleCommand.Enabled = false;
            }
        }

        /// <summary>
        /// Run G-code through the CNC Program Controller
        /// </summary>
        private async void btnRunGCode_Click(object sender, EventArgs e)
        {
            try
            {
                btnRunGCode.Enabled = false;
                
                // Get the G-code lines from the editor
                var gCodeLines = txtGCode.Lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                
                if (gCodeLines.Length == 0)
                {
                    MessageBox.Show("Please enter some G-code to execute.", "No G-Code", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Create the controller and execute the G-code
                // The controller and CNCJob system will handle all logging and notifications
                var controller = new Controllers.CNCProgramController();
                await controller.RunGCode(gCodeLines, startImmediately: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing G-code:\n{ex.Message}", "Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRunGCode.Enabled = true;
            }
        }

        /// <summary>
        /// Run a single G-code command through the CNC Program Controller
        /// </summary>
        private async void btnRunSingleCommand_Click(object sender, EventArgs e)
        {
            try
            {
                btnRunSingleCommand.Enabled = false;
                
                // Get the single G-code command from the editor
                var gCodeText = txtGCode.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(gCodeText))
                {
                    MessageBox.Show("Please enter a G-code command to execute.", "No G-Code", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Create the controller and execute the single command
                // The controller and CNCJob system will handle all logging and notifications
                var controller = new Controllers.CNCProgramController();
                await controller.RunGCodeCommand(gCodeText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing G-code command:\n{ex.Message}", "Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRunSingleCommand.Enabled = true;
            }
        }

        #region Step Run Mode Event Handlers

        /// <summary>
        /// Handle step run mode checkbox change
        /// </summary>
        private void chkStepRunMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateStepRunControls();
        }

        /// <summary>
        /// Start step run mode
        /// </summary>
        private void btnStartStepRun_Click(object sender, EventArgs e)
        {
            try
            {
                btnStartStepRun.Enabled = false;

                // Get G-code lines
                var gCodeLines = txtGCode.Lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();

                if (gCodeLines.Length == 0)
                {
                    MessageBox.Show("Please enter some G-code before starting step run.", "No G-Code", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Start step run using the controller
                var controller = new Controllers.CNCProgramController();
                var response = controller.StartStepRun(gCodeLines);

                if (response.Success)
                {
                    _currentJobId = response.JobId;
                    _isStepRunActive = true;
                    _currentStepLine = 1;
                    _stepRunGCodeLines = gCodeLines;

                    // Load G-code into main form display if available
                    _mainForm?.LoadGCodeForDisplay(gCodeLines);

                    UpdateStepRunControls();
                    MessageBox.Show($"Step run started successfully!\nJob ID: {_currentJobId}", "Step Run Started", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to start step run:\n{response.Error}", "Step Run Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting step run:\n{ex.Message}", "Step Run Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartStepRun.Enabled = true;
            }
        }

        /// <summary>
        /// Execute the next step in step run mode
        /// </summary>
        private void btnNextStep_Click(object sender, EventArgs e)
        {
            try
            {
                btnNextStep.Enabled = false;

                if (!_isStepRunActive || string.IsNullOrEmpty(_currentJobId))
                {
                    MessageBox.Show("No step run is currently active.", "No Active Step Run", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Execute next step using the controller
                var controller = new Controllers.CNCProgramController();
                var response = controller.StepRunNext();

                if (response.Success)
                {
                    _currentStepLine++;
                    UpdateStepRunControls();

                    // Check if we've reached the end of the G-code
                    if (_currentStepLine > _stepRunGCodeLines.Length)
                    {
                        MessageBox.Show("Step run completed successfully!", "Step Run Complete", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ResetStepRun();
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to execute next step:\n{response.Error}", "Step Execution Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing next step:\n{ex.Message}", "Step Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnNextStep.Enabled = true;
            }
        }

        /// <summary>
        /// Run from current step to completion
        /// </summary>
        private void btnRunFromCurrent_Click(object sender, EventArgs e)
        {
            try
            {
                btnRunFromCurrent.Enabled = false;

                if (!_isStepRunActive || string.IsNullOrEmpty(_currentJobId))
                {
                    MessageBox.Show("No step run is currently active.", "No Active Step Run", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Run from current step using the controller
                var controller = new Controllers.CNCProgramController();
                var response = controller.RunFromCurrentStep();

                if (response.Success)
                {
                    MessageBox.Show("Running from current step to completion...", "Running to Completion", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetStepRun();
                }
                else
                {
                    MessageBox.Show($"Failed to run from current step:\n{response.Error}", "Run Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running from current step:\n{ex.Message}", "Run Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRunFromCurrent.Enabled = true;
            }
        }

        #endregion

        #region Step Run Mode Helper Methods

        /// <summary>
        /// Update the visibility and state of step run controls
        /// </summary>
        private void UpdateStepRunControls()
        {
            bool stepRunMode = chkStepRunMode.Checked;
            bool hasStepRunActive = _isStepRunActive && !string.IsNullOrEmpty(_currentJobId);

            if (stepRunMode)
            {
                // Show step run controls, hide regular controls
                btnRunGCode.Visible = false;
                btnRunSingleCommand.Visible = false;
                btnStartStepRun.Visible = true;
                btnNextStep.Visible = true;
                btnRunFromCurrent.Visible = true;
                lblCurrentLine.Visible = true;

                // Enable/disable step run controls based on state
                btnStartStepRun.Enabled = !hasStepRunActive;
                btnNextStep.Enabled = hasStepRunActive;
                btnRunFromCurrent.Enabled = hasStepRunActive;
            }
            else
            {
                // Show regular controls, hide step run controls
                btnRunGCode.Visible = true;
                btnRunSingleCommand.Visible = true;
                btnStartStepRun.Visible = false;
                btnNextStep.Visible = false;
                btnRunFromCurrent.Visible = false;
                lblCurrentLine.Visible = false;

                // Enable regular controls
                btnRunGCode.Enabled = true;
                
                // Re-enable single command button based on G-code content
                UpdateSingleCommandButtonState();
            }

            // Update current line display for step run mode
            if (stepRunMode && hasStepRunActive)
            {
                var totalLines = _stepRunGCodeLines.Length;
                lblCurrentLine.Text = $"Step {_currentStepLine}/{totalLines}";
                
                if (_currentStepLine <= _stepRunGCodeLines.Length)
                {
                    var currentLine = _stepRunGCodeLines[_currentStepLine - 1];
                    var displayLine = currentLine.Length > 40 ? currentLine.Substring(0, 40) + "..." : currentLine;
                    lblCurrentLine.Text += $": {displayLine}";
                }
            }
            else if (stepRunMode)
            {
                lblCurrentLine.Text = "No step run active";
            }
        }

        /// <summary>
        /// Reset step run mode state
        /// </summary>
        private void ResetStepRun()
        {
            _currentJobId = string.Empty;
            _isStepRunActive = false;
            _currentStepLine = 0;
            _stepRunGCodeLines = Array.Empty<string>();
            UpdateStepRunControls();
        }

        #endregion

        /// <summary>
        /// Clean up resources when dialog is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Unsubscribe from text change events
                txtGCode.TextChanged -= OnGCodeTextChanged;
                
                // Clean up step run if active
                if (_isStepRunActive)
                {
                    ResetStepRun();
                }
                
                // Note: We don't disconnect the CNC here since other parts of the application might be using it
                // The CNCConnectionManager will handle connection lifecycle
            }
            catch (Exception ex)
            {
                // Error during cleanup - not critical for dialog closing
                System.Diagnostics.Debug.WriteLine($"Error during GCodeTestDialog cleanup: {ex.Message}");
            }

            base.OnFormClosing(e);
        }
    }
}