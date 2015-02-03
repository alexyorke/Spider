using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    /// <summary>
    /// Class Core.
    /// </summary>
    public class Core
    {
        public static System.Timers.Timer ATimer = new System.Timers.Timer(2000);
        
        /// <summary>
        /// The pool
        /// </summary>
        public static ObjectPool<Connect> pool = new ObjectPool<Connect>(() => new Connect());
        /// <summary>
        /// The lobby new
        /// </summary>
        public static IOrderedEnumerable<KeyValuePair<string, int>> LobbyNew;

        /// <summary>
        /// The crawler tasks
        /// </summary>
        public static Dictionary<string, Dictionary<Task, CancellationTokenSource>> CrawlerTasks =
            new Dictionary<string, Dictionary<Task, CancellationTokenSource>>();

        /// <summary>
        /// Gets or sets the total events.
        /// </summary>
        /// <value>The total events.</value>
        

        /// <summary>
        /// Shows the event rate per minute.
        /// </summary>
        private static void ShowEventRatePerMinute()
        {
            Console.Write("\r " + "Events/min: " + Math.Round(((double) TotalEvents/60)));
			ZeroDoneCounter ();
        }
		private static int _doneCounter;
		public static int TotalEvents { get { return _doneCounter; } }
		public static void IncrementDoneCounter() { Interlocked.Increment(ref _doneCounter); }
		public static void DecrementDoneCounter() { Interlocked.Decrement(ref _doneCounter); }
		public static void ZeroDoneCounter() {
			Interlocked.Exchange (ref _doneCounter, 0);
		}

        /// <summary>
        /// Removes the crawler.
        /// </summary>
        /// <param name="roomKey">The room key.</param>
        public static void RemoveCrawler(string roomKey)
        {
            var stale = CrawlerTasks[roomKey];
            var tokenSource = stale.First().Value;

            tokenSource.Cancel();
            try
            {
                stale.First().Key.Wait(tokenSource.Token); //wait for task to abort
                var taskFinished = true;
                if (!taskFinished)
                {
                    Console.WriteLine("[ERROR] The crawler could not be ended within 5 seconds.");
                }
            }
            catch (TaskCanceledException)
            {
                // don't worry about this; it just means that
                // the task was canceled.
            }
            CrawlerTasks.Remove(roomKey);
        }

        /// <summary>
        /// Creates the crawler.
        /// </summary>
        /// <param name="roomKey">The room key.</param>
        /// <param name="createCrawlerHandle"></param>
        public static void CreateCrawler(string roomKey, AutoResetEvent createCrawlerHandle)
        {
            var cancelToken = new CancellationTokenSource();
            var connector = pool.GetObject();

            var crawlerTask = Task.Factory.StartNew(state =>
            {
                var crawler = new Crawler(connector);

                crawler.Crawl(roomKey, cancelToken.Token);
                Console.WriteLine("[INFO] Created a crawler: " + roomKey);
            }, TaskCreationOptions.LongRunning, cancelToken.Token);

            CrawlerTasks.Add(
                roomKey,
                new Dictionary<Task, CancellationTokenSource>
                {
                    {crawlerTask, cancelToken}
                }
                );
            createCrawlerHandle.Set();
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            ATimer.Enabled = true;
            var cancellationTokenSource = new CancellationTokenSource();

            // list lobby
            var listLobbyTask = Task.Run(() => { Lobby.List(); }, cancellationTokenSource.Token);
            listLobbyTask.Wait(cancellationTokenSource.Token);

            // auto fill/empty worker "pool"
            var autoAdjustPoolTask = Task.Run(() => { Pool.AutoAdjust(); }, cancellationTokenSource.Token);
            autoAdjustPoolTask.Wait(cancellationTokenSource.Token);

            Console.WriteLine("Finished adjusting worker pool.");

            Repeat.Interval(
                TimeSpan.FromMinutes(2), Lobby.List, cancellationTokenSource.Token);

            // replenish and remove stale crawlers every 8 minutes
            Repeat.Interval(
                TimeSpan.FromMinutes(5), Pool.AutoAdjust, cancellationTokenSource.Token);

            // refresh the event counter every minute
            Repeat.Interval(
                TimeSpan.FromMinutes(1), ShowEventRatePerMinute, cancellationTokenSource.Token);

            //Shutdown portion
           /* while (!Console.ReadLine().StartsWith("q"))
            {
                Thread.Sleep(1000);
            }*/

            var info = Console.ReadKey();
            if (info.Key == ConsoleKey.Q)
            {
                Console.WriteLine("[INFO] Shutting down...");
                foreach (var crawler in CrawlerTasks)
                {
					Console.WriteLine("[INFO] Gracefully ending crawler...");
                    //Wait for task to disconnect

                    var x = crawler.Value;
                    x.Last().Value.Cancel();
                    try
                    {
                        x.Last().Key.Wait(cancellationTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[WARNING] " + e.Message);
                    }
                }
            }

            
        }
    }
}