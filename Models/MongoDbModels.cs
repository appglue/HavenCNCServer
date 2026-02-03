using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Machine configuration document stored in MongoDB
    /// </summary>
    public class MachineConfigurationDocument
    {
        /// <summary>
        /// MongoDB document ID
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Machine name identifier
        /// </summary>
        [BsonElement("machineName")]
        [BsonRequired]
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// Configuration file name (e.g., plcSystem.json, configuration.json)
        /// </summary>
        [BsonElement("fileName")]
        [BsonRequired]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this configuration was saved
        /// </summary>
        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// JSON configuration data as string
        /// </summary>
        [BsonElement("jsonData")]
        [BsonRequired]
        public string JsonData { get; set; } = string.Empty;

        /// <summary>
        /// Version number for conflict resolution
        /// </summary>
        [BsonElement("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Optional description or notes
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Default PLC version document stored in MongoDB
    /// Maintains all historical versions
    /// </summary>
    public class DefaultPlcVersionDocument
    {
        /// <summary>
        /// MongoDB document ID
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Version name/identifier (e.g., "V1.0_Initial", "V2.1_Production")
        /// </summary>
        [BsonElement("versionName")]
        [BsonRequired]
        public string VersionName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this version was saved
        /// </summary>
        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// PLC system default JSON data as string
        /// </summary>
        [BsonElement("jsonData")]
        [BsonRequired]
        public string JsonData { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of this version
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this is marked as the latest/active version
        /// </summary>
        [BsonElement("isLatest")]
        public bool IsLatest { get; set; } = false;

        /// <summary>
        /// User who created this version
        /// </summary>
        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Local machine settings stored in data folder
    /// </summary>
    public class LocalMachineSettings
    {
        /// <summary>
        /// Currently active machine name
        /// </summary>
        public string CurrentMachineName { get; set; } = string.Empty;

        /// <summary>
        /// Last sync timestamp
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// Whether MongoDB sync is enabled locally
        /// </summary>
        public bool SyncEnabled { get; set; } = true;
    }

    /// <summary>
    /// Request model for copying machine configuration
    /// </summary>
    public class CopyMachineConfigurationRequest
    {
        /// <summary>
        /// Source machine name to copy from
        /// </summary>
        public string SourceMachineName { get; set; } = string.Empty;

        /// <summary>
        /// New machine name to create
        /// </summary>
        public string NewMachineName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description for the new machine
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request model for storing default PLC
    /// </summary>
    public class StoreDefaultPlcRequest
    {
        /// <summary>
        /// Version name for this default PLC
        /// </summary>
        public string VersionName { get; set; } = string.Empty;

        /// <summary>
        /// PLC JSON data to store
        /// </summary>
        public string JsonData { get; set; } = string.Empty;

        /// <summary>
        /// Optional description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Mark this as the latest version
        /// </summary>
        public bool MarkAsLatest { get; set; } = true;

        /// <summary>
        /// User creating this version
        /// </summary>
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Response model for default PLC versions list
    /// </summary>
    public class DefaultPlcVersionInfo
    {
        /// <summary>
        /// Document ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Version name
        /// </summary>
        public string VersionName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Is this the latest version
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// Created by user
        /// </summary>
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request model for setting current machine
    /// </summary>
    public class SetCurrentMachineRequest
    {
        /// <summary>
        /// Machine name to switch to
        /// </summary>
        public string MachineName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for machine configuration sync status
    /// </summary>
    public class SyncStatusResponse
    {
        /// <summary>
        /// Whether MongoDB is currently online
        /// </summary>
        public bool MongoDbOnline { get; set; }

        /// <summary>
        /// Current machine name
        /// </summary>
        public string CurrentMachineName { get; set; } = string.Empty;

        /// <summary>
        /// Last successful sync time
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// Number of pending local changes
        /// </summary>
        public int PendingChanges { get; set; }

        /// <summary>
        /// Status message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
