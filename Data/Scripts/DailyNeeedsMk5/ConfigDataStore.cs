using System.Collections.Generic;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using System.IO;
using System;

namespace Rek.FoodSystem
{
    public class ConfigDataStore
    {
    	public ConfigData mConfigData;
        public string mFilename;
    
        public ConfigDataStore()
        {
            mFilename = "DailyNeedsConfigData.xml";
            mConfigData = new ConfigData();
        }
        
        public float get_MAX_NEEDS_VALUE() { return mConfigData.MAX_NEEDS_VALUE;}
        public float get_MIN_NEEDS_VALUE() { return mConfigData.MIN_NEEDS_VALUE;}
        public float get_MIN_STAMINA_VALUE() { return mConfigData.MIN_STAMINA_VALUE; }
        public float get_HUNGRY_WHEN() { return mConfigData.HUNGRY_WHEN;}
        public float get_THIRSTY_WHEN() { return mConfigData.THIRSTY_WHEN;}
        public float get_THIRST_PER_DAY() { return mConfigData.THIRST_PER_DAY;}
        public float get_HUNGER_PER_DAY() { return mConfigData.HUNGER_PER_DAY;}
        public float get_DAMAGE_SPEED_HUNGER() { return mConfigData.DAMAGE_SPEED_HUNGER;}
        public float get_DAMAGE_SPEED_THIRST() { return mConfigData.DAMAGE_SPEED_THIRST;}
        public float get_DEFAULT_MODIFIER() { return mConfigData.DEFAULT_MODIFIER;}
        public float get_FLYING_MODIFIER() { return mConfigData.FLYING_MODIFIER;}
        public float get_RUNNING_MODIFIER() { return mConfigData.RUNNING_MODIFIER;}
        public float get_SPRINTING_MODIFIER() { return mConfigData.SPRINTING_MODIFIER;}
        public float get_NO_MODIFIER() { return mConfigData.NO_MODIFIER;}
        public float get_CRAP_AMOUNT() { return mConfigData.CRAP_AMOUNT;}
        public float get_CROSS_CRAP_AMOUNT() { return mConfigData.CROSS_CRAP_AMOUNT;}
        public float get_DEATH_RECOVERY() { return mConfigData.DEATH_RECOVERY;}
        
        public bool  get_FATIGUE_ENABLED() { return mConfigData.FATIGUE_ENABLED;}
        public float get_FATIGUE_SITTING() { return mConfigData.FATIGUE_SITTING;}
        public float get_FATIGUE_CROUCHING() { return mConfigData.FATIGUE_CROUCHING;}
        public float get_FATIGUE_STANDING() { return mConfigData.FATIGUE_STANDING;}
        public float get_FATIGUE_WALKING() { return mConfigData.FATIGUE_WALKING;}
        public float get_FATIGUE_RUNNING() { return mConfigData.FATIGUE_RUNNING;}
        public float get_FATIGUE_SPRINTING() { return mConfigData.FATIGUE_SPRINTING;}
        public float get_FATIGUE_FLYING() { return mConfigData.FATIGUE_FLYING; }
        public float get_EXTRA_THIRST_FROM_FATIGUE() { return mConfigData.EXTRA_THIRST_FROM_FATIGUE;}

        public float get_FATIGUE_LEVEL_NOHEALING() { return mConfigData.FATIGUE_LEVEL_NOHEALING;}
        public float get_FATIGUE_LEVEL_FORCEWALK() { return mConfigData.FATIGUE_LEVEL_FORCEWALK;}
        public float get_FATIGUE_LEVEL_FORCECROUCH() { return mConfigData.FATIGUE_LEVEL_FORCECROUCH;}
        public float get_FATIGUE_LEVEL_HELMET() { return mConfigData.FATIGUE_LEVEL_HELMET;}
        public float get_FATIGUE_LEVEL_HEARTATTACK() { return mConfigData.FATIGUE_LEVEL_HEARTATTACK;}

        public String get_STIMULANT_STRING() {return mConfigData.STIMULANT_STRING;}
        public String get_CHICKEN_SOUP_STRING() {return mConfigData.CHICKEN_SOUP_STRING;}

        public float get_FOOD_BONUS() { return mConfigData.FOOD_BONUS; }
        public float get_DRINK_BONUS() { return mConfigData.DRINK_BONUS; }
        public float get_REST_BONUS() { return mConfigData.REST_BONUS; }

        public float get_STARTING_HUNGER() { return mConfigData.STARTING_HUNGER; }
        public float get_STARTING_THIRST() { return mConfigData.STARTING_THIRST; }
        public float get_STARTING_FATIGUE() { return mConfigData.STARTING_FATIGUE; }
        
        public float get_RESPAWN_HUNGER() { return mConfigData.RESPAWN_HUNGER; }
        public float get_RESPAWN_THIRST() { return mConfigData.RESPAWN_THIRST; }
        public float get_RESPAWN_FATIGUE() { return mConfigData.RESPAWN_FATIGUE; }

        // HUD Values
        public float get_HUNGER_ICON_POSITION_X() { return mConfigData.HUNGER_ICON_POSITION_X; }
        public float get_HUNGER_ICON_POSITION_Y() { return mConfigData.HUNGER_ICON_POSITION_Y; }
        public float get_THIRST_ICON_POSITION_X() { return mConfigData.THIRST_ICON_POSITION_X; }
        public float get_THIRST_ICON_POSITION_Y() { return mConfigData.THIRST_ICON_POSITION_Y; }
        public float get_FATIGUE_ICON_POSITION_X() { return mConfigData.FATIGUE_ICON_POSITION_X; }
        public float get_FATIGUE_ICON_POSITION_Y() { return mConfigData.FATIGUE_ICON_POSITION_Y; }

        // Automatic block re-coloring
        public bool get_AUTOMATIC_BLOCK_COLOR() { return mConfigData.AUTOMATIC_BLOCK_COLOR; }
        public bool get_CREATIVETOOLS_NODECAY() { return mConfigData.CREATIVETOOLS_NODECAY; }
        public bool get_EATING_AND_DRINKING_REQUIRES_PRESSURISATION() { return mConfigData.EATING_AND_DRINKING_REQUIRES_PRESSURISATION; }

        public void Save()
        {
            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(mFilename, typeof(ConfigData));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConfigData>(mConfigData));
                writer.Flush();
                writer.Close();
            } catch (Exception e)
            {
                Logging.Instance.WriteLine(("(FoodSystem) Config Save Data Error: " + e.Message + "\n" + e.StackTrace));
            }
        }

        public bool Load()
        {
            try {

                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(mFilename, typeof(ConfigData)))
                {
                mConfigData = new ConfigData();
                return false;
                }

                TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(mFilename, typeof(ConfigData));
                string xmlText = reader.ReadToEnd();
                reader.Close();

                mConfigData = MyAPIGateway.Utilities.SerializeFromXML<ConfigData>(xmlText);
                

            } catch(Exception e) {
                MyAPIGateway.Utilities.ShowMessage("ERROR", "Error: " + e.Message + "\n" + e.StackTrace);
                mConfigData = new ConfigData();
                return false;
            }
        	
        	return true;
        }

        public void clear() {
                mConfigData = new ConfigData();
        }
    }
}
