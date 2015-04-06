using System;
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

        }

        public static EeStream globalStream = null;
        private bool hasShutdown = false;

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

                    GlobalConnection.OnMessage += delegate(object sender, Message e)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Unsubscribing message handler.");
                            GlobalConnection.OnMessage -= connOnMessageReceivedEventHandler;
                        }
                    };

                    GlobalConnection.Send("init");
                    

                    
                    GlobalConnection.OnDisconnect += delegate(object sender2, string message)
                    {
                        Console.WriteLine("Recieved message to disconnect");
                        Console.WriteLine("Cancellation token has been revoked? " + cancelToken.IsCancellationRequested);
                        Console.WriteLine("Is connected? " + connection.Connected);
                        GlobalConnection.OnMessage -= connOnMessageReceivedEventHandler;

                        Task.Delay(100);
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

            Core.ATimer.Elapsed += (o, args) => ShouldShutdown(args, cancelToken);
        }

        private void ShouldShutdown(ElapsedEventArgs g,CancellationToken cancelToken)
        {
            if (cancelToken.IsCancellationRequested && !hasShutdown)
            {
                Console.WriteLine("Entered ShouldShutdown");
                Thread.Sleep(500);
                Shutdown(cancelToken, globalStream);
                GlobalConnection = null;
                globalStream.revokeCancellationToken();
                globalStream = null;

                hasShutdown = true;
                cancelToken.ThrowIfCancellationRequested();
                
            }

        }
    }
} ;