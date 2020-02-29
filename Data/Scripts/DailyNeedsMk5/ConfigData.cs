using VRage.ModAPI;
using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;

namespace Rek.FoodSystem {
    public class ConfigData
    {

        public ulong steamid;
		public float MAX_NEEDS_VALUE;
		public float MIN_NEEDS_VALUE;
        public float MIN_STAMINA_VALUE;
        public float HUNGRY_WHEN; 
		public float THIRSTY_WHEN;
		public float THIRST_PER_DAY;
		public float HUNGER_PER_DAY;
		public float DAMAGE_SPEED_HUNGER;
		public float DAMAGE_SPEED_THIRST;
		public float FATIGUE_DEFAULT_MULTIPLIER;
		public float FATIGUE_FLYING_MULTIPLIER;
		public float FATIGUE_RUNNING_MULTIPLIER;
		public float FATIGUE_SPRINTING_MODIFIER;
		public float FATIGUE_NO_MODIFIER;
		public float CRAP_AMOUNT;
		public float CROSS_CRAP_AMOUNT;
		public float DEATH_RECOVERY;

        public bool FATIGUE_ENABLED;
		public float FATIGUE_SITTING;
		public float FATIGUE_CROUCHING;
		public float FATIGUE_STANDING;
		public float FATIGUE_WALKING;
		public float FATIGUE_RUNNING;
		public float FATIGUE_SPRINTING;
        public float FATIGUE_FLYING;
        public float EXTRA_THIRST_FROM_FATIGUE;

        public float FATIGUE_LEVEL_NOHEALING;
		public float FATIGUE_LEVEL_FORCEWALK;
		public float FATIGUE_LEVEL_FORCECROUCH;
		public float FATIGUE_LEVEL_HELMET;
		public float FATIGUE_LEVEL_HEARTATTACK;

        public String STIMULANT_STRING;
        public String CHICKEN_SOUP_STRING;

        public float FOOD_BONUS;
        public float DRINK_BONUS;
        public float REST_BONUS;

        public float STARTING_HUNGER;
        public float STARTING_THIRST;
        public float STARTING_FATIGUE;

        public float RESPAWN_HUNGER;
        public float RESPAWN_THIRST;
        public float RESPAWN_FATIGUE;

        public float HUNGER_ICON_POSITION_X;
        public float HUNGER_ICON_POSITION_Y;
        public float THIRST_ICON_POSITION_X;
        public float THIRST_ICON_POSITION_Y;
        public float FATIGUE_ICON_POSITION_X;
        public float FATIGUE_ICON_POSITION_Y;

        public bool AUTOMATIC_BLOCK_COLOR;
        public bool CREATIVETOOLS_NODECAY;

        public bool EATING_AND_DRINKING_REQUIRES_PRESSURISATION;

        public ConfigData()
        {
            MAX_NEEDS_VALUE = 100f;
		    MIN_NEEDS_VALUE = -100f; // if less than zero, a severely starved character will have to consume more
            MIN_STAMINA_VALUE = 0f; // if less than zero, a severely starved character will have to consume more
            HUNGRY_WHEN = 0.5f; // if need is this much of maxval, consume
		    THIRSTY_WHEN = 0.5f; // if need is this much of maxval, consume
		    THIRST_PER_DAY = 100f;
		    HUNGER_PER_DAY = 50f;
		    DAMAGE_SPEED_HUNGER = -0.2f; // 2; // if negative, scale to minvalue for damage. if positive, do this much damage every tick.
		    DAMAGE_SPEED_THIRST = -0.6f; //5; // if negative, scale to minvalue for damage.  if positive, do this much damage every tick.

            FATIGUE_DEFAULT_MULTIPLIER = 1f;
		    FATIGUE_FLYING_MULTIPLIER = 1f;
		    FATIGUE_RUNNING_MULTIPLIER = 1.5f;
		    FATIGUE_SPRINTING_MODIFIER = 3f;
		    FATIGUE_NO_MODIFIER = 1f;

            CRAP_AMOUNT = 0.90f; // if zero, skip creating waste, otherwise, make GreyWater and Organic right after eating, and don't go into details
		    CROSS_CRAP_AMOUNT = 0.0f; // does eating/drinking generate any amount of the "other" waste? formula is (1-crapamount)*this
		    DEATH_RECOVERY = 2.00f; // if true, "evacuate" before dying, based on current hunger and thirst level. This number is how much is evacuated if player is at 100%

            FATIGUE_ENABLED = true;
		    FATIGUE_SITTING = 0.3f;
		    FATIGUE_CROUCHING = 0.2f;
		    FATIGUE_STANDING = 0.1f;
            FATIGUE_WALKING = -0.01f;
		    FATIGUE_RUNNING = -0.02f;
		    FATIGUE_SPRINTING = -0.05f;
            FATIGUE_FLYING = -0.02f;
            EXTRA_THIRST_FROM_FATIGUE = -1.5f; // negative: multiply thirst modifier. positive: add to thirst directly.

            FATIGUE_LEVEL_NOHEALING = 0.01f; // at this fraction of MIN_NEEDS_VALUE, prevent autoheal
		    FATIGUE_LEVEL_FORCEWALK = 0.2f; // at this fraction of MIN_NEEDS_VALUE, try to force walking
		    FATIGUE_LEVEL_FORCECROUCH = 0.5f; // at this fraction of MIN_NEEDS_VALUE, try to force walking
		    FATIGUE_LEVEL_HELMET = 0.70f; // at this fraction of MIN_NEEDS_VALUE, toggle helmet
		    FATIGUE_LEVEL_HEARTATTACK = 0.999f; // at this fraction of MIN_NEEDS_VALUE, heart attack

            CHICKEN_SOUP_STRING = "ChickenSoupString"; // effectively disabled
            STIMULANT_STRING = "StimulantString"; // effectively disabled

            //Determines bonus values.
            FOOD_BONUS = 1.25f;
            DRINK_BONUS = 1.25f;
            REST_BONUS = 1.25f;

            //Determines starting values for new game/charecter.
            STARTING_HUNGER = 125f;
		    STARTING_THIRST = 125f;
		    STARTING_FATIGUE = 125f;

            // Clone Sickness Resspawn Values
            RESPAWN_HUNGER = 31f;
            RESPAWN_THIRST = 31f;
            RESPAWN_FATIGUE = 51f;

            //HUD Values
            HUNGER_ICON_POSITION_X = -0.941f;
            HUNGER_ICON_POSITION_Y = 0.90f;
            THIRST_ICON_POSITION_X = -0.941f;
            THIRST_ICON_POSITION_Y = 0.85f;
            FATIGUE_ICON_POSITION_X = -0.941f;
            FATIGUE_ICON_POSITION_Y = 0.80f;

            AUTOMATIC_BLOCK_COLOR = true;
            CREATIVETOOLS_NODECAY = false;
            EATING_AND_DRINKING_REQUIRES_PRESSURISATION = true;
        }
    }
}
