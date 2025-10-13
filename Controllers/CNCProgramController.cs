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
        #region G-Code Execution Control

        /// <summary>
        /// Stop G-code execution
        /// </summary>
        /// <returns>Stop operation success</returns>
        [HttpPost("Stop")]
        public bool Stop()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot stop: No CNC connection");
                }

                // TODO: Implement proper stop functionality using CentroidAPI
                throw new NotImplementedException("Stop functionality not yet fully implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to stop execution: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resume G-code execution
        /// </summary>
        /// <returns>Resume operation success</returns>
        [HttpPost("Resume")]
        public bool Resume()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot resume: No CNC connection");
                }

                // TODO: Implement proper resume functionality using CentroidAPI
                throw new NotImplementedException("Resume functionality not yet fully implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resume execution: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Resume G-code execution at specific line
        /// </summary>
        /// <param name="lineNumber">Line number to resume at</param>
        /// <returns>Resume operation success</returns>
        [HttpPost("ResumeAt/{lineNumber}")]
        public bool ResumeAt(int lineNumber)
        {
            try
            {
                if (lineNumber <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number must be greater than 0");
                }

                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot resume: No CNC connection");
                }

                // TODO: Implement proper resume at line functionality using CentroidAPI
                throw new NotImplementedException("Resume at line functionality not yet fully implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resume at line {lineNumber}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run G-code from array of lines
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to execute</param>
        /// <param name="startImmediately">Whether to start execution immediately or just load the program</param>
        /// <param name="gcodeParameterString">Optional parameter string to pass to the G-code program</param>
        /// <returns>Run operation success</returns>
        [HttpPost("RunGCode")]
        public async Task<bool> RunGCode([FromBody] string[] gCodeLines, [FromQuery] bool startImmediately = true, [FromQuery] string? gcodeParameterString = null)
        {
            try
            {
                if (gCodeLines == null || gCodeLines.Length == 0)
                {
                    throw new ArgumentException("G-code lines cannot be null or empty", nameof(gCodeLines));
                }

                // Ensure CNC connection is available
                if (!CNCConnectionManager.IsConnected)
                {
                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    if (pipe == null || !pipe.IsConstructed())
                    {
                        throw new InvalidOperationException("Cannot proceed: CNC connection failed");
                    }
                }

                // Create a unique temporary file to avoid conflicts
                var guid = Guid.NewGuid();
                string tempFileName = $"gcode_program_{DateTime.Now:yyyyMMdd_HHmmss}_{guid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string tempFilePath = Path.Combine(SettingsManager.Settings.Files.TempFilesDirectory, tempFileName);
                
                // Ensure temp directory exists
                Directory.CreateDirectory(SettingsManager.Settings.Files.TempFilesDirectory);
                
                // Write G-code content to temporary file
                await IOFile.WriteAllLinesAsync(tempFilePath, gCodeLines);

                // Get CNC programs directory from settings
                string cncProgramsPath = SettingsManager.GetCncProgramsDirectory();
                
                // Create a unique filename to avoid conflicts
                var targetGuid = Guid.NewGuid();
                string uniqueFileName = $"gcode_program_{DateTime.Now:yyyyMMdd_HHmmss}_{targetGuid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string targetPath = Path.Combine(cncProgramsPath, uniqueFileName);
                
                // Ensure target directory exists
                Directory.CreateDirectory(cncProgramsPath);
                
                // Copy G-code to CNC programs directory
                await IOFile.WriteAllLinesAsync(targetPath, gCodeLines);
                
                // Clean up the temporary file
                try
                {
                    IOFile.Delete(tempFilePath);
                }
                catch
                {
                    // Not critical - continue execution
                }

                if (startImmediately)
                {
                    // Execute the G-code program using G65 command
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe == null)
                    {
                        throw new InvalidOperationException("Cannot execute: No CNC connection");
                    }

                    // Use G65 command to run the G-code file directly
                    // If gcodeParameterString is provided, append it to the command
                    string g65Command = string.IsNullOrEmpty(gcodeParameterString) 
                        ? $"G65 \"{targetPath}\""
                        : $"G65 \"{targetPath}\" {gcodeParameterString}";
                    
                    // Execute the G65 command using a new Job instance
                    var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                    var executeResult = cmd.RunCommand(g65Command, false);
                    
                    if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                    {
                        throw new InvalidOperationException($"Failed to execute G65 command: {executeResult}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run G-code: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run single G-code command
        /// </summary>
        /// <param name="gcode">G-code command to run</param>
        /// <returns>Command execution success</returns>
        [HttpPost("RunGCodeCommand")]
        public bool RunGCodeCommand([FromBody] string gcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gcode))
                {
                    throw new ArgumentException("G-code command cannot be empty", nameof(gcode));
                }

                // Clean the command (remove extra whitespace and comments)
                var cleanCommand = gcode.Trim();
                if (cleanCommand.StartsWith(";") || cleanCommand.StartsWith("("))
                {
                    return true; // Comments are "successfully" ignored
                }

                // Ensure CNC connection is available
                if (!CNCConnectionManager.IsConnected)
                {
                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    if (pipe == null || !pipe.IsConstructed())
                    {
                        throw new InvalidOperationException("Cannot proceed: CNC connection failed");
                    }
                }

                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    throw new InvalidOperationException("Cannot execute: No CNC connection");
                }

                // Execute the single command using a new Job instance
                var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = cmd.RunCommand(cleanCommand, false);
                
                if (executeResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    throw new InvalidOperationException($"Failed to execute command: {executeResult}");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to execute G-code command: {ex.Message}", ex);
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
