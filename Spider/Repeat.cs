using System;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    //http://stackoverflow.com/questions/7472013/how-to-create-a-thread-task-with-a-continuous-loop
    /// <summary>
    ///     Class Repeat.
    /// </summary>
    internal static class Repeat
    {
        /// <summary>
        ///     Interval for the specified poll interval.
        /// </summary>
        /// <param name="pollInterval">The poll interval.</param>
        /// <param name="action">The action.</param>
        /// <param name="token">The token.</param>
        /// <returns>Task.</returns>
        public static Task Interval(
            TimeSpan pollInterval,
            Action action,
            CancellationToken token)
        {
            // We don't use Observable.Interval:
            // If we block, the values start bunching up behind each other.
            return Task.Factory.StartNew(
                () =>
                {
                    for (;;)
                    {
                        if (token.WaitCancellationRequested(pollInterval))
                            break;

                        action();
                    }
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}