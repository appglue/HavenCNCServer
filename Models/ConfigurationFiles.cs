using System.Collections.Generic;
using System.Linq;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Centralized list of configuration files that are synchronized with MongoDB
    /// </summary>
    public static class ConfigurationFiles
    {
        /// <summary>
        /// All configuration files that should be synchronized with MongoDB
        /// </summary>
        public static readonly string[] SyncedFiles = new[]
        {
            "plcSystem.json",
            "plcSystemDefault.json",
            "configuration.json",
            "machine.json",
            "machineState.json",
            "fixtures.json",
            "materials.json",
            "tools.json",
            "userActionData.json",
            "setupChecklist.json"
        };

        /// <summary>
        /// Check if a file is a synced configuration file
        /// </summary>
        public static bool IsSyncedFile(string fileName)
        {
            return SyncedFiles.Contains(fileName);
        }

        /// <summary>
        /// Get all synced file names
        /// </summary>
        public static IEnumerable<string> GetSyncedFiles()
        {
            return SyncedFiles;
        }
    }
}
