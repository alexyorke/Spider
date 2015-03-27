using System.Threading;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class Connect.
    /// </summary>
    public class Connect
    {
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