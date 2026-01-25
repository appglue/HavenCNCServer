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
using Krypton.Toolkit;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Components;
using HavenCNCServer.Models;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// Main Windows Forms application that hosts the ASP.NET Core Web API server
    /// </summary>
    public partial class MainForm : KryptonForm
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _startupComplete = false;
        private readonly object _startupLock = new object();

        // Component instances
        private CoordinateDisplayComponent? _coordinateDisplayComponent;
        private FlickerFreeLogViewer? _mainLogViewer;

        // Separate forms for different views
        private Forms.LogsForm? _logsForm;
        private Forms.MessagesForm? _messagesForm;
        private Forms.GCodeForm? _gCodeForm;
        private Forms.SettingsForm? _settingsForm;

        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000?url_to_havencnc_server=http://localhost:5000";

        /// <summary>
        /// Initializes a new instance of the MainForm class
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Set Krypton palette for professional look
            var manager = new KryptonManager();
            manager.GlobalPaletteMode = Krypton.Toolkit.PaletteMode.Office2010Blue;

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

            // Set up timer to check CNC12 process status
            var cnc12StatusTimer = new System.Windows.Forms.Timer();
            cnc12StatusTimer.Interval = 2000; // Check every 2 seconds
            cnc12StatusTimer.Tick += Cnc12StatusTimer_Tick;
            cnc12StatusTimer.Start();

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
        /// Handle form resize
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Coordinate display is docked in pnlTopRight - no manual positioning needed
        }

        /// <summary>
        /// Set up the centralized logging system
        /// </summary>
        private void SetupLogging()
        {
            // Create log viewer for main form if not already created
            if (_mainLogViewer == null)
            {
                _mainLogViewer = new FlickerFreeLogViewer();
                _mainLogViewer.Dock = DockStyle.Fill;
                _mainLogViewer.Name = "mainLogViewer";

                // Add to a middle panel (will be created in InitializeUIComponents)
            }

            // Set maximum log entries from settings or default
            LoggingService.MaxLogEntries = 10000;

            // Register main form's log viewer as a target
            if (_mainLogViewer != null)
            {
                var logTarget = new LoggingService.FlickerFreeLogTarget(_mainLogViewer, this);
                LoggingService.AddTarget(logTarget);
            }

            LogInfo("Logging system initialized", "System");
        }

        /// <summary>
        /// Initialize the independent UI components
        /// </summary>
        private void InitializeUIComponents()
        {
            try
            {
                // Create the coordinate display component and position it properly on the right
                _coordinateDisplayComponent = new CoordinateDisplayComponent();
                PositionCoordinateDisplay();

                // Add the log viewer to the main form if it exists
                if (_mainLogViewer != null)
                {
                    // Find or create a panel between pnlTop and pnlBottom for logs
                    // The log viewer will fill the middle space
                    var middlePanel = this.Controls.Find("pnlMiddle", false).FirstOrDefault() as Panel;
                    if (middlePanel == null)
                    {
                        middlePanel = new Panel
                        {
                            Name = "pnlMiddle",
                            Dock = DockStyle.Fill
                        };
                        this.Controls.Add(middlePanel);
                        middlePanel.BringToFront();
                        pnlBottom.BringToFront(); // Keep bottom panel on top
                    }

                    middlePanel.Controls.Add(_mainLogViewer);
                    _mainLogViewer.BringToFront();
                }

                // Initialize the separate forms (but don't show them yet)
                _logsForm = new Forms.LogsForm();
                _messagesForm = new Forms.MessagesForm();
                _gCodeForm = new Forms.GCodeForm();
                _settingsForm = new Forms.SettingsForm();

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

            // Add coordinate display to the top-right panel with docking
            _coordinateDisplayComponent.Dock = DockStyle.Fill;
            _coordinateDisplayComponent.Name = "coordinateDisplayComponent";

            // Add to the top-right panel (not the form)
            pnlTopRight.Controls.Add(_coordinateDisplayComponent);
        }

        /// <summary>
        /// Load G-code into the display panel
        /// </summary>
        public void LoadGCodeForDisplay(string[] gcode)
        {
            try
            {
                _gCodeForm?.LoadGCodeForDisplay(gcode);
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
                _gCodeForm?.ClearGCodeDisplay();
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
            ScriptDeploymentService.DeployScriptsToCnc12();

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

            // Update retry count display (lock-free read)
            int retryCount = CNCConnectionManager.ConnectionRetryCount;
            if (retryCount > 0)
            {
                lblConnectionRetries.Text = $"Connection Retries: {retryCount}";
                lblConnectionRetries.ForeColor = Color.Orange;
            }
            else
            {
                lblConnectionRetries.Text = "";
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
        /// Timer tick to check CNC12 process status
        /// </summary>
        private void Cnc12StatusTimer_Tick(object? sender, EventArgs e)
        {
            // Lock-free read of process status
            bool isProcessRunning = CNCConnectionManager.IsCnc12ProcessRunning;

            if (!isProcessRunning)
            {
                lblCnc12Status.Text = "CNC12 Process Not Running";
                lblCnc12Status.ForeColor = Color.Red;
            }
            else
            {
                lblCnc12Status.Text = "";
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
                    base.OnFormClosing(e);
                    return;
                }
            }

            // Perform synchronous shutdown
            try
            {
                LogInfo("Application shutdown initiated", "System");

                // Signal all background operations to stop
                _cancellationTokenSource?.Cancel();

                // Stop CNC Job Info Listener with timeout
                var shutdownTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                CNCJobInfoListener.Stop(shutdownTokenSource.Token);

                // Clean up UI components
                _logsForm?.Dispose();
                _messagesForm?.Dispose();
                _gCodeForm?.Dispose();
                _settingsForm?.Dispose();
                _coordinateDisplayComponent?.Dispose();

                // Clear all event listeners
                CNCJobInfoListener.ClearAllListeners();

                // Stop API manager synchronously
                try
                {
                    var stopTask = ApiManager.StopAsync(CancellationToken.None);
                    if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                    {
                        LogWarning("API manager stop timed out", "System");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error stopping API manager: {ex.Message}", "System");
                }

                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;

                // Cleanup the CNC connection manager
                CNCConnectionManager.Disconnect();

                // Dispose cancellation token source
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                LogSuccess("Application shutdown completed", "System");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown: {ex.Message}", "System");
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
                _logsForm?.Dispose();
                _messagesForm?.Dispose();
                _gCodeForm?.Dispose();
                _settingsForm?.Dispose();
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
            alwaysOnTopToolStripMenuItem.Text = this.TopMost ? "Always on Top: ON" : "Always on Top: OFF";

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

        private void lblAdmin_LinkClicked(object sender, EventArgs e)
        {
            adminContextMenu.Show(lblAdmin, new Point(0, lblAdmin.Height));
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
        /// Show dropdown menu with available log files (now opens logs form instead)
        /// </summary>
        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            // This method is no longer used - logs are shown via btnShowLogs_Click
            btnShowLogs_Click(sender, e);
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
        /// Open the data folder in Windows Explorer
        /// </summary>
        private void btnOpenDataFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");

                // Create directory if it doesn't exist
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                // Open in Windows Explorer
                Process.Start("explorer.exe", dataDirectory);
                LogInfo($"Opened data folder: {dataDirectory}", "UI");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open data folder: {ex.Message}", "UI");
                MessageBox.Show($"Failed to open data folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReset_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Reset button pressed - starting skin event 56", "UI");
                var success = CNCUtils.StartSkinEvent(SkinEvent.ResetButtonPressed);

                if (!success)
                {
                    LogWarning("Failed to start Reset button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Reset button press error: {ex.Message}", "UI");
            }
        }

        private void btnReset_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Reset button released - stopping skin event 56", "UI");
                var success = CNCUtils.StopSkinEvent(SkinEvent.ResetButtonPressed);

                if (success)
                {
                    LogSuccess($"Reset button triggered successfully (Event {CNCUtils.RESET_BUTTON_EVENT})", "UI");
                }
                else
                {
                    LogWarning("Failed to release Reset button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Reset button release error: {ex.Message}", "UI");
            }
        }

        private void btnStop_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Stop button pressed - starting skin event 46 (Cycle Cancel)", "UI");
                var success = CNCUtils.StartSkinEvent(SkinEvent.CycleCancel);

                if (!success)
                {
                    LogWarning("Failed to start Cycle Cancel button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Stop button press error: {ex.Message}", "UI");
            }
        }

        private void btnStop_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Stop button released - stopping skin event 46 (Cycle Cancel)", "UI");
                var success = CNCUtils.StopSkinEvent(SkinEvent.CycleCancel);

                if (success)
                {
                    LogSuccess($"Cycle Cancel button triggered successfully (Event {CNCUtils.CYCLE_CANCEL_EVENT})", "UI");
                }
                else
                {
                    LogWarning("Failed to release Cycle Cancel button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Stop button release error: {ex.Message}", "UI");
            }
        }

        private void btnStart_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Start button pressed - starting skin event 50 (Cycle Start)", "UI");
                var success = CNCUtils.StartSkinEvent(SkinEvent.CycleStart);

                if (!success)
                {
                    LogWarning("Failed to start Cycle Start button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Start button press error: {ex.Message}", "UI");
            }
        }

        private void btnStart_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                LogInfo("Start button released - stopping skin event 50 (Cycle Start)", "UI");
                var success = CNCUtils.StopSkinEvent(SkinEvent.CycleStart);

                if (success)
                {
                    LogSuccess($"Cycle Start button triggered successfully (Event {CNCUtils.CYCLE_START_EVENT})", "UI");
                }
                else
                {
                    LogWarning("Failed to release Cycle Start button - CNC may not be connected", "UI");
                }
            }
            catch (Exception ex)
            {
                LogError($"Start button release error: {ex.Message}", "UI");
            }
        }

        /// <summary>
        /// Show the logs form
        /// </summary>
        private void btnShowLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_logsForm != null)
                {
                    _logsForm.Show();
                    _logsForm.BringToFront();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to show logs form: {ex.Message}", "UI");
            }
        }

        /// <summary>
        /// Show the messages form
        /// </summary>
        private void btnShowMessages_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_messagesForm != null)
                {
                    _messagesForm.Show();
                    _messagesForm.BringToFront();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to show messages form: {ex.Message}", "UI");
            }
        }

        /// <summary>
        /// Show the G-Code form
        /// </summary>
        private void btnShowGCode_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_gCodeForm != null)
                {
                    _gCodeForm.Show();
                    _gCodeForm.BringToFront();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to show G-Code form: {ex.Message}", "UI");
            }
        }

        /// <summary>
        /// Show the settings form
        /// </summary>
        private void btnShowSettings_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_settingsForm != null)
                {
                    _settingsForm.Show();
                    _settingsForm.BringToFront();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to show settings form: {ex.Message}", "UI");
            }
        }

        /// <summary>
        /// Load settings into UI controls (no longer needed with separate forms)
        /// </summary>
        private void LoadSettingsIntoUI()
        {
            // Settings are now loaded directly in SettingsForm
        }
    }
}
