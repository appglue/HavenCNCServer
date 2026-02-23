using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// In-memory metadata cache — populated on store/sync, invalidated on delete.
        /// Max 500 jobs means this stays small and re-reads are never needed for listing.
        /// </summary>
        private static readonly Dictionary<string, JobMetadata> _jobMetadataCache = new();

        public static async Task InitializeAsync()
        {
            try
            {
                LogInfo("🔄 JobStorageController.InitializeAsync CALLED", "JobStorage");

                _jobFileManager = new JobFileManager();
                _gcodeFileManager = new GCodeFileManager();
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

        /// <summary>
        /// Called after a machine switch. Reloads the machine name from settings,
        /// clears the in-memory cache, and re-syncs jobs/gcode from MongoDB.
        /// </summary>
        public static async Task ReinitializeAsync()
        {
            try
            {
                LogInfo("🔄 JobStorageController.ReinitializeAsync — reloading for new machine", "JobStorage");

                // Clear in-memory cache
                _jobMetadataCache.Clear();

                // Recreate file managers (pointing at same directories — contents were wiped by caller)
                _jobFileManager = new JobFileManager();
                _gcodeFileManager = new GCodeFileManager();

                // Reload machine name from settings
                var settingsPath = Path.Combine(@"C:\havencncdata", "machineDataStorageSettings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                    _machineName = localSettings?.CurrentMachineName ?? Environment.MachineName;
                }

                LogInfo($"New machine name: {_machineName}", "JobStorage");

                // Sync jobs and gcode for the new machine
                await SyncRecentItemsAsync();

                LogSuccess($"✓ JobStorageController reinitialized for machine '{_machineName}'", "JobStorage");
            }
            catch (Exception ex)
            {
                LogError($"JobStorageController reinitialization failed: {ex.Message}", "JobStorage");
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
                // ---- Step 0: flush pending local tombstones to MongoDB ----
                var pendingTombstones = _jobFileManager.GetPendingTombstones();
                if (pendingTombstones.Length > 0)
                {
                    LogInfo($"Processing {pendingTombstones.Length} pending job deletion(s) against MongoDB...", "JobStorage");
                    foreach (var tombstoneId in pendingTombstones)
                    {
                        try
                        {
                            await _mongoDbService.DeleteJobAsync(tombstoneId, _machineName);
                            _jobFileManager.RemoveTombstone(tombstoneId);
                            LogInfo($"  ✓ Deleted {tombstoneId} from MongoDB and removed tombstone", "JobStorage");
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"  ✗ Could not delete {tombstoneId} from MongoDB (will retry next sync): {ex.Message}", "JobStorage");
                        }
                    }
                }

                // Sync all jobs from MongoDB (up to our 500-job cap).
                // Using ListJobsAsync instead of GetRecentJobsAsync(20) ensures a new/reinstalled
                // machine gets all its jobs back, not just the most-recent 20.
                LogInfo("Syncing all jobs from MongoDB...", "JobStorage");
                var recentJobs = await _mongoDbService.ListJobsAsync(_machineName);
                int jobsSynced = 0;
                foreach (var job in recentJobs)
                {
                    // Don't re-download a job the user has already deleted locally
                    if (_jobFileManager.IsTombstoned(job.JobId))
                        continue;

                    var localVersion = await _jobFileManager.GetVersionAsync(job.JobId);
                    if (job.Version > localVersion && job.Data != null)
                    {
                        await _jobFileManager.WriteAsync(job.JobId, job.Data);
                        await _jobFileManager.WriteVersionAsync(job.JobId, job.Version);
                        jobsSynced++;
                    }

                    // Populate cache from MongoDB metadata if available
                    if (job.Metadata != null)
                    {
                        job.Metadata.Id = job.JobId;
                        _jobMetadataCache[job.JobId] = job.Metadata;
                    }
                    else if (!_jobMetadataCache.ContainsKey(job.JobId) && job.Data != null)
                    {
                        _jobMetadataCache[job.JobId] = ExtractJobMetadata(job.JobId, job.Data);
                    }
                }
                LogInfo($"✓ Synced {jobsSynced} job(s) from MongoDB ({recentJobs.Count} total on server)", "JobStorage");

                // Sync all managed G-code files from MongoDB
                LogInfo("Syncing all G-code files from MongoDB...", "JobStorage");
                var recentFiles = await _mongoDbService.ListGCodeFilesAsync(_machineName);
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
                LogInfo($"✓ Synced {filesSynced} G-code file(s) from MongoDB ({recentFiles.Count} total on server)", "JobStorage");

                // ---- Heal: stamp version=0 on any local job that is missing its version file ----
                // This can happen if jobs were written by old code paths or if the process
                // crashed between WriteAsync and WriteVersionAsync.
                var allLocalIds = _jobFileManager.GetAllJobIds();
                int healed = 0;
                foreach (var localId in allLocalIds)
                {
                    if (!_jobFileManager.HasVersionFile(localId))
                    {
                        await _jobFileManager.WriteVersionAsync(localId, 0);
                        healed++;
                        LogInfo($"Healed missing version file for job: {localId}", "JobStorage");
                    }
                }
                if (healed > 0)
                    LogInfo($"✓ Healed {healed} job(s) with missing version files", "JobStorage");
            }
            catch (Exception ex)
            {
                LogError($"Sync failed: {ex.Message}", "JobStorage");
            }
        }

        // ========== Job Endpoints ==========

        /// <summary>
        /// List all jobs (flat array, served from in-memory cache)
        /// POST /api/JobStorage/jobs/list
        /// </summary>
        [HttpPost("jobs/list")]
        [ProducesResponseType(typeof(List<JobMetadata>), 200)]
        public async Task<IActionResult> ListJobs([FromBody] ListJobsRequest request)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                // Ensure all local jobs are in the cache.
                // We always check (not just when empty) because sync may have only
                // populated the most-recent 20 from MongoDB — older local-only jobs
                // would be silently omitted if we stopped at Count == 0.
                var localJobIds = _jobFileManager.GetAllJobIds();
                foreach (var jobId in localJobIds)
                {
                    if (_jobMetadataCache.ContainsKey(jobId)) continue;
                    var data = await _jobFileManager.ReadAsync(jobId);
                    if (data != null)
                        _jobMetadataCache[jobId] = ExtractJobMetadata(jobId, data);
                }

                var allJobs = _jobMetadataCache.Values.ToList();

                // Apply sorting
                var sorted = ApplySorting(allJobs, request.SortBy, request.SortDirection);

                // Apply maxCount cap
                var maxCount = request.MaxCount ?? 500;
                var result = sorted.Take(maxCount).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log($"Error listing jobs: {ex.Message}", LogLevel.Error, "JobStorageController");
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
                Log($"Error fetching job {id}: {ex.Message}", LogLevel.Error, "JobStorageController");
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

                // Upsert metadata cache
                metadata.Id = request.JobId;
                _jobMetadataCache[request.JobId] = metadata;

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

                // Fire-and-forget prune so it doesn't slow down the store response
                _ = Task.Run(() => PruneOldJobsAsync());

                return Ok(new StoreResponse
                {
                    Success = true,
                    Id = request.JobId,
                    Message = $"Job stored successfully with version {version}"
                });
            }
            catch (Exception ex)
            {
                Log($"Error storing job {request.JobId}: {ex.Message}", LogLevel.Error, "JobStorageController");
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

                // Remove from metadata cache immediately
                _jobMetadataCache.Remove(id);

                // Attempt immediate MongoDB delete; tombstone ensures retry on next sync if offline
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    try
                    {
                        await _mongoDbService.DeleteJobAsync(id, _machineName);
                        // Deletion confirmed — remove tombstone so sync doesn't try again
                        _jobFileManager.RemoveTombstone(id);
                    }
                    catch (Exception mongoEx)
                    {
                        Log($"MongoDB delete failed for job {id} (tombstone retained for retry): {mongoEx.Message}", LogLevel.Warning, "JobStorageController");
                    }
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
                Log($"Error deleting job {id}: {ex.Message}", LogLevel.Error, "JobStorageController");
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
                Log($"Error getting last job: {ex.Message}", LogLevel.Error, "JobStorageController");
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
                Log($"Error saving last job: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== G-Code File Endpoints ==========

        /// <summary>
        /// List G-code files from multiple sources (external dirs + managed + MongoDB)
        /// POST /api/JobStorage/gcode/list
        /// </summary>
        [HttpPost("gcode/list")]
        [ProducesResponseType(typeof(List<GCodeFileMetadata>), 200)]
        public async Task<IActionResult> ListGCodeFiles([FromBody] ListGCodeFilesRequest request)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                var allFiles = new List<GCodeFileMetadata>();

                // 1. Scan external directories (includes LineCount from file content)
                if (request.Directories != null)
                {
                    foreach (var directory in request.Directories)
                    {
                        var externalFiles = _gcodeFileManager.ScanExternalDirectory(directory, request.FileExtensions);
                        // Populate LineCount for each external file
                        foreach (var f in externalFiles)
                        {
                            try
                            {
                                var content = await _gcodeFileManager.ReadExternalAsync(f.Directory, f.Name);
                                f.LineCount = CountLines(content);
                            }
                            catch { /* non-critical */ }
                        }
                        allFiles.AddRange(externalFiles);
                    }
                }

                // 2. Get managed files from local directory (includes LineCount)
                var managedFiles = _gcodeFileManager.GetManagedFileMetadata();
                foreach (var f in managedFiles)
                {
                    try
                    {
                        var content = await _gcodeFileManager.ReadManagedAsync(f.FileId!);
                        f.LineCount = CountLines(content);
                    }
                    catch { /* non-critical */ }
                }
                allFiles.AddRange(managedFiles);

                // 3. Get files from MongoDB (may have additional metadata)
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoFiles = await _mongoDbService.ListGCodeFilesAsync(_machineName);
                    foreach (var doc in mongoFiles)
                    {
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
                            var lineCount = 0;
                            try { lineCount = CountLines(doc.Data); } catch { }
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
                                IsManaged = true,
                                LineCount = lineCount
                            });
                        }
                    }
                }

                // Apply sorting and return flat array
                var sorted = ApplySorting(allFiles, request.SortBy, request.SortDirection);
                return Ok(sorted);
            }
            catch (Exception ex)
            {
                Log($"Error listing G-code files: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Fetch G-code file — returns metadata + content combined
        /// GET /api/JobStorage/gcode?fileId={id}&directory={dir}&fileName={name}
        /// </summary>
        [HttpGet("gcode")]
        [ProducesResponseType(typeof(GCodeFileData), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FetchGCodeFile([FromQuery] string? fileId, [FromQuery] string? directory, [FromQuery] string? fileName)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                // ---- Managed file (by fileId) ----
                if (!string.IsNullOrEmpty(fileId))
                {
                    string? content = null;
                    GCodeFileDocument? mongoDoc = null;

                    if (_mongoDbService != null && _mongoDbService.IsConnected)
                    {
                        mongoDoc = await _mongoDbService.LoadGCodeFileAsync(fileId, _machineName);
                        content = mongoDoc?.Data;
                    }

                    if (content == null)
                        content = await _gcodeFileManager.ReadManagedAsync(fileId);

                    if (content == null)
                        return NotFound(new { error = $"Managed G-code file not found: {fileId}" });

                    var fileInfo = _gcodeFileManager.GetManagedFileMetadata().FirstOrDefault(f => f.FileId == fileId);

                    return Ok(new GCodeFileData
                    {
                        FileId = fileId,
                        Name = mongoDoc?.FileName ?? fileInfo?.Name ?? fileId,
                        Directory = "managed",
                        Content = content,
                        Category = mongoDoc?.Category ?? fileInfo?.Category,
                        Description = mongoDoc?.Description ?? fileInfo?.Description,
                        MaterialType = mongoDoc?.MaterialType ?? fileInfo?.MaterialType,
                        EstimatedTime = mongoDoc?.EstimatedTime ?? fileInfo?.EstimatedTime,
                        LastModified = fileInfo?.LastModified ?? (mongoDoc?.Timestamp ?? DateTime.UtcNow),
                        Size = content.Length,
                        LineCount = CountLines(content),
                        IsManaged = true
                    });
                }

                // ---- External file (by directory + fileName) ----
                if (!string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(fileName))
                {
                    var content = await _gcodeFileManager.ReadExternalAsync(directory, fileName);
                    if (content == null)
                        return NotFound(new { error = $"External G-code file not found: {directory}/{fileName}" });

                    var fullPath = Path.Combine(directory, fileName);
                    var fileInfo = System.IO.File.Exists(fullPath) ? new System.IO.FileInfo(fullPath) : null;

                    return Ok(new GCodeFileData
                    {
                        FileId = null,
                        Name = fileName,
                        Directory = directory,
                        Content = content,
                        LastModified = fileInfo?.LastWriteTimeUtc ?? DateTime.UtcNow,
                        Size = content.Length,
                        LineCount = CountLines(content),
                        IsManaged = false
                    });
                }

                return BadRequest(new { error = "Either fileId or (directory + fileName) must be provided" });
            }
            catch (Exception ex)
            {
                Log($"Error fetching G-code file: {ex.Message}", LogLevel.Error, "JobStorageController");
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
                Log($"Error storing G-code file: {ex.Message}", LogLevel.Error, "JobStorageController");
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
                Log($"Error deleting G-code file {fileId}: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Category Endpoints ==========

        /// <summary>
        /// Update a job's category (lightweight — no full re-fetch needed)
        /// PUT /api/JobStorage/jobs/{id}/category
        /// </summary>
        [HttpPut("jobs/{id}/category")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateJobCategory(string id, [FromBody] UpdateJobCategoryRequest request)
        {
            try
            {
                if (_jobFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "Job storage not initialized" });

                var data = await _jobFileManager.ReadAsync(id);
                if (data == null)
                    return NotFound(new { error = $"Job not found: {id}" });

                // Patch the category field in the JSON
                using var doc = JsonDocument.Parse(data);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data) ?? new();
                dict["category"] = JsonSerializer.SerializeToElement(request.Category);
                var updatedData = JsonSerializer.Serialize(dict);

                var version = await _jobFileManager.IncrementAndWriteAsync(id, updatedData);

                // Update cache
                if (_jobMetadataCache.TryGetValue(id, out var cached))
                {
                    cached.Category = request.Category;
                }
                else
                {
                    var meta = ExtractJobMetadata(id, updatedData);
                    _jobMetadataCache[id] = meta;
                }

                // Background MongoDB sync
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    var meta = _jobMetadataCache[id];
                    _ = Task.Run(async () =>
                    {
                        await mongoService.SaveJobAsync(id, machineName, updatedData, version, meta);
                    });
                }

                return Ok(new { message = $"Category updated for job {id}" });
            }
            catch (Exception ex)
            {
                Log($"Error updating category for job {id}: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update a G-code file's category
        /// PUT /api/JobStorage/gcode/category
        /// </summary>
        [HttpPut("gcode/category")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGCodeCategory([FromBody] UpdateGCodeCategoryRequest request)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                // Managed file
                if (!string.IsNullOrEmpty(request.FileId))
                {
                    var exists = await _gcodeFileManager.ReadManagedAsync(request.FileId) != null;
                    if (!exists && (_mongoDbService == null || !_mongoDbService.IsConnected))
                        return NotFound(new { error = $"G-code file not found: {request.FileId}" });

                    // Update MongoDB metadata only (category is not embedded in the .nc content)
                    if (_mongoDbService != null && _mongoDbService.IsConnected)
                    {
                        var doc = await _mongoDbService.LoadGCodeFileAsync(request.FileId, _machineName);
                        if (doc != null)
                        {
                            await _mongoDbService.SaveGCodeFileAsync(
                                doc.FileId, doc.FileName, _machineName, doc.Data, doc.Version + 1,
                                request.Category, doc.Description, doc.MaterialType, doc.EstimatedTime);
                        }
                    }

                    return Ok(new { message = $"Category updated for G-code file {request.FileId}" });
                }

                // External file — category stored in MongoDB only
                if (!string.IsNullOrEmpty(request.Directory) && !string.IsNullOrEmpty(request.FileName))
                {
                    if (_mongoDbService != null && _mongoDbService.IsConnected)
                    {
                        var mongoFiles = await _mongoDbService.ListGCodeFilesAsync(_machineName);
                        var match = mongoFiles.FirstOrDefault(f => f.FileName == request.FileName);
                        if (match != null)
                        {
                            await _mongoDbService.SaveGCodeFileAsync(
                                match.FileId, match.FileName, _machineName, match.Data, match.Version + 1,
                                request.Category, match.Description, match.MaterialType, match.EstimatedTime);
                            return Ok(new { message = $"Category updated for external G-code file {request.FileName}" });
                        }
                    }
                    return NotFound(new { error = $"External G-code file not found in MongoDB: {request.FileName}" });
                }

                return BadRequest(new { error = "Either fileId or (directory + fileName) must be provided" });
            }
            catch (Exception ex)
            {
                Log($"Error updating G-code category: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Search Endpoints ==========

        /// <summary>
        /// Full-text search across job metadata (searches in-memory cache)
        /// POST /api/JobStorage/jobs/search
        /// </summary>
        [HttpPost("jobs/search")]
        [ProducesResponseType(typeof(List<JobMetadata>), 200)]
        public IActionResult SearchJobs([FromBody] JobSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                    return Ok(new List<JobMetadata>());

                var q = request.Query.Trim().ToLowerInvariant();

                var results = _jobMetadataCache.Values
                    .Where(j =>
                        (j.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (j.Category?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (j.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (j.MaterialType?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
                    .OrderByDescending(j => j.LastModified)
                    .Take(request.MaxCount)
                    .ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                Log($"Error searching jobs: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Full-text search across G-code file metadata
        /// POST /api/JobStorage/gcode/search
        /// </summary>
        [HttpPost("gcode/search")]
        [ProducesResponseType(typeof(List<GCodeFileMetadata>), 200)]
        public async Task<IActionResult> SearchGCodeFiles([FromBody] GCodeSearchRequest request)
        {
            try
            {
                if (_gcodeFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "G-code storage not initialized" });

                if (string.IsNullOrWhiteSpace(request.Query))
                    return Ok(new List<GCodeFileMetadata>());

                var q = request.Query.Trim();

                // Build metadata list from managed + requested external directories
                var allFiles = new List<GCodeFileMetadata>();

                var managedFiles = _gcodeFileManager.GetManagedFileMetadata();
                allFiles.AddRange(managedFiles);

                if (request.Directories != null)
                {
                    var extensions = request.FileExtensions ?? new[] { ".nc", ".txt", ".tap" };
                    foreach (var dir in request.Directories)
                    {
                        var externalFiles = _gcodeFileManager.ScanExternalDirectory(dir, extensions);
                        allFiles.AddRange(externalFiles);
                    }
                }

                // Augment managed files with MongoDB metadata
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoFiles = await _mongoDbService.ListGCodeFilesAsync(_machineName);
                    foreach (var doc in mongoFiles)
                    {
                        var existing = allFiles.FirstOrDefault(f => f.FileId == doc.FileId);
                        if (existing != null)
                        {
                            existing.Category = doc.Category;
                            existing.Description = doc.Description;
                            existing.MaterialType = doc.MaterialType;
                        }
                        else
                        {
                            allFiles.Add(new GCodeFileMetadata
                            {
                                FileId = doc.FileId,
                                Name = doc.FileName,
                                Directory = "mongodb",
                                Category = doc.Category,
                                Description = doc.Description,
                                MaterialType = doc.MaterialType,
                                LastModified = doc.Timestamp,
                                Size = doc.Size,
                                IsManaged = true
                            });
                        }
                    }
                }

                var results = allFiles
                    .Where(f =>
                        (f.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (f.Category?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (f.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                        (f.MaterialType?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
                    .OrderByDescending(f => f.LastModified)
                    .Take(request.MaxCount)
                    .ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                Log($"Error searching G-code files: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Filesystem Navigation Endpoints ==========

        /// <summary>
        /// Get all available drives
        /// GET /api/JobStorage/filesystem/drives
        /// </summary>
        [HttpGet("filesystem/drives")]
        [ProducesResponseType(typeof(List<DriveInfoResponse>), 200)]
        public IActionResult GetDrives()
        {
            try
            {
                var drives = new List<DriveInfoResponse>();
                var allDrives = DriveInfo.GetDrives();

                foreach (var drive in allDrives)
                {
                    try
                    {
                        drives.Add(new DriveInfoResponse
                        {
                            Name = drive.Name,
                            Label = drive.IsReady ? (drive.VolumeLabel ?? string.Empty) : string.Empty,
                            DriveType = drive.DriveType.ToString(),
                            TotalSize = drive.IsReady ? drive.TotalSize : 0,
                            AvailableSpace = drive.IsReady ? drive.AvailableFreeSpace : 0,
                            IsReady = drive.IsReady
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading drive info {drive.Name}: {ex.Message}", LogLevel.Warning, "JobStorageController");
                        // Add drive with minimal info
                        drives.Add(new DriveInfoResponse
                        {
                            Name = drive.Name,
                            DriveType = drive.DriveType.ToString(),
                            IsReady = false
                        });
                    }
                }

                return Ok(drives);
            }
            catch (Exception ex)
            {
                Log($"Error getting drives: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get subdirectories for a given path
        /// GET /api/JobStorage/filesystem/directories?path={path}
        /// </summary>
        [HttpGet("filesystem/directories")]
        [ProducesResponseType(typeof(List<DirectoryInfoResponse>), 200)]
        public IActionResult GetDirectories([FromQuery] string? path)
        {
            try
            {
                // If no path provided, return empty (drives should be fetched first)
                if (string.IsNullOrEmpty(path))
                {
                    return BadRequest(new { error = "Path parameter is required" });
                }

                // Validate path exists
                if (!Directory.Exists(path))
                {
                    return NotFound(new { error = $"Directory not found: {path}" });
                }

                var directories = new List<DirectoryInfoResponse>();
                var subdirs = Directory.GetDirectories(path);

                foreach (var dir in subdirs)
                {
                    try
                    {
                        var dirInfo = new System.IO.DirectoryInfo(dir);

                        // Check if has subdirectories (but don't enumerate for performance)
                        bool hasSubdirs = false;
                        try
                        {
                            hasSubdirs = dirInfo.EnumerateDirectories().Any();
                        }
                        catch
                        {
                            // Access denied or other error - assume no subdirs
                        }

                        directories.Add(new DirectoryInfoResponse
                        {
                            Name = dirInfo.Name,
                            FullPath = dirInfo.FullName,
                            HasSubdirectories = hasSubdirs,
                            LastModified = dirInfo.LastWriteTime
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading directory info {dir}: {ex.Message}", LogLevel.Warning, "JobStorageController");
                    }
                }

                // Sort by name
                directories = directories.OrderBy(d => d.Name).ToList();

                return Ok(directories);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { error = "Access denied to directory" });
            }
            catch (Exception ex)
            {
                Log($"Error getting directories for path {path}: {ex.Message}", LogLevel.Error, "JobStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Helper Methods ==========

        /// <summary>
        /// Ensures the local job store does not exceed 500 jobs.
        /// If it does, deletes the oldest jobs that have no category until the count is at or below 500.
        /// Jobs with a category are never auto-deleted.
        /// </summary>
        private async Task PruneOldJobsAsync()
        {
            const int maxJobs = 500;

            if (_jobFileManager == null) return;

            try
            {
                // Use cache if populated, otherwise enumerate files
                List<(string jobId, DateTime createdAt, string? category)> jobInfos;

                if (_jobMetadataCache.Count > 0)
                {
                    jobInfos = _jobMetadataCache.Values
                        .Select(m => (m.Id, m.CreatedAt, m.Category))
                        .ToList();
                }
                else
                {
                    var allIds = _jobFileManager.GetAllJobIds();
                    if (allIds.Length <= maxJobs) return;

                    jobInfos = new List<(string, DateTime, string?)>();
                    foreach (var jobId in allIds)
                    {
                        var data = await _jobFileManager.ReadAsync(jobId);
                        if (data == null) continue;
                        var meta = ExtractJobMetadata(jobId, data);
                        jobInfos.Add((jobId, meta.CreatedAt, meta.Category));
                    }
                }

                if (jobInfos.Count <= maxJobs) return;

                // Only uncategorised jobs are eligible for pruning
                var uncategorised = jobInfos
                    .Where(j => string.IsNullOrEmpty(j.category))
                    .OrderBy(j => j.createdAt)  // oldest first
                    .ToList();

                int totalJobs = jobInfos.Count;
                int toDelete = totalJobs - maxJobs;

                if (toDelete <= 0 || uncategorised.Count == 0) return;

                var candidates = uncategorised.Take(toDelete).ToList();

                Log($"Pruning {candidates.Count} old uncategorised job(s) (total={totalJobs}, limit={maxJobs})", LogLevel.Info, "JobStorageController");

                foreach (var (jobId, _, _) in candidates)
                {
                    await _jobFileManager.DeleteAsync(jobId);
                    _jobMetadataCache.Remove(jobId);

                    if (_mongoDbService != null && _mongoDbService.IsConnected)
                    {
                        try
                        {
                            await _mongoDbService.DeleteJobAsync(jobId, _machineName!);
                            _jobFileManager.RemoveTombstone(jobId);
                        }
                        catch (Exception mongoEx)
                        {
                            Log($"Prune: MongoDB delete failed for {jobId} (tombstone retained): {mongoEx.Message}", LogLevel.Warning, "JobStorageController");
                        }
                    }

                    Log($"Pruned job: {jobId}", LogLevel.Info, "JobStorageController");
                }
            }
            catch (Exception ex)
            {
                Log($"Error during job pruning: {ex.Message}", LogLevel.Warning, "JobStorageController");
            }
        }

        private static int CountLines(string? content)
        {
            if (string.IsNullOrEmpty(content)) return 0;
            return content.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
        }

        private static JobMetadata ExtractJobMetadata(string jobId, string jobData)
        {
            // Try to get the actual file modification time — much more accurate than UtcNow
            DateTime lastModified = DateTime.UtcNow;
            if (_jobFileManager != null)
            {
                var filePath = _jobFileManager.GetFilePath(jobId);
                if (System.IO.File.Exists(filePath))
                    lastModified = System.IO.File.GetLastWriteTimeUtc(filePath);
            }

            try
            {
                using var doc = JsonDocument.Parse(jobData);
                var root = doc.RootElement;

                // Also honour any lastModified / updatedAt stored inside the JSON itself
                if (root.TryGetProperty("lastModified", out var lm) && lm.TryGetDateTime(out var lmDt))
                    lastModified = lmDt;
                else if (root.TryGetProperty("updatedAt", out var ua) && ua.TryGetDateTime(out var uaDt))
                    lastModified = uaDt;

                return new JobMetadata
                {
                    Id = jobId,
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? jobId : jobId,
                    ExecutionCount = root.TryGetProperty("executionCount", out var execCount) ? execCount.GetInt32() : 0,
                    LastRunDate = root.TryGetProperty("lastRunDate", out var lastRun) && lastRun.TryGetDateTime(out var lrDt) ? lrDt : (DateTime?)null,
                    Category = root.TryGetProperty("category", out var category) ? category.GetString() : null,
                    Size = jobData.Length,
                    LastModified = lastModified,
                    CreatedAt = root.TryGetProperty("createdAt", out var created) && created.TryGetDateTime(out var caDt) ? caDt : lastModified,
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    MaterialType = root.TryGetProperty("materialType", out var material) ? material.GetString() : null,
                    EstimatedTime = root.TryGetProperty("estimatedTime", out var time) ? time.GetString() : null
                };
            }
            catch (Exception ex)
            {
                Log($"Error extracting job metadata from {jobId}: {ex.Message}", LogLevel.Warning, "JobStorageController");
                return new JobMetadata
                {
                    Id = jobId,
                    Name = jobId,
                    Size = jobData.Length,
                    LastModified = lastModified,
                    CreatedAt = lastModified
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
