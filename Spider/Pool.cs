using System.Threading;

namespace Spider
{
    /// <summary>
    ///     Class Pool.
    /// </summary>
    internal static class Pool
    {
        private const int MinUsersFeatured = 4;
        private const int Buffer = 2;
        private const int MinimumUsers = 7;

        /// <summary>
        ///     Automatics the adjust.
        /// </summary>
        public static void AutoAdjust()
        {
            Logger.Log(LogPriority.Debug, "Filling crawler pool...");

            while (Core.LobbyNew == null)
            {
                Thread.Sleep(250);
            }
            foreach (var room in Core.LobbyNew)
            {
                var roomKey = room.Key;

                // if room value is greater than or equal to 4 then if it's not crawling a world then crawl
                // if room value is less than 4 then if it is crawling then don't crawl

                if (room.Value >= MinUsersFeatured ||
                    (room.Key == "PWKK8zFHH8bEI" && room.Value > MinimumUsers) ||
                    (room.Key == "PWL2NjNOdhbEI" && room.Value > MinimumUsers) ||
                    (room.Key == "PWbnzNQNi4a0I" && room.Value > MinimumUsers))
                    // 200 lava minigames, super mario bros (featured), coin level (featured)
                {
                    var crawlerTasks = Core.CrawlerTasks;

                    if (!crawlerTasks.ContainsKey(roomKey) || crawlerTasks.Count == 0)
                    {
                        var createCrawlerHandle = new AutoResetEvent(false);
                        Core.CreateCrawler(roomKey, createCrawlerHandle);
                        createCrawlerHandle.WaitOne();
                        Thread.Sleep(3000); // if there is no delay, the clients will never initialize
                        // because PlayerIO rate limits new connections.
                        // This method should block.
                    }
                }
                else
                {
                    if (Core.CrawlerTasks.ContainsKey(roomKey))
                    {
                        if (room.Value < (MinimumUsers - Buffer))
                        {
                            // add a bit of a buffer so that there aren't too many fragments
                            Logger.Log(LogPriority.Debug, "Removing a crawler");
                            Core.RemoveCrawler(roomKey);
                        }
                    }
                }
            }
        }
    }
}