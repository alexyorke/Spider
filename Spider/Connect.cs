using System;
using System.Threading;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class Connect.
    /// </summary>
    public class Connect
    {
        private static readonly Random Random = new Random();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Connect" /> class.
        /// </summary>
        public Connect()
        {
            Client = LogOn();
        }

        /// <summary>
        ///     Gets the client.
        /// </summary>
        /// <value>The client.</value>
        public Client Client { get; private set; }

        /// <summary>
        ///     Logs the on.
        /// </summary>
        /// <returns>Client.</returns>
        private Client LogOn()
        {
            // emails may be randomized in the future.

            var waitForLogOn = new AutoResetEvent(false);
            var email = Config.Email;

            /*var randomNumber = Random.Next(0, 11);
            switch (randomNumber)
            {
                default:
                    email =  "user@example.com";
            }*/
            PlayerIO.QuickConnect.SimpleConnect(Config.GameId, (email), (Config.Password), null,
                delegate(Client localClient)
                {
                    // finished logging in
                    Client = localClient;
                    waitForLogOn.Set();
                });

            waitForLogOn.WaitOne();
            return Client;
        }
    }
} ;