using System;
using System.Linq;
using HavenCNCServer.Models;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for managing step-by-step G-code execution
    /// </summary>
    public class StepRunService
    {
        private string _currentJobId = string.Empty;
        private bool _isStepRunActive = false;
        private int _currentStepLine = 0;
        private string[] _stepRunGCodeLines = Array.Empty<string>();

        /// <summary>
        /// Event raised when step run status changes
        /// </summary>
        public event EventHandler<StepRunStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// Gets whether step run is currently active
        /// </summary>
        public bool IsActive => _isStepRunActive;

        /// <summary>
        /// Gets the current job ID
        /// </summary>
        public string CurrentJobId => _currentJobId;

        /// <summary>
        /// Gets the current step line number
        /// </summary>
        public int CurrentStepLine => _currentStepLine;

        /// <summary>
        /// Gets the total number of G-code lines
        /// </summary>
        public int TotalLines => _stepRunGCodeLines.Length;

        /// <summary>
        /// Gets the G-code lines for step run
        /// </summary>
        public string[] GCodeLines => _stepRunGCodeLines;

        /// <summary>
        /// Start step run mode with the provided G-code lines
        /// </summary>
        public StepRunResult StartStepRun(string[] gCodeLines)
        {
            if (gCodeLines == null || gCodeLines.Length == 0)
            {
                return new StepRunResult
                {
                    Success = false,
                    Error = "No G-code lines provided"
                };
            }

            // Use the controller to start the step run
            var controller = new Controllers.CNCProgramController();
            var response = controller.StartStepRun(gCodeLines);

            if (response.Success)
            {
                _currentJobId = response.JobId;
                _isStepRunActive = true;
                _currentStepLine = 1;
                _stepRunGCodeLines = gCodeLines;

                // Raise status changed event
                StatusChanged?.Invoke(this, new StepRunStatusChangedEventArgs
                {
                    IsActive = true,
                    CurrentLine = _currentStepLine,
                    TotalLines = _stepRunGCodeLines.Length,
                    JobId = _currentJobId
                });
            }

            return new StepRunResult
            {
                Success = response.Success,
                JobId = response.JobId,
                Error = response.Error ?? string.Empty
            };
        }

        /// <summary>
        /// Execute the next step in step run mode
        /// </summary>
        public StepRunResult ExecuteNextStep()
        {
            if (!_isStepRunActive || string.IsNullOrEmpty(_currentJobId))
            {
                return new StepRunResult
                {
                    Success = false,
                    Error = "No step run is currently active"
                };
            }

            // Execute next step using the controller
            var controller = new Controllers.CNCProgramController();
            var response = controller.StepRunNext();

            if (response.Success)
            {
                _currentStepLine++;

                // Raise status changed event
                StatusChanged?.Invoke(this, new StepRunStatusChangedEventArgs
                {
                    IsActive = true,
                    CurrentLine = _currentStepLine,
                    TotalLines = _stepRunGCodeLines.Length,
                    JobId = _currentJobId
                });

                // Check if we've reached the end of the G-code
                if (_currentStepLine > _stepRunGCodeLines.Length)
                {
                    Reset();
                    return new StepRunResult
                    {
                        Success = true,
                        IsComplete = true,
                        Message = "Step run completed successfully"
                    };
                }
            }

            return new StepRunResult
            {
                Success = response.Success,
                Error = response.Error ?? string.Empty
            };
        }

        /// <summary>
        /// Run from current step to completion
        /// </summary>
        public StepRunResult RunFromCurrentStep()
        {
            if (!_isStepRunActive || string.IsNullOrEmpty(_currentJobId))
            {
                return new StepRunResult
                {
                    Success = false,
                    Error = "No step run is currently active"
                };
            }

            // Run from current step using the controller
            var controller = new Controllers.CNCProgramController();
            var response = controller.RunFromCurrentStep();

            if (response.Success)
            {
                Reset();
            }

            return new StepRunResult
            {
                Success = response.Success,
                Error = response.Error ?? string.Empty
            };
        }

        /// <summary>
        /// Get the current status of step run
        /// </summary>
        public StepRunStatus GetCurrentStatus()
        {
            return new StepRunStatus
            {
                IsActive = _isStepRunActive,
                CurrentLine = _currentStepLine,
                TotalLines = _stepRunGCodeLines.Length,
                JobId = _currentJobId,
                CurrentGCode = _currentStepLine > 0 && _currentStepLine <= _stepRunGCodeLines.Length
                    ? _stepRunGCodeLines[_currentStepLine - 1]
                    : string.Empty
            };
        }

        /// <summary>
        /// Reset step run mode state
        /// </summary>
        public void Reset()
        {
            _currentJobId = string.Empty;
            _isStepRunActive = false;
            _currentStepLine = 0;
            _stepRunGCodeLines = Array.Empty<string>();

            // Raise status changed event
            StatusChanged?.Invoke(this, new StepRunStatusChangedEventArgs
            {
                IsActive = false,
                CurrentLine = 0,
                TotalLines = 0,
                JobId = string.Empty
            });
        }
    }

    /// <summary>
    /// Result of a step run operation
    /// </summary>
    public class StepRunResult
    {
        /// <summary>
        /// Gets or sets whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the job ID for the step run
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the operation failed
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an informational message about the operation
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the step run has completed
        /// </summary>
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// Current status of step run
    /// </summary>
    public class StepRunStatus
    {
        /// <summary>
        /// Gets or sets whether step run is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the current line number being executed
        /// </summary>
        public int CurrentLine { get; set; }

        /// <summary>
        /// Gets or sets the total number of lines in the step run
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Gets or sets the job ID for the step run
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current G-code line being executed
        /// </summary>
        public string CurrentGCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event args for step run status changes
    /// </summary>
    public class StepRunStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets whether step run is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the current line number being executed
        /// </summary>
        public int CurrentLine { get; set; }

        /// <summary>
        /// Gets or sets the total number of lines in the step run
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Gets or sets the job ID for the step run
        /// </summary>
        public string JobId { get; set; } = string.Empty;
    }
}
