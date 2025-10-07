using CentroidAPI;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using HavenCNCServer.Services;

namespace HavenCNCServer
{
    /// <summary>
    /// G-Code Test Dialog for testing Centroid API G-code execution capabilities
    /// </summary>
    public partial class GCodeTestDialog : Form
    {
        private CNCPipe? _cncPipe;
        private bool _isConnected = false;
        private string _currentFilePath = string.Empty;
        private readonly object _logLock = new object();
        private readonly MainForm? _mainForm;

        /// <summary>
        /// Initializes a new instance of the GCodeTestDialog
        /// </summary>
        public GCodeTestDialog(MainForm? mainForm = null)
        {
            InitializeComponent();
            _mainForm = mainForm;
            
            // Set up initial status
            LogStatus("G-Code Test Dialog initialized.");
            LogStatus("Note: Centroid API connection will be attempted when running G-code or testing connection.");
            LogStatus("Ready for G-code editing and file operations.");
        }

        /// <summary>
        /// Initialize connection to Centroid CNC12 via CentroidAPI
        /// </summary>
        private void InitializeCentroidAPI()
        {
            try
            {
                LogStatus("Initializing Centroid API connection...");
                LogToMainWindow("G-Code Test: Initializing Centroid API connection...");
                
                // Reset connection state
                _isConnected = false;
                _cncPipe = null;
                
                // Attempt to create CNCPipe with retry logic from settings
                int maxRetries = SettingsManager.Settings.Cnc.ConnectionRetries;
                int retryDelay = SettingsManager.Settings.Cnc.RetryDelayMs;
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        LogStatus($"Creating CNCPipe instance (attempt {attempt}/{maxRetries})...");
                        LogToMainWindow($"G-Code Test: Creating CNCPipe instance (attempt {attempt}/{maxRetries})...");
                        
                        // Create new CNCPipe instance - this is where the constructor either succeeds or fails
                        _cncPipe = new CNCPipe();
                        
                        // Check if construction was successful
                        if (_cncPipe.IsConstructed())
                        {
                            _isConnected = true;
                            LogStatus("✓ CNCPipe construction completed successfully!");
                            LogToMainWindow("G-Code Test: ✓ CNCPipe construction completed successfully!");
                            
                            // Test basic API functionality
                            TestAPIConnection();
                            return; // Success - exit the retry loop
                        }
                        else
                        {
                            LogStatus($"✗ CNCPipe construction failed (attempt {attempt}/{maxRetries})");
                            LogToMainWindow($"G-Code Test: ✗ CNCPipe construction failed (attempt {attempt}/{maxRetries})");
                            
                            // Clean up failed instance
                            _cncPipe = null;
                            
                            if (attempt < maxRetries)
                            {
                                LogStatus($"  Waiting {retryDelay}ms before retry...");
                                Thread.Sleep(retryDelay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"✗ Exception during CNCPipe creation (attempt {attempt}/{maxRetries}): {ex.Message}");
                        LogToMainWindow($"G-Code Test: ✗ Exception during CNCPipe creation (attempt {attempt}/{maxRetries}): {ex.Message}");
                        
                        // Clean up failed instance
                        _cncPipe = null;
                        
                        if (attempt < maxRetries)
                        {
                            LogStatus($"  Waiting {retryDelay}ms before retry...");
                            Thread.Sleep(retryDelay);
                        }
                        else
                        {
                            // Re-throw on final attempt
                            throw;
                        }
                    }
                }
                
                // If we get here, all attempts failed
                _isConnected = false;
                LogStatus("✗ All CNCPipe connection attempts failed");
                LogStatus("  Possible causes:");
                LogStatus("  • CNC12 software is not running");
                LogStatus("  • CentroidAPI.dll version mismatch");
                LogStatus("  • Hardware/driver communication issues");
                LogStatus("  • Insufficient permissions");
                LogToMainWindow("G-Code Test: ✗ All CNCPipe connection attempts failed");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _cncPipe = null;
                var errorMsg = $"✗ Error initializing Centroid API: {ex.Message}";
                LogStatus(errorMsg);
                LogStatus("  Ensure CentroidAPI.dll is available and CNC12 is running.");
                LogToMainWindow($"G-Code Test: {errorMsg}");
            }
        }

        /// <summary>
        /// Test basic API connection and functionality
        /// </summary>
        private void TestAPIConnection()
        {
            if (_cncPipe == null || !_isConnected) return;

            try
            {
                LogStatus("Testing API functionality...");
                LogToMainWindow("G-Code Test: Testing API functionality...");
                
                // Test parameter reading capability
                var result = _cncPipe.parameter.GetMachineParameterValue(1, out double param1);
                if (result == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogStatus($"✓ API parameter test successful (Parameter 1: {param1})");
                    LogToMainWindow($"G-Code Test: ✓ API parameter test successful (Parameter 1: {param1})");
                }
                else
                {
                    LogStatus($"⚠ API parameter test returned: {result}");
                    LogToMainWindow($"G-Code Test: ⚠ API parameter test returned: {result}");
                }

                // Test system information
                _cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
                LogStatus($"✓ CNC System Type: {version}");
                LogToMainWindow($"G-Code Test: ✓ CNC System Type: {version}");

                // Test additional parameters for more detailed diagnostics
                var param34Result = _cncPipe.parameter.GetMachineParameterValue(34, out double spindleEncoderCounts);
                if (param34Result == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogStatus($"✓ Spindle encoder counts (P34): {spindleEncoderCounts}");
                    LogToMainWindow($"G-Code Test: ✓ Spindle encoder counts (P34): {spindleEncoderCounts}");
                }

                LogStatus("✓ API connection test completed successfully");
                LogToMainWindow("G-Code Test: ✓ API connection test completed successfully");

            }
            catch (Exception ex)
            {
                var errorMsg = $"⚠ API test error: {ex.Message}";
                LogStatus(errorMsg);
                LogToMainWindow($"G-Code Test: {errorMsg}");
            }
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
        /// Log messages to the main window for centralized logging
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogToMainWindow(string message)
        {
            try
            {
                _mainForm?.LogMessage(message);
            }
            catch (Exception ex)
            {
                // Fallback to local logging if main window is not available
                LogStatus($"Failed to log to main window: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new G-code file
        /// </summary>
        private void btnNewFile_Click(object sender, EventArgs e)
        {
            txtGCode.Clear();
            txtGCode.Text = "G00 X0 Y0 Z1\r\nG01 Z-0.1 F100\r\nG01 X10 Y10 F500\r\nG01 X0 Y10\r\nG01 X0 Y0\r\nG00 Z1\r\nM30";
            _currentFilePath = string.Empty;
            txtFileName.Text = "test_gcode.txt";
            LogStatus("New G-code file created with sample content.");
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
                    
                    LogStatus($"✓ Opened file: {_currentFilePath}");
                    LogStatus($"  Lines loaded: {txtGCode.Lines.Length}");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"✗ Error opening file: {ex.Message}");
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
                LogStatus($"✓ Saved G-code to: {_currentFilePath}");
                LogStatus($"  Lines saved: {txtGCode.Lines.Length}");
            }
            catch (Exception ex)
            {
                LogStatus($"✗ Error saving file: {ex.Message}");
                MessageBox.Show($"Error saving file: {ex.Message}", "File Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Run G-code through Centroid API
        /// </summary>
        private void btnRunGCode_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable the run button to prevent multiple concurrent executions
                btnRunGCode.Enabled = false;
                
                // Initialize API if not already connected
                if (!_isConnected)
                {
                    LogStatus("Initializing Centroid API connection...");
                    LogToMainWindow("G-Code Test: Initializing Centroid API connection...");
                    
                    InitializeCentroidAPI();
                    
                    if (!_isConnected)
                    {
                        LogStatus("✗ Cannot proceed: Centroid API connection failed");
                        LogToMainWindow("G-Code Test: ✗ Cannot proceed: Centroid API connection failed");
                        MessageBox.Show("Centroid API connection failed. Please ensure CNC12 is running and try again.", 
                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                LogStatus("Starting G-code execution...");
                LogToMainWindow("G-Code Test: Starting G-code execution...");
                
                // Create a unique temporary file to avoid conflicts using settings
                var guid = Guid.NewGuid();
                string tempFileName = $"gcode_test_{DateTime.Now:yyyyMMdd_HHmmss}_{guid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string tempFilePath = Path.Combine(SettingsManager.Settings.Files.TempFilesDirectory, tempFileName);
                
                // Ensure temp directory exists
                Directory.CreateDirectory(SettingsManager.Settings.Files.TempFilesDirectory);
                
                // Write G-code content directly to avoid file locking
                File.WriteAllText(tempFilePath, txtGCode.Text);
                LogStatus($"✓ G-code saved to temporary file: {Path.GetFileName(tempFilePath)}");
                LogToMainWindow($"G-Code Test: ✓ G-code saved to temporary file: {Path.GetFileName(tempFilePath)}");

                // Load the G-code file into CNC12 via API
                LogStatus($"Loading G-code file into CNC12...");
                
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
                
                LogStatus($"✓ G-code file written to CNC12 programs directory");
                LogToMainWindow($"G-Code Test: ✓ G-code file written to CNC12 programs directory");
                
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
                        LogStatus($"⚠ Could not delete temporary file: {cleanupEx.Message}");
                        // Not critical - continue execution
                    }
                }

                // Execute the G-code program
                LogStatus("Starting G-code program execution...");
                
                if (_cncPipe == null || !_isConnected)
                {
                    LogStatus("✗ Cannot execute: No CNC connection");
                    return;
                }

                // Load the program into CNC12 using the Job.Load method
                var loadResult = _cncPipe.job.Load(_currentFilePath);
                
                if (loadResult == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogStatus("✓ Program loaded successfully into CNC12");
                    LogToMainWindow("G-Code Test: ✓ Program loaded successfully into CNC12");
                    
                    LogStatus("✓ Program ready for execution");
                    LogToMainWindow("G-Code Test: ✓ Program ready for execution");

                    // *** WANT TO RUN IT HERE ***
                    // LogStatus("  To run the program:");
                    // LogStatus("  1. Go to CNC12 main interface");
                    // LogStatus("  2. Press the Cycle Start button");
                    // LogStatus("  3. Monitor execution progress in CNC12");
                    LogStatus($"  Loaded program: {Path.GetFileName(_currentFilePath)}");
                }
                else
                {
                    LogStatus($"✗ Failed to load program: {loadResult}");
                    LogToMainWindow($"G-Code Test: ✗ Failed to load program: {loadResult}");
                    LogStatus("  Check that the G-code file is valid and CNC12 is ready");
                    
                    // Log detailed information for debugging on failure
                    LogStatus($"  Failed file path: {_currentFilePath}");
                    LogStatus($"  File exists: {File.Exists(_currentFilePath)}");
                    if (File.Exists(_currentFilePath))
                    {
                        try
                        {
                            string[] debugGCodeLines = File.ReadAllLines(_currentFilePath);
                            LogStatus($"  File contents ({debugGCodeLines.Length} lines):");
                            LogStatus("  --- G-code Start ---");
                            for (int i = 0; i < Math.Min(debugGCodeLines.Length, 20); i++) // Show max 20 lines
                            {
                                LogStatus($"  {i + 1:D3}: {debugGCodeLines[i].TrimEnd('\r')}");
                            }
                            if (debugGCodeLines.Length > 20)
                            {
                                LogStatus($"  ... ({debugGCodeLines.Length - 20} more lines)");
                            }
                            LogStatus("  --- G-code End ---");
                        }
                        catch (Exception readEx)
                        {
                            LogStatus($"  Could not read file for debugging: {readEx.Message}");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                var errorMsg = $"✗ Error running G-code: {ex.Message}";
                LogStatus(errorMsg);
                LogToMainWindow($"G-Code Test: {errorMsg}");
                MessageBox.Show($"Error running G-code: {ex.Message}", "Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable the run button
                btnRunGCode.Enabled = true;
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
                
                LogStatus("Testing Centroid API connection...");
                LogToMainWindow("G-Code Test: User requested connection test");
                
                // Reset connection state
                _isConnected = false;
                _cncPipe = null;
                
                InitializeCentroidAPI();
                
                if (_isConnected)
                {
                    LogStatus("✓ Connection test completed successfully!");
                    LogToMainWindow("G-Code Test: ✓ Connection test completed successfully!");
                    MessageBox.Show("Centroid API connection successful!\n\nThe API is ready for G-code execution.", 
                        "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogStatus("✗ Connection test failed");
                    LogToMainWindow("G-Code Test: ✗ Connection test failed");
                    MessageBox.Show("Centroid API connection failed.\n\nPossible causes:\n• CNC12 is not running\n• CentroidAPI.dll is not accessible\n• Hardware/driver issues\n• Network connectivity problems", 
                        "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"✗ Connection test error: {ex.Message}";
                LogStatus(errorMsg);
                LogToMainWindow($"G-Code Test: {errorMsg}");
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
        /// Clean up resources when dialog is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_cncPipe != null)
                {
                    LogStatus("Closing Centroid API connection...");
                    // Clean up CNCPipe if needed
                    _cncPipe = null;
                }
            }
            catch (Exception ex)
            {
                LogStatus($"Error during cleanup: {ex.Message}");
            }

            base.OnFormClosing(e);
        }
    }
}