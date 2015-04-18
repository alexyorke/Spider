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
        private const int MinimumUsers = 5;

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
                if ((room.Value >= MinimumUsers && !room.Key.StartsWith("OW")|| (featuredWorlds.Contains(room.Key) && room.Value > MinUsersFeatured)))
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
                    // (1) the amount of users in the room is less than or equal to the minimum users
                    // minus the buffer amount AND it is NOT an open world or


                    if (Core.CrawlerTasks.ContainsKey(room.Key) &&
                        (room.Value < (MinimumUsers - Buffer))
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