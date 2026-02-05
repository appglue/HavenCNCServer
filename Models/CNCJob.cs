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
    public class CNCJob
    {
        private readonly string[] _gCodeLines;
        private readonly string? _gcodeParameterString;
        private readonly string _jobId;
        private readonly string _filePath;
        private CentroidAPI.CNCPipe.Job? _cncJob;

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
        /// All G-code lines in this job
        /// </summary>
        public string[] GCodeLines => _gCodeLines;

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

            // Get CNC programs directory (should be CNC12's programs folder)
            var programsDir = SettingsManager.GetCncProgramsDirectory();
            _filePath = Path.Combine(programsDir, fileName);

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

                // Get CNC pipe
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot start job: No CNC connection");
                }

                // For single-line commands, execute directly without writing to file
                string commandToExecute;
                if (TotalLines == 1 && string.IsNullOrEmpty(_gcodeParameterString))
                {
                    // Single line - execute directly
                    commandToExecute = _gCodeLines[0].Trim();
                    LoggingService.LogInfo($"Job {_jobId} executing single command directly: {commandToExecute}", "CNCJob");
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Single-line mode - executing directly: {commandToExecute}");
                }
                else
                {
                    // Multiple lines or has parameters - write to file and use G65
                    // Ensure target directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                    // Write G-code to file and ensure it's flushed to disk
                    await IOFile.WriteAllLinesAsync(_filePath, _gCodeLines);

                    // CRITICAL: Add a small delay to ensure file system has released the file handle
                    // This prevents "file in use" errors when CNC12 tries to open it immediately after
                    await Task.Delay(100);

                    // Create the G65 command - use full absolute path
                    commandToExecute = string.IsNullOrEmpty(_gcodeParameterString)
                        ? $"G65 \"{_filePath}\""
                        : $"G65 \"{_filePath}\" {_gcodeParameterString}";

                    LoggingService.LogInfo($"Job {_jobId} starting with {TotalLines} G-code lines", "CNCJob");
                    LoggingService.LogInfo($"Job {_jobId} file written to: {_filePath}", "CNCJob");
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Starting job with command: {commandToExecute}");
                    LoggingService.LogInfo($"Job {_jobId} executing G65 command: {commandToExecute}", "CNCJob");
                }

                // Create and execute the job
                _cncJob = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = _cncJob.RunCommand(commandToExecute, false);

                if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LastError = $"Failed to start job with return code: {executeResult} (numeric: {(int)executeResult})";
                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] ERROR: {LastError}");

                    // Log job start failure to main UI
                    LoggingService.LogError($"Job {_jobId} failed to start: {LastError}", "CNCJob");
                    return false;
                }

                // Mark as running
                IsRunning = true;
                StartedAt = DateTime.Now;

                // Log that the job command was sent successfully
                LoggingService.LogInfo($"Job {_jobId} command sent successfully - execution started", "CNCJob");

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

        // Commented out old G65-only implementation - now using conditional logic based on UseG65Mode setting
        // /// <summary>
        // /// Start the job execution
        // /// </summary>
        // /// <returns>True if started successfully</returns>
        // public async Task<bool> StartAsync()
        // {
        //     try
        //     {
        //         if (IsRunning)
        //         {
        //             throw new InvalidOperationException("Job is already running");
        //         }

        //         if (IsComplete)
        //         {
        //             throw new InvalidOperationException("Job has already completed");
        //         }

        //         // Ensure CNC connection is available
        //         if (!CNCConnectionManager.IsConnected)
        //         {
        //             var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
        //             if (pipe == null || !pipe.IsConstructed())
        //             {
        //                 throw new InvalidOperationException("Cannot start job: CNC connection failed");
        //             }
        //         }

        //         // Ensure target directory exists
        //         Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        //         // Write G-code to file
        //         await IOFile.WriteAllLinesAsync(_filePath, _gCodeLines);

        //         // Get CNC pipe
        //         var cncPipe = CNCConnectionManager.GetCNCPipe();
        //         if (cncPipe == null)
        //         {
        //             throw new InvalidOperationException("Cannot start job: No CNC connection");
        //         }

        //         // Create the G65 command
        //         string g65Command = string.IsNullOrEmpty(_gcodeParameterString)
        //             ? $"G65 \"{_filePath}\""
        //             : $"G65 \"{_filePath}\" {_gcodeParameterString}";

        //         // Start listening for job updates before executing the command
        //         // This ensures we capture the program start event
        //         StartListening();

        //         // Log that we're about to start the job
        //         LoggingService.LogInfo($"Job {_jobId} starting with {TotalLines} G-code lines", "CNCJob");

        //         // Log the command to both debug and main UI
        //         System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Starting job with command: {g65Command}");
        //         LoggingService.LogInfo($"Job {_jobId} executing G65 command: {g65Command}", "CNCJob");

        //         // Create and execute the job
        //         _cncJob = new CentroidAPI.CNCPipe.Job(cncPipe);
        //         var executeResult = _cncJob.RunCommand(g65Command, false);

        //         if (executeResult != CNCPipe.ReturnCode.SUCCESS)
        //         {
        //             LastError = $"Failed to start job with return code: {executeResult} (numeric: {(int)executeResult})";
        //             System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] ERROR: {LastError}");

        //             // Log job start failure to main UI
        //             LoggingService.LogError($"Job {_jobId} failed to start: {LastError}", "CNCJob");

        //             // Stop listening since the job failed to start
        //             StopListening();
        //             return false;
        //         }

        //         // Don't set IsRunning = true here - wait for the job start indicator
        //         // IsRunning will be set to true when we receive "API_COMMAND_RUNNING" on line 1 in OnJobInfoReceived
        //         IsPaused = false;
        //         // StartedAt will be set when we receive the actual start message

        //         // Log that the job command was sent successfully
        //         LoggingService.LogInfo($"Job {_jobId} command sent successfully - waiting for execution to begin", "CNCJob");

        //         // Start background polling to monitor job completion via API
        //         _monitorCancellation = new CancellationTokenSource();
        //         _ = Task.Run(() => MonitorJobCompletion(_monitorCancellation.Token), _monitorCancellation.Token);

        //         System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job started successfully");
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         LastError = ex.Message;
        //         System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Start failed: {ex.Message}");

        //         // Log job start exception to main UI
        //         LoggingService.LogError($"Job {_jobId} start failed: {ex.Message}", "CNCJob");

        //         return false;
        //     }
        // }

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

                IsRunning = false;
                IsPaused = false;
                IsComplete = true;
                CompletedAt = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] Job stopped");
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

                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] All steps completed");
                    LoggingService.LogSuccess($"✓ Job {_jobId} step run completed - all {TotalLines} lines executed", "CNCJob");
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

                    System.Diagnostics.Debug.WriteLine($"[CNCJob {_jobId}] No remaining lines to execute");
                    LoggingService.LogSuccess($"✓ Job {_jobId} completed - no remaining lines", "CNCJob");
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
            // NOTE: File cleanup disabled - files will be cleaned up on next application startup
            // This allows examination of generated files for debugging

            // Clean up the G-code file
            //try
            //{
            //    if (IOFile.Exists(_filePath))
            //    {
            //        IOFile.Delete(_filePath);
            //    }
            //}
            //catch
            //{
            //    // Not critical - file cleanup can fail
            //}
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