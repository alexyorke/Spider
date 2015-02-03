using System;
using System.Threading;

namespace Spider
{
    /// <summary>
    /// Class Pool.
    /// </summary>
    internal static class Pool
    {
        /// <summary>
        /// Automatics the adjust.
        /// </summary>
        public static void AutoAdjust()
        {
            Console.WriteLine("[INFO] Filling worker pool...");
            while (Core.LobbyNew == null)
            {
                Thread.Sleep(250);
            }
            foreach (var room in Core.LobbyNew)
            {
                var roomKey = room.Key;

                // if room value is greater than or equal to 4 then if it's not crawling a world then crawl
                // if room value is less than 4 then if it is crawling then don't crawl

                if (room.Value >= 4)
                {
                    var crawlerTasks = Core.CrawlerTasks;

                    if (!crawlerTasks.ContainsKey(roomKey) || crawlerTasks.Count == 0)
                    {
						Thread.Sleep (3000);
                        Console.WriteLine("[INFO] Creating a new crawler!");
                        var createCrawlerHandle = new AutoResetEvent(false);
                        Core.CreateCrawler(roomKey, createCrawlerHandle);
                        createCrawlerHandle.WaitOne();
                    }
                }
                else
                {
                    if (Core.CrawlerTasks.ContainsKey(roomKey))
                    {
                        Console.WriteLine("[INFO] Removing a crawler!");
                        Core.RemoveCrawler(roomKey);
                    }
                }
            }

        }
    }
}