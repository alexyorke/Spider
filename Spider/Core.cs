using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Spider
{
    /// <summary>
    ///     Class Core.
    /// </summary>
    public static class Core
    {
        public static Timer ATimer = new Timer(2000);
        public static Stopwatch Stopwatch = new Stopwatch();

        /// <summary>
        ///     The pool
        /// </summary>
        public static ObjectPool<Connect> Pool = new ObjectPool<Connect>(() => new Connect());

        /// <summary>
        ///     The lobby new
        /// </summary>
        public static IOrderedEnumerable<KeyValuePair<string, int>> LobbyNew;

        /// <summary>
        ///     The crawler tasks
        /// </summary>
        public static Dictionary<string, Dictionary<Task, CancellationTokenSource>> CrawlerTasks =
            new Dictionary<string, Dictionary<Task, CancellationTokenSource>>();

        private static int _doneCounter;

        public static int TotalEvents => _doneCounter;

        /// <summary>
        ///     Gets or sets the total events.
        /// </summary>
        /// <value>The total events.</value>
        /// <summary>
        ///     Shows the event rate per minute.
        /// </summary>
        public static void ShowEventRatePerMinute(bool zeroDoneCounterBool = true)
        {
            Console.Write("\r " + "Events/min: " + TotalEvents);
            if (zeroDoneCounterBool)
            {
                ZeroDoneCounter();
            }
        }

        /// <summary>
        ///     Shows the event rate per minute.
        /// </summary>
        public static void ShowEventRatePerMinute()
        {
            Console.Write("\r " + "Events/min: " + TotalEvents);

            ZeroDoneCounter();
        }

        public static void IncrementDoneCounter()
        {
            Interlocked.Increment(ref _doneCounter);
        }

        public static void DecrementDoneCounter()
        {
            Interlocked.Decrement(ref _doneCounter);
        }

        public static void ZeroDoneCounter()
        {
            Interlocked.Exchange(ref _doneCounter, 0);
        }

        /// <summary>
        ///     Removes the crawler.
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
            }
            catch (TaskCanceledException)
            {
                // don't worry about this; it just means that
                // the task was canceled.
            }
            CrawlerTasks.Remove(roomKey);
        }

        /// <summary>
        ///     Creates the crawler.
        /// </summary>
        /// <param name="roomKey">The room key.</param>
        /// <param name="createCrawlerHandle"></param>
        public static void CreateCrawler(string roomKey, AutoResetEvent createCrawlerHandle)
        {
            Logger.Log(LogPriority.Debug, "Initializing crawler...");
            var cancelToken = new CancellationTokenSource();
            var connector = Pool.GetObject();

            var crawlerTask = Task.Factory.StartNew(state =>
            {
                var crawler = new Crawler(connector);

                crawler.Crawl(roomKey, cancelToken.Token);
                Logger.Log("Crawling: " + roomKey);
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

        private static void Shutdown(CancellationTokenSource cancellationTokenSource)
        {
            Logger.Log(LogPriority.Info, "Shutting down...");
            foreach (var crawler in CrawlerTasks)
            {
                Logger.Log(LogPriority.Debug, "Gracefully ending crawler...");
                //Wait for task to disconnect
                var x = crawler.Value.Last();
                x.Value.Cancel();
                try
                {
                    x.Key.Wait(cancellationTokenSource.Token);
                    Task.Delay(5000);
                }
                catch (Exception e)
                {
                    Logger.Log(LogPriority.Warning, e.Message);
                }
            }
            Logger.Log(LogPriority.Info, "Goodbye.");
        }

        private static void InitializeLobbyAndWorkers(CancellationTokenSource cancellationTokenSource)
        {
            // list lobby
            var listLobbyTask = Task.Run(() => { Lobby.List(); }, cancellationTokenSource.Token);
            listLobbyTask.Wait(cancellationTokenSource.Token);
            // auto fill/empty worker "pool"
            var autoAdjustPoolTask = Task.Run(() => { Spider.Pool.AutoAdjust(); }, cancellationTokenSource.Token);
            autoAdjustPoolTask.Wait(cancellationTokenSource.Token);
        }

        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            Stopwatch.Start();
            ATimer.Enabled = true;
            var cancellationTokenSource = new CancellationTokenSource();

            InitializeLobbyAndWorkers(cancellationTokenSource);

            Repeat.Interval(TimeSpan.FromMinutes(2), Lobby.List, cancellationTokenSource.Token);
            // replenish and remove stale crawlers every 13 minutes
            Repeat.Interval(TimeSpan.FromMinutes(13), Spider.Pool.AutoAdjust, cancellationTokenSource.Token);
            // refresh the event counter every minute
            Repeat.Interval(TimeSpan.FromMinutes(1), ShowEventRatePerMinute, cancellationTokenSource.Token);

            var info = Console.ReadKey();
            if (info.Key == ConsoleKey.Q)
            {
                Console.WriteLine("Recieved shutdown signal from console.");
                Shutdown(cancellationTokenSource);
            }
        }
    }
}