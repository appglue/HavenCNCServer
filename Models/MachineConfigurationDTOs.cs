using System;
using System.Collections.Generic;

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
        public List<CentroidConfigUtil.IOFunction>? Inputs { get; set; }

        /// <summary>
        /// Output I/O function assignments
        /// </summary>
        public List<CentroidConfigUtil.IOFunction>? Outputs { get; set; }

        /// <summary>
        /// Axis configurations
        /// </summary>
        public List<CentroidConfigUtil.AxisConfiguration>? Axes { get; set; }

        /// <summary>
        /// Spindle configuration (required)
        /// </summary>
        public CentroidConfigUtil.SpindleConfiguration Spindle { get; set; } = new();

        /// <summary>
        /// Probe configuration (optional)
        /// </summary>
        public CentroidConfigUtil.ProbeConfiguration? Probe { get; set; }

        /// <summary>
        /// PWM output configurations (optional)
        /// </summary>
        public List<CentroidConfigUtil.PWMConfiguration>? PWMOutputs { get; set; }

        /// <summary>
        /// ATC configuration (optional)
        /// </summary>
        public CentroidConfigUtil.ATCConfiguration? ATC { get; set; }
    }

    /// <summary>
    /// I/O configuration DTO for API
    /// </summary>
    public class IOConfiguration
    {
        /// <summary>
        /// Input I/O function assignments
        /// </summary>
        public List<CentroidConfigUtil.IOFunction>? Inputs { get; set; }

        /// <summary>
        /// Output I/O function assignments
        /// </summary>
        public List<CentroidConfigUtil.IOFunction>? Outputs { get; set; }
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
}