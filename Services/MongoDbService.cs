using MongoDB.Driver;
using HavenCNCServer.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Simple MongoDB service - save/load version + data
    /// </summary>
    public class MongoDbService
    {
        private readonly MongoDbSettings _settings;
        private MongoClient? _client;
        private IMongoDatabase? _database;
        private IMongoCollection<MachineConfigurationDocument>? _machineConfigCollection;
        private IMongoCollection<DefaultPlcVersionDocument>? _defaultPlcCollection;
        private IMongoCollection<JobDocument>? _jobCollection;
        private IMongoCollection<GCodeFileDocument>? _gcodeCollection;
        private bool _isConnected = false;

        public MongoDbService(MongoDbSettings settings)
        {
            _settings = settings;
            if (_settings.Enabled)
            {
                Connect();
            }
        }

        public bool IsConnected => _isConnected && _settings.Enabled;

        private void Connect()
        {
            try
            {
                _client = new MongoClient(_settings.ConnectionString);
                _database = _client.GetDatabase(_settings.DatabaseName);
                _machineConfigCollection = _database.GetCollection<MachineConfigurationDocument>("machineConfigurations");
                _defaultPlcCollection = _database.GetCollection<DefaultPlcVersionDocument>(_settings.DefaultPlcVersionsCollection);
                _jobCollection = _database.GetCollection<JobDocument>("jobs");
                _gcodeCollection = _database.GetCollection<GCodeFileDocument>("gcodeFiles");

                _client.GetDatabase("admin").RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                _isConnected = true;
                LogSuccess("MongoDB connected", "MongoDB");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                LogError($"MongoDB connection failed: {ex.Message}", "MongoDB");
            }
        }

        /// <summary>
        /// Save configuration to MongoDB
        /// </summary>
        public async Task<bool> SaveAsync(string machineName, string fileName, string data, long version)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.And(
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName),
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.FileName, fileName)
                );

                var update = Builders<MachineConfigurationDocument>.Update
                    .Set(x => x.MachineName, machineName)
                    .Set(x => x.FileName, fileName)
                    .Set(x => x.Data, data)
                    .Set(x => x.Version, version)
                    .Set(x => x.Timestamp, DateTime.UtcNow);

                await _machineConfigCollection!.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                LogInfo($"Saved {fileName} v{version} to MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save {fileName} to MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Load configuration from MongoDB
        /// </summary>
        public async Task<MachineConfigurationDocument?> LoadAsync(string machineName, string fileName)
        {
            if (!IsConnected) return null;

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.And(
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName),
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.FileName, fileName)
                );

                return await _machineConfigCollection!.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                LogError($"Failed to load {fileName} from MongoDB: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// Delete a single configuration file from MongoDB for a given machine.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string machineName, string fileName)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.And(
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName),
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.FileName, fileName)
                );

                await _machineConfigCollection!.DeleteOneAsync(filter);
                LogInfo($"Deleted {fileName} from MongoDB for machine '{machineName}'", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete {fileName} from MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Get list of distinct machine names from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<string>> GetMachineNamesAsync()
        {
            if (!IsConnected) return new System.Collections.Generic.List<string>();

            try
            {
                var machineNames = await _machineConfigCollection!
                    .Distinct<string>("MachineName", Builders<MachineConfigurationDocument>.Filter.Empty)
                    .ToListAsync();

                return machineNames;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get machine names from MongoDB: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<string>();
            }
        }

        /// <summary>
        /// Get list of file names for a specific machine from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<string>> GetFileNamesForMachineAsync(string machineName)
        {
            if (!IsConnected) return new System.Collections.Generic.List<string>();

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName);
                var fileNames = await _machineConfigCollection!
                    .Distinct<string>("FileName", filter)
                    .ToListAsync();

                return fileNames;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get file names for machine '{machineName}': {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<string>();
            }
        }

        /// <summary>
        /// Save default PLC version to MongoDB
        /// </summary>
        public async Task<bool> SaveDefaultPlcAsync(string versionName, string jsonData, bool markAsLatest, string? description = null, string? createdBy = null)
        {
            if (!IsConnected) return false;

            try
            {
                // If marking as latest, unmark all others
                if (markAsLatest)
                {
                    var unmarkFilter = Builders<DefaultPlcVersionDocument>.Filter.Eq(x => x.IsLatest, true);
                    var unmarkUpdate = Builders<DefaultPlcVersionDocument>.Update.Set(x => x.IsLatest, false);
                    await _defaultPlcCollection!.UpdateManyAsync(unmarkFilter, unmarkUpdate);
                }

                var document = new DefaultPlcVersionDocument
                {
                    VersionName = versionName,
                    Timestamp = DateTime.UtcNow,
                    JsonData = jsonData,
                    Description = description,
                    IsLatest = markAsLatest,
                    CreatedBy = createdBy
                };

                await _defaultPlcCollection!.InsertOneAsync(document);
                LogSuccess($"✓ Stored default PLC version '{versionName}' to MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to store default PLC: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Get the latest default PLC version
        /// </summary>
        public async Task<DefaultPlcVersionDocument?> GetLatestDefaultPlcAsync()
        {
            if (!IsConnected) return null;

            try
            {
                var filter = Builders<DefaultPlcVersionDocument>.Filter.Eq(x => x.IsLatest, true);
                var sort = Builders<DefaultPlcVersionDocument>.Sort.Descending(x => x.Timestamp);

                var result = await _defaultPlcCollection!.Find(filter).Sort(sort).FirstOrDefaultAsync();

                if (result != null)
                {
                    LogInfo($"Retrieved latest default PLC version '{result.VersionName}'", "MongoDB");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get latest default PLC: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// Get a specific default PLC version by name
        /// </summary>
        public async Task<DefaultPlcVersionDocument?> GetDefaultPlcByVersionAsync(string versionName)
        {
            if (!IsConnected) return null;

            try
            {
                var filter = Builders<DefaultPlcVersionDocument>.Filter.Eq(x => x.VersionName, versionName);
                var result = await _defaultPlcCollection!.Find(filter).FirstOrDefaultAsync();

                if (result != null)
                {
                    LogInfo($"Retrieved default PLC version '{versionName}'", "MongoDB");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get default PLC version: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// List all default PLC versions
        /// </summary>
        public async Task<System.Collections.Generic.List<DefaultPlcVersionInfo>> ListDefaultPlcVersionsAsync()
        {
            if (!IsConnected) return new System.Collections.Generic.List<DefaultPlcVersionInfo>();

            try
            {
                var sort = Builders<DefaultPlcVersionDocument>.Sort.Descending(x => x.Timestamp);
                var documents = await _defaultPlcCollection!.Find(FilterDefinition<DefaultPlcVersionDocument>.Empty)
                    .Sort(sort)
                    .ToListAsync();

                var result = documents.Select(d => new DefaultPlcVersionInfo
                {
                    Id = d.Id!,
                    VersionName = d.VersionName,
                    Timestamp = d.Timestamp,
                    Description = d.Description,
                    IsLatest = d.IsLatest,
                    CreatedBy = d.CreatedBy
                }).ToList();

                LogInfo($"Listed {result.Count} default PLC versions", "MongoDB");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to list default PLC versions: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<DefaultPlcVersionInfo>();
            }
        }

        // ========== Job Storage Methods ==========

        /// <summary>
        /// Save job to MongoDB
        /// </summary>
        public async Task<bool> SaveJobAsync(string jobId, string machineName, string data, long version, JobMetadata? metadata)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<JobDocument>.Filter.And(
                    Builders<JobDocument>.Filter.Eq(x => x.JobId, jobId),
                    Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                var update = Builders<JobDocument>.Update
                    .Set(x => x.JobId, jobId)
                    .Set(x => x.MachineName, machineName)
                    .Set(x => x.Data, data)
                    .Set(x => x.Version, version)
                    .Set(x => x.Timestamp, DateTime.UtcNow)
                    .Set(x => x.Metadata, metadata);

                await _jobCollection!.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                LogInfo($"Saved job {jobId} v{version} to MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save job {jobId} to MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Load job from MongoDB
        /// </summary>
        public async Task<JobDocument?> LoadJobAsync(string jobId, string machineName)
        {
            if (!IsConnected) return null;

            try
            {
                var filter = Builders<JobDocument>.Filter.And(
                    Builders<JobDocument>.Filter.Eq(x => x.JobId, jobId),
                    Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                return await _jobCollection!.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                LogError($"Failed to load job {jobId} from MongoDB: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// Delete job from MongoDB
        /// </summary>
        public async Task<bool> DeleteJobAsync(string jobId, string machineName)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<JobDocument>.Filter.And(
                    Builders<JobDocument>.Filter.Eq(x => x.JobId, jobId),
                    Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                await _jobCollection!.DeleteOneAsync(filter);
                LogInfo($"Deleted job {jobId} from MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete job {jobId} from MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Get last N jobs for a machine from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<JobDocument>> GetRecentJobsAsync(string machineName, int limit = 20)
        {
            if (!IsConnected) return new System.Collections.Generic.List<JobDocument>();

            try
            {
                var filter = Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName);
                var sort = Builders<JobDocument>.Sort.Descending(x => x.Timestamp);

                var jobs = await _jobCollection!.Find(filter).Sort(sort).Limit(limit).ToListAsync();
                LogInfo($"Retrieved {jobs.Count} recent jobs from MongoDB", "MongoDB");
                return jobs;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get recent jobs from MongoDB: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<JobDocument>();
            }
        }

        /// <summary>
        /// List all jobs for a machine (metadata only) from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<JobDocument>> ListJobsAsync(string machineName)
        {
            if (!IsConnected) return new System.Collections.Generic.List<JobDocument>();

            try
            {
                var filter = Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName);
                var sort = Builders<JobDocument>.Sort.Descending(x => x.Timestamp);

                var jobs = await _jobCollection!.Find(filter).Sort(sort).ToListAsync();
                return jobs;
            }
            catch (Exception ex)
            {
                LogError($"Failed to list jobs from MongoDB: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<JobDocument>();
            }
        }

        // ========== G-Code File Storage Methods ==========

        /// <summary>
        /// Save G-code file to MongoDB
        /// </summary>
        public async Task<bool> SaveGCodeFileAsync(string fileId, string fileName, string machineName, string data, long version, string? category = null, string? description = null, string? materialType = null, string? estimatedTime = null)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<GCodeFileDocument>.Filter.And(
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.FileId, fileId),
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                var update = Builders<GCodeFileDocument>.Update
                    .Set(x => x.FileId, fileId)
                    .Set(x => x.FileName, fileName)
                    .Set(x => x.MachineName, machineName)
                    .Set(x => x.Data, data)
                    .Set(x => x.Version, version)
                    .Set(x => x.Timestamp, DateTime.UtcNow)
                    .Set(x => x.Size, data.Length)
                    .Set(x => x.Category, category)
                    .Set(x => x.Description, description)
                    .Set(x => x.MaterialType, materialType)
                    .Set(x => x.EstimatedTime, estimatedTime);

                await _gcodeCollection!.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                LogInfo($"Saved G-code file {fileId} v{version} to MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save G-code file {fileId} to MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Load G-code file from MongoDB
        /// </summary>
        public async Task<GCodeFileDocument?> LoadGCodeFileAsync(string fileId, string machineName)
        {
            if (!IsConnected) return null;

            try
            {
                var filter = Builders<GCodeFileDocument>.Filter.And(
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.FileId, fileId),
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                return await _gcodeCollection!.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                LogError($"Failed to load G-code file {fileId} from MongoDB: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// Delete G-code file from MongoDB
        /// </summary>
        public async Task<bool> DeleteGCodeFileAsync(string fileId, string machineName)
        {
            if (!IsConnected) return false;

            try
            {
                var filter = Builders<GCodeFileDocument>.Filter.And(
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.FileId, fileId),
                    Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName)
                );

                await _gcodeCollection!.DeleteOneAsync(filter);
                LogInfo($"Deleted G-code file {fileId} from MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete G-code file {fileId} from MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Get last N G-code files for a machine from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<GCodeFileDocument>> GetRecentGCodeFilesAsync(string machineName, int limit = 20)
        {
            if (!IsConnected) return new System.Collections.Generic.List<GCodeFileDocument>();

            try
            {
                var filter = Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName);
                var sort = Builders<GCodeFileDocument>.Sort.Descending(x => x.Timestamp);

                var files = await _gcodeCollection!.Find(filter).Sort(sort).Limit(limit).ToListAsync();
                LogInfo($"Retrieved {files.Count} recent G-code files from MongoDB", "MongoDB");
                return files;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get recent G-code files from MongoDB: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<GCodeFileDocument>();
            }
        }

        /// <summary>
        /// List all managed G-code files for a machine from MongoDB
        /// </summary>
        public async Task<System.Collections.Generic.List<GCodeFileDocument>> ListGCodeFilesAsync(string machineName)
        {
            if (!IsConnected) return new System.Collections.Generic.List<GCodeFileDocument>();

            try
            {
                var filter = Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName);
                var sort = Builders<GCodeFileDocument>.Sort.Descending(x => x.Timestamp);

                var files = await _gcodeCollection!.Find(filter).Sort(sort).ToListAsync();
                return files;
            }
            catch (Exception ex)
            {
                LogError($"Failed to list G-code files from MongoDB: {ex.Message}", "MongoDB");
                return new System.Collections.Generic.List<GCodeFileDocument>();
            }
        }

        /// <summary>
        /// Delete ALL documents for a given machine name across all collections:
        /// machine configurations, jobs, and G-code files.
        /// Returns counts of deleted documents per collection.
        /// </summary>
        public async Task<(long configs, long jobs, long gcode)> DeleteAllForMachineAsync(string machineName)
        {
            if (!IsConnected) return (0, 0, 0);

            long configs = 0, jobs = 0, gcode = 0;

            try
            {
                var configFilter = Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName);
                var configResult = await _machineConfigCollection!.DeleteManyAsync(configFilter);
                configs = configResult.DeletedCount;
                LogInfo($"Deleted {configs} config document(s) for machine '{machineName}'", "MongoDB");
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete configs for machine '{machineName}': {ex.Message}", "MongoDB");
            }

            try
            {
                var jobFilter = Builders<JobDocument>.Filter.Eq(x => x.MachineName, machineName);
                var jobResult = await _jobCollection!.DeleteManyAsync(jobFilter);
                jobs = jobResult.DeletedCount;
                LogInfo($"Deleted {jobs} job(s) for machine '{machineName}'", "MongoDB");
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete jobs for machine '{machineName}': {ex.Message}", "MongoDB");
            }

            try
            {
                var gcodeFilter = Builders<GCodeFileDocument>.Filter.Eq(x => x.MachineName, machineName);
                var gcodeResult = await _gcodeCollection!.DeleteManyAsync(gcodeFilter);
                gcode = gcodeResult.DeletedCount;
                LogInfo($"Deleted {gcode} G-code file(s) for machine '{machineName}'", "MongoDB");
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete G-code files for machine '{machineName}': {ex.Message}", "MongoDB");
            }

            return (configs, jobs, gcode);
        }
    }
}
