using System;
using System.Threading;

namespace Spider
{
    //http://stackoverflow.com/questions/7472013/how-to-create-a-thread-task-with-a-continuous-loop

    /// <summary>
    ///     Class CancellationTokenExtensions.
    /// </summary>
    internal static class CancellationTokenExtensions
    {
        /// <summary>
        ///     Waits the cancellation requested.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool WaitCancellationRequested(
            this CancellationToken token,
            TimeSpan timeout)
        {
            return token.WaitHandle.WaitOne(timeout);
        }
    }
}