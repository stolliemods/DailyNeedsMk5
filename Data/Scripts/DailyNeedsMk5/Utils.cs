using System;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game;
using Sandbox.ModAPI;

/*
Thanks to midspace for some code snippets:
https://github.com/midspace/Space-Engineers-Admin-script-mod
*/

namespace Rek.FoodSystem
{
    public static class Utils {
        private static ulong[] Developers = { 76561198006687351 };
        
        public static bool isDev(ulong steamid) {
            return Developers.Contains(steamid);
        }
        
        public static bool isDev() {
            return isDev(MyAPIGateway.Multiplayer.MyId);
        }
        
        public static bool isDev(IMyPlayer player) {
            
            return isDev(player.SteamUserId);
        }
        
        public static bool isAdmin(ulong steamId) {
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer)
            {
                return true;
            }
            
            // determine if client is admin of Dedicated server.
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients != null)
            {
                var client = clients.FirstOrDefault(c => c.SteamId == steamId && c.IsAdmin);
                return client != null;
                // If user is not in the list, automatically assume they are not an Admin.
            }
            
            // clients is null when it's not a dedicated server.
            // Otherwise Treat everyone as Normal Player.
        
            return false;
        }
    }
}
