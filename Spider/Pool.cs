using System.Threading;

namespace Spider
{
    /// <summary>
    ///     Class Pool.
    /// </summary>
    internal static class Pool
    {
        private const int MinUsersFeatured = 3;
        private const int Buffer = 1;
        private const int MinimumUsers = 3;

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

                if (room.Value >= MinimumUsers ||
                    (room.Key == "PWKK8zFHH8bEI" && room.Value > MinUsersFeatured) ||
                    (room.Key == "PWL2NjNOdhbEI" && room.Value > MinUsersFeatured) ||
                    (room.Key == "PWbnzNQNi4a0I" && room.Value > MinUsersFeatured))
                    // 200 lava minigames, super mario bros (featured), coin level (featured)
                {


                    if (!Core.CrawlerTasks.ContainsKey(roomKey) || Core.CrawlerTasks.Count == 0)
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
                    if (Core.CrawlerTasks.ContainsKey(roomKey) && (room.Value < (MinimumUsers - Buffer)))
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