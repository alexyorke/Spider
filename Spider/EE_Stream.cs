using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Environment;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class EeStream.
    /// </summary>
    public class EeStream
    {
        public static readonly string FilePath = GetFolderPath(SpecialFolder.Desktop);
        public static CancellationToken CancelTokenGlobal;

        /// <summary>
        ///     The _RND
        /// </summary>
        private readonly Random _rnd = new Random();

        private BlockingCollection<Dictionary<Message, double>> _dataToWrite =
            new BlockingCollection<Dictionary<Message, double>>();

        private readonly string _donePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EeStream" /> class.
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

            Directory.CreateDirectory(string.Format(FilePath + @"/spider_levels/{0}_{1}", currentDate,
                uniqueString));
            Logger.Log(LogPriority.Debug,
                "Writing to: " + string.Format(FilePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId));

            _donePath = string.Format(FilePath + @"/spider_levels/{0}_{1}/done.txt", currentDate, uniqueString,
                worldId);
            Fs = new FileStream(
                string.Format(FilePath + @"/spider_levels/{0}_{1}/{2}", currentDate, uniqueString,
                    worldId), FileMode.Append, FileAccess.Write);
            Sw = new StreamWriter(Fs) {AutoFlush = false};

            Sw.WriteLine(JsonConvert.SerializeObject(new Dictionary<string,string> { {"date_started", DateTime.Now.ToString()} }));
            //DownloadMinimap(worldId, currentDate, uniqueString);
        }

        /// <summary>
        ///     Gets or sets the sw.
        /// </summary>
        /// <value>The sw.</value>
        private StreamWriter Sw { get; }

        /// <summary>
        ///     Gets or sets the fs.
        /// </summary>
        /// <value>The fs.</value>
        private FileStream Fs { get; }

        public void StartQueueWorker()
        {
            while (true)
            {
                var data = _dataToWrite.Take();
                foreach (var anEvent in data)
                {
                    Core.IncrementDoneCounter();
                    Sw.WriteLine(JsonConvert.SerializeObject(anEvent));
                }
                if (CancelTokenGlobal.IsCancellationRequested)
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
                var data = _dataToWrite.Take();
                foreach (var anEvent in data)
                {
                    Core.IncrementDoneCounter();
                    Sw.WriteLine(JsonConvert.SerializeObject(anEvent));
                }
            }
            Sw.Flush();
            
      
                Fs.Flush();
                //Fs.Close(); // closes sw too
                
           
            //_dataToWrite = null; // trying to fix memory issue
        }

        /// <summary>
        ///     Writes the specified m.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="secondsElapsed">The seconds elapsed.</param>
        public void Write(Message m, double secondsElapsed)
        {
           
                var x = new Dictionary<Message,
                    double> {{m, secondsElapsed}};
            _dataToWrite.Add(x);
                //Core.IncrementDoneCounter();
            
        }

        /// <summary>
        ///     Shutdowns this instance.
        /// </summary>
        public void Shutdown(CancellationToken cancelToken)
        {
            
                Sw.Flush();
                Fs.Flush();
            
            CancelTokenGlobal = cancelToken;
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