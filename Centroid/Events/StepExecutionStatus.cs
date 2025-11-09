namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Status of step execution
    /// </summary>
    public enum StepExecutionStatus
    {
        /// <summary>
        /// Step is about to be executed
        /// </summary>
        AboutToExecute,

        /// <summary>
        /// Step is currently executing
        /// </summary>
        Executing,

        /// <summary>
        /// Step completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Step failed with error
        /// </summary>
        Failed,

        /// <summary>
        /// Step was skipped
        /// </summary>
        Skipped
    }
}