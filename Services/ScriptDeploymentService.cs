using System;
using System.IO;
using System.Windows.Forms;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for deploying CNC scripts and configuration files to CNC12 directories
    /// </summary>
    public static class ScriptDeploymentService
    {
        /// <summary>
        /// Deploy script files to CNC12 directories on startup
        /// </summary>
        public static void DeployScriptsToCnc12()
        {
            try
            {
                LogInfo("=== Starting CopyScriptFilesToCnc12 ===", "Startup");

                string cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
                string appPath = AppDomain.CurrentDomain.BaseDirectory;

                LogInfo($"CNC12 Path: {cnc12Path}", "Startup");
                LogInfo($"App Path: {appPath}", "Startup");

                // Source files
                string plcMsgSource = Path.Combine(appPath, "Centroid", "Scripts", "plcmsg.txt");
                string functionsSource = Path.Combine(appPath, "Centroid", "Scripts", "functions.xml");
                string plcSourceTemplate = Path.Combine(appPath, "Centroid", "Scripts", "acorn_router_plc.src");

                LogInfo($"Source files:", "Startup");
                LogInfo($"  plcmsg.txt: {plcMsgSource} (Exists: {File.Exists(plcMsgSource)})", "Startup");
                LogInfo($"  functions.xml: {functionsSource} (Exists: {File.Exists(functionsSource)})", "Startup");
                LogInfo($"  acorn_router_plc.src: {plcSourceTemplate} (Exists: {File.Exists(plcSourceTemplate)})", "Startup");

                // Destination paths for plcmsg.txt
                string plcMsgDest1 = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "router_plcmsg.txt");
                string plcMsgDest2 = Path.Combine(cnc12Path, "plcmsg.txt");

                // Destination path for functions.xml
                string functionsDest = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "functions.xml");

                // Destination path for PLC source
                string plcSourceDest = Path.Combine(cnc12Path, "acorn_router_plc.src");

                // Copy plcmsg.txt to both locations
                if (File.Exists(plcMsgSource))
                {
                    CopyPlcMessageFile(plcMsgSource, plcMsgDest1);
                    CopyPlcMessageFile(plcMsgSource, plcMsgDest2);
                }
                else
                {
                    LogWarning($"❌ Source file not found: {plcMsgSource}", "Startup");
                }

                // Copy functions.xml
                if (File.Exists(functionsSource))
                {
                    CopyFunctionsFile(functionsSource, functionsDest);
                }
                else
                {
                    LogWarning($"❌ Source file not found: {functionsSource}", "Startup");
                }

                // Copy PLC source template if destination doesn't have our logic
                if (File.Exists(plcSourceTemplate))
                {
                    DeployPlcSourceTemplate(plcSourceTemplate, plcSourceDest);
                }
                else
                {
                    LogWarning($"❌ PLC source template not found: {plcSourceTemplate}", "Startup");
                }

                LogInfo("=== Finished CopyScriptFilesToCnc12 ===", "Startup");
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to copy script files: {ex.Message}", "Startup");
                LogError($"Stack trace: {ex.StackTrace}", "Startup");
            }
        }

        /// <summary>
        /// Copy plcmsg.txt file to destination
        /// </summary>
        private static void CopyPlcMessageFile(string sourcePath, string destPath)
        {
            // Create directories if they don't exist
            string dir = Path.GetDirectoryName(destPath)!;
            LogInfo($"Creating directory (if needed): {dir}", "Startup");
            Directory.CreateDirectory(dir);

            LogInfo($"Copying plcmsg.txt to: {destPath}", "Startup");
            File.Copy(sourcePath, destPath, overwrite: true);
            LogSuccess($"✓ Copied plcmsg.txt to {destPath}", "Startup");
        }

        /// <summary>
        /// Copy functions.xml file to destination
        /// </summary>
        private static void CopyFunctionsFile(string sourcePath, string destPath)
        {
            // Create directory if it doesn't exist
            string dir = Path.GetDirectoryName(destPath)!;
            LogInfo($"Creating directory (if needed): {dir}", "Startup");
            Directory.CreateDirectory(dir);

            LogInfo($"Copying functions.xml to: {destPath}", "Startup");
            File.Copy(sourcePath, destPath, overwrite: true);
            LogSuccess($"✓ Copied functions.xml to {destPath}", "Startup");
        }

        /// <summary>
        /// Deploy PLC source template with HavenCNC logic
        /// </summary>
        private static void DeployPlcSourceTemplate(string templatePath, string destPath)
        {
            bool shouldCopy = false;
            bool needsConfirmation = false;

            if (!File.Exists(destPath))
            {
                // File doesn't exist, copy it without confirmation
                shouldCopy = true;
                LogInfo($"PLC source file not found at {destPath}, will copy template", "Startup");
            }
            else
            {
                // Check if existing file has our HavenCNC markup
                LogInfo($"Checking existing PLC source at {destPath} for HavenCNC markup...", "Startup");
                string existingContent = File.ReadAllText(destPath);

                // Look for the HavenCNC comment markers around the M52-M67 output handling
                string havenCncMarker = "; -- HavenCNC";

                if (!existingContent.Contains(havenCncMarker))
                {
                    // HavenCNC logic not found, ask user if they want to update
                    needsConfirmation = true;
                    LogWarning($"⚠️ PLC source file exists but doesn't contain HavenCNC markup (M52-M67 output handling)", "Startup");

                    var result = MessageBox.Show(
                        "The PLC source file at:\n" +
                        $"{destPath}\n\n" +
                        "does not contain the HavenCNC output control logic (M52-M67).\n\n" +
                        "Would you like to update it with the HavenCNC version?\n\n" +
                        "The existing file will be backed up before updating.",
                        "Update PLC Source File?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        shouldCopy = true;
                        LogInfo($"User confirmed PLC source update", "Startup");
                    }
                    else
                    {
                        LogInfo($"User declined PLC source update", "Startup");
                    }
                }
                else
                {
                    LogSuccess($"✓ PLC source file already contains HavenCNC markup, skipping copy", "Startup");
                }
            }

            if (shouldCopy)
            {
                // Always backup existing file if it exists
                if (File.Exists(destPath))
                {
                    BackupExistingFile(destPath);
                }

                LogInfo($"Copying PLC source template to: {destPath}", "Startup");
                File.Copy(templatePath, destPath, overwrite: true);
                LogSuccess($"✓ Copied PLC source template with HavenCNC logic to {destPath}", "Startup");

                if (needsConfirmation)
                {
                    MessageBox.Show(
                        "PLC source file has been updated successfully.\n\n" +
                        $"Backup saved to:\n{destPath}.backup_[timestamp]\n\n" +
                        "The new file includes M52-M67 output control logic.",
                        "Update Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Backup an existing file with timestamp
        /// </summary>
        private static void BackupExistingFile(string filePath)
        {
            string backupPath = filePath + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            LogInfo($"Backing up existing PLC source to: {backupPath}", "Startup");
            File.Copy(filePath, backupPath, overwrite: true);
            LogSuccess($"✓ Backed up existing PLC source to {backupPath}", "Startup");
        }
    }
}
