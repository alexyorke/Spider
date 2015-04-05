using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using PlayerIOClient;

namespace Spider
{
    public class Crawler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Crawler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Crawler(Connect connection)
        {
            GlobalConnect = connection;
        }

        public static bool HasNotReconnected { get; set; }

        /// <summary>
        ///     Gets the client.
        /// </summary>
        /// <value>The client.</value>
        public static Client Client => GlobalConnect.Client;

        /// <summary>
        ///     Gets or sets the global connection.
        /// </summary>
        /// <value>The global connection.</value>
        private Connection GlobalConnection { get; set; }

        /// <summary>
        ///     Gets or sets the global connect.
        /// </summary>
        /// <value>The global connect.</value>
        private static Connect GlobalConnect { get; set; }

        public static CancellationToken globalCancellationToken;


        /// <summary>
        ///     Shutdowns the specified world identifier.
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="stream">The stream.</param>
        private void Shutdown(CancellationToken cancelToken, EeStream stream)
        {
            GlobalConnection.Disconnect();
            
            stream.CreateDoneFile();
            Client.Logout();
            //Core.Pool.PutObject(GlobalConnect);
            cancelToken.ThrowIfCancellationRequested();
        }

        public static EeStream globalStream = null;
        /// <summary>
        ///     Crawls the specified world identifier.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="reconnect"></param>
        public void Crawl(string worldId, CancellationToken cancelToken, bool reconnect = true)
        {
            var eeEvent = new EeStream(worldId);
            globalStream = eeEvent;
            globalCancellationToken = cancelToken;
            Client.Multiplayer.JoinRoom(worldId, null, // never create a new room. Ever!
                delegate(Connection connection)
                {
                    GlobalConnection = connection;

                    Logger.Log(LogPriority.Info, Client.ConnectUserId + " is connected to " + worldId);

                    MessageReceivedEventHandler connOnMessageReceivedEventHandler = delegate (object sender, Message m)
                    {
                        if (m.Type == "init")
                        {
                            GlobalConnection.Send("init2");
                        }
                        eeEvent.Write(m, Core.Stopwatch.Elapsed.TotalSeconds);
                        Core.IncrementDoneCounter();
                    };

                    GlobalConnection.OnMessage += connOnMessageReceivedEventHandler;


                    GlobalConnection.Send("init");
                    GlobalConnection.Send("init2");

                    
                    GlobalConnection.OnDisconnect += delegate(object sender2, string message)
                    {
                        GlobalConnection.OnMessage -= connOnMessageReceivedEventHandler;

                        if (message.Contains("receivedBytes == 0"))
                        {
                            // client crashed
                            if (reconnect && !HasNotReconnected)
                            {
                                /*Logger.Log(LogPriority.Debug, "Client crashed. Restarting crawler...");
                                HasNotReconnected = true;
                               Thread.Sleep(1000);
                                Crawl(worldId, cancelToken); // reconnect once*/
                            }
                            else
                            {
                                Logger.Log(LogPriority.Warning, "Client reconnect attempts exceeded.");
                            }
                        }
                        else
                        {
                            if (message == "Disconnect")
                            {
                                Logger.Log(LogPriority.Debug, "Crawler successfully disconnected.");
                            }
                            else
                            {
                                Logger.Log(LogPriority.Warning,
                                    "Crawler " + worldId + " disconnected because " + message);
                            }
                        }

                        
                    };
                });

            //http://msdn.microsoft.com/en-us/library/system.timers.timer%28v=vs.110%29.aspx
            // Create a timer with a two second interval.

            // Hook up the Elapsed event for the timer.

            Core.ATimer.Elapsed += ShouldShutdown;
        }

        private void ShouldShutdown(object e, ElapsedEventArgs b)
        {
            if (globalCancellationToken.IsCancellationRequested)
            {
                Core.ATimer.Elapsed -= ShouldShutdown; // allows crawler to be GC'd
                Shutdown(globalCancellationToken, globalStream);
                globalStream.revokeCancellationToken();
                
            }
        }
    }
} ;