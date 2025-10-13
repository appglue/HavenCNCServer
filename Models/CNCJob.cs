using CentroidAPI;
using HavenCNCServer.Services;
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

                // TODO: Implement actual stop functionality using CentroidAPI
                // For now, just update the state
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
                    if (IsRunning && !IsComplete)
                    {
                        if (messageEvent.EventCode == 306)
                        {
                            IsRunning = false;
                            IsComplete = true;
                            CompletedAt = DateTime.Now;
                            StopListening();
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
        /// Handle job info updates from the CNC system (mainly for line number tracking and job start detection)
        /// </summary>
        private void OnJobInfoReceived(JobInfoData jobInfo)
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
                        
                        // Log progress to main UI only for significant lines (every 50 lines or major milestones)
                        if (LineNumber == 1 || LineNumber == TotalLines || LineNumber % 50 == 0)
                        {
                            LoggingService.LogInfo($"Job {_jobId}: Line {LineNumber}/{TotalLines} - {CurrentLine}", "CNCJob");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Line {LineNumber}: {CurrentLine}");
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