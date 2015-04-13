namespace Spider
{
    /// <summary>
    /// Enum Log Priority
    /// </summary>
    public enum LogPriority
    {
        /// <summary>
        /// The debug priority. These messages can be ignored in production.
        /// </summary>
        Debug = -1,
        /// <summary>
        /// The information priority.
        /// </summary>
        Info = 0,
        /// <summary>
        /// The warning priority.
        /// </summary>
        Warning = 1,
        /// <summary>
        /// The error priority.
        /// </summary>
        Error = 2
    }
}