using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Environment;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class EeStream.
    /// </summary>
    public class EeStream
    {
        private static readonly string FilePath = GetFolderPath(SpecialFolder.Desktop);
        private static CancellationTokenSource CancelTokenGlobal = new CancellationTokenSource();

        /// <summary>
        ///     The _RND
        /// </summary>
        private readonly Random _rnd = new Random();

        private BlockingCollection<StrongBox<Dictionary<Message, double>>> _dataToWrite =
            new BlockingCollection<StrongBox<Dictionary<Message, double>>>();

        private readonly string _donePath;

        private int GarbageCollectCounter { get; set; }


        /// <summary>
        ///     Initializes a new instance of the <see cref="EeStream" /> class.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        public EeStream(string worldId)
        {
            GarbageCollectCounter = 1;
            var currentDate = DateTime.Now.ToString("yyyy-M-d");

            // random string from stackoverflow

            var result = new string(
                Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8)
                    .Select(s => s[_rnd.Next(s.Length)])
                    .ToArray());
            var uniqueString = result;
            // make directory

            Directory.CreateDirectory(string.Format(FilePath + @"/spider_levels/{0}_{1}", currentDate,
                uniqueString));
            Logger.Log(LogPriority.Debug,
                "Writing to: " + string.Format(FilePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId));

            _donePath = string.Format(FilePath + @"/spider_levels/{0}_{1}/done.txt", currentDate, uniqueString,
                worldId);
            var fs = new FileStream(
                string.Format(FilePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId), FileMode.Append, FileAccess.Write);

            var sw = new StreamWriter(fs);


            sw.WriteLine(
                JsonConvert.SerializeObject(new Dictionary<string, string>
                {
                    {"date_started", DateTime.Now.ToString(CultureInfo.InvariantCulture)}
                }));
            Console.WriteLine("Event stream has initialized");
            Task.Run(() => StartQueueWorker(sw), CancelTokenGlobal.Token);
        }

        public void revokeCancellationToken()
        {
            Console.WriteLine("cancellation token revoked");
            CancelTokenGlobal.Cancel();
        }

        private void StartQueueWorker(StreamWriter sw)
        {
            while (true)
            {
                WriteEventToFile(sw);
                
                if (CancelTokenGlobal.Token.IsCancellationRequested)
                {
                    // stop the loop
                    Console.WriteLine("Ending worker writer...");
                    break;
                }
            }
            // finish up with rest of file
            Console.WriteLine("Writing rest of queue... Please wait.");
            var queuedEventCount = _dataToWrite.Count;
            for (var i = 0; i <= queuedEventCount; i++)
            {
                Console.WriteLine("Writing event " + i + " of " + queuedEventCount);
                WriteEventToFile(sw);
            }
            Console.WriteLine("StartQueueWorker() exited.");
            //_dataToWrite.Dispose();
            sw.Flush();
            sw.Close();
            sw.Dispose();
        }

        private void WriteEventToFile(StreamWriter sw)
        {
            try
            {
                var data = _dataToWrite.Take(CancelTokenGlobal.Token);

                var data2 = data.Value;
                foreach (var anEvent in data2)
                {
                    //Core.IncrementDoneCounter();
                    sw.WriteLine(JsonConvert.SerializeObject(anEvent));
                    Core.IncrementDoneCounter();
                }
                //_dataToWrite.Dispose();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("WriteToFile has been successfully cancelled.");
            }
        }



    /// <summary>
        ///     Writes the specified m.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="secondsElapsed">The seconds elapsed.</param>
        public void Write(Message m, double secondsElapsed)
        {
            Message m1 = m;
            double secondsElapsed1 = secondsElapsed;
            var strongBox = new StrongBox<Dictionary<Message,double>>(new Dictionary<Message,
                double> { { m1, secondsElapsed1 } });
        
            _dataToWrite.Add(strongBox);
        
        }

        /// <summary>
        ///     Shutdowns this instance.
        /// </summary>
        public void CreateDoneFile()
        {

            try
            {
                using (Stream stream = File.Create(_donePath))
                {
                    TextWriter tw = new StreamWriter(stream); /* this is where the problem was */
                    tw.WriteLine("done");
                    tw.Close();
                }

            }
            catch (Exception)
            {
                Console.WriteLine("Could not create done file");
            }
        }
    }
}