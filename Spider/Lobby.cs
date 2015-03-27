using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerIOClient;

namespace Spider
{
    /// <summary>
    ///     Class Lobby.
    /// </summary>
    internal static class Lobby // this is the least maintainable class in the project, according
        // to code metrics
    {
        /// <summary>
        ///     Lists the specified token.
        /// </summary>
        public static void List()
        {
            var blackList =
                "PWw5SwzgEXb0I\nPWjyTtlTPjbkI\nPWdBHWybO0a0I\nPWADSqksB-a0I\nPWAGTxuoOLa0I\nPW6tE27AhqbkI\nPWqEDKY7EDb0I\nPWZaOSj2GFbkI\nPW7huwlwUFbEI";
                //[TC] goeyfun bot
            var waitForLobby = new AutoResetEvent(false);
            var connection = Core.Pool.GetObject();
            var cli = connection.Client;
            var lobby = new Dictionary<string, int>();

            var roomPrefixes = new List<string> {"PW", "OW", "BW"};
            cli.Multiplayer.ListRooms(null, null, 0, 0, delegate(RoomInfo[] rooms)
            {
                foreach (var room in from t in rooms
                    let roomStart = t.Id.Substring(0, 2)
                    where !t.RoomType.StartsWith("Lobby")
                    where roomPrefixes.Contains(roomStart)
                    select t)
                {
                    if (!blackList.Contains(room.Id))
                    {
                        lobby.Add(room.Id, room.OnlineUsers);
                    }
                    else
                    {
                        Logger.Log(LogPriority.Warning, "Level " + room.Id + " is blacklisted. Skipping...");
                    }
                }

                // return client back to the cold, dark, pool.
                //connection = null;
                Core.Pool.PutObject(connection);
                waitForLobby.Set();
            });
            waitForLobby.WaitOne();

            Core.LobbyNew = lobby.OrderByDescending(pair => pair.Value);
        }
    }
}