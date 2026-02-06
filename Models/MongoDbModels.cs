using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Simple MongoDB document: version + data string
    /// </summary>
    public class MachineConfigurationDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("version")]
        public long Version { get; set; }

        [BsonElement("data")]
        public string Data { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        // Legacy fields - ignored but needed for deserialization of old documents
        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Local machine settings
    /// </summary>
    public class LocalMachineSettings
    {
        public string CurrentMachineName { get; set; } = string.Empty;
        public DateTime? LastSyncTime { get; set; }
    }

    public class SetCurrentMachineRequest
    {
        public string MachineName { get; set; } = string.Empty;
    }

    public class SyncStatusResponse
    {
        public bool MongoDbOnline { get; set; }
        public string CurrentMachineName { get; set; } = string.Empty;
        public DateTime? LastSyncTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
