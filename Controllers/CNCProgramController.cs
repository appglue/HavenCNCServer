using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using CentroidAPI;
using IOFile = System.IO.File;

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
        /// List of active CNC jobs
        /// </summary>
        private static readonly List<CNCJob> _activeJobs = new List<CNCJob>();
        private static readonly object _jobsLock = new object();

        /// <summary>
        /// Get all active jobs
        /// </summary>
        /// <returns>List of active jobs</returns>
        [HttpGet("Jobs")]
        public IActionResult GetJobs()
        {
            lock (_jobsLock)
            {
                var jobSummaries = _activeJobs.Select(job => new
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

                return Ok(jobSummaries);
            }
        }

        /// <summary>
        /// Get specific job details
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns>Job details</returns>
        [HttpGet("Jobs/{jobId}")]
        public IActionResult GetJob(string jobId)
        {
            lock (_jobsLock)
            {
                var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                if (job == null)
                {
                    return NotFound($"Job {jobId} not found");
                }

                return Ok(new
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
                    LastError = job.LastError
                });
            }
        }

        /// <summary>
        /// Start a specific job
        /// </summary>
        /// <param name="jobId">Job ID to start</param>
        /// <returns>Start operation result</returns>
        [HttpPost("Jobs/{jobId}/Start")]
        public async Task<IActionResult> StartJob(string jobId)
        {
            try
            {
                CNCJob jobToStart;
                
                lock (_jobsLock)
                {
                    var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        return NotFound($"Job {jobId} not found");
                    }

                    if (job.IsRunning)
                    {
                        return BadRequest(new { Success = false, JobId = jobId, Error = "Job is already running" });
                    }

                    if (job.IsComplete)
                    {
                        return BadRequest(new { Success = false, JobId = jobId, Error = "Job has already completed" });
                    }

                    jobToStart = job;
                }

                // Start the job outside the lock
                var success = await jobToStart.StartAsync();
                return Ok(new { Success = success, JobId = jobId, Message = success ? "Job started successfully" : jobToStart.LastError });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Clean up completed jobs
        /// </summary>
        /// <returns>Number of jobs cleaned up</returns>
        [HttpPost("Jobs/Cleanup")]
        public IActionResult CleanupJobs()
        {
            lock (_jobsLock)
            {
                var completedJobs = _activeJobs.Where(j => j.IsComplete).ToList();
                foreach (var job in completedJobs)
                {
                    job.Dispose();
                    _activeJobs.Remove(job);
                }

                return Ok(new { CleanupsCount = completedJobs.Count });
            }
        }

        #endregion

        #region G-Code Execution Control

        /// <summary>
        /// Stop specific job execution
        /// </summary>
        /// <param name="jobId">Job ID to stop</param>
        /// <returns>Stop operation success</returns>
        [HttpPost("Jobs/{jobId}/Stop")]
        public IActionResult StopJob(string jobId)
        {
            try
            {
                lock (_jobsLock)
                {
                    var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        return NotFound($"Job {jobId} not found");
                    }

                    var success = job.Stop();
                    return Ok(new { Success = success, JobId = jobId, Message = success ? "Job stopped successfully" : job.LastError });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Stop all running jobs
        /// </summary>
        /// <returns>Stop operation results</returns>
        [HttpPost("Stop")]
        public IActionResult StopAllJobs()
        {
            try
            {
                lock (_jobsLock)
                {
                    var runningJobs = _activeJobs.Where(j => j.IsRunning).ToList();
                    var results = new List<object>();

                    foreach (var job in runningJobs)
                    {
                        var success = job.Stop();
                        results.Add(new { JobId = job.JobId, Success = success, Error = success ? null : job.LastError });
                    }

                    return Ok(new { StoppedJobs = results.Count, Results = results });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Resume specific job execution
        /// </summary>
        /// <param name="jobId">Job ID to resume</param>
        /// <returns>Resume operation success</returns>
        [HttpPost("Jobs/{jobId}/Resume")]
        public IActionResult ResumeJob(string jobId)
        {
            try
            {
                lock (_jobsLock)
                {
                    var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        return NotFound($"Job {jobId} not found");
                    }

                    var success = job.Resume();
                    return Ok(new { Success = success, JobId = jobId, Message = success ? "Job resumed successfully" : job.LastError });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Pause specific job execution
        /// </summary>
        /// <param name="jobId">Job ID to pause</param>
        /// <returns>Pause operation success</returns>
        [HttpPost("Jobs/{jobId}/Pause")]
        public IActionResult PauseJob(string jobId)
        {
            try
            {
                lock (_jobsLock)
                {
                    var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        return NotFound($"Job {jobId} not found");
                    }

                    var success = job.Pause();
                    return Ok(new { Success = success, JobId = jobId, Message = success ? "Job paused successfully" : job.LastError });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Resume job execution at specific line
        /// </summary>
        /// <param name="jobId">Job ID to resume</param>
        /// <param name="lineNumber">Line number to resume at</param>
        /// <returns>Resume operation success</returns>
        [HttpPost("Jobs/{jobId}/ResumeAt/{lineNumber}")]
        public IActionResult ResumeJobAt(string jobId, int lineNumber)
        {
            try
            {
                if (lineNumber <= 0)
                {
                    return BadRequest(new { Success = false, Error = "Line number must be greater than 0" });
                }

                lock (_jobsLock)
                {
                    var job = _activeJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job == null)
                    {
                        return NotFound($"Job {jobId} not found");
                    }

                    var success = job.ResumeAt(lineNumber);
                    return Ok(new { Success = success, JobId = jobId, LineNumber = lineNumber, Message = success ? $"Job resumed at line {lineNumber}" : job.LastError });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new G-code job from array of lines
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to execute</param>
        /// <param name="startImmediately">Whether to start execution immediately or just create the job</param>
        /// <param name="gcodeParameterString">Optional parameter string to pass to the G-code program</param>
        /// <returns>Job creation and start result</returns>
        [HttpPost("RunGCode")]
        public async Task<IActionResult> RunGCode([FromBody] string[] gCodeLines, [FromQuery] bool startImmediately = true, [FromQuery] string? gcodeParameterString = null)
        {
            try
            {
                if (gCodeLines == null || gCodeLines.Length == 0)
                {
                    return BadRequest(new { Success = false, Error = "G-code lines cannot be null or empty" });
                }

                // Create a new CNC job
                CNCJob job;
                lock (_jobsLock)
                {
                    job = new CNCJob(gCodeLines, gcodeParameterString);
                    _activeJobs.Add(job);
                }

                var result = new
                {
                    JobId = job.JobId,
                    TotalLines = job.TotalLines,
                    FilePath = job.FilePath,
                    CreatedAt = job.CreatedAt,
                    StartImmediately = startImmediately
                };

                if (startImmediately)
                {
                    var startSuccess = await job.StartAsync();
                    if (!startSuccess)
                    {
                        return BadRequest(new 
                        { 
                            Success = false, 
                            JobId = job.JobId,
                            Error = job.LastError ?? "Failed to start job",
                            Job = result
                        });
                    }

                    return Ok(new 
                    { 
                        Success = true, 
                        JobId = job.JobId,
                        Message = "Job created and started successfully",
                        Job = new
                        {
                            JobId = job.JobId,
                            TotalLines = job.TotalLines,
                            FilePath = job.FilePath,
                            CreatedAt = job.CreatedAt,
                            StartedAt = job.StartedAt,
                            IsRunning = job.IsRunning
                        }
                    });
                }
                else
                {
                    return Ok(new 
                    { 
                        Success = true, 
                        JobId = job.JobId,
                        Message = "Job created successfully (not started)",
                        Job = result
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Run single G-code command
        /// </summary>
        /// <param name="gcode">G-code command to run</param>
        /// <returns>Command execution result</returns>
        [HttpPost("RunGCodeCommand")]
        public async Task<IActionResult> RunGCodeCommand([FromBody] string gcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gcode))
                {
                    return BadRequest(new { Success = false, Error = "G-code command cannot be empty" });
                }

                // Clean the command (remove extra whitespace and comments)
                var cleanCommand = gcode.Trim();
                if (cleanCommand.StartsWith(";") || cleanCommand.StartsWith("("))
                {
                    return Ok(new { Success = true, Message = "Comment ignored successfully", Command = gcode });
                }

                // Log the command we're about to execute
                System.Diagnostics.Debug.WriteLine($"[G-Code Command] Executing single command: {cleanCommand}");
                System.Diagnostics.Debug.WriteLine($"[G-Code Command] Original command: {gcode}");

                // Convert single command to array and call the main RunGCode method
                // This ensures we have only one code path for G-code execution
                string[] gCodeLines = { cleanCommand };
                return await RunGCode(gCodeLines, startImmediately: true, gcodeParameterString: null);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
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

        #endregion

        #region Step Run Control

        /// <summary>
        /// Start step run mode
        /// </summary>
        /// <returns>Step run start success</returns>
        [HttpPost("StartStepRun")]
        public bool StartStepRun()
        {
            try
            {
                // TODO: Implement start step run functionality using CentroidAPI
                // return CNCUtils.StartStepRunMode();
                throw new NotImplementedException("Start step run functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start step run mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// End step run mode
        /// </summary>
        /// <returns>Step run end success</returns>
        [HttpPost("EndStepRun")]
        public bool EndStepRun()
        {
            try
            {
                // TODO: Implement end step run functionality using CentroidAPI
                // return CNCUtils.EndStepRunMode();
                throw new NotImplementedException("End step run functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to end step run mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute next step in step run mode
        /// </summary>
        /// <returns>Next step execution success</returns>
        [HttpPost("StepRunNext")]
        public bool StepRunNext()
        {
            try
            {
                // TODO: Implement step run next functionality using CentroidAPI
                // return CNCUtils.ExecuteNextStep();
                throw new NotImplementedException("Step run next functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to execute next step: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run from current step
        /// </summary>
        /// <returns>Run from step success</returns>
        [HttpPost("RunFromCurrentStep")]
        public bool RunFromCurrentStep()
        {
            try
            {
                // TODO: Implement run from current step functionality using CentroidAPI
                // return CNCUtils.RunFromCurrentStep();
                throw new NotImplementedException("Run from current step functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run from current step: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
