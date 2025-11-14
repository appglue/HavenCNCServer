using CentroidAPI;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using System.Threading;
using System.Threading.Tasks;
using IOFile = System.IO.File;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Represents a CNC job that can be controlled and monitored
    /// </summary>
    public class CNCJob : ICNCEventListener
    {
        private readonly string[] _gCodeLines;
        private readonly string? _gcodeParameterString;
        private readonly string _jobId;
        private readonly string _filePath;
        private CentroidAPI.CNCPipe.Job? _cncJob;
        private bool _isListening = false;
        private CancellationTokenSource? _monitorCancellation;

        /// <summary>
        /// Callback to invoke when the job completes
        /// </summary>
        public Action<CNCJob>? OnJobCompleted { get; set; }

        #region Properties

        /// <summary>
        /// Unique identifier for this job
        /// </summary>
        public string JobId => _jobId;

        /// <summary>
        /// Current executing line number (1-based)
        /// </summary>
        public int LineNumber { get; private set; } = 0;

        /// <summary>
        /// Current executing G-code line
        /// </summary>
        public string CurrentLine { get; private set; } = string.Empty;

        /// <summary>
        /// Whether the job is currently running
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Whether the job is paused
        /// </summary>
        public bool IsPaused { get; private set; } = false;

        /// <summary>
        /// Whether the job has completed
        /// </summary>
        public bool IsComplete { get; private set; } = false;

        /// <summary>
        /// Whether the job is in step run mode
        /// </summary>
        public bool IsStepRunMode { get; private set; } = false;

        /// <summary>
        /// Current step line number in step run mode
        /// </summary>
        public int StepLineNumber { get; private set; } = 0;

        /// <summary>
        /// Job creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Job start timestamp (null if not started)
        /// </summary>
        public DateTime? StartedAt { get; private set; }

        /// <summary>
        /// Job completion timestamp (null if not completed)
        /// </summary>
        public DateTime? CompletedAt { get; private set; }

        /// <summary>
        /// Total number of lines in the job
        /// </summary>
        public int TotalLines => _gCodeLines.Length;

        /// <summary>
        /// File path where the G-code is stored
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Last error message (if any)
        /// </summary>
        public string? LastError { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new CNC job
        /// </summary>
        /// <param name="gCodeLines">G-code lines to execute</param>
        /// <param name="gcodeParameterString">Optional parameter string</param>
        public CNCJob(string[] gCodeLines, string? gcodeParameterString = null)
        {
            if (gCodeLines == null || gCodeLines.Length == 0)
            {
                throw new ArgumentException("G-code lines cannot be null or empty", nameof(gCodeLines));
            }

            _gCodeLines = gCodeLines;
            _gcodeParameterString = gcodeParameterString;
            _jobId = Guid.NewGuid().ToString("N")[..8]; // Short GUID for readability
            CreatedAt = DateTime.Now;

            // Create the file path for this job
            var fileName = $"job_{_jobId}_{DateTime.Now:yyyyMMdd_HHmmss}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
            _filePath = Path.Combine(SettingsManager.GetCncProgramsDirectory(), fileName);

            // Initialize current line to first non-comment line
            UpdateCurrentLine();
        }

        /// <summary>
        /// Create a new CNC job directly in step run mode
        /// </summary>
        /// <param name="gCodeLines">G-code lines to execute</param>
        /// <param name="gcodeParameterString">Optional parameter string</param>
        /// <returns>New job in step run mode</returns>
        public static CNCJob CreateStepRunJob(string[] gCodeLines, string? gcodeParameterString = null)
        {
            var job = new CNCJob(gCodeLines, gcodeParameterString);

            // Initialize step run mode
            job.IsStepRunMode = true;
            job.StepLineNumber = 1;
            job.LineNumber = 0; // Reset to beginning
            job.UpdateCurrentLine();

            // Start listening for events but don't start the actual job
            job.StartListening();

            System.Diagnostics.Debug.WriteLine($"[CNCJob {job._jobId}] Created in step run mode with {gCodeLines.Length} lines");
            LoggingService.LogInfo($"Job {job._jobId} - Created in step run mode with {gCodeLines.Length} lines", "CNCJob");

            return job;
        }

        #endregion

        #region Job Control Methods

        /// <summary>
        /// Start the job execution
        /// </summary>
        /// <returns>True if started successfully</returns>
        public async Task<bool> StartAsync()
        {
            try
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("Job is already running");
                }

                if (IsComplete)
                {
                    throw new InvalidOperationException("Job has already completed");
                }

                // Ensure CNC connection is available
                if (!CNCConnectionManager.IsConnected)
                {
                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    if (pipe == null || !pipe.IsConstructed())
                    {
                        throw new InvalidOperationException("Cannot start job: CNC connection failed");
                    }
                }

                // Ensure target directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                // Write G-code to file
                await IOFile.WriteAllLinesAsync(_filePath, _gCodeLines);

                // Get CNC pipe
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot start job: No CNC connection");
                }

                // Create the G65 command
                string g65Command = string.IsNullOrEmpty(_gcodeParameterString)
                    ? $"G65 \"{_filePath}\""
                    : $"G65 \"{_filePath}\" {_gcodeParameterString}";

                // Start listening for job updates before executing the command
                // This ensures we capture the program start event
                StartListening();

                // Log that we're about to start the job
                LoggingService.LogInfo($"Job {_jobId} starting with {TotalLines} G-code lines", "CNCJob");

                // Log the command to both debug and main UI
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Starting job with command: {g65Command}");
                LoggingService.LogInfo($"Job {_jobId} executing G65 command: {g65Command}", "CNCJob");

                // Create and execute the job
                _cncJob = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = _cncJob.RunCommand(g65Command, false);

                if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LastError = $"Failed to start job with return code: {executeResult} (numeric: {(int)executeResult})";
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] ERROR: {LastError}");

                    // Log job start failure to main UI
                    LoggingService.LogError($"Job {_jobId} failed to start: {LastError}", "CNCJob");

                    // Stop listening since the job failed to start
                    StopListening();
                    return false;
                }

                // Don't set IsRunning = true here - wait for the job start indicator
                // IsRunning will be set to true when we receive "API_COMMAND_RUNNING" on line 1 in OnJobInfoReceived
                IsPaused = false;
                // StartedAt will be set when we receive the actual start message

                // Log that the job command was sent successfully
                LoggingService.LogInfo($"Job {_jobId} command sent successfully - waiting for execution to begin", "CNCJob");

                // Start background polling to monitor job completion via API
                _monitorCancellation = new CancellationTokenSource();
                _ = Task.Run(() => MonitorJobCompletion(_monitorCancellation.Token), _monitorCancellation.Token);

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job started successfully");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Start failed: {ex.Message}");

                // Log job start exception to main UI
                LoggingService.LogError($"Job {_jobId} start failed: {ex.Message}", "CNCJob");

                return false;
            }
        }

        /// <summary>
        /// Stop the job execution
        /// </summary>
        /// <returns>True if stopped successfully</returns>
        public bool Stop()
        {
            try
            {
                if (!IsRunning)
                {
                    return true; // Already stopped
                }

                // Send Cycle Cancel skin event (SV_SKIN_EVENT_46)
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe != null)
                {
                    try
                    {
                        // Press Cycle Cancel
                        var pressResult = cncPipe.plc.SetSkinEventState((int)SkinEvent.CycleCancel, 1);
                        if (pressResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            System.Threading.Thread.Sleep(100); // Wait 100ms

                            // Release Cycle Cancel
                            var releaseResult = cncPipe.plc.SetSkinEventState((int)SkinEvent.CycleCancel, 0);

                            if (releaseResult != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Failed to release Cycle Cancel: {releaseResult}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Failed to press Cycle Cancel: {pressResult}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Error sending Cycle Cancel: {ex.Message}");
                    }
                }

                // Stop listening to events
                StopListening();

                IsRunning = false;
                IsPaused = false;
                IsComplete = true;
                CompletedAt = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job stopped");

                // Notify completion callback
                OnJobCompleted?.Invoke(this);

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Stop failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pause the job execution
        /// </summary>
        /// <returns>True if paused successfully</returns>
        public bool Pause()
        {
            try
            {
                if (!IsRunning || IsPaused)
                {
                    return IsPaused; // Already paused or not running
                }

                // TODO: Implement actual pause functionality using CentroidAPI
                IsPaused = true;

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job paused");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Pause failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resume the job execution
        /// </summary>
        /// <returns>True if resumed successfully</returns>
        public bool Resume()
        {
            try
            {
                if (!IsPaused)
                {
                    return IsRunning; // Not paused, return current running state
                }

                // TODO: Implement actual resume functionality using CentroidAPI
                IsPaused = false;

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job resumed");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Resume failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resume execution at a specific line number
        /// </summary>
        /// <param name="lineNumber">Line number to resume at (1-based)</param>
        /// <returns>True if resumed successfully</returns>
        public bool ResumeAt(int lineNumber)
        {
            try
            {
                if (lineNumber <= 0 || lineNumber > TotalLines)
                {
                    throw new ArgumentOutOfRangeException(nameof(lineNumber),
                        $"Line number must be between 1 and {TotalLines}");
                }

                // TODO: Implement actual resume at line functionality using CentroidAPI
                LineNumber = lineNumber;
                UpdateCurrentLine();
                IsPaused = false;

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job resumed at line {lineNumber}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Resume at line failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Step Run Methods

        /// <summary>
        /// End step run mode
        /// </summary>
        /// <returns>True if step run mode ended successfully</returns>
        public bool EndStepRun()
        {
            try
            {
                IsStepRunMode = false;
                StepLineNumber = 0;

                if (IsRunning)
                {
                    // Stop the current execution
                    IsRunning = false;
                    IsPaused = false;
                }

                StopListening();

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Step run mode ended");
                LoggingService.LogInfo($"Job {_jobId} - Step run mode ended", "CNCJob");

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] End step run failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute the next step in step run mode
        /// </summary>
        /// <returns>True if next step executed successfully</returns>
        public bool ExecuteNextStep()
        {
            try
            {
                if (!IsStepRunMode)
                {
                    throw new InvalidOperationException("Job is not in step run mode");
                }

                if (StepLineNumber > TotalLines)
                {
                    // All steps completed
                    IsStepRunMode = false;
                    IsComplete = true;
                    CompletedAt = DateTime.Now;
                    StopListening();

                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] All steps completed");
                    LoggingService.LogSuccess($"✓ Job {_jobId} step run completed - all {TotalLines} lines executed", "CNCJob");

                    // Notify completion callback
                    OnJobCompleted?.Invoke(this);
                    return true;
                }

                // Get the current line to execute
                var lineToExecute = _gCodeLines[StepLineNumber - 1];

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(lineToExecute) ||
                    lineToExecute.Trim().StartsWith(";") ||
                    lineToExecute.Trim().StartsWith("("))
                {
                    StepLineNumber++;
                    LineNumber = StepLineNumber;
                    UpdateCurrentLine();

                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Skipped comment/empty line {StepLineNumber - 1}: {lineToExecute}");

                    // Recursively try next line
                    return ExecuteNextStep();
                }

                // Get CNC pipe
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot execute step: No CNC connection");
                }

                // Execute the single line using RunCommand
                _cncJob = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = _cncJob.RunCommand(lineToExecute.Trim(), false);

                if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LastError = $"Failed to execute step at line {StepLineNumber}: {executeResult}";
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Step execution failed: {LastError}");
                    LoggingService.LogError($"Job {_jobId} step execution failed: {LastError}", "CNCJob");
                    return false;
                }

                // Update tracking
                LineNumber = StepLineNumber;
                UpdateCurrentLine();

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Executed step {StepLineNumber}: {lineToExecute}");
                LoggingService.LogInfo($"Job {_jobId}: Step {StepLineNumber}/{TotalLines} - {lineToExecute}", "CNCJob");

                // Move to next step
                StepLineNumber++;

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Execute next step failed: {ex.Message}");
                LoggingService.LogError($"Job {_jobId} execute next step failed: {ex.Message}", "CNCJob");
                return false;
            }
        }

        /// <summary>
        /// Run from current step to completion
        /// </summary>
        /// <returns>True if run from current step started successfully</returns>
        public bool RunFromCurrentStep()
        {
            try
            {
                if (!IsStepRunMode)
                {
                    throw new InvalidOperationException("Job is not in step run mode");
                }

                if (StepLineNumber > TotalLines)
                {
                    throw new InvalidOperationException("All steps have been completed");
                }

                // Exit step run mode and run normally from current line
                IsStepRunMode = false;

                // Create a subset of G-code lines from current step to end
                var remainingLines = _gCodeLines.Skip(StepLineNumber - 1).ToArray();

                if (remainingLines.Length == 0)
                {
                    IsComplete = true;
                    CompletedAt = DateTime.Now;
                    StopListening();

                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] No remaining lines to execute");
                    LoggingService.LogSuccess($"✓ Job {_jobId} completed - no remaining lines", "CNCJob");

                    OnJobCompleted?.Invoke(this);
                    return true;
                }

                // Write remaining lines to a temporary file
                var tempFileName = $"job_{_jobId}_from_step_{StepLineNumber}_{DateTime.Now:yyyyMMdd_HHmmss}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                var tempFilePath = Path.Combine(SettingsManager.GetCncProgramsDirectory(), tempFileName);

                IOFile.WriteAllLines(tempFilePath, remainingLines);

                // Get CNC pipe
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot run from current step: No CNC connection");
                }

                // Create the G65 command for remaining lines
                string g65Command = string.IsNullOrEmpty(_gcodeParameterString)
                    ? $"G65 \"{tempFilePath}\""
                    : $"G65 \"{tempFilePath}\" {_gcodeParameterString}";

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Running from step {StepLineNumber} with command: {g65Command}");
                LoggingService.LogInfo($"Job {_jobId} running from step {StepLineNumber} with {remainingLines.Length} remaining lines", "CNCJob");

                // Execute the remaining job
                _cncJob = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = _cncJob.RunCommand(g65Command, false);

                if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LastError = $"Failed to run from current step with return code: {executeResult}";
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Run from step failed: {LastError}");
                    LoggingService.LogError($"Job {_jobId} run from step failed: {LastError}", "CNCJob");
                    return false;
                }

                // Job is now running normally (completion will be handled by normal event processing)
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Run from current step failed: {ex.Message}");
                LoggingService.LogError($"Job {_jobId} run from current step failed: {ex.Message}", "CNCJob");
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Start listening for job status updates
        /// </summary>
        private void StartListening()
        {
            if (_isListening) return;

            _isListening = true;

            // Subscribe to job info events to track progress
            CNCJobInfoListener.JobInfoReceived += OnJobInfoReceived;

            // Register as an event listener for message events (with error codes)
            CNCJobInfoListener.AddListener(this);

            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Started listening for job updates and message events");
            LoggingService.LogInfo($"Job {_jobId} - Registered as listener. Total listeners: {CNCJobInfoListener.GetListenerCount()}", "CNCJob");
        }

        /// <summary>
        /// Stop listening for job status updates
        /// </summary>
        private void StopListening()
        {
            if (!_isListening) return;

            _isListening = false;

            // Unsubscribe from job info events
            CNCJobInfoListener.JobInfoReceived -= OnJobInfoReceived;

            // Unregister as event listener for message events
            CNCJobInfoListener.RemoveListener(this);

            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Stopped listening for job updates and message events");
        }

        /// <summary>
        /// Handle CNC events (including message events with error codes)
        /// </summary>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            try
            {
                // Debug log events (debug only, not to main UI)
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] EventReceived: {centroidEvent.GetType().Name} - {centroidEvent.Message}");

                if (centroidEvent is MessageEvent messageEvent)
                {
                    // Check for completion messages - code 306 indicates job finished
                    // In step run mode, ignore completion messages since we control execution manually
                    if (IsRunning && !IsComplete && !IsStepRunMode)
                    {
                        if (messageEvent.EventCode == 306)
                        {
                            IsRunning = false;
                            IsComplete = true;
                            CompletedAt = DateTime.Now;
                            StopListening();

                            // Cancel the monitoring task since we detected completion via event
                            _monitorCancellation?.Cancel();

                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job completed - received code 306: {messageEvent.Message}");

                            // Log job completion to main UI in GREEN
                            LoggingService.LogSuccess($"✓ Job {_jobId} completed - {messageEvent.Message}", "CNCJob");

                            // Notify completion callback
                            OnJobCompleted?.Invoke(this);
                        }
                    }

                    // Check for error messages
                    if (CNCJobInfoListener.IsErrorMessage(messageEvent.EventType))
                    {
                        LastError = $"Error {messageEvent.EventCode}: {messageEvent.Message}";
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Error detected - {LastError}");

                        // Log error to main UI
                        LoggingService.LogError($"Job {_jobId} error - {LastError}", "CNCJob");

                        // For critical errors, stop the job
                        if (messageEvent.EventType == MessageEventType.SystemFault ||
                            messageEvent.EventType == MessageEventType.AxisFault ||
                            messageEvent.EventType == MessageEventType.LimitError ||
                            messageEvent.EventType == MessageEventType.MiscellaneousError) // Includes travel exceeded (907)
                        {
                            IsRunning = false;
                            IsComplete = true;
                            CompletedAt = DateTime.Now;
                            StopListening();

                            // Cancel the monitoring task since we detected completion via error
                            _monitorCancellation?.Cancel();

                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job stopped due to critical error: {LastError}");

                            // Log critical error job stop to main UI
                            LoggingService.LogError($"Job {_jobId} stopped due to critical error: {LastError}", "CNCJob");

                            // Notify completion callback
                            OnJobCompleted?.Invoke(this);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Error processing CNC event: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor job completion by polling the Centroid API IsJobRunning method
        /// This provides reliable completion detection for fast jobs that may not send completion events
        /// </summary>
        private async Task MonitorJobCompletion(CancellationToken cancellationToken)
        {
            try
            {
                // Wait a bit for the job to actually start running
                await Task.Delay(100, cancellationToken);

                bool wasRunning = false;

                while (!IsComplete && !cancellationToken.IsCancellationRequested)
                {
                    // Get CNC pipe
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe == null)
                    {
                        // Connection lost
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor: CNC connection lost");
                        break;
                    }

                    // Check if any job is currently running
                    var result = cncPipe.state.IsJobRunning(out bool jobRunning);

                    if (result == CNCPipe.ReturnCode.SUCCESS)
                    {
                        if (jobRunning && !wasRunning)
                        {
                            // Job just started running
                            wasRunning = true;
                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor: Job started running (detected by API)");
                        }
                        else if (!jobRunning && wasRunning && !IsComplete)
                        {
                            // Job WAS running but now stopped - mark as complete
                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor: Job stopped running (detected by API) - marking complete");

                            IsRunning = false;
                            IsComplete = true;
                            CompletedAt = DateTime.Now;
                            StopListening();

                            // Log job completion to main UI
                            LoggingService.LogSuccess($"✓ Job {_jobId} completed (detected by API monitor)", "CNCJob");

                            // Notify completion callback
                            OnJobCompleted?.Invoke(this);
                            break;
                        }
                    }

                    // Poll every 100ms for responsive completion detection
                    await Task.Delay(100, cancellationToken);
                }

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor: Exiting monitoring loop (Cancelled={cancellationToken.IsCancellationRequested})");
            }
            catch (OperationCanceledException)
            {
                // Monitor was cancelled (likely due to 306 message event) - this is normal
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor: Cancelled by event-based completion");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Monitor exception: {ex.Message}");
                LoggingService.LogError($"Job {_jobId} monitoring error: {ex.Message}", "CNCJob");
            }
        }

        /// <summary>
        /// Handle job info updates from the CNC system (mainly for line number tracking and job start detection)
        /// </summary>
        private async void OnJobInfoReceived(JobInfoData jobInfo)
        {
            try
            {
                // Debug log job info messages (debug only, not to main UI)
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] JobInfo: Line {jobInfo.LineNumber}, Message: '{jobInfo.Message}'");

                // Check for job start: Look for actual CNC start messages or line 1 execution
                if (!IsRunning && !IsComplete)
                {
                    var message = jobInfo.Message ?? "";

                    // Look for CNC program start indicators - be more flexible
                    bool jobStartDetected = false;
                    string startReason = "";

                    if (message.Contains("program is now running") ||
                        message.Contains("Running program:"))
                    {
                        jobStartDetected = true;
                        startReason = $"Program start message: {message}";
                    }
                    else if (jobInfo.LineNumber == 1 && !string.IsNullOrEmpty(message))
                    {
                        jobStartDetected = true;
                        startReason = $"Line 1 execution: {message}";
                    }
                    else if (jobInfo.LineNumber > 0 && !string.IsNullOrEmpty(message))
                    {
                        // Any line execution could indicate the job has started
                        jobStartDetected = true;
                        startReason = $"Line {jobInfo.LineNumber} execution: {message}";
                    }

                    if (jobStartDetected)
                    {
                        IsRunning = true;
                        StartedAt = DateTime.Now;
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job started - detected: {startReason}");

                        // Log job started to main UI in GREEN
                        LoggingService.LogSuccess($"✓ Job {_jobId} started successfully - {startReason}", "CNCJob");
                    }
                }

                // Process job info updates if we're running and not complete
                if (IsRunning && !IsComplete)
                {
                    // Validate line number - ignore corrupted/invalid line numbers
                    if (jobInfo.LineNumber > 0 && jobInfo.LineNumber <= TotalLines && jobInfo.LineNumber != LineNumber)
                    {
                        LineNumber = jobInfo.LineNumber;
                        UpdateCurrentLine();

                        // Send StepExecutionEvent for G-code viewer (for both regular and step run jobs)
                        var stepExecutionEvent = new StepExecutionEvent
                        {
                            Timestamp = DateTime.Now,
                            Message = $"Executing line {LineNumber}: {CurrentLine}",
                            JobId = _jobId,
                            LineNumber = LineNumber,
                            CurrentLine = CurrentLine,
                            TotalLines = TotalLines,
                            IsLastStep = (LineNumber >= TotalLines),
                            Status = StepExecutionStatus.Executing
                        };
                        CNCJobInfoListener.PushCustomEvent(stepExecutionEvent);

                        // Log progress to main UI only for significant lines (every 50 lines or major milestones)
                        if (LineNumber == 1 || LineNumber == TotalLines || LineNumber % 50 == 0)
                        {
                            LoggingService.LogInfo($"Job {_jobId}: Line {LineNumber}/{TotalLines} - {CurrentLine}", "CNCJob");
                        }

                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Line {LineNumber}: {CurrentLine}");

                        // Check if we've reached the last line - wait for machine to actually finish
                        // This ensures we don't send completion message before the last move completes
                        if (LineNumber >= TotalLines)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Reached last line {LineNumber}/{TotalLines}, waiting for machine to finish...");

                            // Wait for up to 10 seconds for the machine to actually finish
                            bool machineFinished = await WaitForMachineToFinish(TimeSpan.FromSeconds(10));

                            IsRunning = false;
                            IsComplete = true;
                            CompletedAt = DateTime.Now;
                            StopListening();

                            // Cancel the monitoring task since we detected completion via line count
                            _monitorCancellation?.Cancel();

                            if (machineFinished)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job completed - machine finished executing");
                                LoggingService.LogSuccess($"✓ Job {_jobId} completed - executed all {TotalLines} lines", "CNCJob");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job completed - timeout waiting for machine (sent completion anyway)");
                                LoggingService.LogWarning($"Job {_jobId} completed with timeout - machine may still be executing final moves", "CNCJob");
                            }

                            // Notify completion callback
                            OnJobCompleted?.Invoke(this);
                        }
                    }
                    else if (jobInfo.LineNumber > TotalLines)
                    {
                        // Debug log invalid line numbers (debug only, not to main UI)
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Ignoring invalid line number: {jobInfo.LineNumber} (max: {TotalLines})");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Error processing job info: {ex.Message}");
            }
        }

        /// <summary>
        /// Wait for the machine to finish executing (IsJobRunning returns false)
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if machine finished within timeout, false if timed out</returns>
        private async Task<bool> WaitForMachineToFinish(TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    // Connection lost - assume finished
                    return true;
                }

                try
                {
                    var result = cncPipe.state.IsJobRunning(out bool jobRunning);
                    if (result == CNCPipe.ReturnCode.SUCCESS)
                    {
                        if (!jobRunning)
                        {
                            // Machine is no longer running - finished successfully
                            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Machine finished after {stopwatch.ElapsedMilliseconds}ms");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Error checking IsJobRunning: {ex.Message}");
                }

                // Poll every 50ms for responsive detection
                await Task.Delay(50);
            }

            // Timed out
            System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Timeout waiting for machine to finish after {stopwatch.ElapsedMilliseconds}ms");
            return false;
        }

        /// <summary>
        /// Update the current line text based on line number
        /// </summary>
        private void UpdateCurrentLine()
        {
            if (LineNumber > 0 && LineNumber <= TotalLines)
            {
                CurrentLine = _gCodeLines[LineNumber - 1]; // Convert to 0-based index
            }
            else if (LineNumber == 0 && TotalLines > 0)
            {
                // Find first non-comment line
                for (int i = 0; i < TotalLines; i++)
                {
                    var line = _gCodeLines[i].Trim();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith(";") && !line.StartsWith("("))
                    {
                        CurrentLine = line;
                        break;
                    }
                }
            }
            else
            {
                CurrentLine = string.Empty;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            // Stop listening first to unregister event handlers
            StopListening();

            // Cancel and dispose the monitoring task
            if (_monitorCancellation != null)
            {
                _monitorCancellation.Cancel();
                _monitorCancellation.Dispose();
                _monitorCancellation = null;
            }

            // Clean up the G-code file
            try
            {
                if (IOFile.Exists(_filePath))
                {
                    IOFile.Delete(_filePath);
                }
            }
            catch
            {
                // Not critical - file cleanup can fail
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a string representation of the CNC job
        /// </summary>
        /// <returns>String representation showing job ID, status, and progress</returns>
        public override string ToString()
        {
            var status = IsComplete ? "Complete" :
                        IsPaused ? "Paused" :
                        IsRunning ? "Running" : "Ready";

            return $"CNCJob[{_jobId}] {status} - Line {LineNumber}/{TotalLines}";
        }

        #endregion
    }
}