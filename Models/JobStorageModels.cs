using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Job document stored in MongoDB and local files
    /// </summary>
    public class JobDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("jobId")]
        [BsonRequired]
        public string JobId { get; set; } = string.Empty;

        [BsonElement("machineName")]
        [BsonRequired]
        public string MachineName { get; set; } = string.Empty;

        [BsonElement("version")]
        public long Version { get; set; }

        [BsonElement("data")]
        [BsonRequired]
        public string Data { get; set; } = string.Empty;  // Full job JSON

        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; }

        [BsonElement("metadata")]
        public JobMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Lightweight job metadata for list operations
    /// </summary>
    public class JobMetadata
    {
        public string Name { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public DateTime? LastRunDate { get; set; }
        public string? Category { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
        public string? MaterialType { get; set; }
        public string? EstimatedTime { get; set; }
    }

    /// <summary>
    /// G-Code file document stored in MongoDB
    /// Local managed files stored by ID: {fileId}.nc
    /// </summary>
    public class GCodeFileDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("fileId")]
        [BsonRequired]
        public string FileId { get; set; } = string.Empty;  // GUID for managed files

        [BsonElement("fileName")]
        [BsonRequired]
        public string FileName { get; set; } = string.Empty;  // Original filename

        [BsonElement("machineName")]
        [BsonRequired]
        public string MachineName { get; set; } = string.Empty;

        [BsonElement("version")]
        public long Version { get; set; }

        [BsonElement("data")]
        [BsonRequired]
        public string Data { get; set; } = string.Empty;  // G-code content

        [BsonElement("timestamp")]
        [BsonRequired]
        public DateTime Timestamp { get; set; }

        [BsonElement("size")]
        public long Size { get; set; }

        [BsonElement("category")]
        public string? Category { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("materialType")]
        public string? MaterialType { get; set; }

        [BsonElement("estimatedTime")]
        public string? EstimatedTime { get; set; }
    }

    /// <summary>
    /// G-Code file metadata (for list operations)
    /// Includes source indicator (managed vs external)
    /// </summary>
    public class GCodeFileMetadata
    {
        public string? FileId { get; set; }  // Null for external files, GUID for managed
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;  // Full path or "managed"
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? MaterialType { get; set; }
        public string? EstimatedTime { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public bool IsManaged { get; set; }  // True if in MongoDB/managed directory
    }

    /// <summary>
    /// Request model for paging and sorting
    /// </summary>
    public class PageRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Paged result wrapper
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Request to list G-code files
    /// </summary>
    public class ListGCodeFilesRequest
    {
        public string[] Directories { get; set; } = Array.Empty<string>();
        public PageRequest Paging { get; set; } = new();
    }

    /// <summary>
    /// Request to store a job
    /// </summary>
    public class StoreJobRequest
    {
        public string JobId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;  // Full job JSON
        public JobMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Request to store a G-code file
    /// </summary>
    public class StoreGCodeFileRequest
    {
        public string? FileId { get; set; }  // Optional - generated if not provided
        public string FileName { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;  // G-code content
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? MaterialType { get; set; }
        public string? EstimatedTime { get; set; }
    }

    /// <summary>
    /// Request to save last job
    /// </summary>
    public class SaveLastJobRequest
    {
        public string JobId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response for store operations
    /// </summary>
    public class StoreResponse
    {
        public bool Success { get; set; }
        public string? Id { get; set; }  // Generated ID for new items
        public string? Message { get; set; }
    }
}
