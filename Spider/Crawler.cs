using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using PlayerIOClient;

namespace Spider
{
    public class Crawler
    {
        private EeStream _globalStream;

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
        private Connect GlobalConnect { get; set; }
        public bool hasShown { get; private set; }


        /// <summary>
        ///     Shutdowns the specified world identifier.
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="stream">The stream.</param>
        private void Shutdown(CancellationToken cancelToken, EeStream stream)
        {
            GlobalConnection.Disconnect();

            //stream.CreateDoneFile();
            Client.Logout();
            //myWaitEvent.Set();
        }

        private CancellationToken _globalCancellationToken;
        private MessageReceivedEventHandler _connOnMessageReceivedEventHandler;
        /// <summary>
        ///     Crawls the specified world identifier.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="reconnect"></param>
        public void Crawl(string worldId, CancellationToken cancelToken, bool reconnect = true)
        {
            var eeEvent = new EeStream(worldId);
            _globalStream = eeEvent;
            _globalCancellationToken = cancelToken;
            
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

                        eeEvent.Write(m, Core.Stopwatch.Elapsed.TotalSeconds);
                        
                    };

                    GlobalConnection.OnMessage += _connOnMessageReceivedEventHandler;

                    GlobalConnection.Send("init");


                    GlobalConnection.OnDisconnect += delegate(object sender2, string message)
                    {
                        Console.WriteLine("Recieved message to disconnect: " + message);
                        Console.WriteLine("Cancellation token has been revoked? " + cancelToken.IsCancellationRequested);
                        Console.WriteLine("Is connected? " + connection.Connected);
                        GlobalConnection.OnMessage -= _connOnMessageReceivedEventHandler;

                            ShouldShutdown(null, null, true);
                        
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
            Core.ATimer.Elapsed += delegate(object sender, ElapsedEventArgs e)
            {
                if (cancelToken.IsCancellationRequested && !hasShown)
                {
                    Console.WriteLine("Cancellation requested for: " + worldId);
                    hasShown = true;
                }
            };
        }


        private void ShouldShutdown(object sender, ElapsedEventArgs e)
        {
            ShouldShutdown(null, null, false);
        }

        private void ShouldShutdown(object sender, ElapsedEventArgs e, bool overrideCancel = false)
        {
            if (_globalCancellationToken.IsCancellationRequested || overrideCancel)
            {
                Console.WriteLine("[!!!] Unsubscribing message handler.");
                _connOnMessageReceivedEventHandler = null;
                Core.ATimer.Elapsed -= ShouldShutdown;
                Shutdown(_globalCancellationToken, _globalStream);
                _globalStream.revokeCancellationToken();

                Console.WriteLine("Finished shutting down.");
                _globalCancellationToken.ThrowIfCancellationRequested();
            }
        }


/*
        private void ShouldShutdown(ElapsedEventArgs g, CancellationToken cancelToken)
        {

            if (cancelToken.IsCancellationRequested && !hasShutdown)
            {
                var myWaitHandle = new AutoResetEvent(false);
                Shutdown(cancelToken, globalStream, myWaitHandle);
                GlobalConnection = null;

                //myWaitHandle.WaitOne();
                globalStream.revokeCancellationToken();
                //globalStream = null;

                hasShutdown = true;
                cancelToken.ThrowIfCancellationRequested();
            }
        }
*/
    }
} ;