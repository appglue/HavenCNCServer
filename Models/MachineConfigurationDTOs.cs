using System;
using System.Collections.Generic;
using HavenCNCServer.Centroid.Data;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Complete machine configuration DTO for API
    /// </summary>
    public class CompleteMachineConfiguration
    {
        /// <summary>
        /// Axis configurations
        /// </summary>
        public List<AxisConfiguration>? Axes { get; set; }

        /// <summary>
        /// Spindle configuration (required)
        /// </summary>
        public SpindleConfiguration Spindle { get; set; } = new();

        /// <summary>
        /// Probe configuration (optional)
        /// </summary>
        public ProbeConfiguration? Probe { get; set; }

        /// <summary>
        /// PWM output configurations (optional)
        /// </summary>
        public List<PWMConfiguration>? PWMOutputs { get; set; }

        /// <summary>
        /// Global system configuration (optional)
        /// Includes step frequency, drive fault delay, and other global settings
        /// </summary>
        public GlobalSystemConfiguration? GlobalSystem { get; set; }
    }

    /// <summary>
    /// Parameter value DTO for parameter operations
    /// </summary>
    public class ParameterValue
    {
        /// <summary>
        /// Parameter number
        /// </summary>
        public int Parameter { get; set; }

        /// <summary>
        /// Parameter name
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Parameter value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Descriptive message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parameter information DTO for available parameters
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Parameter number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parameter description
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available parameters response DTO
    /// </summary>
    public class AvailableParametersResponse
    {
        /// <summary>
        /// Descriptive message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Total parameter count
        /// </summary>
        public int ParameterCount { get; set; }

        /// <summary>
        /// List of available parameters
        /// </summary>
        public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();
    }
}