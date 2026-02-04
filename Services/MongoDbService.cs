using MongoDB.Driver;
using HavenCNCServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for MongoDB operations - handles machine configuration synchronization
    /// </summary>
    public class MongoDbService
    {
        private readonly MongoDbSettings _settings;
        private MongoClient? _client;
        private IMongoDatabase? _database;
        private IMongoCollection<MachineConfigurationDocument>? _machineConfigCollection;
        private IMongoCollection<DefaultPlcVersionDocument>? _defaultPlcCollection;
        private bool _isConnected = false;

        public MongoDbService(MongoDbSettings settings)
        {
            _settings = settings;

            LogInfo("=== MongoDB Initialization ===", "MongoDB");
            LogInfo($"  Enabled: {_settings.Enabled}", "MongoDB");
            LogInfo($"  Database: {_settings.DatabaseName}", "MongoDB");
            LogInfo($"  Connection timeout: {_settings.ConnectionTimeoutMs}ms", "MongoDB");
            LogInfo($"  Continue on offline: {_settings.ContinueOnOffline}", "MongoDB");
            LogInfo($"  Connection string configured: {!string.IsNullOrEmpty(_settings.ConnectionString)}", "MongoDB");

            if (_settings.Enabled)
            {
                LogInfo("MongoDB is enabled - attempting connection...", "MongoDB");
                InitializeConnection();
            }
            else
            {
                LogWarning("MongoDB is disabled in settings - all operations will be local only", "MongoDB");
            }
        }

        /// <summary>
        /// Initialize MongoDB connection
        /// </summary>
        private void InitializeConnection()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ConnectionString))
                {
                    LogWarning("MongoDB connection string is empty. MongoDB features disabled.", "MongoDB");
                    return;
                }

                var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
                clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(_settings.ConnectionTimeoutMs);
                clientSettings.ConnectTimeout = TimeSpan.FromMilliseconds(_settings.ConnectionTimeoutMs);

                _client = new MongoClient(clientSettings);
                _database = _client.GetDatabase(_settings.DatabaseName);

                _machineConfigCollection = _database.GetCollection<MachineConfigurationDocument>(_settings.MachineConfigurationsCollection);
                _defaultPlcCollection = _database.GetCollection<DefaultPlcVersionDocument>(_settings.DefaultPlcVersionsCollection);

                // Test connection
                _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}").Wait(_settings.ConnectionTimeoutMs);

                _isConnected = true;
                LogSuccess("✓ MongoDB connected successfully", "MongoDB");

                // Create indexes
                CreateIndexes();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                LogError($"Failed to connect to MongoDB: {ex.Message}", "MongoDB");

                // Log inner exception details for better diagnostics
                if (ex.InnerException != null)
                {
                    LogError($"  Inner exception: {ex.InnerException.Message}", "MongoDB");
                }

                // Log connection details (without sensitive info)
                LogError($"  Database: {_settings.DatabaseName}", "MongoDB");
                LogError($"  Timeout: {_settings.ConnectionTimeoutMs}ms", "MongoDB");

                // Log exception type for troubleshooting
                LogError($"  Exception type: {ex.GetType().Name}", "MongoDB");

                if (!_settings.ContinueOnOffline)
                {
                    throw;
                }
                else
                {
                    LogWarning("Continuing in offline mode - local files only", "MongoDB");
                }
            }
        }

        /// <summary>
        /// Create necessary indexes for performance
        /// </summary>
        private void CreateIndexes()
        {
            try
            {
                if (_machineConfigCollection != null)
                {
                    // Compound index on machineName and fileName for fast lookups
                    var machineConfigIndexKeys = Builders<MachineConfigurationDocument>.IndexKeys
                        .Ascending(x => x.MachineName)
                        .Ascending(x => x.FileName);
                    var machineConfigIndexModel = new CreateIndexModel<MachineConfigurationDocument>(
                        machineConfigIndexKeys,
                        new CreateIndexOptions { Unique = true, Name = "machineName_fileName_unique" }
                    );
                    _machineConfigCollection.Indexes.CreateOne(machineConfigIndexModel);
                }

                if (_defaultPlcCollection != null)
                {
                    // Index on versionName
                    var versionNameIndexKeys = Builders<DefaultPlcVersionDocument>.IndexKeys
                        .Ascending(x => x.VersionName);
                    var versionNameIndexModel = new CreateIndexModel<DefaultPlcVersionDocument>(
                        versionNameIndexKeys,
                        new CreateIndexOptions { Name = "versionName_index" }
                    );
                    _defaultPlcCollection.Indexes.CreateOne(versionNameIndexModel);

                    // Index on isLatest
                    var isLatestIndexKeys = Builders<DefaultPlcVersionDocument>.IndexKeys
                        .Descending(x => x.IsLatest)
                        .Descending(x => x.Timestamp);
                    var isLatestIndexModel = new CreateIndexModel<DefaultPlcVersionDocument>(
                        isLatestIndexKeys,
                        new CreateIndexOptions { Name = "isLatest_timestamp_index" }
                    );
                    _defaultPlcCollection.Indexes.CreateOne(isLatestIndexModel);
                }

                LogInfo("MongoDB indexes created successfully", "MongoDB");
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to create MongoDB indexes: {ex.Message}", "MongoDB");
            }
        }

        /// <summary>
        /// Check if MongoDB is currently connected
        /// </summary>
        public bool IsConnected => _isConnected && _settings.Enabled;

        /// <summary>
        /// Save or update machine configuration
        /// </summary>
        public async Task<bool> SaveMachineConfigurationAsync(string machineName, string fileName, string jsonData, string? description = null)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot save {fileName} for '{machineName}' - MongoDB offline", "MongoDB");
                return false;
            }

            try
            {
                var document = new MachineConfigurationDocument
                {
                    MachineName = machineName,
                    FileName = fileName,
                    Timestamp = DateTime.UtcNow,
                    JsonData = jsonData,
                    Description = description,
                    Version = 1
                };

                var filter = Builders<MachineConfigurationDocument>.Filter.And(
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName),
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.FileName, fileName)
                );

                // Check if document exists to increment version
                var existing = await _machineConfigCollection!.Find(filter).FirstOrDefaultAsync();
                if (existing != null)
                {
                    document.Version = existing.Version + 1;
                }

                var options = new ReplaceOptions { IsUpsert = true };
                await _machineConfigCollection!.ReplaceOneAsync(filter, document, options);

                LogSuccess($"✓ Saved {fileName} for machine '{machineName}' to MongoDB", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save configuration to MongoDB: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Get machine configuration
        /// </summary>
        public async Task<MachineConfigurationDocument?> GetMachineConfigurationAsync(string machineName, string fileName)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot get {fileName} for '{machineName}' - MongoDB offline", "MongoDB");
                return null;
            }

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.And(
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName),
                    Builders<MachineConfigurationDocument>.Filter.Eq(x => x.FileName, fileName)
                );

                var result = await _machineConfigCollection!.Find(filter).FirstOrDefaultAsync();

                if (result != null)
                {
                    LogInfo($"Retrieved {fileName} for machine '{machineName}' from MongoDB", "MongoDB");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get configuration from MongoDB: {ex.Message}", "MongoDB");
                return null;
            }
        }

        /// <summary>
        /// Get all machine names
        /// </summary>
        public async Task<List<string>> GetAllMachineNamesAsync()
        {
            if (!IsConnected)
            {
                LogWarning("Cannot get machine names - MongoDB offline", "MongoDB");
                return new List<string>();
            }

            try
            {
                var machineNames = await _machineConfigCollection!
                    .Distinct(x => x.MachineName, FilterDefinition<MachineConfigurationDocument>.Empty)
                    .ToListAsync();

                LogInfo($"Retrieved {machineNames.Count} machine names from MongoDB", "MongoDB");
                return machineNames;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get machine names from MongoDB: {ex.Message}", "MongoDB");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get all configuration files for a specific machine
        /// </summary>
        public async Task<List<MachineConfigurationDocument>> GetMachineAllConfigurationsAsync(string machineName)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot get configurations for '{machineName}' - MongoDB offline", "MongoDB");
                return new List<MachineConfigurationDocument>();
            }

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName);
                var configs = await _machineConfigCollection!.Find(filter).ToListAsync();

                LogInfo($"Retrieved {configs.Count} configurations for machine '{machineName}'", "MongoDB");
                return configs;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get machine configurations from MongoDB: {ex.Message}", "MongoDB");
                return new List<MachineConfigurationDocument>();
            }
        }

        /// <summary>
        /// Copy all configurations from one machine to another
        /// </summary>
        public async Task<bool> CopyMachineConfigurationAsync(string sourceMachineName, string newMachineName, string? description = null)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot copy '{sourceMachineName}' to '{newMachineName}' - MongoDB offline", "MongoDB");
                return false;
            }

            try
            {
                // Get all configurations for source machine
                var sourceConfigs = await GetMachineAllConfigurationsAsync(sourceMachineName);

                if (sourceConfigs.Count == 0)
                {
                    LogWarning($"No configurations found for machine '{sourceMachineName}'", "MongoDB");
                    return false;
                }

                // Create new documents for the new machine
                var copyTasks = sourceConfigs.Select(async config =>
                {
                    var newDoc = new MachineConfigurationDocument
                    {
                        MachineName = newMachineName,
                        FileName = config.FileName,
                        Timestamp = DateTime.UtcNow,
                        JsonData = config.JsonData,
                        Description = description ?? $"Copied from {sourceMachineName}",
                        Version = 1
                    };

                    await _machineConfigCollection!.InsertOneAsync(newDoc);
                });

                await Task.WhenAll(copyTasks);

                LogSuccess($"✓ Copied {sourceConfigs.Count} configurations from '{sourceMachineName}' to '{newMachineName}'", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to copy machine configuration: {ex.Message}", "MongoDB");
                return false;
            }
        }

        /// <summary>
        /// Store a new default PLC version
        /// </summary>
        public async Task<bool> StoreDefaultPlcAsync(string versionName, string jsonData, string? description = null, bool markAsLatest = true, string? createdBy = null)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot store default PLC '{versionName}' - MongoDB offline", "MongoDB");
                return false;
            }

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
            if (!IsConnected)
            {
                LogWarning("Cannot get latest default PLC - MongoDB offline", "MongoDB");
                return null;
            }

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
            if (!IsConnected)
            {
                LogWarning($"Cannot get default PLC '{versionName}' - MongoDB offline", "MongoDB");
                return null;
            }

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
        public async Task<List<DefaultPlcVersionInfo>> ListDefaultPlcVersionsAsync()
        {
            if (!IsConnected)
            {
                LogWarning("Cannot list default PLC versions - MongoDB offline", "MongoDB");
                return new List<DefaultPlcVersionInfo>();
            }

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

                LogInfo($"Retrieved {result.Count} default PLC versions", "MongoDB");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Failed to list default PLC versions: {ex.Message}", "MongoDB");
                return new List<DefaultPlcVersionInfo>();
            }
        }

        /// <summary>
        /// Delete a machine's all configurations
        /// </summary>
        public async Task<bool> DeleteMachineConfigurationAsync(string machineName)
        {
            if (!IsConnected)
            {
                LogWarning($"Cannot delete configurations for '{machineName}' - MongoDB offline", "MongoDB");
                return false;
            }

            try
            {
                var filter = Builders<MachineConfigurationDocument>.Filter.Eq(x => x.MachineName, machineName);
                var result = await _machineConfigCollection!.DeleteManyAsync(filter);

                LogSuccess($"✓ Deleted {result.DeletedCount} configurations for machine '{machineName}'", "MongoDB");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete machine configuration: {ex.Message}", "MongoDB");
                return false;
            }
        }
    }
}
