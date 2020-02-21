using VRage.ModAPI;
using VRage.Game;
using System.Xml.Serialization;

namespace Rek.FoodSystem {
    public class PlayerData
    {
        public ulong steamid;
        public long playerId;
        public float hunger;
        public float thirst;
        public float fatigue;
        public float juice;
        public bool dead;

        [XmlIgnoreAttribute]
        public VRage.Game.MyCharacterMovementEnum lastmovement;
        
        [XmlIgnoreAttribute]
        public IMyEntity entity;
        
        [XmlIgnoreAttribute]
        public bool loaded;

        public PlayerData(ulong id)
        {
            steamid = id;
            playerId = 0;
            hunger = 100;
            thirst = 100;
            fatigue = 100;
            juice = 0;
            lastmovement = 0;
            dead = false;
            entity = null;
            loaded = false;
        }

        public PlayerData() {
            playerId = 0;
            hunger = 100;
            thirst = 100;
            fatigue = 100;
            juice = 0;
            lastmovement = 0;
            dead = false;
            entity = null;
            loaded = false;
        }
    }
}
