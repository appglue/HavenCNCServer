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
        /// Input I/O function assignments
        /// </summary>
        public List<IOFunction>? Inputs { get; set; }

        /// <summary>
        /// Output I/O function assignments
        /// </summary>
        public List<IOFunction>? Outputs { get; set; }

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
        /// ATC configuration (optional)
        /// </summary>
        public ATCConfiguration? ATC { get; set; }
    }

    /// <summary>
    /// I/O configuration DTO for API
    /// </summary>
    public class IOConfiguration
    {
        /// <summary>
        /// Input I/O function assignments
        /// </summary>
        public List<IOFunction>? Inputs { get; set; }

        /// <summary>
        /// Output I/O function assignments
        /// </summary>
        public List<IOFunction>? Outputs { get; set; }
    }

    /// <summary>
    /// I/O number availability response DTO
    /// </summary>
    public class IOAvailabilityResponse
    {
        /// <summary>
        /// Available input port numbers
        /// </summary>
        public int[] AvailableInputs { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Available output port numbers
        /// </summary>
        public int[] AvailableOutputs { get; set; } = Array.Empty<int>();

        /// <summary>
        /// System information string
        /// </summary>
        public string SystemInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Input inversion configuration DTO
    /// </summary>
    public class InputInversionConfiguration
    {
        /// <summary>
        /// Dictionary of input number to invert setting
        /// </summary>
        public Dictionary<int, bool> InputSettings { get; set; } = new();
    }

    /// <summary>
    /// Validation result DTO for configuration validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// List of validation issues found
        /// </summary>
        public string[] Issues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Input count
        /// </summary>
        public int InputCount { get; set; }

        /// <summary>
        /// Output count
        /// </summary>
        public int OutputCount { get; set; }
    }

    /// <summary>
    /// ATC validation result DTO for ATC configuration validation
    /// </summary>
    public class ATCValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// List of validation issues found
        /// </summary>
        public string[] Issues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// ATC type
        /// </summary>
        public string Type { get; set; } = string.Empty;
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

    /// <summary>
    /// Travel limits for a single axis
    /// </summary>
    public class AxisTravelLimits
    {
        /// <summary>
        /// Axis number (1-8)
        /// </summary>
        public int AxisNumber { get; set; }

        /// <summary>
        /// Axis label (X, Y, Z, A, B, C, U, V, W)
        /// </summary>
        public string AxisLabel { get; set; } = string.Empty;

        /// <summary>
        /// Plus travel limit (maximum position)
        /// </summary>
        public double PlusLimit { get; set; }

        /// <summary>
        /// Minus travel limit (minimum position)
        /// </summary>
        public double MinusLimit { get; set; }
    }

    /// <summary>
    /// Travel limits for all configured axes
    /// </summary>
    public class TravelLimitsResponse
    {
        /// <summary>
        /// Travel limits for each axis
        /// </summary>
        public List<AxisTravelLimits> Axes { get; set; } = new();

        /// <summary>
        /// Descriptive message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}