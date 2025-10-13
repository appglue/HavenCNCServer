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
        private string _currentFilePath = string.Empty;
        private readonly object _logLock = new object();
        private readonly MainForm? _mainForm;
        private readonly ICNCProgramService _programService;

        /// <summary>
        /// Initializes a new instance of the GCodeTestDialog
        /// </summary>
        public GCodeTestDialog(MainForm? mainForm = null)
        {
            InitializeComponent();
            _mainForm = mainForm;
            _programService = new CNCProgramService();
            
            // Set up logging for this dialog
            SetupDialogLogging();
            
            // Subscribe to connection status changes
            CNCConnectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Subscribe to G-code text changes to enable/disable single command button
            txtGCode.TextChanged += OnGCodeTextChanged;
            
            // Set up initial status
            LogInfo("G-Code Test Dialog initialized.", "G-Code");
            LogInfo("Note: Centroid API connection will be attempted when running G-code or testing connection.", "G-Code");
            LogInfo("Ready for G-code editing and file operations.", "G-Code");
            
            // Display current connection status
            UpdateConnectionStatus();
            
            // Update button states initially
            UpdateSingleCommandButtonState();
            UpdateListenerButtonState();
            
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
        /// Set up logging for this dialog
        /// </summary>
        private void SetupDialogLogging()
        {
            // Find the status text box (assuming it exists in the dialog)
            var statusTextBox = this.Controls.Find("txtStatus", true);
            if (statusTextBox.Length > 0 && statusTextBox[0] is TextBox txtStatus)
            {
                // Create and register a log target for the dialog's status text box
                var logTarget = new TextBoxLogTarget(txtStatus, this);
                LoggingService.AddTarget(logTarget);
            }
        }

        /// <summary>
        /// Handle connection status changes from the connection manager
        /// </summary>
        private void OnConnectionStatusChanged(bool connected, string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnConnectionStatusChanged(connected, message));
                return;
            }

            if (connected)
            {
                LogSuccess(message, "CNC");
                
                // Auto-start disabled for debugging
                /*
                Task.Run(() =>
                {
                    try
                    {
                        // Small delay to ensure connection is fully established
                        Thread.Sleep(1000);
                        CNCJobInfoListener.AutoStartIfConnected();
                        
                        // Update the button status on UI thread
                        if (!IsDisposed && !Disposing)
                        {
                            Invoke(() => UpdateListenerButtonState());
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error auto-starting listener: {ex.Message}", "JobInfo");
                    }
                });
                */
            }
            else
            {
                LogWarning(message, "CNC");
                UpdateListenerButtonState();
            }
        }

        /// <summary>
        /// Update the display with current connection status
        /// </summary>
        private void UpdateConnectionStatus()
        {
            var status = CNCConnectionManager.GetStatus();
            
            if (status.IsConnected)
            {
                LogSuccess("CNC is connected and ready", "CNC");
                var systemInfo = CNCConnectionManager.GetSystemInfo();
                if (systemInfo != null)
                {
                    LogInfo($"System Type: {systemInfo.SystemType}", "CNC");
                    if (systemInfo.Parameter1Value.HasValue)
                    {
                        LogInfo($"Parameter 1: {systemInfo.Parameter1Value.Value}", "CNC");
                    }
                }
            }
            else if (status.IsConnecting)
            {
                LogInfo("CNC connection in progress...", "CNC");
            }
            else
            {
                LogWarning("CNC not connected", "CNC");
                if (status.LastConnectionAttempt != DateTime.MinValue)
                {
                    LogInfo($"Last attempt: {status.LastConnectionAttempt:HH:mm:ss}", "CNC");
                }
            }
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
        /// Initialize connection to Centroid CNC12 via CNCConnectionManager
        /// </summary>
        private bool InitializeCentroidAPI()
        {
            try
            {
                // Use the connection manager to get or create a connection
                var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                
                if (pipe != null)
                {
                    // Get and display system info (only on first successful connection)
                    var systemInfo = CNCConnectionManager.GetSystemInfo();
                    if (systemInfo != null)
                    {
                        LogSuccess($"CNC connected: {systemInfo.SystemType}", "API");
                    }
                    else
                    {
                        LogSuccess("CNC connection successful", "API");
                    }
                    
                    return true;
                }
                else
                {
                    LogError("CNC connection failed", "API");
                    
                    var status = CNCConnectionManager.GetStatus();
                    LogInfo("Possible causes:", "API");
                    LogInfo("• CNC12 software is not running", "API");
                    LogInfo("• CentroidAPI.dll version mismatch", "API");
                    LogInfo("• Hardware/driver communication issues", "API");
                    LogInfo("• Insufficient permissions", "API");
                    LogInfo($"Connection timeout: {status.ConnectionSettings.TimeoutMs}ms", "API");
                    LogInfo($"Retry attempts: {status.ConnectionSettings.Retries}", "API");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error initializing Centroid API: {ex.Message}";
                LogError(errorMsg, "API");
                LogInfo("Ensure CentroidAPI.dll is available and CNC12 is running.", "API");
                return false;
            }
        }

        /// <summary>
        /// Test basic API connection and functionality (now handled by connection manager)
        /// </summary>
        private void TestAPIConnection()
        {
            // This method is now handled by the CNCConnectionManager
            // The connection manager automatically tests the connection
            UpdateConnectionStatus();
        }

        /// <summary>
        /// Log status messages to the status text box with timestamp
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogStatus(string message)
        {
            lock (_logLock)
            {
                if (InvokeRequired)
                {
                    Invoke(() => LogStatus(message));
                    return;
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                txtStatus.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                txtStatus.SelectionStart = txtStatus.Text.Length;
                txtStatus.ScrollToCaret();
            }
        }

        /// <summary>
        /// Create a new G-code file
        /// </summary>
        private void btnNewFile_Click(object sender, EventArgs e)
        {
            txtGCode.Clear();
            txtGCode.Text = "G00 X1 Y1 Z-1\r\nG00 X0 Y0 Z-2\r\nM30";
            _currentFilePath = string.Empty;
            txtFileName.Text = "test_gcode.txt";
            LogSuccess("New G-code file created with sample content.", "File");
            UpdateSingleCommandButtonState(); // Update button state after content change
        }

        /// <summary>
        /// Open an existing G-code file
        /// </summary>
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _currentFilePath = openFileDialog.FileName;
                    txtFileName.Text = Path.GetFileName(_currentFilePath);
                    
                    string content = File.ReadAllText(_currentFilePath);
                    txtGCode.Text = content;
                    
                    LogSuccess($"Opened file: {_currentFilePath}", "File");
                    LogInfo($"Lines loaded: {txtGCode.Lines.Length}", "File");
                    UpdateSingleCommandButtonState(); // Update button state after content change
                }
            }
            catch (Exception ex)
            {
                LogError($"Error opening file: {ex.Message}", "File");
                MessageBox.Show($"Error opening file: {ex.Message}", "File Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load file using legacy button (compatibility)
        /// </summary>
        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            btnOpenFile_Click(sender, e);
        }

        /// <summary>
        /// Save G-code to file
        /// </summary>
        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    // Use Save As dialog
                    saveFileDialog.FileName = txtFileName.Text;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        _currentFilePath = saveFileDialog.FileName;
                        txtFileName.Text = Path.GetFileName(_currentFilePath);
                    }
                    else
                    {
                        return; // User canceled
                    }
                }

                File.WriteAllText(_currentFilePath, txtGCode.Text);
                LogSuccess($"Saved G-code to: {_currentFilePath}", "File");
                LogInfo($"Lines saved: {txtGCode.Lines.Length}", "File");
            }
            catch (Exception ex)
            {
                LogError($"Error saving file: {ex.Message}", "File");
                MessageBox.Show($"Error saving file: {ex.Message}", "File Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Run G-code through Centroid API
        /// </summary>
        private void btnRunGCode_Click(object sender, EventArgs e)
        {
            bool wasListening = false; // Track listener state for restart
            
            try
            {
                // Disable the run button to prevent multiple concurrent executions
                btnRunGCode.Enabled = false;
                
                // Initialize API if not already connected
                if (!CNCConnectionManager.IsConnected)
                {
                    LogInfo("Initializing Centroid API connection...", "G-Code");
                    
                    var connected = InitializeCentroidAPI();
                    
                    if (!connected)
                    {
                        LogError("Cannot proceed: Centroid API connection failed", "G-Code");
                        MessageBox.Show("Centroid API connection failed. Please ensure CNC12 is running and try again.", 
                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                LogInfo("Starting G-code execution...", "G-Code");
                
                // Create a unique temporary file to avoid conflicts using settings
                var guid = Guid.NewGuid();
                string tempFileName = $"gcode_test_{DateTime.Now:yyyyMMdd_HHmmss}_{guid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string tempFilePath = Path.Combine(SettingsManager.Settings.Files.TempFilesDirectory, tempFileName);
                
                // Ensure temp directory exists
                Directory.CreateDirectory(SettingsManager.Settings.Files.TempFilesDirectory);
                
                // Write G-code content directly to avoid file locking
                File.WriteAllText(tempFilePath, txtGCode.Text);
                LogSuccess($"G-code saved to temporary file: {Path.GetFileName(tempFilePath)}", "G-Code");

                // Load the G-code file into CNC12 via API
                LogInfo($"Loading G-code file into CNC12...", "G-Code");
                
                // Read the G-code content
                string[] gCodeLines = File.ReadAllLines(tempFilePath);
                
                // Get CNC programs directory from settings
                string cncProgramsPath = SettingsManager.GetCncProgramsDirectory();
                
                // Create a unique filename to avoid conflicts
                var targetGuid = Guid.NewGuid();
                string uniqueFileName = $"gcode_test_{DateTime.Now:yyyyMMdd_HHmmss}_{targetGuid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string targetPath = Path.Combine(cncProgramsPath, uniqueFileName);
                
                // Ensure target directory exists
                Directory.CreateDirectory(cncProgramsPath);
                
                // Use File.WriteAllLines instead of File.Copy to avoid file locking issues
                File.WriteAllLines(targetPath, gCodeLines);
                
                LogSuccess($"G-code file written to CNC12 programs directory", "G-Code");
                
                // Store the target path for execution
                _currentFilePath = targetPath;
                
                // Clean up the temporary file if it's different from target
                if (!string.Equals(tempFilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        LogWarning($"Could not delete temporary file: {cleanupEx.Message}", "G-Code");
                        // Not critical - continue execution
                    }
                }

                // Execute the G-code program using G65 command
                LogInfo("Executing G-code program using G65 command...", "G-Code");
                
                // Note: Listener continues running during G-code execution for job monitoring
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot execute: No CNC connection", "G-Code");
                    
                    // Restart listener if it was running
                    if (wasListening)
                    {
                        Task.Run(() => CNCJobInfoListener.AutoStartIfConnected());
                    }
                    return;
                }

                // Use G65 command to run the G-code file directly
                // Format: G65 "path/to/file" where the path is the full path to the program to run
                string g65Command = $"G65 \"{_currentFilePath}\"";
                LogInfo($"Sending command: {g65Command}", "G-Code");
                LogInfo($"Working directory: C:\\cncfiles", "G-Code");
                LogInfo($"File path: {_currentFilePath}", "G-Code");
                
                // Execute the G65 command using a new Job instance with full path in command
                var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = cmd.RunCommand(g65Command,  false);
                
                if (executeResult == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogSuccess("G65 command executed successfully", "G-Code");
                    LogSuccess("G-code program is now running in CNC12", "G-Code");
                    LogInfo($"Running program: {Path.GetFileName(_currentFilePath)}", "G-Code");
                    LogInfo("Monitor progress in CNC12 interface", "G-Code");
                }
                else
                {
                    LogError($"Failed to execute G65 command: {executeResult}", "G-Code");
                    LogInfo("Check that the G-code file is valid and CNC12 is ready", "G-Code");
                    
                    // Log detailed information for debugging on failure
                    LogDebug($"Failed file path: {_currentFilePath}", "G-Code");
                    LogDebug($"File exists: {File.Exists(_currentFilePath)}", "G-Code");
                    LogDebug($"G65 command: {g65Command}", "G-Code");
                    if (File.Exists(_currentFilePath))
                    {
                        try
                        {
                            string[] debugGCodeLines = File.ReadAllLines(_currentFilePath);
                            LogDebug($"File contents ({debugGCodeLines.Length} lines):", "G-Code");
                            LogDebug("--- G-code Start ---", "G-Code");
                            for (int i = 0; i < Math.Min(debugGCodeLines.Length, 10); i++) // Show max 10 lines for G65 debug
                            {
                                LogDebug($"{i + 1:D3}: {debugGCodeLines[i].TrimEnd('\r')}", "G-Code");
                            }
                            if (debugGCodeLines.Length > 10)
                            {
                                LogDebug($"... ({debugGCodeLines.Length - 10} more lines)", "G-Code");
                            }
                            LogDebug("--- G-code End ---", "G-Code");
                        }
                        catch (Exception readEx)
                        {
                            LogError($"Could not read file for debugging: {readEx.Message}", "G-Code");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogError($"Error running G-code: {ex.Message}", "G-Code");
                MessageBox.Show($"Error running G-code: {ex.Message}", "Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable the run button
                btnRunGCode.Enabled = true;
                
                // Restart the listener if it was running before execution
                if (wasListening)
                {
                    Task.Run(() => 
                    {
                        // Small delay to ensure command execution is fully complete
                        Thread.Sleep(1000);
                        CNCJobInfoListener.AutoStartIfConnected();
                        
                        if (!IsDisposed && !Disposing)
                        {
                            Invoke(() => 
                            {
                                LogInfo("Restarted JOB_INFO listener after G-code execution", "JobInfo");
                                UpdateListenerButtonState();
                            });
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Test connection to Centroid API without running G-code
        /// </summary>
        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                btnTestConnection.Enabled = false;
                btnTestConnection.Text = "Testing...";
                
                LogInfo("Testing Centroid API connection...", "Test");
                
                // Reset connection state through the manager
                CNCConnectionManager.Reset();
                
                var connected = InitializeCentroidAPI();
                
                if (connected)
                {
                    LogSuccess("Connection test completed successfully!", "Test");
                    MessageBox.Show("Centroid API connection successful!\n\nThe API is ready for G-code execution.", 
                        "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogError("Connection test failed", "Test");
                    MessageBox.Show("Centroid API connection failed.\n\nPossible causes:\n• CNC12 is not running\n• CentroidAPI.dll is not accessible\n• Hardware/driver issues\n• Network connectivity problems", 
                        "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"✗ Connection test error: {ex.Message}";
                LogError(errorMsg, "Test");
                MessageBox.Show($"Connection test failed with error:\n{ex.Message}\n\nThis may indicate a serious issue with the CentroidAPI or underlying system.", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestConnection.Enabled = true;
                btnTestConnection.Text = "Test Connection";
            }
        }

        /// <summary>
        /// Toggle JOB_INFO listener for debugging and monitoring
        /// </summary>
        private async void btnJobInfoListener_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable button during operation
                btnJobInfoListener.Enabled = false;
                
                // Toggle listener state
                if (CNCJobInfoListener.IsListening)
                {
                    // Stop listener in background to avoid UI blocking
                    await Task.Run(() => 
                    {
                        CNCJobInfoListener.StopListening();
                        LogInfo("JOB_INFO listener stopped", "JobInfo");
                    });
                    
                    // Update button state
                    btnJobInfoListener.Text = "Start Listener";
                    btnJobInfoListener.BackColor = Color.LightYellow;
                }
                else
                {
                    // Start listener in background to avoid UI blocking
                    bool started = await Task.Run(() => 
                    {
                        if (CNCConnectionManager.IsConnected)
                        {
                            var result = CNCJobInfoListener.StartListening();
                            if (result)
                            {
                                LogSuccess("JOB_INFO listener started", "JobInfo");
                            }
                            return result;
                        }
                        else
                        {
                            LogWarning("Cannot start listener: CNC not connected", "JobInfo");
                            return false;
                        }
                    });
                    
                    // Update button state based on result
                    if (started)
                    {
                        btnJobInfoListener.Text = "Stop Listener";
                        btnJobInfoListener.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        btnJobInfoListener.Text = "Start Listener";
                        btnJobInfoListener.BackColor = Color.LightCoral;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error stopping listener: {ex.Message}", "JobInfo");
                MessageBox.Show($"Error with JOB_INFO listener: {ex.Message}", 
                    "Listener Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Run a single G-code command directly without creating files
        /// </summary>
        private void btnRunSingleCommand_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if G-code content is a single command
                var gCodeText = txtGCode.Text.Trim();
                if (string.IsNullOrWhiteSpace(gCodeText))
                {
                    LogWarning("No G-code command to execute", "Single Command");
                    MessageBox.Show("Please enter a G-code command to execute.", "No Command", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Split into lines and filter out empty lines and comments
                var lines = gCodeText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line.Trim()) && 
                                   !line.Trim().StartsWith(";") && 
                                   !line.Trim().StartsWith("("))
                    .Select(line => line.Trim())
                    .ToList();

                if (lines.Count == 0)
                {
                    LogWarning("No valid G-code commands found (only comments or empty lines)", "Single Command");
                    MessageBox.Show("No valid G-code commands found. Only comments or empty lines detected.", 
                        "No Valid Commands", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (lines.Count > 1)
                {
                    LogWarning($"Multiple commands detected ({lines.Count} lines). Use 'Run G-Code' for multi-line programs.", "Single Command");
                    MessageBox.Show($"Multiple commands detected ({lines.Count} lines).\n\nFor multi-line programs, use the 'Run G-Code' button instead.\n\nFor single command execution, please enter only one G-code line.", 
                        "Multiple Commands", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // We have exactly one command
                string command = lines[0];
                
                // Initialize API if not already connected
                if (!CNCConnectionManager.IsConnected)
                {
                    var connected = InitializeCentroidAPI();
                    if (!connected)
                    {
                        LogError("CNC connection failed", "Single Command");
                        MessageBox.Show("Centroid API connection failed. Please ensure CNC12 is running and try again.", 
                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot execute: No CNC connection", "Single Command");
                    return;
                }

                // Disable the button during execution
                btnRunSingleCommand.Enabled = false;
                btnRunSingleCommand.Text = "Executing...";

                // Execute the single command in background to avoid UI blocking
                _ = Task.Run(() =>
                {
                    try
                    {
                        // Execute the single command using a new Job instance
                        var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                        var executeResult = cmd.RunCommand(command, false);
                        
                        // Update UI on main thread
                        if (!IsDisposed && !Disposing)
                        {
                            Invoke(() =>
                            {
                                if (executeResult == CNCPipe.ReturnCode.SUCCESS)
                                {
                                    LogSuccess($"Command executed: {command}", "Single Command");
                                }
                                else
                                {
                                    LogError($"Command failed: {command} ({executeResult})", "Single Command");
                                    MessageBox.Show($"Failed to execute G-code command:\n\n{command}\n\nError: {executeResult}", 
                                        "Command Execution Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                
                                // Re-enable the button
                                btnRunSingleCommand.Enabled = true;
                                btnRunSingleCommand.Text = "Run Single Command";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!IsDisposed && !Disposing)
                        {
                            Invoke(() =>
                            {
                                LogError($"Error executing command: {ex.Message}", "Single Command");
                                MessageBox.Show($"Error executing command: {ex.Message}", "Execution Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                
                                // Re-enable the button
                                btnRunSingleCommand.Enabled = true;
                                btnRunSingleCommand.Text = "Run Single Command";
                            });
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                LogError($"Error setting up single command execution: {ex.Message}", "Single Command");
                MessageBox.Show($"Error setting up command execution: {ex.Message}", "Setup Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Re-enable the button on setup error
                btnRunSingleCommand.Enabled = true;
                btnRunSingleCommand.Text = "Run Single Command";
            }
        }

        /// <summary>
        /// Example method demonstrating Job class creation and usage
        /// Execute a movement command using a new Job instance
        /// </summary>
        private CNCPipe.ReturnCode ExecuteMovementWithNewJob(double xDistance, double feedRate)
        {
            try
            {
                // Get the current CNC pipe
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot execute movement: No CNC connection", "Job");
                    return CNCPipe.ReturnCode.STATUS_UNKNOWN;
                }

                // Create a new Job instance using the CNC pipe
                var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                
                // Build the G-code command
                string gCodeCommand = $"G1 X{xDistance} F{feedRate}";
                LogInfo($"Executing movement command with new Job: {gCodeCommand}", "Job");
                
                // Get the working directory (you can adjust this path as needed)
                string workingDirectory = Path.GetDirectoryName(_currentFilePath) ?? @"C:\cncfiles";
                
                // Execute the command using the new Job instance
                var result = cmd.RunCommand(gCodeCommand, workingDirectory, false);
                
                if (result == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogSuccess($"Movement command executed successfully: {gCodeCommand}", "Job");
                }
                else
                {
                    LogError($"Movement command failed: {gCodeCommand} (Result: {result})", "Job");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error executing movement with new Job: {ex.Message}", "Job");
                return CNCPipe.ReturnCode.STATUS_UNKNOWN;
            }
        }

        /// <summary>
        /// Test method to demonstrate the new Job creation pattern
        /// This can be called from a button or other UI event
        /// </summary>
        private void TestNewJobCreation()
        {
            try
            {
                if (!CNCConnectionManager.IsConnected)
                {
                    LogError("Cannot test Job creation: CNC not connected", "Job");
                    return;
                }

                LogInfo("Testing new Job creation pattern...", "Job");
                
                // Example: Move X axis 10 units at 100 feed rate
                var result = ExecuteMovementWithNewJob(10.0, 100.0);
                
                if (result == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogSuccess("New Job creation test completed successfully", "Job");
                }
                else
                {
                    LogWarning($"New Job creation test completed with result: {result}", "Job");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in new Job creation test: {ex.Message}", "Job");
            }
        }

        /// <summary>
        /// Update the Job Info Listener button state based on connection and listening status
        /// </summary>
        private void UpdateListenerButtonState()
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateListenerButtonState());
                return;
            }

            try
            {
                bool isListening = CNCJobInfoListener.IsListening;
                bool isConnected = CNCConnectionManager.IsConnected;

                if (!isConnected)
                {
                    btnJobInfoListener.Text = "CNC Not Connected";
                    btnJobInfoListener.BackColor = Color.LightGray;
                    btnJobInfoListener.Enabled = false;
                }
                else if (isListening)
                {
                    btnJobInfoListener.Text = "Stop Listener";
                    btnJobInfoListener.BackColor = Color.LightGreen;
                    btnJobInfoListener.Enabled = true;
                }
                else
                {
                    btnJobInfoListener.Text = "Start Listener";
                    btnJobInfoListener.BackColor = Color.LightYellow;
                    btnJobInfoListener.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating listener button state: {ex.Message}", "UI");
                btnJobInfoListener.Text = "Status Error";
                btnJobInfoListener.BackColor = Color.LightPink;
            }
        }

        /// <summary>
        /// Clean up resources when dialog is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Unsubscribe from connection status changes
                CNCConnectionManager.ConnectionStatusChanged -= OnConnectionStatusChanged;
                
                // Unsubscribe from text change events
                txtGCode.TextChanged -= OnGCodeTextChanged;
                
                // Stop JOB_INFO listener if running
                if (CNCJobInfoListener.IsListening)
                {
                    CNCJobInfoListener.StopListening();
                    LogInfo("Stopped JOB_INFO listener on dialog close", "JobInfo");
                }
                
                LogInfo("Closing G-Code Test Dialog...", "System");
                // Note: We don't disconnect the CNC here since other parts of the application might be using it
                // The CNCConnectionManager will handle connection lifecycle
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup: {ex.Message}", "System");
            }

            base.OnFormClosing(e);
        }
    }
}