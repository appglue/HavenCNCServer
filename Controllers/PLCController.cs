using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;
using SysIO = System.IO;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// PLC Management Controller - Handles PLC data, compilation, and installation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PLCController : ControllerBase
    {
        private readonly CNCConfigurationController _configController;
        private readonly string _cnc12Path;
        private readonly string _compilerPath;

        /// <summary>
        /// Initializes a new instance of the PLCController
        /// </summary>
        public PLCController()
        {
            _configController = new CNCConfigurationController();
            _cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
            _compilerPath = SysIO.Path.Combine(_cnc12Path, "compiler.cmd");

            LogInfo($"PLCController initialized. CNC12 path: {_cnc12Path}", "PLC");
        }

        #region PLC Data Management

        /// <summary>
        /// Get PLC data by name
        /// </summary>
        /// <param name="name">Name of the PLC data file</param>
        /// <returns>PLC data content as string array (lines)</returns>
        [HttpGet("GetPLCData/{name}")]
        [ProducesResponseType(typeof(string[]), 200)]
        [ProducesResponseType(404)]
        public ActionResult<string[]> GetPLCData(string name)
        {
            try
            {
                LogInfo($"📖 GetPLCData request: '{name}'", "PLC");

                var content = _configController.GetData(name);
                if (content == null)
                {
                    LogWarning($"PLC data '{name}' not found", "PLC");
                    return NotFound(new { message = $"PLC data '{name}' not found" });
                }

                // Split content into lines
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                LogSuccess($"✓ GetPLCData '{name}' returned {lines.Length} lines", "PLC");
                return Ok(lines);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get PLC data '{name}': {ex.Message}", "PLC");
                return StatusCode(500, new { message = $"Failed to get PLC data: {ex.Message}" });
            }
        }

        /// <summary>
        /// Set PLC data with specified name and content
        /// </summary>
        /// <param name="request">PLC data setting request with name and lines</param>
        /// <returns>Success response</returns>
        [HttpPost("SetPLCData")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult SetPLCData([FromBody] SetPLCDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Name is required" });
                }

                if (request.Lines == null || request.Lines.Length == 0)
                {
                    return BadRequest(new { message = "Lines array is required and cannot be empty" });
                }

                LogInfo($"💾 SetPLCData request: '{request.Name}' with {request.Lines.Length} lines", "PLC");

                // Convert lines array to content string
                var content = string.Join(Environment.NewLine, request.Lines);

                // Use the configuration controller to save (which handles backups)
                var setDataRequest = new ConfigurationDataRequest
                {
                    Name = request.Name,
                    Content = content
                };

                _configController.SetData(setDataRequest);

                LogSuccess($"✓ SetPLCData '{request.Name}' saved successfully", "PLC");
                return Ok(new { message = "PLC data saved successfully" });
            }
            catch (Exception ex)
            {
                LogError($"Failed to set PLC data '{request.Name}': {ex.Message}", "PLC");
                return StatusCode(500, new { message = $"Failed to set PLC data: {ex.Message}" });
            }
        }

        #endregion

        #region PLC Compilation

        /// <summary>
        /// Compile PLC source code
        /// </summary>
        /// <param name="request">Compilation request with PLC source lines</param>
        /// <returns>Compilation result with success status and issues</returns>
        [HttpPost("CompilePLC")]
        [ProducesResponseType(typeof(PLCCompilationResult), 200)]
        [ProducesResponseType(400)]
        public ActionResult<PLCCompilationResult> CompilePLC([FromBody] CompilePLCRequest request)
        {
            try
            {
                if (request.PlcLines == null || request.PlcLines.Length == 0)
                {
                    return BadRequest(new { message = "PlcLines array is required and cannot be empty" });
                }

                LogInfo($"🔧 CompilePLC request with {request.PlcLines.Length} lines", "PLC");

                // Create a temporary file for compilation
                var tempFileName = $"temp_plc_{Guid.NewGuid():N}.src";
                var tempFilePath = SysIO.Path.Combine(SysIO.Path.GetTempPath(), tempFileName);

                try
                {
                    // Write PLC source to temp file
                    SysIO.File.WriteAllLines(tempFilePath, request.PlcLines);

                    // Compile the PLC
                    var result = CompilePLCFile(tempFilePath);

                    LogInfo($"Compilation completed: Success={result.Success}, Issues={result.Issues.Length}", "PLC");
                    return Ok(result);
                }
                finally
                {
                    // Clean up temp file
                    if (SysIO.File.Exists(tempFilePath))
                    {
                        try { SysIO.File.Delete(tempFilePath); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to compile PLC: {ex.Message}", "PLC");
                return StatusCode(500, new { message = $"Failed to compile PLC: {ex.Message}", success = false, issues = new string[] { ex.Message } });
            }
        }

        /// <summary>
        /// Compile and install PLC with messages
        /// </summary>
        /// <param name="request">Installation request with PLC source and messages</param>
        /// <returns>Installation result with compilation status and installation success</returns>
        [HttpPost("CompileAndInstallPLC")]
        [ProducesResponseType(typeof(PLCInstallationResult), 200)]
        [ProducesResponseType(400)]
        public ActionResult<PLCInstallationResult> CompileAndInstallPLC([FromBody] CompileAndInstallPLCRequest request)
        {
            try
            {
                if (request.PlcLines == null || request.PlcLines.Length == 0)
                {
                    return BadRequest(new { message = "PlcLines array is required and cannot be empty" });
                }

                if (request.MessageLines == null || request.MessageLines.Length == 0)
                {
                    return BadRequest(new { message = "MessageLines array is required and cannot be empty" });
                }

                LogInfo($"🚀 CompileAndInstallPLC request with {request.PlcLines.Length} PLC lines and {request.MessageLines.Length} message lines", "PLC");

                var result = new PLCInstallationResult
                {
                    CompilationResult = new PLCCompilationResult { Success = false, Issues = new string[0] },
                    InstallationSuccess = false,
                    Message = ""
                };

                // Create temporary files
                var tempPlcFileName = $"temp_plc_{Guid.NewGuid():N}.src";
                var tempPlcFilePath = SysIO.Path.Combine(SysIO.Path.GetTempPath(), tempPlcFileName);

                try
                {
                    // Step 1: Write PLC source to temp file
                    SysIO.File.WriteAllLines(tempPlcFilePath, request.PlcLines);

                    // Step 2: Compile the PLC
                    LogInfo("Step 1: Compiling PLC...", "PLC");
                    result.CompilationResult = CompilePLCFile(tempPlcFilePath);

                    if (!result.CompilationResult.Success)
                    {
                        result.Message = "Compilation failed. See issues for details.";
                        LogError($"PLC compilation failed with {result.CompilationResult.Issues.Length} issues", "PLC");
                        return Ok(result);
                    }

                    LogSuccess("✓ PLC compiled successfully", "PLC");

                    // Step 3: Copy compiled PLC file to CNC directory
                    LogInfo("Step 2: Installing compiled PLC...", "PLC");
                    var compiledPlcPath = SysIO.Path.ChangeExtension(tempPlcFilePath, ".plc");
                    var targetPlcPath = SysIO.Path.Combine(_cnc12Path, "acorn_router_plc.plc"); // Using standard name

                    if (!SysIO.File.Exists(compiledPlcPath))
                    {
                        result.Message = "Compiled PLC file not found. Compilation may have failed silently.";
                        LogError("Compiled .plc file not found after compilation", "PLC");
                        return Ok(result);
                    }

                    // Backup existing PLC if it exists
                    if (SysIO.File.Exists(targetPlcPath))
                    {
                        var backupPath = targetPlcPath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                        SysIO.File.Copy(targetPlcPath, backupPath, true);
                        LogInfo($"Backed up existing PLC to: {backupPath}", "PLC");
                    }

                    SysIO.File.Copy(compiledPlcPath, targetPlcPath, true);
                    LogSuccess($"✓ Copied compiled PLC to: {targetPlcPath}", "PLC");

                    // Step 4: Copy PLC source file
                    var targetSrcPath = SysIO.Path.Combine(_cnc12Path, "acorn_router_plc.src");
                    if (SysIO.File.Exists(targetSrcPath))
                    {
                        var backupSrcPath = targetSrcPath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                        SysIO.File.Copy(targetSrcPath, backupSrcPath, true);
                        LogInfo($"Backed up existing source to: {backupSrcPath}", "PLC");
                    }
                    SysIO.File.WriteAllLines(targetSrcPath, request.PlcLines);
                    LogSuccess($"✓ Saved PLC source to: {targetSrcPath}", "PLC");

                    // Step 5: Copy plcmsg.txt
                    LogInfo("Step 3: Installing PLC messages...", "PLC");
                    var targetMsgPath = SysIO.Path.Combine(_cnc12Path, "plcmsg.txt");

                    if (SysIO.File.Exists(targetMsgPath))
                    {
                        var backupMsgPath = targetMsgPath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                        SysIO.File.Copy(targetMsgPath, backupMsgPath, true);
                        LogInfo($"Backed up existing messages to: {backupMsgPath}", "PLC");
                    }

                    SysIO.File.WriteAllLines(targetMsgPath, request.MessageLines);
                    LogSuccess($"✓ Saved PLC messages to: {targetMsgPath}", "PLC");

                    // Success!
                    result.InstallationSuccess = true;
                    result.Message = "PLC compiled and installed successfully. Please restart CNC12 for changes to take effect.";

                    LogSuccess($"✓ PLC installation completed successfully", "PLC");
                    return Ok(result);
                }
                finally
                {
                    // Clean up temp files
                    if (SysIO.File.Exists(tempPlcFilePath))
                    {
                        try { SysIO.File.Delete(tempPlcFilePath); } catch { }
                    }

                    var compiledTempPlc = SysIO.Path.ChangeExtension(tempPlcFilePath, ".plc");
                    if (SysIO.File.Exists(compiledTempPlc))
                    {
                        try { SysIO.File.Delete(compiledTempPlc); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to compile and install PLC: {ex.Message}", "PLC");
                return StatusCode(500, new
                {
                    message = $"Failed to compile and install PLC: {ex.Message}",
                    compilationResult = new { success = false, issues = new string[] { ex.Message } },
                    installationSuccess = false
                });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Compile a PLC source file using the CNC12 compiler
        /// </summary>
        /// <param name="sourceFilePath">Path to the PLC source file</param>
        /// <returns>Compilation result</returns>
        private PLCCompilationResult CompilePLCFile(string sourceFilePath)
        {
            var result = new PLCCompilationResult
            {
                Success = false,
                Issues = new string[0]
            };

            try
            {
                if (!SysIO.File.Exists(_compilerPath))
                {
                    result.Issues = new[] { $"PLC compiler not found at: {_compilerPath}" };
                    LogError($"Compiler not found: {_compilerPath}", "PLC");
                    return result;
                }

                LogInfo($"Running compiler: {_compilerPath} with source: {sourceFilePath}", "PLC");

                var processInfo = new ProcessStartInfo
                {
                    FileName = _compilerPath,
                    Arguments = $"\"{sourceFilePath}\"",
                    WorkingDirectory = SysIO.Path.GetDirectoryName(sourceFilePath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var issues = new List<string>();
                var output = new StringBuilder();
                var errors = new StringBuilder();

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            output.AppendLine(e.Data);
                            LogInfo($"Compiler output: {e.Data}", "PLC");
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errors.AppendLine(e.Data);
                            LogWarning($"Compiler error: {e.Data}", "PLC");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    var exitCode = process.ExitCode;
                    LogInfo($"Compiler exit code: {exitCode}", "PLC");

                    // Parse compiler output for errors and warnings
                    var allOutput = output.ToString() + errors.ToString();
                    var lines = allOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        // Look for common compiler error/warning patterns
                        if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("failed", StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(line.Trim());
                        }
                    }

                    // Check if compiled file was created
                    var compiledFile = SysIO.Path.ChangeExtension(sourceFilePath, ".plc");
                    var compilationSucceeded = SysIO.File.Exists(compiledFile) && exitCode == 0;

                    if (!compilationSucceeded && issues.Count == 0)
                    {
                        // Compilation failed but no specific issues found
                        if (exitCode != 0)
                        {
                            issues.Add($"Compilation failed with exit code: {exitCode}");
                        }
                        if (!SysIO.File.Exists(compiledFile))
                        {
                            issues.Add("Compiled output file was not created");
                        }
                    }

                    result.Success = compilationSucceeded;
                    result.Issues = issues.ToArray();

                    if (result.Success)
                    {
                        LogSuccess($"✓ PLC compilation successful", "PLC");
                    }
                    else
                    {
                        LogError($"PLC compilation failed with {issues.Count} issues", "PLC");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Exception during PLC compilation: {ex.Message}", "PLC");
                result.Issues = new[] { $"Compilation error: {ex.Message}" };
                return result;
            }
        }

        #endregion
    }

    #region Request/Response Models

    /// <summary>
    /// Request to set PLC data
    /// </summary>
    public class SetPLCDataRequest
    {
        /// <summary>
        /// Name of the PLC data file
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// PLC data content as array of lines
        /// </summary>
        public string[] Lines { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Request to compile PLC
    /// </summary>
    public class CompilePLCRequest
    {
        /// <summary>
        /// PLC source code as array of lines
        /// </summary>
        public string[] PlcLines { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Request to compile and install PLC with messages
    /// </summary>
    public class CompileAndInstallPLCRequest
    {
        /// <summary>
        /// PLC source code as array of lines
        /// </summary>
        public string[] PlcLines { get; set; } = Array.Empty<string>();

        /// <summary>
        /// PLC message file content as array of lines
        /// </summary>
        public string[] MessageLines { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Result of PLC compilation
    /// </summary>
    public class PLCCompilationResult
    {
        /// <summary>
        /// Whether compilation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Array of compilation issues (errors/warnings)
        /// </summary>
        public string[] Issues { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Result of PLC compilation and installation
    /// </summary>
    public class PLCInstallationResult
    {
        /// <summary>
        /// Compilation result
        /// </summary>
        public PLCCompilationResult CompilationResult { get; set; } = new PLCCompilationResult();

        /// <summary>
        /// Whether installation was successful
        /// </summary>
        public bool InstallationSuccess { get; set; }

        /// <summary>
        /// Result message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
