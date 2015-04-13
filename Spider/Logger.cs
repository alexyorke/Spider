using System;
using NodaTime;

namespace Spider
{
    //Logging class partially copied from Yonom's CupCake

    /// <summary>
    /// Class Logger.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// To a friendly string.
        /// </summary>
        /// <param name="me">Me.</param>
        /// <returns>System.String.</returns>
        private static string ToFriendlyString(LogPriority me)
        {
            //http://stackoverflow.com/questions/479410/enum-tostring-with-user-friendly-strings
            switch (me)
            {
                case LogPriority.Debug:
                    return "[DEBUG] ";
                case LogPriority.Info:
                    return "[INFO] ";
                case LogPriority.Warning:
                    return "[WARN] ";
                case LogPriority.Error:
                    return "[ERROR] ";
                default:
                    return "[INFO] ";
            }
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetDate()
        {
            return SystemClock.Instance.GetCurrentInstant().InUtc().ToInstant().ToString();
        }
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Log(string message)
        {

            Console.WriteLine("[" + GetDate() + "] " + ToFriendlyString(LogPriority.Debug) + message);
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <param name="message">The message.</param>
        public static void Log(LogPriority priority, string message)
        {
            Console.WriteLine("[" + GetDate() + "] " + ToFriendlyString(priority) + message);
            //Core.ShowEventRatePerMinute (false);
        }
    }
}