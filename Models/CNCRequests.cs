using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Request model for moving to a specific point
    /// </summary>
    public class MoveToRequest
    {
        /// <summary>
        /// Target point to move to
        /// </summary>
        public MachinePoint Point { get; set; } = new MachinePoint();

        /// <summary>
        /// XY movement speed (optional)
        /// </summary>
        public double? XYSpeed { get; set; }

        /// <summary>
        /// Z movement speed (optional)
        /// </summary>
        public double? ZSpeed { get; set; }

        /// <summary>
        /// Movement strategy (direct or Z-first)
        /// </summary>
        public MoveStrategy Strategy { get; set; } = MoveStrategy.Direct;
    }

    /// <summary>
    /// Request model for moving until an IO event occurs
    /// </summary>
    public class MoveToUntilRequest : MoveToRequest
    {
        /// <summary>
        /// IO event to wait for
        /// </summary>
        public IOEvent Event { get; set; }
    }

    /// <summary>
    /// Request model for directional movement until an event
    /// </summary>
    public class MoveDirectionUntilRequest
    {
        /// <summary>
        /// Direction to move
        /// </summary>
        public MoveDirection Direction { get; set; }

        /// <summary>
        /// IO event to wait for
        /// </summary>
        public IOEvent Event { get; set; }

        /// <summary>
        /// Movement speed (optional)
        /// </summary>
        public double? Speed { get; set; }
    }

    /// <summary>
    /// Request model for setting fixture to a point
    /// </summary>
    public class SetFixtureRequest
    {
        /// <summary>
        /// Fixture number (e.g., "G54", "G55", etc.)
        /// </summary>
        public string FixtureNumber { get; set; } = string.Empty;

        /// <summary>
        /// Point to set the fixture to
        /// </summary>
        public MachinePoint Point { get; set; } = new MachinePoint();
    }

    /// <summary>
    /// Request model for program execution
    /// </summary>
    public class ProgramExecuteRequest
    {
        /// <summary>
        /// Program name or identifier
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// Optional starting line number
        /// </summary>
        public int? StartLine { get; set; }
    }

    /// <summary>
    /// Request model for setting IO override
    /// </summary>
    public class IOOverrideRequest
    {
        /// <summary>
        /// Input/Output number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Override value
        /// </summary>
        public bool Value { get; set; }
    }

    /// <summary>
    /// Request model for configuration data operations
    /// </summary>
    public class ConfigurationDataRequest
    {
        /// <summary>
        /// Name of the data item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Content/value of the data item
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for saving checkpoints
    /// </summary>
    public class CheckpointSaveRequest
    {
        /// <summary>
        /// Name of the checkpoint
        /// </summary>
        public string CheckpointName { get; set; } = string.Empty;

        /// <summary>
        /// Array of data items to save in the checkpoint
        /// </summary>
        public IEnumerable<ConfigurationDataRequest> Data { get; set; } = new List<ConfigurationDataRequest>();
    }

    /// <summary>
    /// Request model for setting output values
    /// </summary>
    public class SetOutputRequest
    {
        /// <summary>
        /// Output number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Output value
        /// </summary>
        public bool Value { get; set; }
    }

    /// <summary>
    /// Request model for loading G-code
    /// </summary>
    public class LoadGCodeRequest
    {
        /// <summary>
        /// G-code content
        /// </summary>
        public string GCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional program name
        /// </summary>
        public string? ProgramName { get; set; }
    }

    /// <summary>
    /// Request model for IO override operations
    /// </summary>
    public class OverrideIORequest
    {
        /// <summary>
        /// IO number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Override value
        /// </summary>
        public bool Value { get; set; }
    }

    #region Response Models

    /// <summary>
    /// Response model for job operations
    /// </summary>
    public class JobOperationResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Job ID
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response model for job summary information
    /// </summary>
    public class JobSummary
    {
        /// <summary>
        /// Job identifier
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Current line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Current G-code line
        /// </summary>
        public string CurrentLine { get; set; } = string.Empty;

        /// <summary>
        /// Whether job is currently running
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Whether job is paused
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Whether job is complete
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Job creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Job start timestamp
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Job completion timestamp
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Total number of lines in the job
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Response model for detailed job information
    /// </summary>
    public class JobDetails : JobSummary
    {
        /// <summary>
        /// File path of the job
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Whether the job is in step run mode
        /// </summary>
        public bool IsStepRunMode { get; set; }

        /// <summary>
        /// Current step line number in step run mode
        /// </summary>
        public int StepLineNumber { get; set; }
    }

    /// <summary>
    /// Response model for queue status
    /// </summary>
    public class QueueStatus
    {
        /// <summary>
        /// Currently running job
        /// </summary>
        public JobSummary? CurrentJob { get; set; }

        /// <summary>
        /// Queued jobs
        /// </summary>
        public List<JobSummary> QueuedJobs { get; set; } = new List<JobSummary>();

        /// <summary>
        /// Number of jobs in queue
        /// </summary>
        public int QueueLength { get; set; }
    }

    /// <summary>
    /// Response model for cleanup operations
    /// </summary>
    public class CleanupResponse
    {
        /// <summary>
        /// Number of jobs cleaned up
        /// </summary>
        public int CleanupsCount { get; set; }
    }

    /// <summary>
    /// Response model for stop all jobs operation
    /// </summary>
    public class StopAllJobsResponse
    {
        /// <summary>
        /// Number of jobs stopped
        /// </summary>
        public int StoppedJobs { get; set; }

        /// <summary>
        /// Results for each job stop operation
        /// </summary>
        public List<JobOperationResponse> Results { get; set; } = new List<JobOperationResponse>();
    }

    /// <summary>
    /// Response model for resume job at specific line operation
    /// </summary>
    public class ResumeJobAtResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Job ID
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Line number to resume at
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response model for G-code execution operations
    /// </summary>
    public class RunGCodeResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Job ID
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Job details
        /// </summary>
        public JobDetails Job { get; set; } = new JobDetails();

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response model for single G-code command execution
    /// </summary>
    public class RunGCodeCommandResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The command that was executed
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Job details if a job was created
        /// </summary>
        public JobDetails? Job { get; set; }

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? Error { get; set; }
    }

    #endregion
}
