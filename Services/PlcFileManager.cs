using System;
using System.IO;
using System.Windows.Forms;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages PLC source file deployment and updates
    /// </summary>
    public static class PlcFileManager
    {
        private const string HavenCncMarker = "; -- HavenCNC";

        /// <summary>
        /// Checks if a PLC file contains HavenCNC markup
        /// </summary>
        public static bool HasHavenCncMarkup(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                string content = File.ReadAllText(filePath);
                return content.Contains(HavenCncMarker);
            }
            catch (Exception ex)
            {
                LogError($"Error checking PLC file for HavenCNC markup: {ex.Message}", "PlcFileManager");
                return false;
            }
        }

        /// <summary>
        /// Copies script files (plcmsg.txt, functions.xml, acorn_router_plc.src) to CNC12 directory
        /// </summary>
        public static void CopyScriptFilesToCnc12(bool showConfirmation = true)
        {
            try
            {
                LogInfo("=== Starting CopyScriptFilesToCnc12 ===", "PlcFileManager");

                string cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
                string appPath = AppDomain.CurrentDomain.BaseDirectory;

                LogInfo($"CNC12 Path: {cnc12Path}", "PlcFileManager");
                LogInfo($"App Path: {appPath}", "PlcFileManager");

                // Source files
                string plcMsgSource = Path.Combine(appPath, "Centroid", "Scripts", "plcmsg.txt");
                string functionsSource = Path.Combine(appPath, "Centroid", "Scripts", "functions.xml");
                string plcSourceTemplate = Path.Combine(appPath, "Centroid", "Scripts", "acorn_router_plc.src");

                LogInfo($"Source files:", "PlcFileManager");
                LogInfo($"  plcmsg.txt: {plcMsgSource} (Exists: {File.Exists(plcMsgSource)})", "PlcFileManager");
                LogInfo($"  functions.xml: {functionsSource} (Exists: {File.Exists(functionsSource)})", "PlcFileManager");
                LogInfo($"  acorn_router_plc.src: {plcSourceTemplate} (Exists: {File.Exists(plcSourceTemplate)})", "PlcFileManager");

                // Destination paths for plcmsg.txt
                string plcMsgDest1 = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "router_plcmsg.txt");
                string plcMsgDest2 = Path.Combine(cnc12Path, "plcmsg.txt");

                // Destination path for functions.xml
                string functionsDest = Path.Combine(cnc12Path, "resources", "wizard", "default", "plc", "functions.xml");

                // Destination path for PLC source
                string plcSourceDest = Path.Combine(cnc12Path, "acorn_router_plc.src");

                // Copy plcmsg.txt to both locations
                CopyPlcMsgFile(plcMsgSource, plcMsgDest1, plcMsgDest2);

                // Copy functions.xml
                CopyFunctionsFile(functionsSource, functionsDest);

                // Copy PLC source template if needed
                CopyPlcSourceFile(plcSourceTemplate, plcSourceDest, showConfirmation);

                LogInfo("=== Finished CopyScriptFilesToCnc12 ===", "PlcFileManager");
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to copy script files: {ex.Message}", "PlcFileManager");
                LogError($"Stack trace: {ex.StackTrace}", "PlcFileManager");
                throw;
            }
        }

        /// <summary>
        /// Copies the PLC message file to destination locations
        /// </summary>
        private static void CopyPlcMsgFile(string sourcePath, string dest1, string dest2)
        {
            if (File.Exists(sourcePath))
            {
                // Create directories if they don't exist
                string dir1 = Path.GetDirectoryName(dest1)!;
                LogInfo($"Creating directory (if needed): {dir1}", "PlcFileManager");
                Directory.CreateDirectory(dir1);

                LogInfo($"Copying plcmsg.txt to: {dest1}", "PlcFileManager");
                File.Copy(sourcePath, dest1, overwrite: true);
                LogSuccess($"✓ Copied plcmsg.txt to {dest1}", "PlcFileManager");

                LogInfo($"Copying plcmsg.txt to: {dest2}", "PlcFileManager");
                File.Copy(sourcePath, dest2, overwrite: true);
                LogSuccess($"✓ Copied plcmsg.txt to {dest2}", "PlcFileManager");
            }
            else
            {
                LogWarning($"❌ Source file not found: {sourcePath}", "PlcFileManager");
            }
        }

        /// <summary>
        /// Copies the functions.xml file to destination
        /// </summary>
        private static void CopyFunctionsFile(string sourcePath, string destPath)
        {
            if (File.Exists(sourcePath))
            {
                // Create directory if it doesn't exist
                string dir = Path.GetDirectoryName(destPath)!;
                LogInfo($"Creating directory (if needed): {dir}", "PlcFileManager");
                Directory.CreateDirectory(dir);

                LogInfo($"Copying functions.xml to: {destPath}", "PlcFileManager");
                File.Copy(sourcePath, destPath, overwrite: true);
                LogSuccess($"✓ Copied functions.xml to {destPath}", "PlcFileManager");
            }
            else
            {
                LogWarning($"❌ Source file not found: {sourcePath}", "PlcFileManager");
            }
        }

        /// <summary>
        /// Copies the PLC source file to destination if it doesn't have HavenCNC markup
        /// </summary>
        private static void CopyPlcSourceFile(string templatePath, string destPath, bool showConfirmation)
        {
            if (!File.Exists(templatePath))
            {
                LogWarning($"❌ PLC source template not found: {templatePath}", "PlcFileManager");
                return;
            }

            bool shouldCopy = false;
            bool needsConfirmation = false;

            if (!File.Exists(destPath))
            {
                // File doesn't exist, copy it without confirmation
                shouldCopy = true;
                LogInfo($"PLC source file not found at {destPath}, will copy template", "PlcFileManager");
            }
            else
            {
                // Check if existing file has our HavenCNC markup
                LogInfo($"Checking existing PLC source at {destPath} for HavenCNC markup...", "PlcFileManager");

                if (!HasHavenCncMarkup(destPath))
                {
                    // HavenCNC logic not found
                    needsConfirmation = true;
                    LogWarning($"⚠️ PLC source file exists but doesn't contain HavenCNC markup (M52-M67 output handling)", "PlcFileManager");

                    if (showConfirmation)
                    {
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
                            LogInfo($"User confirmed PLC source update", "PlcFileManager");
                        }
                        else
                        {
                            LogInfo($"User declined PLC source update", "PlcFileManager");
                        }
                    }
                    else
                    {
                        // No confirmation needed (e.g., manual button press)
                        shouldCopy = true;
                    }
                }
                else
                {
                    LogSuccess($"✓ PLC source file already contains HavenCNC markup, skipping copy", "PlcFileManager");
                }
            }

            if (shouldCopy)
            {
                // Always backup existing file if it exists
                if (File.Exists(destPath))
                {
                    string backupPath = destPath + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    LogInfo($"Backing up existing PLC source to: {backupPath}", "PlcFileManager");
                    File.Copy(destPath, backupPath, overwrite: true);
                    LogSuccess($"✓ Backed up existing PLC source to {backupPath}", "PlcFileManager");
                }

                LogInfo($"Copying PLC source template to: {destPath}", "PlcFileManager");
                File.Copy(templatePath, destPath, overwrite: true);
                LogSuccess($"✓ Copied PLC source template with HavenCNC logic to {destPath}", "PlcFileManager");

                if (needsConfirmation && showConfirmation)
                {
                    MessageBox.Show(
                        "PLC source file has been updated successfully.\n\n" +
                        $"Backup saved to:\n{Path.GetFileName(destPath)}.backup_[timestamp]\n\n" +
                        "The new file includes M52-M67 output control logic.",
                        "Update Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }
    }
}
