using Sandbox.ModAPI;
using System;
using VRageMath;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRage.Game.ModAPI;

namespace Rek.FoodSystem
{
    public class NeedsApi {
        public class Event {
            public enum Type {
                RegisterEdibleItem,
                RegisterDrinkableItem
            };
    
            public Type type;
            public object payload;
        }

        public class RegisterEdibleItemEvent {
            public RegisterEdibleItemEvent(string szItemName,  float value) {
                this.item = szItemName;
                this.value = value;
            }
        
            public string item;
            public float value;
        }
        
        public class RegisterDrinkableItemEvent {
            public RegisterDrinkableItemEvent(string szItemName,  float value) {
                this.item = szItemName;
                this.value = value;
            }
        
            public string item;
            public float value;
        }
    
        public NeedsApi() {
        
        }
        
        public void RegisterEdibleItem(string szItemName, float value)
        {
            Event message = new Event();
            message.type = Event.Type.RegisterEdibleItem;
            message.payload = new RegisterEdibleItemEvent(szItemName, value);
            
            MyAPIGateway.Utilities.SendModMessage(1339, message);
        }
        
        public void RegisterDrinkableItem(string szItemName, float value)
        {
            Event message = new Event();
            message.type = Event.Type.RegisterDrinkableItem;
            message.payload = new RegisterDrinkableItemEvent(szItemName, value);
            
            MyAPIGateway.Utilities.SendModMessage(1339, message);
        }

        public void SetPlayerHunger(ulong player, float value) {
        
        }
        
        public void SetPlayerThirst(ulong player, float value) {
        
        }
        
        public void AddPlayerHunger(ulong player, float value) {
        
        }
        
        public void AddPlayerThirst(ulong player, float value) {
        
        }  
    }

}