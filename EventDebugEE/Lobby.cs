using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    /// Class Lobby.
    /// </summary>
    internal static class Lobby // this is the least maintainable class in the project, according
        // to code metrics
    {
        /// <summary>
        /// Lists the specified token.
        /// </summary>
        public static void List()
        {
			List<string> blackList = new List<string> ();
			blackList.Add ("PW3tNZWPXQbkI");
            var waitForLobby = new AutoResetEvent(false);
            var connection = Core.pool.GetObject();
            var cli = connection.Client;
            var lobby = new Dictionary<string, int>();

            var roomPrefixes = new List<string> {"PW", "OW", "BW"};
            cli.Multiplayer.ListRooms(null, null, 0, 0, delegate(RoomInfo[] rooms)
            {
                foreach (var t in from t in rooms
                    let roomStart = t.Id.Substring(0, 2)
                    where !t.RoomType.StartsWith("Lobby")
                    where roomPrefixes.Contains(roomStart)
                    select t)
                {
						if (!blackList.Contains(t.Id)) {
                    lobby.Add(t.Id, t.OnlineUsers);
						}
                }

                // return client back to the cold, dark, pool.
                Core.pool.PutObject(connection);
                waitForLobby.Set();
            });
            waitForLobby.WaitOne();

            Core.LobbyNew = lobby.OrderByDescending(pair => pair.Value);
        }
    }
}