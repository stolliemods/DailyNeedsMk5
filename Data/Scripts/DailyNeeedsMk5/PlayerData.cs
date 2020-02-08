using VRage.ModAPI;
using VRage.Game;
using System.Xml.Serialization;

namespace Rek.FoodSystem {
    public class PlayerData
    {
        public ulong steamid;
        public float hunger;
        public float thirst;
        public float fatigue;

        [XmlIgnoreAttribute]
        public VRage.Game.MyCharacterMovementEnum lastmovement;
        
        [XmlIgnoreAttribute]
        public IMyEntity entity;
        
        [XmlIgnoreAttribute]
        public bool loaded;

        public PlayerData(ulong id)
        {
            thirst = 100;
            hunger = 100;
            fatigue = 100;
            lastmovement = 0;
            entity = null;
            steamid = id;
            loaded = false;
        }

        public PlayerData() {
            thirst = 100;
            hunger = 100;
            fatigue = 100;
            lastmovement = 0;
            entity = null;
            loaded = false;
        }
    }
}
