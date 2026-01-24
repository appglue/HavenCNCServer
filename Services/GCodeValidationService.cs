using System;
using System.Linq;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for validating G-code commands and content
    /// </summary>
    public static class GCodeValidationService
    {
        /// <summary>
        /// Count valid G-code lines (excluding comments and empty lines)
        /// </summary>
        public static int CountValidLines(string gCodeText)
        {
            if (string.IsNullOrWhiteSpace(gCodeText))
            {
                return 0;
            }

            var lines = ParseGCodeLines(gCodeText);
            return lines.Count(line => !IsCommentOrEmptyLine(line));
        }

        /// <summary>
        /// Check if the G-code text contains exactly one command
        /// </summary>
        public static bool IsSingleCommand(string gCodeText)
        {
            return CountValidLines(gCodeText) == 1;
        }

        /// <summary>
        /// Parse G-code text into individual lines
        /// </summary>
        public static string[] ParseGCodeLines(string gCodeText)
        {
            if (string.IsNullOrWhiteSpace(gCodeText))
            {
                return Array.Empty<string>();
            }

            return gCodeText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();
        }

        /// <summary>
        /// Check if a line is a comment or empty
        /// </summary>
        public static bool IsCommentOrEmptyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            var trimmedLine = line.Trim();
            return trimmedLine.StartsWith(";") || trimmedLine.StartsWith("(");
        }

        /// <summary>
        /// Get G-code validation info for UI display
        /// </summary>
        public static GCodeValidationInfo GetValidationInfo(string gCodeText)
        {
            var validLineCount = CountValidLines(gCodeText);

            if (string.IsNullOrWhiteSpace(gCodeText))
            {
                return new GCodeValidationInfo
                {
                    ValidLineCount = 0,
                    IsSingleCommand = false,
                    IsValid = false,
                    ButtonText = "Run Single Command",
                    IsEnabled = false
                };
            }

            if (validLineCount == 1)
            {
                return new GCodeValidationInfo
                {
                    ValidLineCount = 1,
                    IsSingleCommand = true,
                    IsValid = true,
                    ButtonText = "Run Single Command",
                    IsEnabled = true
                };
            }

            if (validLineCount == 0)
            {
                return new GCodeValidationInfo
                {
                    ValidLineCount = 0,
                    IsSingleCommand = false,
                    IsValid = false,
                    ButtonText = "Run Single Command",
                    IsEnabled = false
                };
            }

            return new GCodeValidationInfo
            {
                ValidLineCount = validLineCount,
                IsSingleCommand = false,
                IsValid = false,
                ButtonText = $"Multiple Commands ({validLineCount})",
                IsEnabled = false
            };
        }

        /// <summary>
        /// Get display-friendly version of G-code line (truncated if too long)
        /// </summary>
        public static string GetDisplayLine(string gCodeLine, int maxLength = 40)
        {
            if (string.IsNullOrWhiteSpace(gCodeLine))
            {
                return string.Empty;
            }

            var trimmedLine = gCodeLine.Trim();
            if (trimmedLine.Length <= maxLength)
            {
                return trimmedLine;
            }

            return trimmedLine.Substring(0, maxLength) + "...";
        }
    }

    /// <summary>
    /// G-code validation information for UI display
    /// </summary>
    public class GCodeValidationInfo
    {
        /// <summary>
        /// Gets or sets the number of valid G-code lines
        /// </summary>
        public int ValidLineCount { get; set; }

        /// <summary>
        /// Gets or sets whether the G-code contains exactly one command
        /// </summary>
        public bool IsSingleCommand { get; set; }

        /// <summary>
        /// Gets or sets whether the G-code is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the button text to display for the validation result
        /// </summary>
        public string ButtonText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the button should be enabled
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
