using System.Collections.Generic;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using System.IO;
using System;
using Digi;

namespace Stollie.DailyNeeds
{
    public class PlayerDataStore
    {
        private Dictionary<ulong, PlayerData> mPlayerData;
        private string mFilename;
    
        public PlayerDataStore()
        {
            mFilename = "PlayerData.xml";
            mPlayerData = new Dictionary<ulong, PlayerData>();
        }
        
        public PlayerData get(IMyPlayer player) {
            PlayerData result;
            if(!mPlayerData.TryGetValue(player.SteamUserId , out result)) {
                result = new PlayerData(player.SteamUserId);
                mPlayerData.Add(player.SteamUserId , result);
            }
            return result;
        }

        public void Save()
        {
            try
            {
                PlayerData[] tmp = new PlayerData[mPlayerData.Count];
                mPlayerData.Values.CopyTo(tmp, 0);
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(mFilename, typeof(PlayerDataStore));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML<PlayerData[]>(tmp));
                writer.Flush();
                writer.Close();
            } catch (Exception e)
            {
                Log.Error("Player Data Store Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void Load()
        {
            try {
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "Loading file");

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(mFilename, typeof(PlayerDataStore)))
                    return;

                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(mFilename, typeof(PlayerDataStore));
                string xmlText = reader.ReadToEnd();
                reader.Close();

                PlayerData[] tmp = MyAPIGateway.Utilities.SerializeFromXML<PlayerData[]>(xmlText);

                foreach (PlayerData x in tmp)
                {
                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "found player");
                    x.loaded = true;
                    mPlayerData.Add(x.steamid, x);
                }
            } catch(Exception e) {
                MyAPIGateway.Utilities.ShowMessage("ERROR", "Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        
        public void clear() {
            mPlayerData.Clear();
        }
    }
}
