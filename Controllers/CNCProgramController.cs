using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid.Events;
using CentroidAPI;
using System.Threading;
using IOFile = System.IO.File;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Program Control - Handles G-code execution, step run, and program control
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCProgramController : ControllerBase, IEventSubscriber
    {
        #region Job Management

        /// <summary>
        /// Current active CNC job (only one job can run at a time)
        /// </summary>
        private static CNCJob? _currentJob = null;

        private static readonly object _jobLock = new object();

        /// <summary>
        /// Static constructor to set up event subscriptions
        /// </summary>
        static CNCProgramController()
        {
            // Subscribe to CNC events for error detection
            CNCEventBus.Instance.Subscribe(new JobErrorMonitor());
        }

        /// <summary>
        /// Cancellation token for job monitoring task
        /// </summary>
        private static CancellationTokenSource? _monitorCancellation = null;

        /// <summary>
        /// Check if any job is currently running (calls Centroid API - single source of truth)
        /// </summary>
        /// <returns>True if a job is running</returns>
        public static bool IsJobRunning()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe != null)
                {
                    var result = cncPipe.state.IsJobRunning(out bool jobRunning);
                    if (result == CNCPipe.ReturnCode.SUCCESS)
                    {
                        return jobRunning;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error checking IsJobRunning from API: {ex.Message}", "CNCProgramController");
            }

            return false;
        }

        /// <summary>
        /// Get the current active job (for internal use by error monitoring)
        /// </summary>
        /// <returns>Current job or null</returns>
        internal static CNCJob? GetCurrentJob()
        {
            lock (_jobLock)
            {
                return _currentJob;
            }
        }

        /// <summary>
        /// Monitor current job for completion
        /// Polls frequently to catch even very short jobs
        /// </summary>
        private static async void StartMonitoringJob(string jobId)
        {
            _monitorCancellation?.Cancel();
            _monitorCancellation = new CancellationTokenSource();
            var token = _monitorCancellation.Token;

            try
            {
                LogInfo($"📊 Started monitoring job {jobId}", "CNCProgramController");

                await Task.Delay(50, token); // Initial delay for job to start

                bool everDetectedRunning = false;
                int pollCount = 0;
                int elapsedMs = 0;
                const int POLL_INTERVAL_MS = 100;
                const int TIMEOUT_MS = 1000; // 1 second timeout if job never detected

                while (!token.IsCancellationRequested)
                {
                    var isRunning = IsJobRunning();
                    pollCount++;
                    elapsedMs += POLL_INTERVAL_MS;

                    if (isRunning)
                    {
                        if (!everDetectedRunning)
                        {
                            everDetectedRunning = true;
                            LogSuccess($"✓ Job {jobId} detected as running (poll #{pollCount})", "CNCProgramController");
                        }
                    }
                    else // !isRunning
                    {
                        if (everDetectedRunning)
                        {
                            // Job was running and now stopped - normal completion
                            LogSuccess($"✓ Job {jobId} completed (detected after {pollCount} polls, ~{elapsedMs}ms)", "CNCProgramController");

                            // Small delay to let any error signals settle
                            await Task.Delay(200, token);

                            // Check if job still exists (error monitor may have already handled it)
                            lock (_jobLock)
                            {
                                if (_currentJob != null && _currentJob.JobId == jobId)
                                {
                                    LogInfo($"Broadcasting JobCompleted event for {jobId}", "CNCProgramController");
                                    HandleJobCompletion(jobId, true, null);
                                }
                            }
                            break;
                        }
                        else if (elapsedMs >= TIMEOUT_MS)
                        {
                            // Job never detected as running after 1 second - probably completed too fast
                            lock (_jobLock)
                            {
                                if (_currentJob != null && _currentJob.JobId == jobId)
                                {
                                    LogWarning($"Job {jobId} never detected as running after {TIMEOUT_MS}ms ({pollCount} polls) - assuming completed", "CNCProgramController");
                                    HandleJobCompletion(jobId, true, null);
                                }
                            }
                            break;
                        }
                    }

                    await Task.Delay(POLL_INTERVAL_MS, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Monitoring cancelled - job completed via other means
                LogInfo($"Job {jobId} monitoring cancelled", "CNCProgramController");
            }
            catch (Exception ex)
            {
                LogError($"Error monitoring job {jobId}: {ex.Message}", "CNCProgramController");
            }
        }

        /// <summary>
        /// Handle job completion - clear current job and send completion event
        /// </summary>
        private static void HandleJobCompletion(string jobId, bool success = true, string? errorMessage = null)
        {
            CNCJob? completedJob;

            lock (_jobLock)
            {
                completedJob = _currentJob;
                _currentJob = null;
            }

            if (completedJob != null)
            {
                // Mark job as completed with error if provided
                completedJob.MarkCompleted(errorMessage);

                // Calculate job duration
                var duration = completedJob.CompletedAt.HasValue && completedJob.StartedAt.HasValue
                    ? completedJob.CompletedAt.Value - completedJob.StartedAt.Value
                    : TimeSpan.Zero;

                // Determine final success status
                bool finalSuccess = success && completedJob.IsComplete && string.IsNullOrEmpty(completedJob.LastError);
                string? finalErrorMessage = errorMessage ?? completedJob.LastError;

                // Push job completed event
                var jobCompletedEvent = new JobCompletedEvent
                {
                    Timestamp = DateTime.Now,
                    Message = finalSuccess
                        ? $"Job {completedJob.JobId} completed successfully"
                        : $"Job {completedJob.JobId} failed: {finalErrorMessage}",
                    JobId = completedJob.JobId,
                    Success = finalSuccess,
                    ErrorMessage = finalErrorMessage,
                    Duration = duration,
                    LinesExecuted = completedJob.LineNumber,
                    FilePath = completedJob.FilePath
                };

                CNCEventBus.Instance.PublishMessage(jobCompletedEvent);

                if (finalSuccess)
                {
                    LogSuccess($"✓ Job {jobId} completed successfully", "CNCProgramController");
                }
                else
                {
                    LogError($"✗ Job {jobId} failed: {finalErrorMessage}", "CNCProgramController");
                }

                // Dispose the completed job
                completedJob.Dispose();
            }

            // Cancel monitoring task
            _monitorCancellation?.Cancel();
        }

        #endregion

        #region G-Code Execution Control

        /// <summary>
        /// Stop current job execution
        /// </summary>
        /// <returns>Stop operation success</returns>
        [HttpPost("Stop")]
        public JobOperationResponse StopCurrentJob()
        {
            try
            {
                lock (_jobLock)
                {
                    var job = _currentJob;
                    if (job == null)
                    {
                        throw new InvalidOperationException("No job is currently running");
                    }

                    var success = job.Stop();

                    // Clear current job and stop monitoring
                    _currentJob = null;
                    _monitorCancellation?.Cancel();

                    return new JobOperationResponse
                    {
                        Success = success,
                        JobId = job.JobId,
                        Message = success ? "Job stopped successfully" : job.LastError ?? "Unknown error",
                        Error = success ? null : job.LastError
                    };
                }
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Stop all running jobs (same as Stop since only one job can run)
        /// </summary>
        /// <returns>Stop operation results</returns>
        [HttpPost("StopAll")]
        public StopAllJobsResponse StopAllJobs()
        {
            try
            {
                lock (_jobLock)
                {
                    var results = new List<JobOperationResponse>();

                    if (_currentJob != null && _currentJob.IsRunning)
                    {
                        var success = _currentJob.Stop();
                        results.Add(new JobOperationResponse
                        {
                            JobId = _currentJob.JobId,
                            Success = success,
                            Error = success ? null : _currentJob.LastError,
                            Message = success ? "Job stopped successfully" : _currentJob.LastError ?? "Unknown error"
                        });

                        // Clear current job and stop monitoring
                        _currentJob = null;
                        _monitorCancellation?.Cancel();
                    }

                    return new StopAllJobsResponse { StoppedJobs = results.Count, Results = results };
                }
            }
            catch (Exception ex)
            {
                return new StopAllJobsResponse
                {
                    StoppedJobs = 0,
                    Results = new List<JobOperationResponse>
                    {
                        new JobOperationResponse
                        {
                            Success = false,
                            Error = ex.Message,
                            Message = ex.Message,
                            JobId = "N/A"
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Resume current job execution
        /// </summary>
        /// <returns>Resume operation success</returns>
        [HttpPost("Resume")]
        public JobOperationResponse ResumeCurrentJob()
        {
            try
            {
                lock (_jobLock)
                {
                    var job = _currentJob;
                    if (job == null)
                    {
                        throw new InvalidOperationException("No job is currently available to resume");
                    }

                    var success = job.Resume();
                    return new JobOperationResponse
                    {
                        Success = success,
                        JobId = job.JobId,
                        Message = success ? "Job resumed successfully" : job.LastError ?? "Unknown error",
                        Error = success ? null : job.LastError
                    };
                }
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Pause current job execution
        /// </summary>
        /// <returns>Pause operation success</returns>
        [HttpPost("Pause")]
        public JobOperationResponse PauseCurrentJob()
        {
            try
            {
                lock (_jobLock)
                {
                    var job = _currentJob;
                    if (job == null)
                    {
                        throw new InvalidOperationException("No job is currently running to pause");
                    }

                    var success = job.Pause();
                    return new JobOperationResponse
                    {
                        Success = success,
                        JobId = job.JobId,
                        Message = success ? "Job paused successfully" : job.LastError ?? "Unknown error",
                        Error = success ? null : job.LastError
                    };
                }
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Resume current job execution at specific line
        /// </summary>
        /// <param name="lineNumber">Line number to resume at</param>
        /// <returns>Resume operation success</returns>
        [HttpPost("ResumeAt/{lineNumber}")]
        public ResumeJobAtResponse ResumeCurrentJobAt(int lineNumber)
        {
            try
            {
                if (lineNumber <= 0)
                {
                    throw new ArgumentException("Line number must be greater than 0");
                }

                lock (_jobLock)
                {
                    var job = _currentJob;
                    if (job == null)
                    {
                        throw new InvalidOperationException("No job is currently available to resume");
                    }

                    var success = job.ResumeAt(lineNumber);
                    return new ResumeJobAtResponse
                    {
                        Success = success,
                        JobId = job.JobId,
                        LineNumber = lineNumber,
                        Message = success ? $"Job resumed at line {lineNumber}" : job.LastError ?? "Unknown error",
                        Error = success ? null : job.LastError
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResumeJobAtResponse
                {
                    Success = false,
                    JobId = "",
                    LineNumber = lineNumber,
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Create a new G-code job from array of lines
        /// </summary>
        /// <param name="request">G-code execution request containing lines, fixture point, and execution parameters</param>
        /// <returns>Job creation and start result</returns>
        [HttpPost("RunGCode")]
        public async Task<RunGCodeResponse> RunGCode([FromBody] RunGCodeRequest request)
        {
            try
            {
                LogInfo($"🚀 RunGCode called with {request?.GCodeLines?.Length ?? 0} lines, startImmediately={request?.StartImmediately ?? true}", "Program");

                if (request == null || request.GCodeLines == null || request.GCodeLines.Length == 0)
                {
                    LogWarning("RunGCode: G-code lines are null or empty", "Program");
                    throw new ArgumentException("G-code lines cannot be null or empty");
                }

                // Check if a job is already running - Centroid API is the single source of truth
                if (IsJobRunning())
                {
                    LogWarning("Job execution rejected - another job is already running", "Program");
                    return new RunGCodeResponse
                    {
                        Success = false,
                        Error = "A job is already running. Please wait for it to complete or stop it first.",
                        Message = "Job already running",
                        JobId = "",
                        Job = new JobDetails()
                    };
                }

                LogInfo($"G-code lines to execute: {string.Join(" | ", request.GCodeLines)}", "Program");

                // If fixture point is provided, check if it's different from the last one and set it if needed
                if (request.FixturePoint != null)
                {
                    try
                    {
                        var lastFixturePoint = CNCMovementController.LastFixturePoint;
                        bool needsUpdate = lastFixturePoint == null ||
                                         Math.Abs(lastFixturePoint.X - request.FixturePoint.X) > 0.0001 ||
                                         Math.Abs(lastFixturePoint.Y - request.FixturePoint.Y) > 0.0001 ||
                                         Math.Abs(lastFixturePoint.Z - request.FixturePoint.Z) > 0.0001 ||
                                         Math.Abs(lastFixturePoint.A - request.FixturePoint.A) > 0.0001;

                        if (needsUpdate)
                        {
                            LogInfo($"📍 Setting fixture point before G-code execution: X={request.FixturePoint.X:F4}, Y={request.FixturePoint.Y:F4}, Z={request.FixturePoint.Z:F4}, A={request.FixturePoint.A:F4}", "Program");

                            var movementController = new CNCMovementController();
                            await movementController.SetFixturePoint(request.FixturePoint);

                            LogInfo("✓ Fixture point set successfully", "Program");
                        }
                        else
                        {
                            LogInfo($"📍 Fixture point unchanged (X={request.FixturePoint.X:F4}, Y={request.FixturePoint.Y:F4}, Z={request.FixturePoint.Z:F4}, A={request.FixturePoint.A:F4}) - skipping update", "Program");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to set fixture point: {ex.Message}", "Program");
                        // Don't fail the entire job if fixture point fails - log and continue
                        LogWarning("Continuing with G-code execution despite fixture point error", "Program");
                    }
                }

                // Check and reset Centroid state before attempting to run G-code
                try
                {
                    LogInfo("Checking Centroid state before running G-code...", "Program");
                    bool isReady = Centroid.CNCUtils.CheckAndResetCentroidState();
                    if (!isReady)
                    {
                        LogWarning("Centroid state check failed - system not ready", "Program");
                        return new RunGCodeResponse
                        {
                            Success = false,
                            Error = "CNC system is not ready to accept commands. SV_STOP is active or API is restricted.",
                            Message = "CNC system not ready - check machine state",
                            JobId = "",
                            Job = new JobDetails()
                        };
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to verify CNC state: {ex.Message}", "Program");
                    return new RunGCodeResponse
                    {
                        Success = false,
                        Error = $"Failed to verify CNC state: {ex.Message}",
                        Message = "Failed to verify CNC state before running G-code",
                        JobId = "",
                        Job = new JobDetails()
                    };
                }

                LogInfo("✓ Centroid state verified - ready to create job", "Program");

                // Create a new CNC job
                CNCJob job;
                lock (_jobLock)
                {
                    LogInfo($"Creating new CNC job with {request.GCodeLines.Length} lines", "Program");
                    job = new CNCJob(request.GCodeLines, request.GcodeParameterString);
                    _currentJob = job;
                }

                // Push job started event
                var jobStartedEvent = new JobStartedEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"G-code job created with {request.GCodeLines.Length} lines",
                    JobId = job.JobId,
                    GCodeLines = request.GCodeLines,
                    TotalLines = request.GCodeLines.Length,
                    IsStepRunMode = false,
                    FilePath = null // Jobs from array don't have a file path
                };
                CNCEventBus.Instance.PublishMessage(jobStartedEvent);

                // Start the job if requested
                if (request.StartImmediately)
                {
                    LogInfo($"🎬 Starting job {job.JobId} immediately...", "Program");
                    var startSuccess = await job.StartAsync();
                    if (!startSuccess)
                    {
                        LogError($"Failed to start job {job.JobId}: {job.LastError}", "Program");

                        // Clear current job on failure
                        lock (_jobLock)
                        {
                            _currentJob = null;
                        }

                        return new RunGCodeResponse
                        {
                            Success = false,
                            JobId = job.JobId,
                            Error = job.LastError ?? "Failed to start job",
                            Message = job.LastError ?? "Failed to start job",
                            Job = new JobDetails
                            {
                                JobId = job.JobId,
                                LastError = job.LastError,
                                TotalLines = job.TotalLines
                            }
                        };
                    }

                    LogInfo($"✅ Job {job.JobId} started successfully", "Program");

                    // Start monitoring the job for completion
                    StartMonitoringJob(job.JobId);

                    return new RunGCodeResponse
                    {
                        Success = true,
                        JobId = job.JobId,
                        Message = "Job created and started successfully",
                        Job = new JobDetails
                        {
                            JobId = job.JobId,
                            LineNumber = job.LineNumber,
                            CurrentLine = job.CurrentLine,
                            IsRunning = job.IsRunning,
                            IsPaused = job.IsPaused,
                            IsComplete = job.IsComplete,
                            CreatedAt = job.CreatedAt,
                            StartedAt = job.StartedAt,
                            CompletedAt = job.CompletedAt,
                            TotalLines = job.TotalLines,
                            FilePath = job.FilePath,
                            LastError = job.LastError,
                            IsStepRunMode = job.IsStepRunMode,
                            StepLineNumber = job.StepLineNumber
                        },
                        FilePath = job.FilePath
                    };
                }
                else
                {
                    LogInfo($"Job {job.JobId} created but not started (startImmediately=false)", "Program");

                    return new RunGCodeResponse
                    {
                        Success = true,
                        JobId = job.JobId,
                        Message = "Job created successfully (not started)",
                        Job = new JobDetails
                        {
                            JobId = job.JobId,
                            LineNumber = job.LineNumber,
                            CurrentLine = job.CurrentLine,
                            IsRunning = job.IsRunning,
                            IsPaused = job.IsPaused,
                            IsComplete = job.IsComplete,
                            CreatedAt = job.CreatedAt,
                            StartedAt = job.StartedAt,
                            CompletedAt = job.CompletedAt,
                            TotalLines = job.TotalLines,
                            FilePath = job.FilePath,
                            LastError = job.LastError,
                            IsStepRunMode = job.IsStepRunMode,
                            StepLineNumber = job.StepLineNumber
                        },
                        FilePath = job.FilePath
                    };
                }
            }
            catch (Exception ex)
            {
                LogError($"RunGCode exception: {ex.Message}\n{ex.StackTrace}", "Program");
                return new RunGCodeResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Message = ex.Message,
                    JobId = "",
                    Job = new JobDetails()
                };
            }
        }

        /// <summary>
        /// Run single G-code command
        /// </summary>
        /// <param name="request">G-code command request containing the command and optional fixture point</param>
        /// <returns>Command execution result</returns>
        [HttpPost("RunGCodeCommand")]
        public async Task<RunGCodeCommandResponse> RunGCodeCommand([FromBody] RunGCodeCommandRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.GCode))
                {
                    throw new ArgumentException("G-code command cannot be empty");
                }

                // Clean the command (remove extra whitespace and comments)
                var cleanCommand = request.GCode.Trim();
                if (cleanCommand.StartsWith(";") || cleanCommand.StartsWith("("))
                {
                    return new RunGCodeCommandResponse
                    {
                        Success = true,
                        Message = "Comment ignored successfully",
                        Command = request.GCode
                    };
                }

                // Log the command we're about to execute
                System.Diagnostics.Debug.WriteLine($"[G-Code Command] Executing single command: {cleanCommand}");
                System.Diagnostics.Debug.WriteLine($"[G-Code Command] Original command: {request.GCode}");

                // Convert single command to array and call the main RunGCode method
                // This ensures we have only one code path for G-code execution
                var runGCodeRequest = new RunGCodeRequest
                {
                    GCodeLines = new[] { cleanCommand },
                    StartImmediately = true,
                    GcodeParameterString = null,
                    FixturePoint = request.FixturePoint
                };
                var result = await RunGCode(runGCodeRequest);

                return new RunGCodeCommandResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    Command = request.GCode,
                    Job = result.Success ? result.Job : null,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                return new RunGCodeCommandResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Message = ex.Message,
                    Command = request?.GCode ?? ""
                };
            }
        }

        #endregion

        #region G-Code Management

        /// <summary>
        /// Get all G-code lines from the currently running job
        /// </summary>
        /// <returns>All G-code lines, or empty array if no job running</returns>
        [HttpGet("GetCurrentGCode")]
        public string[] GetCurrentGCode()
        {
            try
            {
                lock (_jobLock)
                {
                    var currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        return Array.Empty<string>();
                    }

                    // Return all G-code lines from the job
                    return currentJob.GCodeLines;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current G-code: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current line number being executed
        /// </summary>
        /// <returns>Current line number (1-based), or 0 if no job running</returns>
        [HttpGet("GetCurrentLineNumber")]
        public int GetCurrentLineNumber()
        {
            try
            {
                lock (_jobLock)
                {
                    var currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        return 0;
                    }

                    return currentJob.LineNumber;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current line number: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if a job is currently running (calls Centroid API)
        /// </summary>
        /// <returns>True if a job is running, false otherwise</returns>
        [HttpGet("IsJobRunning")]
        public bool GetIsJobRunning()
        {
            return IsJobRunning();
        }

        /// <summary>
        /// Get current job status including step run information
        /// </summary>
        /// <returns>Current job details</returns>
        [HttpGet("GetCurrentJobStatus")]
        public JobDetails? GetCurrentJobStatus()
        {
            try
            {
                lock (_jobLock)
                {
                    var currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        return null;
                    }

                    return new JobDetails
                    {
                        JobId = currentJob.JobId,
                        LineNumber = currentJob.LineNumber,
                        CurrentLine = currentJob.CurrentLine,
                        IsRunning = currentJob.IsRunning,
                        IsPaused = currentJob.IsPaused,
                        IsComplete = currentJob.IsComplete,
                        CreatedAt = currentJob.CreatedAt,
                        StartedAt = currentJob.StartedAt,
                        CompletedAt = currentJob.CompletedAt,
                        TotalLines = currentJob.TotalLines,
                        FilePath = currentJob.FilePath,
                        LastError = currentJob.LastError,
                        IsStepRunMode = currentJob.IsStepRunMode,
                        StepLineNumber = currentJob.StepLineNumber
                    };
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current job status: {ex.Message}", ex);
            }
        }

        #endregion

        #region Step Run Control

        /// <summary>
        /// Start step run mode with G-code
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to execute in step mode</param>
        /// <param name="gcodeParameterString">Optional parameter string</param>
        /// <returns>Job creation result</returns>
        [HttpPost("StartStepRun")]
        public RunGCodeResponse StartStepRun([FromBody] string[] gCodeLines, [FromQuery] string? gcodeParameterString = null)
        {
            try
            {
                if (gCodeLines == null || gCodeLines.Length == 0)
                {
                    throw new ArgumentException("G-code lines cannot be null or empty");
                }

                // Check if a job is already running
                if (IsJobRunning())
                {
                    return new RunGCodeResponse
                    {
                        Success = false,
                        Error = "A job is already running. Please wait for it to complete or stop it first.",
                        Message = "Job already running",
                        JobId = "",
                        Job = new JobDetails()
                    };
                }

                // Create a new job in step run mode
                CNCJob job;
                lock (_jobLock)
                {
                    job = CNCJob.CreateStepRunJob(gCodeLines, gcodeParameterString);
                    _currentJob = job;
                }

                // Push job started event
                var jobStartedEvent = new JobStartedEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"Step run job started with {gCodeLines.Length} lines",
                    JobId = job.JobId,
                    GCodeLines = gCodeLines,
                    TotalLines = gCodeLines.Length,
                    IsStepRunMode = true,
                    FilePath = null // Step run jobs are created from array, not file
                };
                CNCEventBus.Instance.PublishMessage(jobStartedEvent);

                var jobDetails = new JobDetails
                {
                    JobId = job.JobId,
                    LineNumber = job.LineNumber,
                    CurrentLine = job.CurrentLine,
                    IsRunning = job.IsRunning,
                    IsPaused = job.IsPaused,
                    IsComplete = job.IsComplete,
                    CreatedAt = job.CreatedAt,
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    TotalLines = job.TotalLines,
                    FilePath = job.FilePath,
                    LastError = job.LastError,
                    IsStepRunMode = job.IsStepRunMode,
                    StepLineNumber = job.StepLineNumber
                };

                return new RunGCodeResponse
                {
                    Success = true,
                    JobId = job.JobId,
                    Message = $"Step run job created successfully with {gCodeLines.Length} lines",
                    Job = jobDetails
                };
            }
            catch (Exception ex)
            {
                return new RunGCodeResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Message = ex.Message,
                    JobId = "",
                    Job = new JobDetails()
                };
            }
        }

        /// <summary>
        /// End step run mode
        /// </summary>
        /// <returns>Step run end success</returns>
        [HttpPost("EndStepRun")]
        public JobOperationResponse EndStepRun()
        {
            try
            {
                lock (_jobLock)
                {
                    var currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        throw new InvalidOperationException("No job available to end step run mode");
                    }

                    if (!currentJob.IsStepRunMode)
                    {
                        throw new InvalidOperationException("Current job is not in step run mode");
                    }

                    var success = currentJob.EndStepRun();

                    // Clear current job since step run is ending
                    if (success)
                    {
                        currentJob.Dispose();
                        _currentJob = null;
                    }

                    return new JobOperationResponse
                    {
                        Success = success,
                        JobId = currentJob.JobId,
                        Message = success ? "Step run mode ended successfully" : currentJob.LastError ?? "Unknown error",
                        Error = success ? null : currentJob.LastError
                    };
                }
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Execute next step in step run mode
        /// </summary>
        /// <returns>Next step execution success</returns>
        [HttpPost("StepRunNext")]
        public JobOperationResponse StepRunNext()
        {
            try
            {
                CNCJob? currentJob;
                lock (_jobLock)
                {
                    currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        throw new InvalidOperationException("No job available for step execution");
                    }

                    if (!currentJob.IsStepRunMode)
                    {
                        throw new InvalidOperationException("Current job is not in step run mode");
                    }
                }

                // Send "about to execute" step event
                var aboutToExecuteEvent = new StepExecutionEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"About to execute step {currentJob.StepLineNumber}",
                    JobId = currentJob.JobId,
                    LineNumber = currentJob.StepLineNumber,
                    CurrentLine = currentJob.CurrentLine,
                    TotalLines = currentJob.TotalLines,
                    IsLastStep = currentJob.StepLineNumber >= currentJob.TotalLines,
                    Status = StepExecutionStatus.AboutToExecute
                };
                CNCEventBus.Instance.PublishMessage(aboutToExecuteEvent);

                // Execute outside the lock to avoid blocking
                var success = currentJob.ExecuteNextStep();

                // Send step execution event
                var stepEvent = new StepExecutionEvent
                {
                    Timestamp = DateTime.Now,
                    Message = success ? "Step executed successfully" : "Step execution failed",
                    JobId = currentJob.JobId,
                    LineNumber = currentJob.StepLineNumber,
                    CurrentLine = currentJob.CurrentLine,
                    TotalLines = currentJob.TotalLines,
                    IsLastStep = currentJob.IsComplete,
                    Status = success ?
                        (currentJob.IsComplete ? StepExecutionStatus.Completed : StepExecutionStatus.Completed) :
                        StepExecutionStatus.Failed
                };
                CNCEventBus.Instance.PublishMessage(stepEvent);

                return new JobOperationResponse
                {
                    Success = success,
                    JobId = currentJob.JobId,
                    Message = success
                        ? (currentJob.IsComplete
                            ? "Step run completed - all steps executed"
                            : $"Step {currentJob.StepLineNumber - 1}/{currentJob.TotalLines} executed: {currentJob.CurrentLine}")
                        : currentJob.LastError ?? "Unknown error",
                    Error = success ? null : currentJob.LastError
                };
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Run from current step
        /// </summary>
        /// <returns>Run from step success</returns>
        [HttpPost("RunFromCurrentStep")]
        public JobOperationResponse RunFromCurrentStep()
        {
            try
            {
                CNCJob? currentJob;
                lock (_jobLock)
                {
                    currentJob = _currentJob;
                    if (currentJob == null)
                    {
                        throw new InvalidOperationException("No job available to run from current step");
                    }

                    if (!currentJob.IsStepRunMode)
                    {
                        throw new InvalidOperationException("Current job is not in step run mode");
                    }
                }

                // Send "about to execute" step event for run from current step
                var aboutToExecuteEvent = new StepExecutionEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"About to run from step {currentJob.StepLineNumber} to completion",
                    JobId = currentJob.JobId,
                    LineNumber = currentJob.StepLineNumber,
                    CurrentLine = currentJob.CurrentLine,
                    TotalLines = currentJob.TotalLines,
                    IsLastStep = currentJob.StepLineNumber >= currentJob.TotalLines,
                    Status = StepExecutionStatus.AboutToExecute
                };
                CNCEventBus.Instance.PublishMessage(aboutToExecuteEvent);

                // Execute outside the lock to avoid blocking
                var success = currentJob.RunFromCurrentStep();

                // Send step execution event for run from current step
                var stepEvent = new StepExecutionEvent
                {
                    Timestamp = DateTime.Now,
                    Message = success ? "Running from current step to completion" : "Run from current step failed",
                    JobId = currentJob.JobId,
                    LineNumber = currentJob.StepLineNumber,
                    CurrentLine = currentJob.CurrentLine,
                    TotalLines = currentJob.TotalLines,
                    IsLastStep = currentJob.IsComplete,
                    Status = success ? StepExecutionStatus.Executing : StepExecutionStatus.Failed
                };
                CNCEventBus.Instance.PublishMessage(stepEvent);

                return new JobOperationResponse
                {
                    Success = success,
                    JobId = currentJob.JobId,
                    Message = success
                        ? $"Running from step {currentJob.StepLineNumber} to completion"
                        : currentJob.LastError ?? "Unknown error",
                    Error = success ? null : currentJob.LastError
                };
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = "",
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Check tool (pauses and raises tool, remeasures if tool changed)
        /// </summary>
        /// <returns>Tool check success</returns>
        [HttpPost("CheckTool")]
        public bool CheckTool()
        {
            try
            {
                // TODO: Implement check tool functionality using CentroidAPI
                // return CNCUtils.CheckTool();
                throw new NotImplementedException("Check tool functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check tool: {ex.Message}", ex);
            }
        }

        #endregion

        #region IEventSubscriber Implementation (for non-static controller instances)

        public EventTypeFlags GetSubscribedEvents()
        {
            return EventTypeFlags.None; // Static JobErrorMonitor handles event subscriptions
        }

        public void OnPositionUpdate(DROEvent position) { }
        public void OnLogMessage(LogEvent log) { }
        public void OnServerStatus(ServerStatusEvent status) { }
        public void OnCNCMessage(ICentroidEvent message) { }

        #endregion
    }

    /// <summary>
    /// Internal class to monitor CNC messages for job errors (particularly error 913 - file not found)
    /// </summary>
    internal class JobErrorMonitor : IEventSubscriber
    {
        public EventTypeFlags GetSubscribedEvents()
        {
            return EventTypeFlags.Messages; // Subscribe to CNC messages
        }

        public void OnPositionUpdate(DROEvent position) { }
        public void OnLogMessage(LogEvent log) { }
        public void OnServerStatus(ServerStatusEvent status) { }

        public void OnCNCMessage(ICentroidEvent message)
        {
            if (message is MessageEvent messageEvent)
            {
                // Check for critical errors that indicate job failure
                bool isCriticalError = false;
                string? errorDescription = null;

                // Error 913 - File not found
                if (messageEvent.EventCode == 913)
                {
                    isCriticalError = true;
                    errorDescription = $"File not found (Error 913): {messageEvent.Message}";
                    LogError($"🚨 Critical error detected during job execution: {errorDescription}", "JobErrorMonitor");
                }
                // Other miscellaneous errors (900-949 range) that could indicate job failure
                else if (messageEvent.EventCode >= 900 && messageEvent.EventCode <= 949)
                {
                    // Check if it's a genuine error that should stop the job
                    if (messageEvent.Message.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        messageEvent.Message.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
                        messageEvent.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                        messageEvent.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                    {
                        isCriticalError = true;
                        errorDescription = $"Error {messageEvent.EventCode}: {messageEvent.Message}";
                        LogError($"🚨 Critical error detected during job execution: {errorDescription}", "JobErrorMonitor");
                    }
                }
                // Syntax errors (500-599)
                else if (messageEvent.EventType == MessageEventType.SyntaxError ||
                         messageEvent.EventType == MessageEventType.GCodeError)
                {
                    isCriticalError = true;
                    errorDescription = $"Syntax error {messageEvent.EventCode}: {messageEvent.Message}";
                    LogError($"🚨 Syntax error detected during job execution: {errorDescription}", "JobErrorMonitor");
                }
                // System faults (400-499)
                else if (messageEvent.EventType == MessageEventType.SystemFault ||
                         messageEvent.EventType == MessageEventType.AxisFault ||
                         messageEvent.EventType == MessageEventType.LimitError)
                {
                    isCriticalError = true;
                    errorDescription = $"{messageEvent.EventType} {messageEvent.EventCode}: {messageEvent.Message}";
                    LogError($"🚨 System fault detected during job execution: {errorDescription}", "JobErrorMonitor");
                }
                // Job cancellation (user abort)
                else if (messageEvent.EventType == MessageEventType.JobCancelled)
                {
                    isCriticalError = true;
                    errorDescription = $"Job cancelled: {messageEvent.Message}";
                    LogWarning($"⚠️ Job cancellation detected: {errorDescription}", "JobErrorMonitor");
                }

                // If critical error detected and a job is running, mark it as failed
                if (isCriticalError)
                {
                    var currentJob = CNCProgramController.GetCurrentJob();
                    if (currentJob != null && currentJob.IsRunning)
                    {
                        LogWarning($"⚠ Marking job {currentJob.JobId} as failed due to critical error", "JobErrorMonitor");
                        // Use reflection to call the private static HandleJobCompletion method
                        typeof(CNCProgramController)
                            .GetMethod("HandleJobCompletion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            ?.Invoke(null, new object?[] { currentJob.JobId, false, errorDescription });
                    }
                }
            }
        }
    }
}
