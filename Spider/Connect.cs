using System;
using System.Threading;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    /// Class Connect.
    /// </summary>
    public class Connect
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connect"/> class.
        /// </summary>
        public Connect()
        {
            Client = LogOn();
        }

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        public Client Client { get; private set; }

        /// <summary>
        /// Logs the on.
        /// </summary>
        /// <returns>Client.</returns>
        private Client LogOn()
        {
            // emails may be randomized in the future.
            
            Console.WriteLine("Got to LogOn()");
            var waitForLogOn = new AutoResetEvent(false);
			PlayerIO.QuickConnect.SimpleConnect(Config.GameId, Config.email, Config.password, null,
                delegate(Client localClient)
                {
                    Console.WriteLine("Successfully produced a client object.");
                    // finished logging in
                    Client = localClient;
                    waitForLogOn.Set();
                });

            waitForLogOn.WaitOne();
            return Client;
        }
    }
} ;