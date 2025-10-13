namespace HavenCNCServer.Models
{
    /// <summary>
    /// Probe type enumeration
    /// </summary>
    public enum ProbeType
    {
        /// <summary>
        /// Mechanical touch probe
        /// </summary>
        Mechanical = 0,
        
        /// <summary>
        /// Electronic probe
        /// </summary>
        Electronic = 1
    }

    /// <summary>
    /// Probe input state when tripped
    /// </summary>
    public enum ProbeInputState
    {
        /// <summary>
        /// Input opens when probe is tripped
        /// </summary>
        Open = 0,
        
        /// <summary>
        /// Input closes when probe is tripped  
        /// </summary>
        Closed = 1
    }

public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents probe configuration
        /// </summary>
        public class ProbeConfiguration
        {
            /// <summary>
            /// Probe PLC input number
            /// </summary>
            public int? ProbePLCInput { get; set; }
            
            /// <summary>
            /// Probe type (Mechanical, Electronic)
            /// </summary>
            public ProbeType? ProbeType { get; set; }
            
            /// <summary>
            /// Input state when probe is tripped (Open/Closed)
            /// </summary>
            public ProbeInputState? InputStateWhenTripped { get; set; }
            
            /// <summary>
            /// Probe tool number
            /// </summary>
            public int? ProbeToolNumber { get; set; }
            
            /// <summary>
            /// Fast probe rate
            /// </summary>
            public double? FastProbeRate { get; set; }
            
            /// <summary>
            /// Slow probe rate
            /// </summary>
            public double? SlowProbeRate { get; set; }
            
            /// <summary>
            /// Recovery distance after probe contact
            /// </summary>
            public double? RecoveryDistance { get; set; }
            
            /// <summary>
            /// Maximum probing distance
            /// </summary>
            public double? MaximumProbingDistance { get; set; }
            
            /// <summary>
            /// Probe protection enabled
            /// </summary>
            public bool? ProbeProtectionEnabled { get; set; }
            
            /// <summary>
            /// Probe protection based on tool number
            /// </summary>
            public bool? ProbeProtectionBasedOnToolNumber { get; set; }
            
            /// <summary>
            /// Display warning to verify that probe is functioning properly
            /// </summary>
            public bool? DisplayWarningToVerifyProbe { get; set; }
            
            /// <summary>
            /// Inhibit spindle when detect is on (Green)
            /// </summary>
            public bool? InhibitSpindleWhenDetectOn { get; set; }
            
            /// <summary>
            /// Probe slow jog speeds for each axis (in/min)
            /// Array index corresponds to axis (0=Axis1, 1=Axis2, etc.)
            /// </summary>
            public double[]? ProbeSlowJogSpeeds { get; set; }
            
            /// <summary>
            /// Probe fast jog negative speeds for each axis (in/min)
            /// Array index corresponds to axis (0=Axis1, 1=Axis2, etc.)
            /// </summary>
            public double[]? ProbeFastJogNegativeSpeeds { get; set; }
            
            /// <summary>
            /// Probe fast jog positive speeds for each axis (in/min)
            /// Array index corresponds to axis (0=Axis1, 1=Axis2, etc.)
            /// </summary>
            public double[]? ProbeFastJogPositiveSpeeds { get; set; }
            
            // Legacy properties for backward compatibility
            /// <summary>
            /// Probe input number (legacy - use ProbePLCInput instead)
            /// </summary>
            public int? InputNumber { get; set; }
            
            /// <summary>
            /// Probe input type (legacy - use InputStateWhenTripped instead)
            /// </summary>
            public int? InputType { get; set; }
            
            /// <summary>
            /// Probe feed rate (legacy - use FastProbeRate/SlowProbeRate instead)
            /// </summary>
            public double? FeedRate { get; set; }
            
            /// <summary>
            /// Touch plate thickness
            /// </summary>
            public double? TouchPlateThickness { get; set; }
            
            /// <summary>
            /// Touch plate input number (if different from probe)
            /// </summary>
            public int? TouchPlateInputNumber { get; set; }
            
            /// <summary>
            /// Touch plate input type
            /// </summary>
            public int? TouchPlateInputType { get; set; }
            
            /// <summary>
            /// Probe type configuration (legacy - use ProbeType enum instead)
            /// </summary>
            public int? ProbeTypeInt { get; set; }
            
            /// <summary>
            /// Display probe warning (legacy - use DisplayWarningToVerifyProbe instead)
            /// </summary>
            public bool? DisplayProbeWarning { get; set; }
            
            /// <summary>
            /// Probe protection/inhibit settings (legacy)
            /// </summary>
            public int? ProbeInhibit { get; set; }
        }

    }
}
