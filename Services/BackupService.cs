using System;
using System.IO;
using System.Linq;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for managing file backups with daily and versioned backup strategies
    /// </summary>
    public static class BackupService
    {
        private const int MaxDailyBackups = 100;
        private const int MaxVersionedBackups = 100;

        /// <summary>
        /// Create backups before saving a file
        /// Creates one daily backup per day and maintains versioned backups
        /// </summary>
        /// <param name="filePath">Full path to the file being saved</param>
        public static void CreateBackup(string filePath)
        {
            try
            {
                // Only create backup if the file already exists
                if (!File.Exists(filePath))
                {
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var extension = fileInfo.Extension;
                var dataDirectory = fileInfo.DirectoryName;

                if (string.IsNullOrEmpty(dataDirectory))
                {
                    LogWarning($"Could not determine directory for backup: {filePath}", "Backup");
                    return;
                }

                // Create backup directories
                var backupsRoot = Path.Combine(dataDirectory, "backups");
                var dailyBackupDir = Path.Combine(backupsRoot, "daily");
                var versionedBackupDir = Path.Combine(backupsRoot, fileName);

                Directory.CreateDirectory(dailyBackupDir);
                Directory.CreateDirectory(versionedBackupDir);

                // Create daily backup (one per day)
                CreateDailyBackup(filePath, dailyBackupDir, fileName, extension);

                // Create versioned backup (up to 100 versions)
                CreateVersionedBackup(filePath, versionedBackupDir, fileName, extension);

                // Cleanup old backups
                CleanupOldBackups(dailyBackupDir, MaxDailyBackups);
                CleanupOldBackups(versionedBackupDir, MaxVersionedBackups);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create backup for {filePath}: {ex.Message}", "Backup");
            }
        }

        /// <summary>
        /// Create daily backup - only one backup per day
        /// </summary>
        private static void CreateDailyBackup(string sourceFile, string dailyBackupDir, string fileName, string extension)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var dailyBackupFileName = $"{fileName}_{today}{extension}";
                var dailyBackupPath = Path.Combine(dailyBackupDir, dailyBackupFileName);

                // Only create daily backup if it doesn't already exist for today
                if (!File.Exists(dailyBackupPath))
                {
                    File.Copy(sourceFile, dailyBackupPath, overwrite: false);
                    LogInfo($"Created daily backup: {dailyBackupFileName}", "Backup");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to create daily backup: {ex.Message}", "Backup");
            }
        }

        /// <summary>
        /// Create versioned backup with timestamp
        /// </summary>
        private static void CreateVersionedBackup(string sourceFile, string versionedBackupDir, string fileName, string extension)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var versionedBackupFileName = $"{fileName}_{timestamp}{extension}";
                var versionedBackupPath = Path.Combine(versionedBackupDir, versionedBackupFileName);

                File.Copy(sourceFile, versionedBackupPath, overwrite: false);
                LogInfo($"Created versioned backup: {versionedBackupFileName}", "Backup");
            }
            catch (Exception ex)
            {
                LogError($"Failed to create versioned backup: {ex.Message}", "Backup");
            }
        }

        /// <summary>
        /// Remove old backups keeping only the specified maximum number
        /// </summary>
        private static void CleanupOldBackups(string backupDirectory, int maxBackups)
        {
            try
            {
                if (!Directory.Exists(backupDirectory))
                {
                    return;
                }

                var backupFiles = Directory.GetFiles(backupDirectory)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                if (backupFiles.Count <= maxBackups)
                {
                    return;
                }

                // Delete oldest backups beyond the limit
                var filesToDelete = backupFiles.Skip(maxBackups).ToList();
                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        fileToDelete.Delete();
                        LogDebug($"Deleted old backup: {fileToDelete.Name}", "Backup");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to delete old backup {fileToDelete.Name}: {ex.Message}", "Backup");
                    }
                }

                if (filesToDelete.Count > 0)
                {
                    LogInfo($"Cleaned up {filesToDelete.Count} old backup(s) from {backupDirectory}", "Backup");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup old backups in {backupDirectory}: {ex.Message}", "Backup");
            }
        }

        /// <summary>
        /// Get backup statistics for a file
        /// </summary>
        public static BackupStats GetBackupStats(string filePath)
        {
            var stats = new BackupStats();

            try
            {
                if (!File.Exists(filePath))
                {
                    return stats;
                }

                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var dataDirectory = fileInfo.DirectoryName;

                if (string.IsNullOrEmpty(dataDirectory))
                {
                    return stats;
                }

                var backupsRoot = Path.Combine(dataDirectory, "backups");
                var dailyBackupDir = Path.Combine(backupsRoot, "daily");
                var versionedBackupDir = Path.Combine(backupsRoot, fileName);

                if (Directory.Exists(dailyBackupDir))
                {
                    stats.DailyBackupCount = Directory.GetFiles(dailyBackupDir).Length;
                }

                if (Directory.Exists(versionedBackupDir))
                {
                    stats.VersionedBackupCount = Directory.GetFiles(versionedBackupDir).Length;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to get backup stats for {filePath}: {ex.Message}", "Backup");
            }

            return stats;
        }
    }

    /// <summary>
    /// Backup statistics
    /// </summary>
    public class BackupStats
    {
        /// <summary>
        /// Number of daily backups available
        /// </summary>
        public int DailyBackupCount { get; set; }

        /// <summary>
        /// Number of versioned backups available
        /// </summary>
        public int VersionedBackupCount { get; set; }
    }
}
