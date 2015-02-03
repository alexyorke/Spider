using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    /// Class Crawler.
    /// </summary>
    public class Crawler
    {
        /// <summary>
        /// The _SW
        /// </summary>
        private static StreamWriter _sw;
        /// <summary>
        /// The _stopwatch
        /// </summary>
        private static Stopwatch _stopwatch;
        /// <summary>
        /// The _FS
        /// </summary>
        private static FileStream _fs;

        /// <summary>
        /// Initializes a new instance of the <see cref="Crawler"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Crawler(Connect connection)
        {
            GlobalConnect = connection;
        }

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        public static Client Client
        {
            get { return GlobalConnect.Client; }
        }

        /// <summary>
        /// Gets or sets the world identifier.
        /// </summary>
        /// <value>The world identifier.</value>
        private string TheWorldId { get; set; }
        /// <summary>
        /// Gets or sets the global connection.
        /// </summary>
        /// <value>The global connection.</value>
        private Connection GlobalConnection { get; set; }
        /// <summary>
        /// Gets or sets the global connect.
        /// </summary>
        /// <value>The global connect.</value>
        private static Connect GlobalConnect { get; set; }

        /// <summary>
        /// Shutdowns the specified world identifier.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="stream">The stream.</param>
        private void Shutdown(string worldId, CancellationToken cancelToken, EeStream stream)
        {
            Console.WriteLine("[INFO] Received signal to cancel for " + worldId);
            GlobalConnection.Disconnect();
			Console.WriteLine ("[INFO] Connected? " + GlobalConnection.Connected.ToString ());
            //connection.Shutdown();
            Core.pool.PutObject(GlobalConnect);
            //Console.WriteLine(worldId + " cleaned up. Bye!");
            cancelToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Crawls the specified world identifier.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        public void Crawl(string worldId, CancellationToken cancelToken)
        {
            TheWorldId = worldId;
            var eeEvent = new EeStream(worldId);

            Client.Multiplayer.JoinRoom(worldId, null, // never create a new room. Ever!
                delegate(Connection connection)
                {
                    GlobalConnection = connection;

                    _stopwatch = new Stopwatch();
                    _stopwatch.Start();
                    Console.WriteLine(Client.ConnectUserId + " is connected to " + worldId);

                    GlobalConnection.Send("init");
                    GlobalConnection.Send("init2");

                    GlobalConnection.OnMessage +=
								delegate(object sender, Message m) {
								eeEvent.Write(m, _stopwatch.Elapsed.TotalSeconds);
								if (cancelToken.IsCancellationRequested)
								{ connection.Disconnect();
							Shutdown(worldId,cancelToken,eeEvent);
								}
							};
							

                    GlobalConnection.OnDisconnect += delegate(object sender, string message)
                    {
                        /*if (message == "receivedBytes == 0")
                        {
                            Console.WriteLine("Disconnected because of immediate kick");
                        }
                        else
                        {*/
						Console.WriteLine("[INFO] Connection disconnected for " + worldId + " because " + message.ToString());
                        //}

                        if (!cancelToken.IsCancellationRequested)
                        {
							//connection.Disconnect();
                            Shutdown(worldId, cancelToken, eeEvent);
                        }
                    };
                });

            //http://msdn.microsoft.com/en-us/library/system.timers.timer%28v=vs.110%29.aspx
            // Create a timer with a two second interval.
            
            // Hook up the Elapsed event for the timer. 
            Core.ATimer.Elapsed += delegate(Object source, ElapsedEventArgs e)
            {
                //Console.WriteLine("Timer fired for " + worldId);
                if (cancelToken.IsCancellationRequested)
                { 
                    Shutdown(worldId, cancelToken, eeEvent);
                }
            };
            
        }
    }
} ;