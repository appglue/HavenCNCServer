using MongoDB.Driver;
using HavenCNCServer.Models;
using System;
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
    }
}
