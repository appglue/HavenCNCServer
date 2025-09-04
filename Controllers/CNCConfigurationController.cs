using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HavenCNCServer.Models;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Configuration Management - Handles configuration data storage, retrieval, and checkpoint operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCConfigurationController : ControllerBase
    {
        private readonly string _dataDirectory;
        private readonly string _checkpointsDirectory;

        /// <summary>
        /// Initializes a new instance of the CNCConfigurationController
        /// </summary>
        public CNCConfigurationController()
        {
            _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
            _checkpointsDirectory = Path.Combine(_dataDirectory, "checkpoints");
            
            // Ensure directories exist
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(_checkpointsDirectory);
        }

        #region Data Management

        /// <summary>
        /// Get data by name
        /// </summary>
        /// <param name="name">Name of the data to retrieve</param>
        /// <returns>Data content</returns>
        [HttpGet("GetData/{name}")]
        public async Task<IActionResult> GetData(string name)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{name}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = $"Data '{name}' not found" });
                }

                var content = await System.IO.File.ReadAllTextAsync(filePath);
                return Ok(new { name, content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to read data '{name}': {ex.Message}" });
            }
        }

        /// <summary>
        /// Set data with specified name and content
        /// </summary>
        /// <param name="request">Data setting request</param>
        /// <returns>Success response</returns>
        [HttpPost("SetData")]
        public async Task<IActionResult> SetData([FromBody] ConfigurationDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Data name cannot be empty" });
                }

                var filePath = Path.Combine(_dataDirectory, $"{request.Name}.json");
                await System.IO.File.WriteAllTextAsync(filePath, request.Content ?? string.Empty);
                
                return Ok(new { message = $"Data '{request.Name}' saved successfully", name = request.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to save data '{request.Name}': {ex.Message}" });
            }
        }

        /// <summary>
        /// List all available data names
        /// </summary>
        /// <returns>Array of data names</returns>
        [HttpGet("ListData")]
        public IActionResult ListData()
        {
            try
            {
                var files = Directory.GetFiles(_dataDirectory, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();

                return Ok(new { dataNames = files });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to list data: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete data by name
        /// </summary>
        /// <param name="name">Name of the data to delete</param>
        /// <returns>Success response</returns>
        [HttpDelete("DeleteData/{name}")]
        public IActionResult DeleteData(string name)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{name}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = $"Data '{name}' not found" });
                }

                System.IO.File.Delete(filePath);
                return Ok(new { message = $"Data '{name}' deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete data '{name}': {ex.Message}" });
            }
        }

        #endregion

        #region Checkpoint Management

        /// <summary>
        /// Save checkpoint with multiple data items
        /// </summary>
        /// <param name="request">Checkpoint save request</param>
        /// <returns>Success response</returns>
        [HttpPost("SaveCheckpoint")]
        public async Task<IActionResult> SaveCheckpoint([FromBody] CheckpointSaveRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CheckpointName))
                {
                    return BadRequest(new { message = "Checkpoint name cannot be empty" });
                }

                if (request.Data == null || !request.Data.Any())
                {
                    return BadRequest(new { message = "At least one data item is required" });
                }

                var checkpointDir = Path.Combine(_checkpointsDirectory, request.CheckpointName);
                Directory.CreateDirectory(checkpointDir);

                // Save each data item to the checkpoint directory
                foreach (var dataItem in request.Data)
                {
                    if (string.IsNullOrWhiteSpace(dataItem.Name))
                        continue;

                    var filePath = Path.Combine(checkpointDir, $"{dataItem.Name}.json");
                    await System.IO.File.WriteAllTextAsync(filePath, dataItem.Content ?? string.Empty);
                }

                return Ok(new { 
                    message = $"Checkpoint '{request.CheckpointName}' saved successfully", 
                    checkpointName = request.CheckpointName,
                    itemCount = request.Data.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to save checkpoint '{request.CheckpointName}': {ex.Message}" });
            }
        }

        /// <summary>
        /// List all available checkpoints
        /// </summary>
        /// <returns>Array of checkpoint names with metadata</returns>
        [HttpGet("ListCheckpoints")]
        public IActionResult ListCheckpoints()
        {
            try
            {
                var checkpoints = Directory.GetDirectories(_checkpointsDirectory)
                    .Select(dir => {
                        var name = Path.GetFileName(dir);
                        var fileCount = Directory.GetFiles(dir, "*.json").Length;
                        var createdTime = Directory.GetCreationTime(dir);
                        return new { 
                            name, 
                            fileCount, 
                            created = createdTime.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })
                    .OrderByDescending(c => c.created)
                    .ToArray();

                return Ok(new { checkpoints });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to list checkpoints: {ex.Message}" });
            }
        }

        /// <summary>
        /// Restore checkpoint by copying all its data to the main data directory
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint to restore</param>
        /// <returns>Success response</returns>
        [HttpPost("RestoreCheckpoint/{checkpointName}")]
        public async Task<IActionResult> RestoreCheckpoint(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);
                
                if (!Directory.Exists(checkpointDir))
                {
                    return NotFound(new { message = $"Checkpoint '{checkpointName}' not found" });
                }

                var checkpointFiles = Directory.GetFiles(checkpointDir, "*.json");
                var restoredCount = 0;

                foreach (var checkpointFile in checkpointFiles)
                {
                    var fileName = Path.GetFileName(checkpointFile);
                    var targetPath = Path.Combine(_dataDirectory, fileName);
                    
                    var content = await System.IO.File.ReadAllTextAsync(checkpointFile);
                    await System.IO.File.WriteAllTextAsync(targetPath, content);
                    restoredCount++;
                }

                return Ok(new { 
                    message = $"Checkpoint '{checkpointName}' restored successfully", 
                    checkpointName,
                    restoredCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to restore checkpoint '{checkpointName}': {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete checkpoint
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint to delete</param>
        /// <returns>Success response</returns>
        [HttpDelete("DeleteCheckpoint/{checkpointName}")]
        public IActionResult DeleteCheckpoint(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);
                
                if (!Directory.Exists(checkpointDir))
                {
                    return NotFound(new { message = $"Checkpoint '{checkpointName}' not found" });
                }

                Directory.Delete(checkpointDir, true);
                return Ok(new { message = $"Checkpoint '{checkpointName}' deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete checkpoint '{checkpointName}': {ex.Message}" });
            }
        }

        /// <summary>
        /// Get checkpoint contents (list of data items in checkpoint)
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint</param>
        /// <returns>Array of data items in the checkpoint</returns>
        [HttpGet("GetCheckpointContents/{checkpointName}")]
        public async Task<IActionResult> GetCheckpointContents(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);
                
                if (!Directory.Exists(checkpointDir))
                {
                    return NotFound(new { message = $"Checkpoint '{checkpointName}' not found" });
                }

                var files = Directory.GetFiles(checkpointDir, "*.json");
                var contents = new List<object>();

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var content = await System.IO.File.ReadAllTextAsync(file);
                    contents.Add(new { name, content });
                }

                return Ok(new { checkpointName, data = contents.ToArray() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to get checkpoint contents '{checkpointName}': {ex.Message}" });
            }
        }

        #endregion
    }
}
