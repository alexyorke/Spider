using System;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    /// <summary>
    ///     Class Pool.
    /// </summary>
    internal static class Pool
    {
        private const int MinUsersFeatured = 3;
        private const int Buffer = 0;
        private const int MinimumUsers = 3;

        /// <summary>
        ///     Automatics the adjust.
        /// </summary>
        public static async void AutoAdjust()
        {
            Logger.Log(LogPriority.Debug, "Filling crawler pool...");

            while (Core.LobbyNew == null)
            {
                Thread.Sleep(250);
            }
            foreach (var room in Core.LobbyNew)
            {
                if (room.Value >= MinimumUsers ||
                    (room.Key == "PWKK8zFHH8bEI" && room.Value > MinUsersFeatured) ||
                    (room.Key == "PWL2NjNOdhbEI" && room.Value > MinUsersFeatured) ||
                    (room.Key == "PWbnzNQNi4a0I" && room.Value > MinUsersFeatured) ||
                    (room.Key == "PWAIjKWOiLbEI")) // tutorial room
                    // 200 lava minigames, super mario bros (featured), coin level (featured)
                {
                    if (!Core.CrawlerTasks.ContainsKey(room.Key) || Core.CrawlerTasks.Count == 0)
                    {
                        Console.WriteLine("[IMP] Began to crawl a room: value: " + room.Value);
                        var createCrawlerHandle = new AutoResetEvent(false);
                        Core.CreateCrawler(room.Key, createCrawlerHandle);
                        createCrawlerHandle.WaitOne();
                        await Task.Delay(3000);
                        //Thread.Sleep(3000); // if there is no delay, the clients will never initialize
                        // because PlayerIO rate limits new connections.
                        // This method should block.
                    }
                }
                else
                {
                    if (Core.CrawlerTasks.ContainsKey(room.Key) && (room.Value < (MinimumUsers - Buffer)))
                    {
                        // add a bit of a buffer so that there aren't too many fragments
                        Logger.Log(LogPriority.Debug, "Removing a crawler");
                        Core.RemoveCrawler(room.Key);
                    }
                }
            }
        }
    }
}