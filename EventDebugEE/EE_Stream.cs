using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    /// Class EeStream.
    /// </summary>
    public class EeStream
    {
        /// <summary>
        /// The _RND
        /// </summary>
        private readonly Random _rnd = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="EeStream"/> class.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        public EeStream(string worldId)
        {
            var currentDate = DateTime.Now.ToString("yyyy-M-d");
            var ranNumber = _rnd.Next(1, 100000);
            // make directory

            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Directory.CreateDirectory(string.Format("/media/USERNAME/EOS_DIGITAL/spider_levels/{0}_{1}", currentDate,
                ranNumber));
            Console.WriteLine("[INFO] Writing to: " +
                              string.Format("/media/USERNAME/EOS_DIGITAL/spider_levels/{0}_{1}/{2}", currentDate, ranNumber,
                                  worldId));
            Fs =
                new FileStream(
					string.Format("/media/USERNAME/EOS_DIGITAL/spider_levels/{0}_{1}/{2}", currentDate, ranNumber,
                        worldId), FileMode.Append, FileAccess.Write);
            Sw = new StreamWriter(Fs);
        }

        /// <summary>
        /// Gets or sets the sw.
        /// </summary>
        /// <value>The sw.</value>
        private StreamWriter Sw { get; set; }
        /// <summary>
        /// Gets or sets the fs.
        /// </summary>
        /// <value>The fs.</value>
        private FileStream Fs { get; set; }

        /// <summary>
        /// Writes the specified m.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="secondsElapsed">The seconds elapsed.</param>
        public void Write(Message m, double secondsElapsed)
        {
            //This function is taking the majority of the CPU time.
            Sw.WriteLine(JsonConvert.SerializeObject(m) + Environment.NewLine +
                secondsElapsed);
            //var totalEvents = Core.TotalEvents;
			Core.IncrementDoneCounter ();
            //Core.TotalEvents = totalEvents;
            //return sw;
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public void Shutdown()
        {
            Sw.Flush();
            Fs.Flush();
            //Sw.Close();
            //Fs.Close();
        }
    }
} ;