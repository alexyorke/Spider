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
        /// The connection on message received event handler
        /// </summary>
        private MessageReceivedEventHandler _connOnMessageReceivedEventHandler;
        /// <summary>
        /// The global cancellation token
        /// </summary>
        private CancellationToken _globalCancellationToken;
        /// <summary>
        /// The global stream
        /// </summary>
        private EeStream _globalStream;
        /// <summary>
        /// The global world id
        /// </summary>
        private string _worldIdGlobal;

        /// <summary>
        /// Initializes a new instance of the <see cref="Crawler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Crawler(Connect connection)
        {
            GlobalConnect = connection;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has not reconnected.
        /// </summary>
        /// <value><c>true</c> if this instance has not reconnected; otherwise, <c>false</c>.</value>
        private static bool HasNotReconnected { get; set; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        private Client Client => GlobalConnect.Client;

        /// <summary>
        ///     Gets or sets the global connection.
        /// </summary>
        /// <value>The global connection.</value>
        private Connection GlobalConnection { get; set; }

        /// <summary>
        ///     Gets or sets the global connect.
        /// </summary>
        /// <value>The global connect.</value>
        private Connect GlobalConnect { get; }

        private bool HasShown { get; set; }

        /// <summary>
        ///     Stops the current crawler.
        /// </summary>
        private void Shutdown()
        {
            GlobalConnection.Disconnect();
            Client.Logout();

        }

        /// <summary>
        ///     Crawls the specified world.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="reconnect"></param>
        public void Crawl(string worldId, CancellationToken cancelToken, bool reconnect = true)
        {
            var eeEvent = new EeStream(worldId);
            _globalStream = eeEvent;
            _globalCancellationToken = cancelToken;
            _worldIdGlobal = worldId;
            Client.Multiplayer.JoinRoom(worldId, null, // never create a new room. Ever!
                delegate(Connection connection)
                {
                    GlobalConnection = connection;

                    Logger.Log(LogPriority.Info, Client.ConnectUserId + " is connected to " + worldId);

                    _connOnMessageReceivedEventHandler = delegate(object sender, Message m)
                    {
                        if (m.Type == "init")
                        {
                            GlobalConnection.Send("init2");
                        }

                        Core.IncrementCrawlerCounter();

                        eeEvent.Write(m, Core.Stopwatch.Elapsed.TotalSeconds);
                        
                    };

                    GlobalConnection.OnMessage += _connOnMessageReceivedEventHandler;

                    GlobalConnection.Send("init");


                    GlobalConnection.OnDisconnect += delegate(object sender2, string message)
                    {
                        Logger.Log(LogPriority.Debug,"Recieved message to disconnect: " + message);
                        GlobalConnection.OnMessage -= _connOnMessageReceivedEventHandler;

                        ShouldShutdown(true);

                        if (message.Contains("receivedBytes == 0"))
                        {
                            // client crashed
                            if (!reconnect || HasNotReconnected)
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
            Core.ATimer.Elapsed += IsCancellationRequested;
        }

        private void IsCancellationRequested(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_globalCancellationToken.IsCancellationRequested && !HasShown)
            {
                Logger.Log(LogPriority.Debug,"Cancellation requested for: " + _worldIdGlobal);
                HasShown = true;
            }
        }

        private void ShouldShutdown(object sender, ElapsedEventArgs e)
        {
            ShouldShutdown();
        }

        private void ShouldShutdown(bool overrideCancel = false)
        {
            if (_globalCancellationToken.IsCancellationRequested || overrideCancel)
            {
                _connOnMessageReceivedEventHandler = null;
                Core.ATimer.Elapsed -= ShouldShutdown;
                Shutdown();
                _globalStream.RevokeCancellationToken();

                Logger.Log(LogPriority.Debug,"Finished shutting down.");
                _globalCancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
} ;