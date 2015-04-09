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
        public static readonly Timer ATimer = new Timer(1000);
        public static readonly Stopwatch Stopwatch = new Stopwatch();

        /// <summary>
        ///     The lobby new
        /// </summary>
        public static IOrderedEnumerable<KeyValuePair<string, int>> LobbyNew;

        /// <summary>
        ///     The crawler tasks
        /// </summary>
        public static readonly Dictionary<string, Dictionary<Task, CancellationTokenSource>> CrawlerTasks =
            new Dictionary<string, Dictionary<Task, CancellationTokenSource>>();

        private static int _doneCounter;
        private static int _doneCounterCrawler;
        private static int TotalEvents => _doneCounter;
        private static int _totalEventCrawler = _doneCounterCrawler;
        /// <summary>
        ///     Shows the event rate per minute.
        /// </summary>
        private static void ShowEventRatePerMinute()
        {
            Console.Write("\r " + "Events/min: " + TotalEvents + " | " + _totalEventCrawler);

            ZeroDoneCounter();
        }

        public static void IncrementCrawlerCounter() => Interlocked.Increment(ref _doneCounterCrawler);
        public static void IncrementDoneCounter() => Interlocked.Increment(ref _doneCounter);

        private static void ZeroDoneCounter()
        {
            Interlocked.Exchange(ref _doneCounter, 0);
            Interlocked.Exchange(ref _doneCounterCrawler, 0);
        }

        /// <summary>
        ///     Removes the crawler.
        /// </summary>
        /// <param name="roomKey">The room key.</param>
        public static void RemoveCrawler(string roomKey)
        {
            Dictionary<Task, CancellationTokenSource> crawlerToBeRemoved;
            CrawlerTasks.TryGetValue(roomKey, out crawlerToBeRemoved);

            if (crawlerToBeRemoved != null)
            {
                crawlerToBeRemoved.ElementAt(0).Value.Cancel();
                try
                {
                    crawlerToBeRemoved.ElementAt(0).Key.Wait();
                    CrawlerTasks.Remove(roomKey);
                }
                catch (Exception e)
                {
                    Logger.Log(LogPriority.Warning, e.Message);
                }
            }
            else
                Logger.Log(LogPriority.Error, "The specified crawler could not be found.");
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
            //var connector = Pool.GetObject();
            var connector = new Connect();

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

        private static void Shutdown()
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
                    x.Key.Wait();
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
            var autoAdjustPoolTask = Task.Run(() => { Pool.AutoAdjust(); }, cancellationTokenSource.Token);
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

            Repeat.Interval(TimeSpan.FromMinutes(1), Lobby.List, cancellationTokenSource.Token);
            // replenish and remove stale crawlers every 7 minutes
            Repeat.Interval(TimeSpan.FromMinutes(4), Pool.AutoAdjust, cancellationTokenSource.Token);
            // refresh the event counter every minute
            Repeat.Interval(TimeSpan.FromMinutes(1), ShowEventRatePerMinute, cancellationTokenSource.Token);

            Repeat.Interval(TimeSpan.FromMinutes(1), GarbageCollect, cancellationTokenSource.Token);
            var info = Console.ReadKey();
            if (info.Key == ConsoleKey.Q)
            {
                Console.WriteLine("Recieved shutdown signal from console.");
                Shutdown();
            }
        }

        private static void GarbageCollect()
        {
            GC.GetTotalMemory(true);
        }
    }
}