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

    /// <summary>
    /// Default PLC version document stored in MongoDB
    /// </summary>
    public class DefaultPlcVersionDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("versionName")]
        [BsonRequired]
        public string VersionName { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("jsonData")]
        [BsonRequired]
        public string JsonData { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("isLatest")]
        public bool IsLatest { get; set; } = false;

        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request model for storing default PLC
    /// </summary>
    public class StoreDefaultPlcRequest
    {
        public string VersionName { get; set; } = string.Empty;
        public string JsonData { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool MarkAsLatest { get; set; } = true;
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Response model for default PLC versions list
    /// </summary>
    public class DefaultPlcVersionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }
        public bool IsLatest { get; set; }
        public string? CreatedBy { get; set; }
    }
}
