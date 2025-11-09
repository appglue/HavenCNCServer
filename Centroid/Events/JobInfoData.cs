using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Data structure containing JOB_INFO message details
    /// </summary>
    public class JobInfoData
    {
        /// <summary>
        /// Timestamp when the message was received
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current executing line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Reported stack level
        /// </summary>
        public int StackLevel { get; set; }

        /// <summary>
        /// The current running job/program name
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Type of communication message
        /// </summary>
        public string CommunicationType { get; set; } = "";

        /// <summary>
        /// String representation of the job info
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] Line {LineNumber}, Stack {StackLevel}: {Message}";
        }
    }
}