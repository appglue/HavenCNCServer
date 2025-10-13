using CentroidAPI;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Interface for CNC program execution services
    /// </summary>
    public interface ICNCProgramService
    {
        /// <summary>
        /// Run G-code from array of lines
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to execute</param>
        /// <param name="startImmediately">Whether to start execution immediately or just load the program</param>
        /// <param name="gcodeParameterString">Optional parameter string to pass to the G-code program</param>
        /// <returns>Run operation success</returns>
        Task<bool> RunGCodeAsync(string[] gCodeLines, bool startImmediately = true, string? gcodeParameterString = null);

        /// <summary>
        /// Run single G-code command
        /// </summary>
        /// <param name="gcode">G-code command to run</param>
        /// <returns>Command execution success</returns>
        Task<bool> RunGCodeCommandAsync(string gcode);

        /// <summary>
        /// Stop G-code execution
        /// </summary>
        /// <returns>Stop operation success</returns>
        bool Stop();

        /// <summary>
        /// Resume G-code execution
        /// </summary>
        /// <returns>Resume operation success</returns>
        bool Resume();

        /// <summary>
        /// Resume G-code execution at specific line
        /// </summary>
        /// <param name="lineNumber">Line number to resume at</param>
        /// <returns>Resume operation success</returns>
        bool ResumeAt(int lineNumber);

        /// <summary>
        /// Get current G-code
        /// </summary>
        /// <returns>Current G-code lines</returns>
        string[] GetCurrentGCode();

        /// <summary>
        /// Get current line number
        /// </summary>
        /// <returns>Current line number</returns>
        int GetCurrentLineNumber();
    }

    /// <summary>
    /// Service for CNC program execution
    /// </summary>
    public class CNCProgramService : ICNCProgramService
    {
        /// <summary>
        /// Run G-code from array of lines
        /// </summary>
        /// <param name="gCodeLines">Array of G-code lines to execute</param>
        /// <param name="startImmediately">Whether to start execution immediately or just load the program</param>
        /// <param name="gcodeParameterString">Optional parameter string to pass to the G-code program</param>
        /// <returns>Run operation success</returns>
        public async Task<bool> RunGCodeAsync(string[] gCodeLines, bool startImmediately = true, string? gcodeParameterString = null)
        {
            try
            {
                if (gCodeLines == null || gCodeLines.Length == 0)
                {
                    LogError("G-code lines cannot be null or empty", "CNCProgram");
                    return false;
                }

                LogInfo("Starting G-code execution...", "CNCProgram");
                
                // Ensure CNC connection is available
                if (!CNCConnectionManager.IsConnected)
                {
                    LogInfo("Initializing CNC connection...", "CNCProgram");
                    
                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    if (pipe == null || !pipe.IsConstructed())
                    {
                        LogError("Cannot proceed: CNC connection failed", "CNCProgram");
                        return false;
                    }
                    
                    LogSuccess("CNC connection established", "CNCProgram");
                }

                // Create a unique temporary file to avoid conflicts
                var guid = Guid.NewGuid();
                string tempFileName = $"gcode_program_{DateTime.Now:yyyyMMdd_HHmmss}_{guid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string tempFilePath = Path.Combine(SettingsManager.Settings.Files.TempFilesDirectory, tempFileName);
                
                // Ensure temp directory exists
                Directory.CreateDirectory(SettingsManager.Settings.Files.TempFilesDirectory);
                
                // Write G-code content to temporary file
                await File.WriteAllLinesAsync(tempFilePath, gCodeLines);
                LogSuccess($"G-code saved to temporary file: {Path.GetFileName(tempFilePath)}", "CNCProgram");

                // Get CNC programs directory from settings
                string cncProgramsPath = SettingsManager.GetCncProgramsDirectory();
                
                // Create a unique filename to avoid conflicts
                var targetGuid = Guid.NewGuid();
                string uniqueFileName = $"gcode_program_{DateTime.Now:yyyyMMdd_HHmmss}_{targetGuid.ToString("N")[..8]}{SettingsManager.Settings.Files.DefaultGCodeExtension}";
                string targetPath = Path.Combine(cncProgramsPath, uniqueFileName);
                
                // Ensure target directory exists
                Directory.CreateDirectory(cncProgramsPath);
                
                // Copy G-code to CNC programs directory
                await File.WriteAllLinesAsync(targetPath, gCodeLines);
                LogSuccess("G-code file written to CNC programs directory", "CNCProgram");
                
                // Clean up the temporary file
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception cleanupEx)
                {
                    LogWarning($"Could not delete temporary file: {cleanupEx.Message}", "CNCProgram");
                    // Not critical - continue execution
                }

                if (startImmediately)
                {
                    // Execute the G-code program using G65 command
                    LogInfo("Executing G-code program using G65 command...", "CNCProgram");
                    
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe == null)
                    {
                        LogError("Cannot execute: No CNC connection", "CNCProgram");
                        return false;
                    }

                    // Use G65 command to run the G-code file directly
                    string g65Command = $"G65 \"{targetPath}\"";
                    LogInfo($"Sending command: {g65Command}", "CNCProgram");
                    
                    // Execute the G65 command using a new Job instance
                    var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                    var executeResult = cmd.RunCommand(g65Command, false);
                    
                    if (executeResult == CNCPipe.ReturnCode.SUCCESS)
                    {
                        LogSuccess("G65 command executed successfully", "CNCProgram");
                        LogSuccess("G-code program is now running in CNC12", "CNCProgram");
                        LogInfo($"Running program: {Path.GetFileName(targetPath)}", "CNCProgram");
                        return true;
                    }
                    else
                    {
                        LogError($"Failed to execute G65 command: {executeResult}", "CNCProgram");
                        return false;
                    }
                }
                else
                {
                    LogSuccess($"G-code program loaded successfully: {Path.GetFileName(targetPath)}", "CNCProgram");
                    LogInfo("Program is ready for execution (startImmediately was false)", "CNCProgram");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error running G-code: {ex.Message}", "CNCProgram");
                return false;
            }
        }

        /// <summary>
        /// Run single G-code command
        /// </summary>
        /// <param name="gcode">G-code command to run</param>
        /// <returns>Command execution success</returns>
        public async Task<bool> RunGCodeCommandAsync(string gcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gcode))
                {
                    LogError("G-code command cannot be empty", "CNCProgram");
                    return false;
                }

                // Clean the command (remove extra whitespace and comments)
                var cleanCommand = gcode.Trim();
                if (cleanCommand.StartsWith(";") || cleanCommand.StartsWith("("))
                {
                    LogWarning("Ignoring comment line", "CNCProgram");
                    return true; // Comments are "successfully" ignored
                }

                LogInfo($"Executing single G-code command: {cleanCommand}", "CNCProgram");
                
                // Ensure CNC connection is available
                if (!CNCConnectionManager.IsConnected)
                {
                    LogInfo("Initializing CNC connection...", "CNCProgram");
                    
                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    if (pipe == null || !pipe.IsConstructed())
                    {
                        LogError("Cannot proceed: CNC connection failed", "CNCProgram");
                        return false;
                    }
                    
                    LogSuccess("CNC connection established", "CNCProgram");
                }

                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot execute: No CNC connection", "CNCProgram");
                    return false;
                }

                // Execute the single command using a new Job instance
                var cmd = new CentroidAPI.CNCPipe.Job(cncPipe);
                var executeResult = cmd.RunCommand(cleanCommand, false);
                
                // Ensure this method actually runs asynchronously
                await Task.CompletedTask;
                
                if (executeResult == CNCPipe.ReturnCode.SUCCESS)
                {
                    LogSuccess($"✓ Command executed successfully: {cleanCommand}", "CNCProgram");
                    return true;
                }
                else
                {
                    LogError($"✗ Failed to execute command: {executeResult}", "CNCProgram");
                    LogError($"Command: {cleanCommand}", "CNCProgram");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing single command: {ex.Message}", "CNCProgram");
                return false;
            }
        }

        /// <summary>
        /// Stop G-code execution
        /// </summary>
        /// <returns>Stop operation success</returns>
        public bool Stop()
        {
            try
            {
                LogInfo("Stopping G-code execution...", "CNCProgram");
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot stop: No CNC connection", "CNCProgram");
                    return false;
                }

                // TODO: Implement proper stop functionality using CentroidAPI
                // This might involve sending a feed hold command or stop command
                LogWarning("Stop functionality not yet fully implemented", "CNCProgram");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error stopping execution: {ex.Message}", "CNCProgram");
                return false;
            }
        }

        /// <summary>
        /// Resume G-code execution
        /// </summary>
        /// <returns>Resume operation success</returns>
        public bool Resume()
        {
            try
            {
                LogInfo("Resuming G-code execution...", "CNCProgram");
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot resume: No CNC connection", "CNCProgram");
                    return false;
                }

                // TODO: Implement proper resume functionality using CentroidAPI
                LogWarning("Resume functionality not yet fully implemented", "CNCProgram");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error resuming execution: {ex.Message}", "CNCProgram");
                return false;
            }
        }

        /// <summary>
        /// Resume G-code execution at specific line
        /// </summary>
        /// <param name="lineNumber">Line number to resume at</param>
        /// <returns>Resume operation success</returns>
        public bool ResumeAt(int lineNumber)
        {
            try
            {
                if (lineNumber <= 0)
                {
                    LogError("Line number must be greater than 0", "CNCProgram");
                    return false;
                }

                LogInfo($"Resuming G-code execution at line {lineNumber}...", "CNCProgram");
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot resume: No CNC connection", "CNCProgram");
                    return false;
                }

                // TODO: Implement proper resume at line functionality using CentroidAPI
                LogWarning("Resume at line functionality not yet fully implemented", "CNCProgram");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error resuming at line {lineNumber}: {ex.Message}", "CNCProgram");
                return false;
            }
        }

        /// <summary>
        /// Get current G-code
        /// </summary>
        /// <returns>Current G-code lines</returns>
        public string[] GetCurrentGCode()
        {
            try
            {
                LogInfo("Getting current G-code...", "CNCProgram");
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot get G-code: No CNC connection", "CNCProgram");
                    return Array.Empty<string>();
                }

                // TODO: Implement get current G-code using CentroidAPI
                LogWarning("Get current G-code functionality not yet fully implemented", "CNCProgram");
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                LogError($"Error getting current G-code: {ex.Message}", "CNCProgram");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Get current line number
        /// </summary>
        /// <returns>Current line number</returns>
        public int GetCurrentLineNumber()
        {
            try
            {
                LogInfo("Getting current line number...", "CNCProgram");
                
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("Cannot get line number: No CNC connection", "CNCProgram");
                    return 0;
                }

                // TODO: Implement get current line number using CentroidAPI
                LogWarning("Get current line number functionality not yet fully implemented", "CNCProgram");
                return 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting current line number: {ex.Message}", "CNCProgram");
                return 0;
            }
        }
    }
}