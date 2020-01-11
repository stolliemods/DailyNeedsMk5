using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Rek.FoodSystem
{
    public class Config {
        private string mFilename;
        private ConfigData mConfigData;
        
        public Config() {}
        
        private Config(string filename, ConfigData data) {
            mFilename = filename;
            mConfigData = data;
        }
        
        public void Save() {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(mFilename, typeof(ConfigData));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(mConfigData));
            writer.Flush();
            writer.Close();
        }
        
        public static Config Load(string filename) {
            if(!MyAPIGateway.Utilities.FileExistsInLocalStorage(filename, typeof(ConfigData))) {
                return new Config(filename, new ConfigData());
            } else {
                ConfigData data;
                TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(filename, typeof(ConfigData));
                string xmlText = reader.ReadToEnd();
                reader.Close();
                
                if (string.IsNullOrWhiteSpace(xmlText)) {
                    data = new ConfigData();
                } else {
                    data = MyAPIGateway.Utilities.SerializeFromXML<ConfigData>(xmlText);
                }
                
                return new Config(filename, data);
            }
        }
        
        public bool BlacklistAdd(ulong steamId) {
            return mConfigData.FoodBlacklist.Add(steamId);
        }
        
        public bool BlacklistContains(ulong steamId) {
            return mConfigData.FoodBlacklist.Contains(steamId);
        }
        
        public class ConfigData
        {
            [XmlArray("FoodBlacklist")]
            [XmlArrayItem("Player")]
            public HashSet<ulong> FoodBlacklist;
        }
    
    }
    
}
