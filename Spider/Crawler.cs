using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class Crawler.
    /// </summary>
    [Serializable]
    public class DataMessageThing
    {
        public DataMessageThing(Message theValue)
        {
            M = theValue;
        }

        public static Message M { get; set; }
    }

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
            stream.Shutdown(cancelToken);

            //Core.Pool.PutObject(GlobalConnect);
            cancelToken.ThrowIfCancellationRequested();
        }

        public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof (T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                writer?.Close();
            }
        }

        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);

            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        ///     Crawls the specified world identifier.
        /// </summary>
        /// <param name="worldId">The world identifier.</param>
        /// <param name="cancelToken">The cancel token.</param>
        public void Crawl(string worldId, CancellationToken cancelToken, bool reconnect = false)
        {
            var eeEvent = new EeStream(worldId);

            Task.Run(() => eeEvent.StartQueueWorker(), cancelToken);

            Client.Multiplayer.JoinRoom(worldId, null, // never create a new room. Ever!
                delegate(Connection connection)
                {
                    GlobalConnection = connection;

                    Logger.Log(LogPriority.Info, Client.ConnectUserId + " is connected to " + worldId);

                    GlobalConnection.OnMessage += delegate(object sender, Message m)
                    {
                        if (m.Type == "init")
                        {
                            GlobalConnection.Send("init2");
                        }
                        eeEvent.Write(m, Core.Stopwatch.Elapsed.TotalSeconds);
                        if (cancelToken.IsCancellationRequested)
                        {
                            try
                            {
                                GlobalConnection.Send("/bye");
                            }
                            catch (Exception)
                            {
                                // silence the error because we know this command is invalid.
                                // this is just to push a system message to us and so therefore
                                // the cancellation message will be pushed through too.
                            }
                            connection.Disconnect();
                            Shutdown(cancelToken, eeEvent);
                        }
                    };


                    GlobalConnection.Send("init");
                    GlobalConnection.Send("init2");

                    
                    GlobalConnection.OnDisconnect += delegate(object sender2, string message)
                    {
                        if (message.Contains("receivedBytes == 0"))
                        {
                            // client crashed
                            if (reconnect)
                            {
                                Logger.Log(LogPriority.Debug, "Client crashed. Restarting crawler...");

                                var t = Task.Run(async delegate { await Task.Delay(3000); });
                                t.Wait();
                                Crawl(worldId, cancelToken); // reconnect once
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

                        if (!cancelToken.IsCancellationRequested)
                        {
                            var t = Task.Run(async delegate
                            {
                                Shutdown(cancelToken, eeEvent);
                                Logger.Log(LogPriority.Info, "Crawler stopped successfully.");
                            }, cancelToken);
                            t.Wait(cancelToken);
                        }
                    };
                });

            //http://msdn.microsoft.com/en-us/library/system.timers.timer%28v=vs.110%29.aspx
            // Create a timer with a two second interval.

            // Hook up the Elapsed event for the timer. 
            Core.ATimer.Elapsed += delegate
            {
                if (cancelToken.IsCancellationRequested)
                {
                    Shutdown(cancelToken, eeEvent);
                    //eeEvent = null; // possible solution to memory issue
                }
            };
        }
    }
} ;