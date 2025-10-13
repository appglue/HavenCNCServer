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
            
            // Auto-start listener disabled for debugging
            /*
            Task.Run(() =>
            {
                try
                {
                    // Small delay to ensure UI is fully initialized
                    Thread.Sleep(500);
                    CNCJobInfoListener.AutoStartIfConnected();
                    
                    // Update UI after auto-start attempt
                    if (!IsDisposed && !Disposing)
                    {
                        Invoke(() => UpdateListenerButtonState());
                    }
                }
                catch (Exception ex)
                {
                    if (!IsDisposed && !Disposing)
                    {
                        Invoke(() => LogError($"Error auto-starting listener on dialog load: {ex.Message}", "JobInfo"));
                    }
                }
            });
            */
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

        /// <summary>
        /// Clean up resources when dialog is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Unsubscribe from text change events
                txtGCode.TextChanged -= OnGCodeTextChanged;
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