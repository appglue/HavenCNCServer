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
    public class CamStorageController : ControllerBase
    {
        private static CamFileManager? _camFileManager;
        private static MongoDbService? _mongoDbService;
        private static string? _machineName;

        public static async Task InitializeAsync()
        {
            try
            {
                LogInfo("🔄 CamStorageController.InitializeAsync CALLED", "CamStorage");

                _camFileManager = new CamFileManager();
                LogInfo("✓ CAM file manager created", "CamStorage");

                // Load MongoDB settings
                var mongoSettings = SettingsManager.Settings.MongoDB;
                if (mongoSettings != null && mongoSettings.Enabled)
                {
                    LogInfo($"MongoDB settings loaded: Enabled={mongoSettings.Enabled}", "CamStorage");
                    _mongoDbService = new MongoDbService(mongoSettings);
                }
                else
                {
                    LogWarning("MongoDB settings not found or disabled", "CamStorage");
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
                LogInfo($"Machine name: {_machineName}", "CamStorage");

                // Initial sync - download recent CAM projects
                LogInfo("Starting initial sync...", "CamStorage");
                await SyncRecentProjectsAsync();

                LogSuccess("✓ CamStorageController initialized", "CamStorage");
            }
            catch (Exception ex)
            {
                LogError($"CamStorageController initialization failed: {ex.Message}", "CamStorage");
                LogError($"Stack trace: {ex.StackTrace}", "CamStorage");
            }
        }

        private static async Task SyncRecentProjectsAsync()
        {
            if (_mongoDbService == null || !_mongoDbService.IsConnected || _camFileManager == null || _machineName == null)
            {
                LogInfo("Skipping MongoDB sync (not connected or not initialized)", "CamStorage");
                return;
            }

            try
            {
                // Download last 20 CAM projects
                LogInfo("Syncing recent CAM projects from MongoDB...", "CamStorage");
                var recentProjects = await _mongoDbService.GetRecentCamProjectsAsync(_machineName, 20);
                int projectsSynced = 0;
                foreach (var project in recentProjects)
                {
                    var localVersion = await _camFileManager.GetVersionAsync(project.ProjectId);
                    if (project.Version > localVersion)
                    {
                        await _camFileManager.WriteAsync(project.ProjectId, project.Data);
                        await _camFileManager.WriteVersionAsync(project.ProjectId, project.Version);
                        projectsSynced++;
                    }
                }
                LogInfo($"✓ Synced {projectsSynced} CAM projects from MongoDB", "CamStorage");
            }
            catch (Exception ex)
            {
                LogError($"CAM sync failed: {ex.Message}", "CamStorage");
            }
        }

        // ========== CAM Project Endpoints ==========

        /// <summary>
        /// List all CAM projects with paging and sorting
        /// POST /api/CamStorage/projects/list
        /// </summary>
        [HttpPost("projects/list")]
        [ProducesResponseType(typeof(PagedResult<CamProjectMetadata>), 200)]
        public async Task<IActionResult> ListProjects([FromBody] PageRequest request)
        {
            try
            {
                if (_camFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "CAM storage not initialized" });

                var allProjects = new List<CamProjectMetadata>();

                // 1. Get projects from MongoDB
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoProjects = await _mongoDbService.ListCamProjectsAsync(_machineName);
                    foreach (var doc in mongoProjects)
                    {
                        if (doc.Metadata != null)
                        {
                            allProjects.Add(doc.Metadata);
                        }
                    }
                }

                // 2. Get projects from local storage (not in MongoDB)
                var localProjectIds = _camFileManager.GetAllProjectIds();
                foreach (var projectId in localProjectIds)
                {
                    if (allProjects.Any(p => p.ProjectId == projectId))
                        continue;  // Already have from MongoDB

                    var data = await _camFileManager.ReadAsync(projectId);
                    if (data != null)
                    {
                        var metadata = ExtractCamProjectMetadata(projectId, data);
                        allProjects.Add(metadata);
                    }
                }

                // Apply sorting
                var sorted = ApplySorting(allProjects, request.SortBy, request.SortDirection);

                // Apply paging
                var totalCount = sorted.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var paged = sorted.Skip(skip).Take(request.PageSize).ToList();

                return Ok(new PagedResult<CamProjectMetadata>
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
                Log($"Error listing CAM projects: {ex.Message}", LogLevel.Error, "CamStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Fetch a specific CAM project by ID
        /// GET /api/CamStorage/projects/{id}
        /// </summary>
        [HttpGet("projects/{id}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> FetchProject(string id)
        {
            try
            {
                if (_camFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "CAM storage not initialized" });

                // Try MongoDB first
                if (_mongoDbService != null && _mongoDbService.IsConnected)
                {
                    var mongoDoc = await _mongoDbService.LoadCamProjectAsync(id, _machineName);
                    if (mongoDoc != null)
                    {
                        return Ok(mongoDoc.Data);
                    }
                }

                // Fallback to local
                var localData = await _camFileManager.ReadAsync(id);
                if (localData != null)
                {
                    return Ok(localData);
                }

                return NotFound(new { error = $"CAM project not found: {id}" });
            }
            catch (Exception ex)
            {
                Log($"Error fetching CAM project {id}: {ex.Message}", LogLevel.Error, "CamStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Store a CAM project
        /// POST /api/CamStorage/projects
        /// </summary>
        [HttpPost("projects")]
        [ProducesResponseType(typeof(StoreResponse), 200)]
        public async Task<IActionResult> StoreProject([FromBody] StoreCamProjectRequest request)
        {
            try
            {
                if (_camFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "CAM storage not initialized" });

                if (string.IsNullOrEmpty(request.ProjectId) || string.IsNullOrEmpty(request.Data))
                {
                    return BadRequest(new { error = "ProjectId and Data are required" });
                }

                // Extract or use provided metadata
                var metadata = request.Metadata ?? ExtractCamProjectMetadata(request.ProjectId, request.Data);

                // Write to local first
                var version = await _camFileManager.IncrementAndWriteAsync(request.ProjectId, request.Data);

                // Sync to MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.SaveCamProjectAsync(request.ProjectId, machineName, request.Data, version, metadata);
                    });
                }

                return Ok(new StoreResponse
                {
                    Success = true,
                    Id = request.ProjectId,
                    Message = $"CAM project stored successfully with version {version}"
                });
            }
            catch (Exception ex)
            {
                Log($"Error storing CAM project {request.ProjectId}: {ex.Message}", LogLevel.Error, "CamStorageController");
                return StatusCode(500, new StoreResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a CAM project
        /// DELETE /api/CamStorage/projects/{id}
        /// </summary>
        [HttpDelete("projects/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteProject(string id)
        {
            try
            {
                if (_camFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "CAM storage not initialized" });

                var localDeleted = await _camFileManager.DeleteAsync(id);

                // Delete from MongoDB in background
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.DeleteCamProjectAsync(id, machineName);
                    });
                }

                if (localDeleted)
                {
                    return Ok(new { message = $"CAM project deleted: {id}" });
                }
                else
                {
                    return NotFound(new { error = $"CAM project not found: {id}" });
                }
            }
            catch (Exception ex)
            {
                Log($"Error deleting CAM project {id}: {ex.Message}", LogLevel.Error, "CamStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update CAM project category
        /// PUT /api/CamStorage/projects/{id}/category
        /// </summary>
        [HttpPut("projects/{id}/category")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateProjectCategory(string id, [FromBody] UpdateCamProjectCategoryRequest request)
        {
            try
            {
                if (_camFileManager == null || _machineName == null)
                    return StatusCode(500, new { error = "CAM storage not initialized" });

                // Read existing project
                var existingData = await _camFileManager.ReadAsync(id);
                if (existingData == null)
                {
                    return NotFound(new { error = $"CAM project not found: {id}" });
                }

                // Parse and update category
                using var doc = JsonDocument.Parse(existingData);
                var root = doc.RootElement;
                
                // Create updated JSON with new category
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    
                    // Copy all existing properties
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != "category")
                        {
                            property.WriteTo(writer);
                        }
                    }
                    
                    // Write new category
                    if (request.Category != null)
                    {
                        writer.WriteString("category", request.Category);
                    }
                    
                    writer.WriteEndObject();
                }
                
                var updatedData = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                
                // Save updated project
                var version = await _camFileManager.IncrementAndWriteAsync(id, updatedData);

                // Update metadata and sync to MongoDB
                var metadata = ExtractCamProjectMetadata(id, updatedData);
                if (_mongoDbService != null)
                {
                    var machineName = _machineName;
                    var mongoService = _mongoDbService;
                    _ = Task.Run(async () =>
                    {
                        await mongoService.SaveCamProjectAsync(id, machineName, updatedData, version, metadata);
                    });
                }

                return Ok(new { message = "Category updated successfully" });
            }
            catch (Exception ex)
            {
                Log($"Error updating CAM project category {id}: {ex.Message}", LogLevel.Error, "CamStorageController");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== Helper Methods ==========

        private CamProjectMetadata ExtractCamProjectMetadata(string projectId, string projectData)
        {
            try
            {
                // Try to parse CAM project JSON to extract metadata
                using var doc = JsonDocument.Parse(projectData);
                var root = doc.RootElement;

                return new CamProjectMetadata
                {
                    ProjectId = projectId,
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? projectId : projectId,
                    Category = root.TryGetProperty("category", out var category) ? category.GetString() : null,
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    Size = projectData.Length,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = root.TryGetProperty("createdAt", out var created) ? created.GetDateTime() : DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Log($"Error extracting CAM project metadata from {projectId}: {ex.Message}", LogLevel.Warning, "CamStorageController");
                return new CamProjectMetadata
                {
                    ProjectId = projectId,
                    Name = projectId,
                    Size = projectData.Length,
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
