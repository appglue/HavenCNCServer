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
    }
}
