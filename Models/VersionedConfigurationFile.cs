using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Wrapper for configuration files with versioning metadata
    /// Ensures we can always determine which version is newer
    /// </summary>
    public class VersionedConfigurationFile
    {
        /// <summary>
        /// Metadata about this configuration file version
        /// </summary>
        [JsonPropertyName("metadata")]
        public ConfigurationMetadata Metadata { get; set; } = new();

        /// <summary>
        /// The actual configuration data as JSON string
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; } = "{}";

        /// <summary>
        /// Create a new versioned configuration file
        /// </summary>
        public static VersionedConfigurationFile Create(string fileName, string data, long? previousVersion = null)
        {
            return new VersionedConfigurationFile
            {
                Metadata = new ConfigurationMetadata
                {
                    FileName = fileName,
                    Version = (previousVersion ?? 0) + 1,
                    Timestamp = DateTime.UtcNow,
                    Hash = ComputeHash(data)
                },
                Data = data
            };
        }

        /// <summary>
        /// Compute SHA256 hash of data for integrity checking
        /// </summary>
        private static string ComputeHash(string data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify the data hash matches
        /// </summary>
        public bool VerifyHash()
        {
            return Metadata.Hash == ComputeHash(Data);
        }
    }

    /// <summary>
    /// Metadata for versioned configuration files
    /// </summary>
    public class ConfigurationMetadata
    {
        /// <summary>
        /// Incrementing version number - always increases, never repeats
        /// This is the primary comparison for determining which file is newer
        /// </summary>
        [JsonPropertyName("version")]
        public long Version { get; set; }

        /// <summary>
        /// UTC timestamp when this version was created
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Name of the configuration file
        /// </summary>
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 hash of the data for integrity verification
        /// </summary>
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;
    }
}
