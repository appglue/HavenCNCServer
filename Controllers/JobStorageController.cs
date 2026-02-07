using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobStorageController : ControllerBase
    {
        private static JobFileManager? _jobFileManager;
        private static GCodeFileManager? _gcodeFileManager;
        private static MongoDbService? _mongoDbService;
        private static string? _machineName;
        private readonly ILogger<JobStorageController> _logger;

        public JobStorageController(ILogger<JobStorageController> logger)
        {
            _logger = logger;
        }

        public static async Task InitializeAsync()
        {
            try
            {
                LogInfo("🔄 JobStorageController.InitializeAsync CALLED", "JobStorage");

                _jobFileManager = new JobFileManager(null);
                _gcodeFileManager = new GCodeFileManager(null);
                LogInfo("✓ File managers created", "JobStorage");

                // Load MongoDB settings
                var mongoSettings = SettingsManager.Settings.MongoDB;
                if (mongoSettings != null && mongoSettings.Enabled)
                {
                    LogInfo($"MongoDB settings loaded: Enabled={mongoSettings.Enabled}", "JobStorage");
                    _mongoDbService = new MongoDbService(mongoSettings);
                }
                else
                {
                    LogWarning("MongoDB settings not found or disabled", "JobStorage");
                }

                // Load machine name
                var settingsPath = Path.Combine(@"C:\havencncdata", "machineDataStorageSettings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                    _machineName = localSettings?.CurrentMachineName ?? Environment.MachineName;
                }
                else
                {
                    _machineName = Environment.MachineName;
                }
                LogInfo($"Machine name: {_machineName}", "JobStorage");

                // Initial sync - download recent jobs and gcode files
                LogInfo("Starting initial sync...", "JobStorage");
                await SyncRecentItemsAsync();

                LogSuccess("✓ JobStorageController initialized", "JobStorage");
            }
            catch (Exception ex)
            {
                LogError($"JobStorageController initialization failed: {ex.Message}", "JobStorage");
                LogError($"Stack trace: {ex.StackTrace}", "JobStorage");
            }
        }

        private static async Task SyncRecentItemsAsync()
        {
            if (_mongoDbService == null || !_mongoDbService.IsConnected || _jobFileManager == null || _gcodeFileManager == null || _machineName == null)
            {
                LogInfo("Skipping MongoDB sync (not connected or not initialized)", "JobStorage");
                return;
            }

            try
            {
                // Download last 20 jobs
                LogInfo("Syncing recent jobs from MongoDB...", "JobStorage");
                var recentJobs = await _mongoDbService.GetRecentJobsAsync(_machineName, 20);
                int jobsSynced = 0;
                foreach (var job in recentJobs)
                {
                    var localVersion = await _jobFileManager.GetVersionAsync(job.JobId);
                    if (job.Version > localVersion)
                    {
                        await _jobFileManager.WriteAsync(job.JobId, job.Data);
                        await _jobFileManager.WriteVersionAsync(job.JobId, job.Version);
                        jobsSynced++;
                    }
                }
                LogInfo($"✓ Synced {jobsSynced} jobs from MongoDB", "JobStorage");

                // Download last 20 gcode files
                LogInfo("Syncing recent G-code files from MongoDB...", "JobStorage");
                var recentFiles = await _mongoDbService.GetRecentGCodeFilesAsync(_machineName, 20);
                int filesSynced = 0;
                foreach (var file in recentFiles)
                {
                    var localVersion = await _gcodeFileManager.GetVersionAsync(file.FileId);
                    if (file.Version > localVersion)
                    {
                        await _gcodeFileManager.WriteManagedAsync(file.FileId, file.Data);
                        await _gcodeFileManager.WriteVersionAsync(file.FileId, file.Version);
                        filesSynced++;
                    }
                }
                LogInfo($"✓ Synced {filesSynced} G-code files from MongoDB", "JobStorage");
            }
            catch (Exception ex)
            {
                LogError($"Sync failed: {ex.Message}", "JobStorage");
            }
        }

        // ========== Job Endpoints ==========

        /// <summary>
        /// List all jobs with paging and sorting
        /// POST /api/JobStorage/jobs/list
        /// </summary>
        [HttpPost("jobs/list")]
        [ProducesResponseType(typeof(PagedResult<JobMetadata>), 200)]
        public async Task<IActionResult> ListJobs([FromBody] PageRequest request)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                var allJobs = new List<JobMetadata>();

                // 1. Get jobs from MongoDB
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoJobs = await _mongoDbService.ListJobsAsync(_machineName);
                    foreach (var doc in mongoJobs)
                    {
                        if (doc.Metadata != null)
                        {
                            allJobs.Add(doc.Metadata);
                        }
                    }
                }

                // 2. Get jobs from local storage (not in MongoDB)
                var localJobIds = _jobFileManager.GetAllJobIds();
                foreach (var jobId in localJobIds)
                {
                    if (allJobs.Any(j => j.Name == jobId))
                        continue;  // Already have from MongoDB

                    var data = await _jobFileManager.ReadAsync(jobId);
                    if (data != null)
                    {
                        var metadata = ExtractJobMetadata(jobId, data);
                        allJobs.Add(metadata);
                    }
                }

                // Apply sorting
                var sorted = ApplySorting(allJobs, request.SortBy, request.SortDirection);

                // Apply paging
                var totalCount = sorted.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var paged = sorted.Skip(skip).Take(request.PageSize).ToList();

                return Ok(new PagedResult<JobMetadata>
                {
                    Items = paged,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing jobs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Fetch a specific job by ID
        /// GET /api/JobStorage/jobs/{id}
        /// </summary>
        [HttpGet("jobs/{id}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FetchJob(string id)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                // Try MongoDB first
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoDoc = await _mongoDbService.LoadJobAsync(id, _machineName);
                    if (mongoDoc != null)
                    {
                        return Ok(mongoDoc.Data);
                    }
                }

                // Fallback to local
                var localData = await _jobFileManager.ReadAsync(id);
                if (localData != null)
                {
                    return Ok(localData);
                }

                return NotFound(new { error = $"Job not found: {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job: {JobId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Store a job
        /// POST /api/JobStorage/jobs
        /// </summary>
        [HttpPost("jobs")]
        [ProducesResponseType(typeof(StoreResponse), 200)]
        public async Task<IActionResult> StoreJob([FromBody] StoreJobRequest request)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                if (string.IsNullOrEmpty(request.JobId) || string.IsNullOrEmpty(request.Data))
                {
                    return BadRequest(new { error = "JobId and Data are required" });
                }

                // Extract or use provided metadata
                var metadata = request.Metadata ?? ExtractJobMetadata(request.JobId, request.Data);

                // Write to local first
                var version = await _jobFileManager.IncrementAndWriteAsync(request.JobId, request.Data);

                // Sync to MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.SaveJobAsync(request.JobId, machineName, request.Data, version, metadata);
                    });
                }

                return Ok(new StoreResponse
                {
                    Success = true,
                    Id = request.JobId,
                    Message = $"Job stored successfully with version {version}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing job: {JobId}", request.JobId);
                return StatusCode(500, new StoreResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a job
        /// DELETE /api/JobStorage/jobs/{id}
        /// </summary>
        [HttpDelete("jobs/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteJob(string id)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                var localDeleted = await _jobFileManager.DeleteAsync(id);

                // Delete from MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.DeleteJobAsync(id, machineName);
                    });
                }

                if (localDeleted)
                {
                    return Ok(new { message = $"Job deleted: {id}" });
                }
                else
                {
                    return NotFound(new { error = $"Job not found: {id}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job: {JobId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get the last executed job ID
        /// GET /api/JobStorage/jobs/last
        /// </summary>
        [HttpGet("jobs/last")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLastJob()
        {
            try
            {
                var lastJobPath = Path.Combine(@"C:\havencncdata\jobs", "lastJob.txt");
                if (System.IO.File.Exists(lastJobPath))
                {
                    var jobId = await System.IO.File.ReadAllTextAsync(lastJobPath);
                    return Ok(jobId.Trim());
                }
                return NotFound(new { error = "No last job found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last job");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Save the last executed job ID
        /// POST /api/JobStorage/jobs/last
        /// </summary>
        [HttpPost("jobs/last")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SaveLastJob([FromBody] SaveLastJobRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.JobId))
                {
                    return BadRequest(new { error = "JobId is required" });
                }

                var lastJobPath = Path.Combine(@"C:\havencncdata\jobs", "lastJob.txt");
                await System.IO.File.WriteAllTextAsync(lastJobPath, request.JobId);
                return Ok(new { message = "Last job ID saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving last job");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== G-Code File Endpoints ==========

        /// <summary>
        /// List G-code files from multiple sources (external dirs + managed + MongoDB)
        /// POST /api/JobStorage/gcode/list
        /// </summary>
        [HttpPost("gcode/list")]
        [ProducesResponseType(typeof(PagedResult<GCodeFileMetadata>), 200)]
        public async Task<IActionResult> ListGCodeFiles([FromBody] ListGCodeFilesRequest request)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                var allFiles = new List<GCodeFileMetadata>();

                // 1. Scan external directories
                if (request.Directories != null)
                {
                    foreach (var directory in request.Directories)
                    {
                        var externalFiles = _gcodeFileManager.ScanExternalDirectory(directory);
                        allFiles.AddRange(externalFiles);
                    }
                }

                // 2. Get managed files from local directory
                var managedFiles = _gcodeFileManager.GetManagedFileMetadata();
                allFiles.AddRange(managedFiles);

                // 3. Get files from MongoDB (may have additional metadata)
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoFiles = await _mongoDbService.ListGCodeFilesAsync(_machineName);
                    foreach (var doc in mongoFiles)
                    {
                        // Check if already in list from local managed directory
                        var existing = allFiles.FirstOrDefault(f => f.FileId == doc.FileId);
                        if (existing != null)
                        {
                            // Update with MongoDB metadata
                            existing.Category = doc.Category;
                            existing.Description = doc.Description;
                            existing.MaterialType = doc.MaterialType;
                            existing.EstimatedTime = doc.EstimatedTime;
                        }
                        else
                        {
                            // Add from MongoDB only (not in local managed)
                            allFiles.Add(new GCodeFileMetadata
                            {
                                FileId = doc.FileId,
                                Name = doc.FileName,
                                Directory = "mongodb",
                                Category = doc.Category,
                                Description = doc.Description,
                                MaterialType = doc.MaterialType,
                                EstimatedTime = doc.EstimatedTime,
                                LastModified = doc.Timestamp,
                                Size = doc.Size,
                                IsManaged = true
                            });
                        }
                    }
                }

                // Apply sorting
                var sorted = ApplySorting(allFiles, request.Paging.SortBy, request.Paging.SortDirection);

                // Apply paging
                var totalCount = sorted.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.Paging.PageSize);
                var skip = (request.Paging.Page - 1) * request.Paging.PageSize;
                var paged = sorted.Skip(skip).Take(request.Paging.PageSize).ToList();

                return Ok(new PagedResult<GCodeFileMetadata>
                {
                    Items = paged,
                    TotalCount = totalCount,
                    Page = request.Paging.Page,
                    PageSize = request.Paging.PageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing G-code files");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Fetch G-code file content
        /// GET /api/JobStorage/gcode?fileId={id}&directory={dir}&fileName={name}
        /// </summary>
        [HttpGet("gcode")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FetchGCodeFile([FromQuery] string? fileId, [FromQuery] string? directory, [FromQuery] string? fileName)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                // If fileId provided, fetch managed file
                if (!string.IsNullOrEmpty(fileId))
                {
                    // Try MongoDB first
                    if (_mongoDbService != null && _mongoDbService.IsConnected)
                    {
                        var mongoDoc = await _mongoDbService.LoadGCodeFileAsync(fileId, _machineName);
                        if (mongoDoc != null)
                        {
                            return Ok(mongoDoc.Data);
                        }
                    }

                    // Fallback to local managed
                    var managedData = await _gcodeFileManager.ReadManagedAsync(fileId);
                    if (managedData != null)
                    {
                        return Ok(managedData);
                    }

                    return NotFound(new { error = $"Managed G-code file not found: {fileId}" });
                }

                // If directory and fileName provided, fetch external file
                if (!string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(fileName))
                {
                    var externalData = await _gcodeFileManager.ReadExternalAsync(directory, fileName);
                    if (externalData != null)
                    {
                        return Ok(externalData);
                    }

                    return NotFound(new { error = $"External G-code file not found: {directory}/{fileName}" });
                }

                return BadRequest(new { error = "Either fileId or (directory + fileName) must be provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching G-code file");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Store G-code file (creates managed file)
        /// POST /api/JobStorage/gcode
        /// </summary>
        [HttpPost("gcode")]
        [ProducesResponseType(typeof(StoreResponse), 200)]
        public async Task<IActionResult> StoreGCodeFile([FromBody] StoreGCodeFileRequest request)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                if (string.IsNullOrEmpty(request.FileName) || string.IsNullOrEmpty(request.Data))
                {
                    return BadRequest(new { error = "FileName and Data are required" });
                }

                // Generate fileId if not provided
                var fileId = request.FileId ?? Guid.NewGuid().ToString();

                // Write to local managed directory first
                var version = await _gcodeFileManager.IncrementAndWriteAsync(fileId, request.Data);

                // Sync to MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.SaveGCodeFileAsync(
                            fileId,
                            request.FileName,
                            machineName,
                            request.Data,
                            version,
                            request.Category,
                            request.Description,
                            request.MaterialType,
                            request.EstimatedTime);
                    });
                }

                return Ok(new StoreResponse
                {
                    Success = true,
                    Id = fileId,
                    Message = $"G-code file stored successfully with version {version}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing G-code file");
                return StatusCode(500, new StoreResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete managed G-code file
        /// DELETE /api/JobStorage/gcode?fileId={id}
        /// </summary>
        [HttpDelete("gcode")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGCodeFile([FromQuery] string fileId)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                if (string.IsNullOrEmpty(fileId))
                {
                    return BadRequest(new { error = "fileId is required" });
                }

                var localDeleted = await _gcodeFileManager.DeleteManagedAsync(fileId);

                // Delete from MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.DeleteGCodeFileAsync(fileId, machineName);
                    });
                }

                if (localDeleted)
                {
                    return Ok(new { message = $"G-code file deleted: {fileId}" });
                }
                else
                {
                    return NotFound(new { error = $"G-code file not found: {fileId}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting G-code file: {FileId}", fileId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Helper Methods ==========

        private JobMetadata ExtractJobMetadata(string jobId, string jobData)
        {
            try
            {
                // Try to parse job JSON to extract metadata
                using var doc = JsonDocument.Parse(jobData);
                var root = doc.RootElement;

                return new JobMetadata
                {
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? jobId : jobId,
                    ExecutionCount = root.TryGetProperty("executionCount", out var execCount) ? execCount.GetInt32() : 0,
                    LastRunDate = root.TryGetProperty("lastRunDate", out var lastRun) ? lastRun.GetDateTime() : (DateTime?)null,
                    Category = root.TryGetProperty("category", out var category) ? category.GetString() : null,
                    Size = jobData.Length,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = root.TryGetProperty("createdAt", out var created) ? created.GetDateTime() : DateTime.UtcNow,
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    MaterialType = root.TryGetProperty("materialType", out var material) ? material.GetString() : null,
                    EstimatedTime = root.TryGetProperty("estimatedTime", out var time) ? time.GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting job metadata from {JobId}", jobId);
                return new JobMetadata
                {
                    Name = jobId,
                    Size = jobData.Length,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }

        private List<T> ApplySorting<T>(List<T> items, string? sortBy, string? sortDirection)
        {
            if (string.IsNullOrEmpty(sortBy))
                return items;

            var isDescending = sortDirection?.ToLower() == "desc";

            var property = typeof(T).GetProperty(sortBy, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property == null)
                return items;

            return isDescending
                ? items.OrderByDescending(x => property.GetValue(x)).ToList()
                : items.OrderBy(x => property.GetValue(x)).ToList();
        }
    }
}
