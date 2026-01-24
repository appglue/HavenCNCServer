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

        // Step run service
        private readonly StepRunService _stepRunService = new StepRunService();

        // Drag and drop fields
        private bool _isDragging = false;
        private Point _dragStartPoint;

        /// <summary>
        /// Initializes a new instance of the GCodeTestDialog
        /// </summary>
        public GCodeTestDialog(MainForm? mainForm = null)
        {
            InitializeComponent();
            _mainForm = mainForm;

            // Subscribe to G-code text changes to enable/disable single command button
            txtGCode.TextChanged += OnGCodeTextChanged;

            // Enable form dragging
            this.MouseDown += GCodeTestDialog_MouseDown;
            this.MouseMove += GCodeTestDialog_MouseMove;
            this.MouseUp += GCodeTestDialog_MouseUp;

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
                var validationInfo = GCodeValidationService.GetValidationInfo(gCodeText);

                btnRunSingleCommand.Enabled = validationInfo.IsEnabled;
                btnRunSingleCommand.Text = validationInfo.ButtonText;

                if (validationInfo.IsSingleCommand)
                {
                    btnRunSingleCommand.BackColor = Color.LightGreen;
                }
                else if (validationInfo.ValidLineCount == 0)
                {
                    btnRunSingleCommand.BackColor = Color.LightGray;
                }
                else
                {
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
                var request = new Models.RunGCodeRequest
                {
                    GCodeLines = gCodeLines,
                    StartImmediately = true
                };
                await controller.RunGCode(request);
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
                var request = new Models.RunGCodeCommandRequest
                {
                    GCode = gCodeText
                };
                await controller.RunGCodeCommand(request);
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

                // Start step run using the service
                var result = _stepRunService.StartStepRun(gCodeLines);

                if (result.Success)
                {
                    // Load G-code into main form display if available
                    _mainForm?.LoadGCodeForDisplay(gCodeLines);

                    UpdateStepRunControls();
                    MessageBox.Show($"Step run started successfully!\nJob ID: {result.JobId}", "Step Run Started",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to start step run:\n{result.Error}", "Step Run Error",
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

                if (!_stepRunService.IsActive)
                {
                    MessageBox.Show("No step run is currently active.", "No Active Step Run",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Execute next step using the service
                var result = _stepRunService.ExecuteNextStep();

                if (result.Success)
                {
                    UpdateStepRunControls();

                    // Check if step run completed
                    if (result.IsComplete)
                    {
                        MessageBox.Show("Step run completed successfully!", "Step Run Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to execute next step:\n{result.Error}", "Step Execution Error",
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

                if (!_stepRunService.IsActive)
                {
                    MessageBox.Show("No step run is currently active.", "No Active Step Run",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Run from current step using the service
                var result = _stepRunService.RunFromCurrentStep();

                if (result.Success)
                {
                    MessageBox.Show("Running from current step to completion...", "Running to Completion",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStepRunControls();
                }
                else
                {
                    MessageBox.Show($"Failed to run from current step:\n{result.Error}", "Run Error",
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
            bool hasStepRunActive = _stepRunService.IsActive;

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
                var status = _stepRunService.GetCurrentStatus();
                lblCurrentLine.Text = $"Step {status.CurrentLine}/{status.TotalLines}";

                if (!string.IsNullOrEmpty(status.CurrentGCode))
                {
                    var displayLine = GCodeValidationService.GetDisplayLine(status.CurrentGCode, 40);
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
            _stepRunService.Reset();
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
                if (_stepRunService.IsActive)
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

        #region Form Dragging

        /// <summary>
        /// Handle mouse down event to start dragging
        /// </summary>
        private void GCodeTestDialog_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        /// <summary>
        /// Handle mouse move event to drag the form
        /// </summary>
        private void GCodeTestDialog_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point newLocation = this.Location;
                newLocation.X += e.X - _dragStartPoint.X;
                newLocation.Y += e.Y - _dragStartPoint.Y;
                this.Location = newLocation;
            }
        }

        /// <summary>
        /// Handle mouse up event to stop dragging
        /// </summary>
        private void GCodeTestDialog_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        #endregion
    }
}