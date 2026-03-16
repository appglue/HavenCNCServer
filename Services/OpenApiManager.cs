using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages OpenAPI specification generation and file operations
    /// </summary>
    public static class OpenApiManager
    {
        /// <summary>
        /// Auto-generates OpenAPI specification if it doesn't exist
        /// </summary>
        /// <param name="apiUrl">The base URL of the API server</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task AutoGenerateIfNeededAsync(string apiUrl)
        {
            try
            {
                LogInfo("Generating OpenAPI specification...", "OpenAPI");
                await GenerateSpecificationAsync(apiUrl);
            }
            catch (Exception ex)
            {
                LogError($"Auto-generation of OpenAPI specification failed: {ex.Message}", "OpenAPI");
                // Don't throw - auto-generation failures shouldn't crash the application
            }
        }

        /// <summary>
        /// Generates OpenAPI specification from the running API server
        /// </summary>
        /// <param name="apiUrl">The base URL of the API server</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task GenerateSpecificationAsync(string apiUrl)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var openApiUrl = $"{apiUrl}/swagger/v1/swagger.json";

            // Retry up to 10 times with 1s gap - server may not be ready immediately
            string? openApiJson = null;
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    var response = await httpClient.GetAsync(openApiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        openApiJson = await response.Content.ReadAsStringAsync();
                        LogInfo($"OpenAPI spec fetched on attempt {attempt}", "OpenAPI");
                        break;
                    }
                    var body = await response.Content.ReadAsStringAsync();
                    LogWarning($"OpenAPI fetch attempt {attempt} returned {response.StatusCode}, retrying...", "OpenAPI");
                    if (attempt == 1 || attempt == 10)
                        LogError($"Swagger error body: {body.Substring(0, Math.Min(1000, body.Length))}", "OpenAPI");
                }
                catch (Exception ex)
                {
                    LogWarning($"OpenAPI fetch attempt {attempt} failed: {ex.Message}, retrying...", "OpenAPI");
                }
                await Task.Delay(1000);
            }

            if (openApiJson == null)
                throw new HttpRequestException($"Failed to download OpenAPI specification from {openApiUrl} after 10 attempts");

            // Save to project root
            var projectRoot = Directory.GetCurrentDirectory();
            var openApiPath = Path.Combine(projectRoot, "openapi.json");
            await File.WriteAllTextAsync(openApiPath, openApiJson);

            LogSuccess("OpenAPI specification generated successfully!", "OpenAPI");
            LogInfo($"Saved to: {openApiPath}", "OpenAPI");
        }

        /// <summary>
        /// Gets the path to the OpenAPI specification file
        /// </summary>
        /// <returns>The full path to the openapi.json file</returns>
        public static string GetSpecificationPath()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            return Path.Combine(projectRoot, "openapi.json");
        }

        /// <summary>
        /// Checks if the OpenAPI specification file exists
        /// </summary>
        /// <returns>True if the file exists, false otherwise</returns>
        public static bool SpecificationExists()
        {
            return File.Exists(GetSpecificationPath());
        }

        /// <summary>
        /// Deletes the existing OpenAPI specification file
        /// </summary>
        /// <returns>True if the file was deleted, false if it didn't exist</returns>
        public static bool DeleteSpecification()
        {
            var path = GetSpecificationPath();
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    LogInfo("OpenAPI specification file deleted", "OpenAPI");
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to delete OpenAPI specification: {ex.Message}", "OpenAPI");
                    throw;
                }
            }
            return false;
        }
    }
}