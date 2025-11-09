using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid.Events;
using CentroidAPI;
using IOFile = System.IO.File;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Program Control - Handles G-code execution, step run, and program control
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCProgramController : ControllerBase
    {
        #region Job Management

        /// <summary>
        /// List of all CNC jobs (first job is currently running, rest are queued)
        /// </summary>
        private static readonly List<CNCJob> _jobs = new List<CNCJob>();

        private static readonly object _jobsLock = new object();

        /// <summary>
        /// Get all active jobs (internal method)
        /// </summary>
        /// <returns>List of active jobs</returns>
        private List<JobSummary> GetJobs()
        {
            lock (_jobsLock)
            {
                return _jobs.Select(job => new JobSummary
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
                    LastError = job.LastError
                }).ToList();
            }
        }

        /// <summary>
        /// Get specific job details (internal method)
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns>Job details</returns>
        private JobDetails GetJob(string jobId)
        {
            lock (_jobsLock)
            {
                var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
                if (job == null)
                {
                    throw new KeyNotFoundException($"Job {jobId} not found");
                }

                return new JobDetails
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
            }
        }

        /// <summary>
        /// Start a specific job (internal method)
        /// </summary>
        /// <param name="jobId">Job ID to start</param>
        /// <returns>Start operation result</returns>
        private async Task<JobOperationResponse> StartJob(string jobId)
        {
            try
            {
                CNCJob jobToStart;

                lock (_jobsLock)
                {
                    var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        throw new KeyNotFoundException($"Job {jobId} not found");
                    }

                    if (job.IsRunning)
                    {
                        throw new InvalidOperationException("Job is already running");
                    }

                    if (job.IsComplete)
                    {
                        throw new InvalidOperationException("Job has already completed");
                    }

                    jobToStart = job;
                }

                // Start the job outside the lock
                var success = await jobToStart.StartAsync();
                return new JobOperationResponse
                {
                    Success = success,
                    JobId = jobId,
                    Message = success ? "Job started successfully" : jobToStart.LastError ?? "Unknown error",
                    Error = success ? null : jobToStart.LastError
                };
            }
            catch (Exception ex)
            {
                return new JobOperationResponse
                {
                    Success = false,
                    JobId = jobId,
                    Error = ex.Message,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Clean up completed jobs (internal method)
        /// </summary>
        /// <returns>Number of jobs cleaned up</returns>
        private CleanupResponse CleanupJobs()
        {
            lock (_jobsLock)
            {
                var completedJobs = _jobs.Where(j => j.IsComplete).ToList();
                foreach (var job in completedJobs)
                {
                    job.Dispose();
                    _jobs.Remove(job);
                }

                return new CleanupResponse { CleanupsCount = completedJobs.Count };
            }
        }

        /// <summary>
        /// Check if there is a current job (first job in the list)
        /// </summary>
        /// <returns>True if there is a current job</returns>
        private static bool HasCurrentJob()
        {
            lock (_jobsLock)
            {
                return _jobs.Count > 0;
            }
        }

        /// <summary>
        /// Get the current job (first job in the list)
        /// </summary>
        /// <returns>Current job or null if no jobs</returns>
        private static CNCJob? GetCurrentJob()
        {
            lock (_jobsLock)
            {
                return _jobs.FirstOrDefault();
            }
        }

        /// <summary>
        /// Handle job completion - remove completed job and start next job in queue
        /// </summary>
        /// <param name="completedJob">The job that completed</param>
        private static async void OnJobCompleted(CNCJob completedJob)
        {
            try
            {
                // Calculate job duration
                var duration = completedJob.CompletedAt.HasValue && completedJob.StartedAt.HasValue
                    ? completedJob.CompletedAt.Value - completedJob.StartedAt.Value
                    : TimeSpan.Zero;

                // Push job completed event
                var jobCompletedEvent = new JobCompletedEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"Job {completedJob.JobId} completed",
                    JobId = completedJob.JobId,
                    Success = completedJob.IsComplete && string.IsNullOrEmpty(completedJob.LastError),
                    ErrorMessage = completedJob.LastError,
                    Duration = duration,
                    LinesExecuted = completedJob.LineNumber
                };
                CNCJobInfoListener.PushCustomEvent(jobCompletedEvent);

                lock (_jobsLock)
                {
                    // Remove the completed job
                    _jobs.Remove(completedJob);
                    LoggingService.LogInfo($"Job {completedJob.JobId} completed and removed from queue", "CNCJob");
                }

                // Dispose the completed job
                completedJob.Dispose();

                // Try to start the next job in the queue
                await ProcessJobQueue();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error handling job completion: {ex.Message}", "CNCJob");
            }
        }

        /// <summary>
        /// Process the job queue - start the first job in the list if it hasn't started yet
        /// </summary>
        /// <returns>True if a job was started from the queue</returns>
        private static async Task<bool> ProcessJobQueue()
        {
            CNCJob? jobToStart = null;

            lock (_jobsLock)
            {
                // If no jobs to process, nothing to do
                if (_jobs.Count == 0)
                {
                    return false;
                }

                // Get the first job in the list - this is the current job
                var firstJob = _jobs.First();

                // Only start it if it hasn't been started yet
                if (!firstJob.IsRunning && !firstJob.IsComplete)
                {
                    jobToStart = firstJob;
                }
            }

            if (jobToStart != null)
            {
                LoggingService.LogInfo($"Starting next job in queue: {jobToStart.JobId}", "CNCJob");
                var success = await jobToStart.StartAsync();

                if (!success)
                {
                    LoggingService.LogError($"Failed to start queued job {jobToStart.JobId}: {jobToStart.LastError}", "CNCJob");
                }

                return success;
            }

            return false;
        }

        /// <summary>
        /// Get queue status (internal method)
        /// </summary>
        /// <returns>Queue status information</returns>
        private QueueStatus GetQueueStatus()
        {
            lock (_jobsLock)
            {
                var currentJob = GetCurrentJob();
                var queuedJobs = _jobs.Skip(1).Where(j => !j.IsComplete).Select(job => new JobSummary
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
                    LastError = job.LastError
                }).ToList();

                return new QueueStatus
                {
                    CurrentJob = currentJob != null ? new JobSummary
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
                        LastError = currentJob.LastError
                    } : null,
                    QueuedJobs = queuedJobs,
                    QueueLength = _jobs.Skip(1).Count(j => !j.IsComplete)
                };
            }
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
                lock (_jobsLock)
                {
                    var job = GetCurrentJob();
                    if (job == null)
                    {
                        throw new InvalidOperationException("No job is currently running");
                    }

                    var success = job.Stop();
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
        /// Stop all running jobs
        /// </summary>
        /// <returns>Stop operation results</returns>
        [HttpPost("StopAll")]
        public StopAllJobsResponse StopAllJobs()
        {
            try
            {
                lock (_jobsLock)
                {
                    var runningJobs = _jobs.Where(j => j.IsRunning).ToList();
                    var results = new List<JobOperationResponse>();

                    foreach (var job in runningJobs)
                    {
                        var success = job.Stop();
                        results.Add(new JobOperationResponse
                        {
                            JobId = job.JobId,
                            Success = success,
                            Error = success ? null : job.LastError,
                            Message = success ? "Job stopped successfully" : job.LastError ?? "Unknown error"
                        });
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
                lock (_jobsLock)
                {
                    var job = GetCurrentJob();
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
                lock (_jobsLock)
                {
                    var job = GetCurrentJob();
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

                lock (_jobsLock)
                {
                    var job = GetCurrentJob();
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
                bool shouldStartNow = false;

                lock (_jobsLock)
                {
                    LogInfo($"Creating new CNC job with {request.GCodeLines.Length} lines", "Program");
                    job = new CNCJob(request.GCodeLines, request.GcodeParameterString);

                    // Set up completion callback to handle job completion
                    job.OnJobCompleted = OnJobCompleted;

                    _jobs.Add(job);
                    LogInfo($"Job {job.JobId} added to queue. Total jobs in queue: {_jobs.Count}", "Program");

                    // Check if we should start a job now:
                    // - startImmediately must be true
                    // - If this is the only job, start it
                    // - If there are jobs ahead, check if the first job is not started - if so, start it
                    if (request.StartImmediately)
                    {
                        var firstJob = _jobs.First();
                        bool firstJobNotStarted = !firstJob.IsRunning && !firstJob.IsComplete;

                        if (_jobs.Count == 1)
                        {
                            // This is the only job - start it
                            shouldStartNow = true;
                            LogInfo($"Job {job.JobId} is the only job in queue - will start immediately", "Program");
                        }
                        else if (firstJobNotStarted)
                        {
                            // There are jobs ahead, but the first job hasn't started - start it
                            shouldStartNow = true;
                            LogInfo($"Found {_jobs.Count} jobs in queue. First job {firstJob.JobId} not started (Running={firstJob.IsRunning}, Complete={firstJob.IsComplete}) - will start it now", "Program");
                            // Change job reference to start the first job instead of the newly added one
                            job = firstJob;
                        }
                        else
                        {
                            // Jobs ahead and first job is running/complete
                            LogInfo($"Jobs ahead in queue. First job {firstJob.JobId} status: Running={firstJob.IsRunning}, Complete={firstJob.IsComplete} - new job will wait", "Program");
                        }
                    }
                    else
                    {
                        LogInfo($"startImmediately=false - job will not be started automatically", "Program");
                    }
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
                CNCJobInfoListener.PushCustomEvent(jobStartedEvent);

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

                if (shouldStartNow)
                {
                    LogInfo($"🎬 Starting job {job.JobId} immediately...", "Program");
                    var startSuccess = await job.StartAsync();
                    if (!startSuccess)
                    {
                        LogError($"Failed to start job {job.JobId}: {job.LastError}", "Program");
                        return new RunGCodeResponse
                        {
                            Success = false,
                            JobId = job.JobId,
                            Error = job.LastError ?? "Failed to start job",
                            Message = job.LastError ?? "Failed to start job",
                            Job = jobDetails
                        };
                    }

                    LogInfo($"✅ Job {job.JobId} started successfully", "Program");
                    // Update job details after starting
                    jobDetails.StartedAt = job.StartedAt;
                    jobDetails.IsRunning = job.IsRunning;

                    return new RunGCodeResponse
                    {
                        Success = true,
                        JobId = job.JobId,
                        Message = "Job created and started successfully",
                        Job = jobDetails
                    };
                }
                else
                {
                    string message = request.StartImmediately ?
                        "Job created and queued (jobs ahead in queue)" :
                        "Job created successfully (not started)";

                    LogInfo($"Job {job.JobId} created but not started: {message}", "Program");

                    return new RunGCodeResponse
                    {
                        Success = true,
                        JobId = job.JobId,
                        Message = message,
                        Job = jobDetails
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
        /// Get current G-code
        /// </summary>
        /// <returns>Current G-code lines</returns>
        [HttpGet("GetCurrentGCode")]
        public string[] GetCurrentGCode()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot get G-code: No CNC connection");
                }

                // TODO: Implement get current G-code using CentroidAPI
                throw new NotImplementedException("Get current G-code functionality not yet fully implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current G-code: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current line number
        /// </summary>
        /// <returns>Current line number</returns>
        [HttpGet("GetCurrentLineNumber")]
        public int GetCurrentLineNumber()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot get line number: No CNC connection");
                }

                // TODO: Implement get current line number using CentroidAPI
                throw new NotImplementedException("Get current line number functionality not yet fully implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current line number: {ex.Message}", ex);
            }
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
                lock (_jobsLock)
                {
                    var currentJob = GetCurrentJob();
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

                // Check if there's already a job running
                lock (_jobsLock)
                {
                    var currentJob = GetCurrentJob();
                    if (currentJob != null && currentJob.IsRunning)
                    {
                        throw new InvalidOperationException("Cannot start step run mode while another job is running");
                    }
                }

                // Create a new job in step run mode
                CNCJob job;
                lock (_jobsLock)
                {
                    job = CNCJob.CreateStepRunJob(gCodeLines, gcodeParameterString);

                    // Set up completion callback to handle job completion
                    job.OnJobCompleted = OnJobCompleted;

                    _jobs.Add(job);
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
                CNCJobInfoListener.PushCustomEvent(jobStartedEvent);

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
                lock (_jobsLock)
                {
                    var currentJob = GetCurrentJob();
                    if (currentJob == null)
                    {
                        throw new InvalidOperationException("No job available to end step run mode");
                    }

                    if (!currentJob.IsStepRunMode)
                    {
                        throw new InvalidOperationException("Current job is not in step run mode");
                    }

                    var success = currentJob.EndStepRun();

                    // Remove the job from the list since step run is ending
                    if (success)
                    {
                        currentJob.Dispose();
                        _jobs.Remove(currentJob);
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
                lock (_jobsLock)
                {
                    currentJob = GetCurrentJob();
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
                CNCJobInfoListener.PushCustomEvent(aboutToExecuteEvent);

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
                CNCJobInfoListener.PushCustomEvent(stepEvent);

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
                lock (_jobsLock)
                {
                    currentJob = GetCurrentJob();
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
                CNCJobInfoListener.PushCustomEvent(aboutToExecuteEvent);

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
                CNCJobInfoListener.PushCustomEvent(stepEvent);

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
    }
}
