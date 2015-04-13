using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Class EE Stream.
    /// </summary>
    public class EeStream
    {
        /// <summary>
        /// The data queue
        /// </summary>
        private readonly BlockingCollection<StrongBox<Dictionary<Message, double>>> _dataToWrite =
            new BlockingCollection<StrongBox<Dictionary<Message, double>>>();

        /// <summary>
        /// The random instance
        /// </summary>
        private readonly Random _rnd = new Random();

        /// <summary>
        /// The global cancellation token
        /// </summary>
        private readonly CancellationTokenSource _cancelTokenGlobal = new CancellationTokenSource();
        /// <summary>
        /// The file path
        /// </summary>
        private readonly string _filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        /// <summary>
        /// Initializes a new instance of the <see cref="EeStream" /> class.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        public EeStream(string worldId)
        {
            var currentDate = DateTime.Now.ToString("yyyy-M-d");

            // random string from stackoverflow

            var result = new string(
                Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8)
                    .Select(s => s[_rnd.Next(s.Length)])
                    .ToArray());
            var uniqueString = result;
            // make directory

            Directory.CreateDirectory(string.Format(_filePath + @"/spider_levels/{0}_{1}", currentDate,
                uniqueString));
            Logger.Log(LogPriority.Debug,
                "Writing to: " + string.Format(_filePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId));

            var fs = new FileStream(
                string.Format(_filePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId), FileMode.Append, FileAccess.Write);

            var sw = new StreamWriter(fs);


            sw.WriteLine(
                JsonConvert.SerializeObject(new Dictionary<string, string>
                {
                    {"date_started", DateTime.Now.ToString(CultureInfo.InvariantCulture)}
                }));
            Task.Run(() => StartQueueWorker(sw), _cancelTokenGlobal.Token);
        }

        /// <summary>
        /// Revokes the cancellation token.
        /// </summary>
        public void RevokeCancellationToken()
        {
            _cancelTokenGlobal.Cancel();
        }

        /// <summary>
        /// Starts the queue worker.
        /// </summary>
        /// <param name="sw">The sw.</param>
        private void StartQueueWorker(StreamWriter sw)
        {
            while (true)
            {
                WriteEventToFile(sw);

                if (_cancelTokenGlobal.Token.IsCancellationRequested)
                {
                    // stop the loop
                    Logger.Log(LogPriority.Debug,"Ending worker writer...");
                    break;
                }
            }
            // finish up with rest of file
            Logger.Log(LogPriority.Debug,"Writing rest of queue... Please wait.");
            var queuedEventCount = _dataToWrite.Count;
            for (var i = 0; i <= queuedEventCount; i++)
            {
                WriteEventToFile(sw);
            }
            //_dataToWrite.Dispose();
            sw.Flush();
            sw.Close();
            sw.Dispose();
        }

        /// <summary>
        /// Writes the event to file.
        /// </summary>
        /// <param name="sw">The sw.</param>
        private void WriteEventToFile(StreamWriter sw)
        {
            try
            {
                var data = _dataToWrite.Take(_cancelTokenGlobal.Token);

                var data2 = data.Value;
                foreach (var anEvent in data2)
                {
                    sw.WriteLine(JsonConvert.SerializeObject(anEvent));
                    Core.IncrementDoneCounter();
                }
                //_dataToWrite.Dispose();
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Writes the specified message to the queue.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="secondsElapsed">The seconds elapsed.</param>
        public void Write(Message m, double secondsElapsed)
        {
            var m1 = m;
            var secondsElapsed1 = secondsElapsed;
            var strongBox = new StrongBox<Dictionary<Message, double>>(new Dictionary<Message,
                double> {{m1, secondsElapsed1}});
            _dataToWrite.Add(strongBox);
        }
    }
}