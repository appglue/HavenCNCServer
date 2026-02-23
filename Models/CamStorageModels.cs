using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// CAM project document stored in MongoDB and local files
    /// </summary>
    public class CamProjectDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("projectId")]
        [BsonRequired]
        public string ProjectId { get; set; } = string.Empty;

        [BsonElement("machineName")]
        [BsonRequired]
        public string MachineName { get; set; } = string.Empty;

        [BsonElement("version")]
        public long Version { get; set; }

        [BsonElement("data")]
        [BsonRequired]
        public string Data { get; set; } = string.Empty;  // Full CAM project JSON

        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; }

        [BsonElement("metadata")]
        public CamProjectMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Lightweight CAM project metadata for list operations
    /// </summary>
    public class CamProjectMetadata
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to store a CAM project
    /// </summary>
    public class StoreCamProjectRequest
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;  // Full CAM project JSON
        public CamProjectMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Request to update CAM project category
    /// </summary>
    public class UpdateCamProjectCategoryRequest
    {
        public string? Category { get; set; }
    }
}
