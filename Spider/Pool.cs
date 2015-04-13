using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    /// <summary>
    /// Class Pool.
    /// </summary>
    internal static class Pool
    {
        /// <summary>
        /// The minimum users in a featured world that the crawler will crawl.
        /// </summary>
        private const int MinUsersFeatured = 3;
        /// <summary>
        /// The buffer amount. How many users should leave before the crawler leaves?
        /// </summary>
        private const int Buffer = 0;
        /// <summary>
        /// The minimum amount of users needed in order to crawl in a normal room.
        /// </summary>
        private const int MinimumUsers = 3;

        /// <summary>
        /// Adjust the crawler pool by checking whether the rooms have enough players.
        /// </summary>
        public static async void AutoAdjust()
        {
            Logger.Log(LogPriority.Debug, "Filling crawler pool...");

            while (Core.LobbyNew == null)
            {
                await Task.Delay(250);
            }

            var featuredWorlds = new List<string> { "PWKK8zFHH8bEI", "PWL2NjNOdhbEI", "PWbnzNQNi4a0I", "PWAIjKWOiLbEI" };
            foreach (var room in Core.LobbyNew)
            {
                if (room.Value >= MinimumUsers || (featuredWorlds.Contains(room.Key) && room.Value > MinUsersFeatured) ||
                    (room.Key.StartsWith("OW")))
                {
                    if (!Core.CrawlerTasks.ContainsKey(room.Key) || Core.CrawlerTasks.Count == 0)
                    {
                        var createCrawlerHandle = new AutoResetEvent(false);
                        Core.CreateCrawler(room.Key, createCrawlerHandle);
                        createCrawlerHandle.WaitOne();
                        await Task.Delay(3000); // if there is no delay, the crawler will never initialize.
                    }
                }
                else
                {
                    // the crawler tasks must contain the room key (required)
                    // either one of the following conditions must exist:
                    // (1) the amount of users in the room is less than or equal to the minimum users
                    // minus the buffer amount AND it is NOT an open world or
                    // (2) the amount of users in the room is more than one (including me), AND
                    // must be an open world.

                    // The reason why open worlds are crawled when there is only one user (me) is
                    // because open worlds can have the same ids, and so when compiling the data the
                    // similar ids can be mixed up, causing some of the data to be duplicated for another
                    // completely unrelated level. When there is only one user left (me) the world disappears
                    // and the session state is not preserved. If another world of the same id appears,
                    // I can be very confident that it is a different session and a different world.

                    if (Core.CrawlerTasks.ContainsKey(room.Key) &&
                        (room.Value < (MinimumUsers - Buffer) && !room.Key.StartsWith("OW")) ||
                        (room.Value <= 1 && room.Key.StartsWith("OW"))
                        )
                    {
                        Logger.Log(LogPriority.Debug, "Removing a crawler");
                        Core.RemoveCrawler(room.Key);
                    }
                }
            }
        }
    }
}