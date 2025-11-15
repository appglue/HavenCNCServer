using CentroidAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Components;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// Main Windows Forms application that hosts the ASP.NET Core Web API server
    /// </summary>
    public partial class MainForm : Form
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _startupComplete = false;
        private readonly object _startupLock = new object();

        // Component instances
        private MessageDisplayComponent? _messageDisplayComponent;
        private GCodeViewerComponent? _gCodeViewerComponent;
        private CoordinateDisplayComponent? _coordinateDisplayComponent;

        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000?url_to_havencnc_server=http://localhost:5000";

        /// <summary>
        /// Initializes a new instance of the MainForm class
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Initialize cancellation token source for coordinated shutdown
            _cancellationTokenSource = new CancellationTokenSource();

            // Set up 50/50 layout for messages and logs
            SetupLayout();

            // Set up centralized logging
            SetupLogging();

            // Initialize UI components
            InitializeUIComponents();

            // Initialize application settings
            try
            {
                SettingsManager.LoadSettings();
                LogSuccess($"Settings loaded from: {SettingsManager.GetSettingsFilePath()}", "Settings");
                LogInfo($"Temp files directory: {SettingsManager.Settings.Files.TempFilesDirectory}", "Settings");
                LogInfo($"CNC programs directory: {SettingsManager.GetCncProgramsDirectory()}", "Settings");

                // Initialize MachinePositionService to listen for DRO events
                MachinePositionService.Initialize();

                // Subscribe to CNC connection status changes
                CNCConnectionManager.ConnectionStatusChanged += OnCNCConnectionStatusChanged;

                // Try auto-connect if enabled
                _ = Task.Run(async () => await CNCConnectionManager.TryAutoConnectAsync());
            }
            catch (Exception ex)
            {
                LogWarning($"Settings initialization failed: {ex.Message}", "Settings");
            }

            // Register this form with the UI control service
            Services.UIControlService.RegisterMainForm(this);

            // Subscribe to status change events
            ApiManager.StatusChanged += OnApiStatusChanged;

            // Start the API server automatically when the form loads
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        /// <summary>
        /// Set up the layout for tabs
        /// </summary>
        private void SetupLayout()
        {
            // Subscribe to resize event to position coordinate display
            this.Resize += MainForm_Resize;

            // Initial layout setup will be done in resize handler
            MainForm_Resize(null, EventArgs.Empty);
        }

        /// <summary>
        /// Handle form resize to position coordinate display
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Keep coordinate display positioned on the right side
            if (_coordinateDisplayComponent != null)
            {
                _coordinateDisplayComponent.Location = new Point(this.ClientSize.Width - 340, 12);
            }
        }

        /// <summary>
        /// Set up the centralized logging system
        /// </summary>
        private void SetupLogging()
        {
            // Create and register a log target for the main form's text box
            var logTarget = new RichTextBoxLogTarget(txtLog, this);
            LoggingService.AddTarget(logTarget);

            // Set maximum log entries from settings or default
            LoggingService.MaxLogEntries = 2000;

            LogInfo("Logging system initialized", "System");
        }

        /// <summary>
        /// Initialize the independent UI components
        /// </summary>
        private void InitializeUIComponents()
        {
            try
            {
                // Create the message display component and add to Messages tab
                _messageDisplayComponent = new MessageDisplayComponent();
                _messageDisplayComponent.Dock = DockStyle.Fill;
                tabMessages.Controls.Add(_messageDisplayComponent);

                // Create the G-code viewer component and add to G-Code tab
                _gCodeViewerComponent = new GCodeViewerComponent();
                _gCodeViewerComponent.Dock = DockStyle.Fill;
                tabGCode.Controls.Add(_gCodeViewerComponent);

                // Create the coordinate display component and position it properly on the right
                _coordinateDisplayComponent = new CoordinateDisplayComponent();
                PositionCoordinateDisplay();

                LogInfo("UI components initialized successfully", "Components");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize UI components: {ex.Message}", "Components");
            }
        }

        /// <summary>
        /// Replace an existing control with a new component
        /// </summary>
        private void ReplaceControl(Control oldControl, Control newControl)
        {
            // Copy position and size properties
            newControl.Location = oldControl.Location;
            newControl.Size = oldControl.Size;
            newControl.Anchor = oldControl.Anchor;
            newControl.Name = oldControl.Name + "_Component";

            // Replace the control
            var parent = oldControl.Parent;
            if (parent != null)
            {
                var index = parent.Controls.GetChildIndex(oldControl);
                parent.Controls.Remove(oldControl);
                parent.Controls.Add(newControl);
                parent.Controls.SetChildIndex(newControl, index);
            }

            // Dispose the old control
            oldControl.Dispose();
        }

        /// <summary>
        /// Position the coordinate display component on the right side of the form
        /// </summary>
        private void PositionCoordinateDisplay()
        {
            if (_coordinateDisplayComponent == null) return;

            // Position coordinate display on the right side, inline with buttons
            _coordinateDisplayComponent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _coordinateDisplayComponent.Location = new Point(this.ClientSize.Width - 340, 12);
            _coordinateDisplayComponent.Name = "coordinateDisplayComponent";

            // Add to the form (not the control panel)
            this.Controls.Add(_coordinateDisplayComponent);
            _coordinateDisplayComponent.BringToFront();
        }

        /// <summary>
        /// Load G-code into the display panel
        /// </summary>
        public void LoadGCodeForDisplay(string[] gcode)
        {
            try
            {
                _gCodeViewerComponent?.LoadGCodeForDisplay(gcode);
                LogInfo($"Loaded {gcode?.Length ?? 0} lines of G-code for display", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error loading G-code for display: {ex.Message}", "GCodeDisplay");
            }
        }

        /// <summary>
        /// Clear the G-code display
        /// </summary>
        public void ClearGCodeDisplay()
        {
            try
            {
                _gCodeViewerComponent?.ClearGCode();
                LogInfo("G-code display cleared", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error clearing G-code display: {ex.Message}", "GCodeDisplay");
            }
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

            // Load settings into UI
            LoadSettingsIntoUI();

            // Copy CNC script files to CNC12 directories
            CopyScriptFilesToCnc12();

            await ApiManager.StartAsync(cancellationToken);

            // Start job listener with background monitoring
            CNCJobInfoListener.Start(cancellationToken);

            // Mark startup as complete
            lock (_startupLock)
            {
                _startupComplete = true;
                LogSuccess("Application startup completed", "System");
            }

            // Set up SignalR event listeners asynchronously after API is fully ready
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for API to be fully initialized
                    await Task.Delay(3000);
                    SignalRManager.SetupEventListeners();
                }
                catch (Exception ex)
                {
                    LogError($"Failed to setup SignalR listeners: {ex.Message}", "SignalR");
                }
            });
        }        /// <summary>
                 /// Handle CNC connection status changes
                 /// </summary>
        private void OnCNCConnectionStatusChanged(bool connected, string message)
        {
            // Use Invoke to ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => OnCNCConnectionStatusChanged(connected, message));
                return;
            }

            if (connected)
            {
                LogSuccess(message, "CNC");
            }
            else
            {
                LogWarning(message, "CNC");
            }
        }

        /// <summary>
        /// Handles API server status changes from ApiManager
        /// </summary>
        private void OnApiStatusChanged(string status, Color color)
        {
            // Use Invoke to ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => OnApiStatusChanged(status, color));
                return;
            }

            // TODO: Update status label when available
            // For now, just log the status change
            LogInfo($"API server status: {status}", "API");
        }

        /// <summary>
        /// Cleanup when form is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Check if startup is complete before allowing shutdown
            lock (_startupLock)
            {
                if (!_startupComplete)
                {
                    LogInfo("Startup in progress - delaying shutdown", "System");
                    e.Cancel = true;

                    // Schedule a retry in 500ms
                    var retryTimer = new System.Windows.Forms.Timer();
                    retryTimer.Interval = 500;
                    retryTimer.Tick += (s, args) =>
                    {
                        retryTimer.Stop();
                        retryTimer.Dispose();
                        this.Close(); // Try closing again
                    };
                    retryTimer.Start();
                    return;
                }
            }

            // Cancel the close to handle it asynchronously
            if (!e.Cancel)
            {
                e.Cancel = true;

                // Run shutdown asynchronously but with better error handling and timeout
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Add a timeout to the entire shutdown process
                        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        try
                        {
                            await PerformShutdownAsync().WaitAsync(shutdownTimeout.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            LogError("⏰ Shutdown process timed out after 10 seconds - forcing exit", "System");
                            Environment.Exit(1);
                            return;
                        }

                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        // Log any errors in the shutdown process
                        try
                        {
                            this.Invoke(() =>
                            {
                                LogError($"❌ Fatal error during shutdown: {ex.Message}", "System");
                                LogInfo("🚪 Forcing exit due to error...", "System");
                            });
                            await Task.Delay(2000); // Give time to see the error
                        }
                        catch
                        {
                            // If even logging fails, just exit
                        }
                        finally
                        {
                            Environment.Exit(1);
                        }
                    }
                });
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Perform the actual shutdown operations asynchronously
        /// </summary>
        private async Task PerformShutdownAsync()
        {
            try
            {
                LogInfo("Application shutdown initiated", "System");

                // Create a separate shutdown cancellation token with timeout
                using var shutdownTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var shutdownToken = shutdownTokenSource.Token;

                // Signal all background operations to stop gracefully
                _cancellationTokenSource?.Cancel();

                // Stop CNC Job Info Listener
                CNCJobInfoListener.Stop(shutdownToken);

                // Clean up UI components
                _messageDisplayComponent?.Dispose();
                _gCodeViewerComponent?.Dispose();
                _coordinateDisplayComponent?.Dispose();

                // Clear all event listeners to prevent callbacks during shutdown
                CNCJobInfoListener.ClearAllListeners();

                // Stop API manager
                try
                {
                    // Create a timeout-protected call
                    using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, timeoutSource.Token);

                    await ApiManager.StopAsync(combinedSource.Token);
                }
                catch (OperationCanceledException)
                {
                    LogWarning("API manager stop timed out after 3 seconds", "System");
                }
                catch (Exception ex)
                {
                    LogError($"Error stopping API manager: {ex.Message}", "System");
                }

                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;

                // Cleanup the CNC connection manager
                CNCConnectionManager.Disconnect();

                // Wait a moment for any final cleanup
                await Task.Delay(200);

                // Force garbage collection to clean up any remaining resources
                GC.Collect();
                GC.WaitForPendingFinalizers();

                LogSuccess("Application shutdown completed successfully", "System");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown cleanup: {ex.Message}", "System");
            }
            finally
            {
                // Dispose cancellation token source
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Toggle "Always on Top" window behavior
        /// </summary>
        private void btnAlwaysOnTop_Click(object sender, EventArgs e)
        {
            SetAlwaysOnTop(!this.TopMost);
        }

        /// <summary>
        /// Set the "Always on Top" state programmatically (can be called from API)
        /// </summary>
        public void SetAlwaysOnTop(bool alwaysOnTop)
        {
            this.TopMost = alwaysOnTop;
            btnAlwaysOnTop.Text = this.TopMost ? "Always on Top: ON" : "Always on Top: OFF";
            btnAlwaysOnTop.BackColor = this.TopMost ? Color.LightGreen : SystemColors.Control;

            // Update any owned forms (child windows) to match the TopMost state
            foreach (Form ownedForm in this.OwnedForms)
            {
                if (!ownedForm.IsDisposed)
                {
                    ownedForm.TopMost = alwaysOnTop;
                }
            }

            LogInfo($"Always on Top: {(this.TopMost ? "Enabled" : "Disabled")}", "UI");
        }

        /// <summary>
        /// Show the browser UI form
        /// </summary>
        private void btnShowUI_Click(object sender, EventArgs e)
        {
            try
            {
                LogInfo("Opening browser UI...", "UI");
                var browserForm = new BrowserForm(ReactAppUrl);

                // Set the browser form width to match MainForm
                browserForm.Width = this.Width;
                browserForm.StartPosition = FormStartPosition.Manual;
                browserForm.Location = new System.Drawing.Point(this.Location.X, this.Location.Y);

                // Set this form as the owner so the browser form stays on top of the main form
                browserForm.Owner = this;

                // If main form is TopMost, make browser form TopMost too
                if (this.TopMost)
                {
                    browserForm.TopMost = true;
                }

                browserForm.Show();
                LogInfo("Browser UI opened successfully", "UI");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open browser UI: {ex.Message}", "UI");
                MessageBox.Show($"Failed to open browser UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenSwagger_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SwaggerUrl,
                    UseShellExecute = true
                });
                LogInfo("Opened Swagger UI in browser", "UI");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open Swagger UI: {ex.Message}", "UI");
                MessageBox.Show($"Failed to open Swagger UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGCodeTest_Click(object sender, EventArgs e)
        {
            try
            {
                LogInfo("Opening G-Code Test Dialog...", "UI");

                using (var gCodeDialog = new GCodeTestDialog(this))
                {
                    // If main form is TopMost, make dialog TopMost too
                    if (this.TopMost)
                    {
                        gCodeDialog.TopMost = true;
                    }

                    gCodeDialog.ShowDialog(this);
                }

                LogInfo("G-Code Test Dialog closed.", "UI");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error opening G-Code Test Dialog: {ex.Message}";
                LogError(errorMessage, "UI");
                MessageBox.Show(errorMessage, "Dialog Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Show dropdown menu with available log files
        /// </summary>
        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                // Clear existing items
                contextMenuLogs.Items.Clear();

                // Get log directories
                var mainLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                var jobListenerLogDir = SettingsManager.Settings.Files.JobListenerLogsDirectory;

                // Make job listener log dir absolute if relative
                if (!Path.IsPathRooted(jobListenerLogDir))
                {
                    jobListenerLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jobListenerLogDir);
                }

                // Create directories if they don't exist
                Directory.CreateDirectory(mainLogDir);
                Directory.CreateDirectory(jobListenerLogDir);

                var hasFiles = false;

                // Add Job Listener logs
                if (Directory.Exists(jobListenerLogDir))
                {
                    var jobListenerFiles = Directory.GetFiles(jobListenerLogDir, "JobListener_*.log")
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(10)
                        .ToArray();

                    if (jobListenerFiles.Length > 0)
                    {
                        var headerItem = new ToolStripMenuItem("📋 Job Listener Logs") { Enabled = false };
                        contextMenuLogs.Items.Add(headerItem);

                        foreach (var logFile in jobListenerFiles)
                        {
                            var fileInfo = new FileInfo(logFile);
                            var fileName = fileInfo.Name;
                            var fileSize = fileInfo.Length / 1024; // KB
                            var lastModified = fileInfo.LastWriteTime;

                            var menuItem = new ToolStripMenuItem($"  {fileName} ({fileSize:N0} KB) - {lastModified:HH:mm:ss}");
                            menuItem.Tag = logFile;
                            menuItem.Click += LogFileMenuItem_Click;
                            contextMenuLogs.Items.Add(menuItem);
                        }

                        hasFiles = true;
                    }
                }

                // Add separator if we have job listener logs
                if (hasFiles)
                {
                    contextMenuLogs.Items.Add(new ToolStripSeparator());
                }

                // Add other log files from temp directory
                if (Directory.Exists(mainLogDir))
                {
                    var otherLogFiles = Directory.GetFiles(mainLogDir, "*.log")
                        .Where(f => !f.Contains("JobListener_"))
                        .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                        .Take(10)
                        .ToArray();

                    if (otherLogFiles.Length > 0)
                    {
                        var headerItem = new ToolStripMenuItem("📄 Other Logs") { Enabled = false };
                        contextMenuLogs.Items.Add(headerItem);

                        foreach (var logFile in otherLogFiles)
                        {
                            var fileInfo = new FileInfo(logFile);
                            var fileName = fileInfo.Name;
                            var fileSize = fileInfo.Length / 1024; // KB
                            var lastModified = fileInfo.LastWriteTime;

                            var menuItem = new ToolStripMenuItem($"  {fileName} ({fileSize:N0} KB) - {lastModified:HH:mm:ss}");
                            menuItem.Tag = logFile;
                            menuItem.Click += LogFileMenuItem_Click;
                            contextMenuLogs.Items.Add(menuItem);
                        }

                        hasFiles = true;
                    }
                }

                // Add "Open Log Folder" option
                if (hasFiles)
                {
                    contextMenuLogs.Items.Add(new ToolStripSeparator());
                }

                var openFolderItem = new ToolStripMenuItem("📁 Open Log Folder");
                openFolderItem.Click += (s, ev) =>
                {
                    try
                    {
                        Process.Start("explorer.exe", jobListenerLogDir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                contextMenuLogs.Items.Add(openFolderItem);

                if (!hasFiles)
                {
                    contextMenuLogs.Items.Add(new ToolStripMenuItem("No log files found") { Enabled = false });
                }

                // Show the menu below the button
                contextMenuLogs.Show(btnViewLogs, new Point(0, btnViewLogs.Height));
            }
            catch (Exception ex)
            {
                LogError($"Error showing log files menu: {ex.Message}", "UI");
                MessageBox.Show($"Failed to show log files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Open a log file in a viewer window
        /// </summary>
        private void LogFileMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string logFilePath)
            {
                try
                {
                    if (File.Exists(logFilePath))
                    {
                        // Open in notepad
                        Process.Start("notepad.exe", logFilePath);
                    }
                    else
                    {
                        MessageBox.Show("Log file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to open log file: {ex.Message}", "UI");
                    MessageBox.Show($"Failed to open log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Browse for CNC12 installation path
        /// </summary>
        private void btnBrowseCnc12Path_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select CNC12 Installation Directory";
                folderDialog.SelectedPath = txtCnc12Path.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtCnc12Path.Text = folderDialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Update settings from UI
                SettingsManager.Settings.Cnc.Cnc12Path = txtCnc12Path.Text;
                SettingsManager.Settings.Cnc.UserName = txtUserName.Text;
                SettingsManager.Settings.Cnc.MachineName = txtMachineName.Text;

                // Save to file
                SettingsManager.SaveSettings();

                LogSuccess("Settings saved successfully", "Settings");
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"Failed to save settings: {ex.Message}", "Settings");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load settings into UI controls
        /// </summary>
        private void LoadSettingsIntoUI()
        {
            try
            {
                txtCnc12Path.Text = SettingsManager.Settings.Cnc.Cnc12Path;
                txtUserName.Text = SettingsManager.Settings.Cnc.UserName;
                txtMachineName.Text = SettingsManager.Settings.Cnc.MachineName;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to load settings into UI: {ex.Message}", "Settings");
            }
        }

        /// <summary>
        /// Copy script files to CNC12 directories on startup
        /// </summary>
        private void CopyScriptFilesToCnc12()
        {
            try
            {
                LogInfo("=== Starting CopyScriptFilesToCnc12 ===", "Startup");

                string cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
                string appPath = AppDomain.CurrentDomain.BaseDirectory;

                LogInfo($"CNC12 Path: {cnc12Path}", "Startup");
                LogInfo($"App Path: {appPath}", "Startup");

                // Source files
                string plcMsgSource = Path.Combine(appPath, "Centroid", "Scripts", "plcmsg.txt");
                string functionsSource = Path.Combine(appPath, "Centroid", "Scripts", "functions.xml");
                string plcSourceTemplate = Path.Combine(appPath, "Centroid", "Scripts", "acorn_router_plc.src");

                LogInfo($"Source files:", "Startup");
                LogInfo($"  plcmsg.txt: {plcMsgSource} (Exists: {File.Exists(plcMsgSource)})", "Startup");
                LogInfo($"  functions.xml: {functionsSource} (Exists: {File.Exists(functionsSource)})", "Startup");
                LogInfo($"  acorn_router_plc.src: {plcSourceTemplate} (Exists: {File.Exists(plcSourceTemplate)})", "Startup");

                // Destination paths for plcmsg.txt
                string plcMsgDest1 = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "router_plcmsg.txt");
                string plcMsgDest2 = Path.Combine(cnc12Path, "plcmsg.txt");

                // Destination path for functions.xml
                string functionsDest = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "functions.xml");

                // Destination path for PLC source
                string plcSourceDest = Path.Combine(cnc12Path, "acorn_router_plc.src");

                // Copy plcmsg.txt to both locations
                if (File.Exists(plcMsgSource))
                {
                    // Create directories if they don't exist
                    string dir1 = Path.GetDirectoryName(plcMsgDest1)!;
                    LogInfo($"Creating directory (if needed): {dir1}", "Startup");
                    Directory.CreateDirectory(dir1);

                    LogInfo($"Copying plcmsg.txt to: {plcMsgDest1}", "Startup");
                    File.Copy(plcMsgSource, plcMsgDest1, overwrite: true);
                    LogSuccess($"✓ Copied plcmsg.txt to {plcMsgDest1}", "Startup");

                    LogInfo($"Copying plcmsg.txt to: {plcMsgDest2}", "Startup");
                    File.Copy(plcMsgSource, plcMsgDest2, overwrite: true);
                    LogSuccess($"✓ Copied plcmsg.txt to {plcMsgDest2}", "Startup");
                }
                else
                {
                    LogWarning($"❌ Source file not found: {plcMsgSource}", "Startup");
                }

                // Copy functions.xml
                if (File.Exists(functionsSource))
                {
                    // Create directory if it doesn't exist
                    string dir2 = Path.GetDirectoryName(functionsDest)!;
                    LogInfo($"Creating directory (if needed): {dir2}", "Startup");
                    Directory.CreateDirectory(dir2);

                    LogInfo($"Copying functions.xml to: {functionsDest}", "Startup");
                    File.Copy(functionsSource, functionsDest, overwrite: true);
                    LogSuccess($"✓ Copied functions.xml to {functionsDest}", "Startup");
                }
                else
                {
                    LogWarning($"❌ Source file not found: {functionsSource}", "Startup");
                }

                // Copy PLC source template if destination doesn't have our logic
                if (File.Exists(plcSourceTemplate))
                {
                    bool shouldCopy = false;
                    bool needsConfirmation = false;

                    if (!File.Exists(plcSourceDest))
                    {
                        // File doesn't exist, copy it without confirmation
                        shouldCopy = true;
                        LogInfo($"PLC source file not found at {plcSourceDest}, will copy template", "Startup");
                    }
                    else
                    {
                        // Check if existing file has our HavenCNC markup
                        LogInfo($"Checking existing PLC source at {plcSourceDest} for HavenCNC markup...", "Startup");
                        string existingContent = File.ReadAllText(plcSourceDest);

                        // Look for the HavenCNC comment markers around the M52-M67 output handling
                        string havenCncMarker = "; -- HavenCNC";

                        if (!existingContent.Contains(havenCncMarker))
                        {
                            // HavenCNC logic not found, ask user if they want to update
                            needsConfirmation = true;
                            LogWarning($"⚠️ PLC source file exists but doesn't contain HavenCNC markup (M52-M67 output handling)", "Startup");

                            var result = MessageBox.Show(
                                "The PLC source file at:\n" +
                                $"{plcSourceDest}\n\n" +
                                "does not contain the HavenCNC output control logic (M52-M67).\n\n" +
                                "Would you like to update it with the HavenCNC version?\n\n" +
                                "The existing file will be backed up before updating.",
                                "Update PLC Source File?",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes)
                            {
                                shouldCopy = true;
                                LogInfo($"User confirmed PLC source update", "Startup");
                            }
                            else
                            {
                                LogInfo($"User declined PLC source update", "Startup");
                            }
                        }
                        else
                        {
                            LogSuccess($"✓ PLC source file already contains HavenCNC markup, skipping copy", "Startup");
                        }
                    }

                    if (shouldCopy)
                    {
                        // Always backup existing file if it exists
                        if (File.Exists(plcSourceDest))
                        {
                            string backupPath = plcSourceDest + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            LogInfo($"Backing up existing PLC source to: {backupPath}", "Startup");
                            File.Copy(plcSourceDest, backupPath, overwrite: true);
                            LogSuccess($"✓ Backed up existing PLC source to {backupPath}", "Startup");
                        }

                        LogInfo($"Copying PLC source template to: {plcSourceDest}", "Startup");
                        File.Copy(plcSourceTemplate, plcSourceDest, overwrite: true);
                        LogSuccess($"✓ Copied PLC source template with HavenCNC logic to {plcSourceDest}", "Startup");

                        if (needsConfirmation)
                        {
                            MessageBox.Show(
                                "PLC source file has been updated successfully.\n\n" +
                                $"Backup saved to:\n{plcSourceDest}.backup_[timestamp]\n\n" +
                                "The new file includes M52-M67 output control logic.",
                                "Update Complete",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
                else
                {
                    LogWarning($"❌ PLC source template not found: {plcSourceTemplate}", "Startup");
                }

                LogInfo("=== Finished CopyScriptFilesToCnc12 ===", "Startup");
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to copy script files: {ex.Message}", "Startup");
                LogError($"Stack trace: {ex.StackTrace}", "Startup");
            }
        }

    }
}
