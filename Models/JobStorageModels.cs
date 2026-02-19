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
        public string Id { get; set; } = string.Empty;  // Job identifier for fetching
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
        public int LineCount { get; set; }   // Number of non-empty lines
    }

    /// <summary>
    /// Request to list jobs — replaces old paged PageRequest
    /// </summary>
    public class ListJobsRequest
    {
        public int? MaxCount { get; set; }       // Null = all (capped at 500)
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Request to list G-code files (no paging)
    /// </summary>
    public class ListGCodeFilesRequest
    {
        public string[] Directories { get; set; } = Array.Empty<string>();
        public string[] FileExtensions { get; set; } = new[] { ".nc", ".txt", ".tap" };
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Full G-code file: metadata + content combined
    /// Returned by GET /gcode
    /// </summary>
    public class GCodeFileData
    {
        public string? FileId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? MaterialType { get; set; }
        public string? EstimatedTime { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public int LineCount { get; set; }
        public bool IsManaged { get; set; }
    }

    /// <summary>
    /// Request to update a job's category
    /// </summary>
    public class UpdateJobCategoryRequest
    {
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to update a G-code file's category
    /// </summary>
    public class UpdateGCodeCategoryRequest
    {
        public string? FileId { get; set; }       // For managed files
        public string? Directory { get; set; }    // For external files
        public string? FileName { get; set; }     // For external files
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to search jobs
    /// </summary>
    public class JobSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int MaxCount { get; set; } = 20;
    }

    /// <summary>
    /// Request to search G-code files
    /// </summary>
    public class GCodeSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public string[]? Directories { get; set; }
        public string[]? FileExtensions { get; set; }
        public int MaxCount { get; set; } = 20;
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

    /// <summary>
    /// Drive information for filesystem navigation
    /// </summary>
    public class DriveInfoResponse
    {
        public string Name { get; set; } = string.Empty;  // e.g., "C:\"
        public string Label { get; set; } = string.Empty;  // Volume label
        public string DriveType { get; set; } = string.Empty;  // Fixed, Removable, Network, etc.
        public long TotalSize { get; set; }
        public long AvailableSpace { get; set; }
        public bool IsReady { get; set; }
    }

    /// <summary>
    /// Directory information for filesystem navigation
    /// </summary>
    public class DirectoryInfoResponse
    {
        public string Name { get; set; } = string.Empty;  // Directory name only
        public string FullPath { get; set; } = string.Empty;  // Full path
        public bool HasSubdirectories { get; set; }
        public DateTime LastModified { get; set; }
    }
}
