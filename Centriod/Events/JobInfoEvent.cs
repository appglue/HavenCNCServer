using System;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Event containing job execution information
    /// </summary>
    public class JobInfoEvent : ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the job info event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Current executing line number in the G-code program
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Current stack level for nested programs or subroutines
        /// </summary>
        public int StackLevel { get; set; }
        
        /// <summary>
        /// Message associated with the job info event
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Name of the currently running job
        /// </summary>
        public string JobName { get; set; } = string.Empty;
    }
}