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
using VRage.ModAPI;
using Sandbox.Game;
using Sandbox.Game.World;
using VRageMath;
using VRageRender;

namespace Rek.FoodSystem
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Server : MySessionComponentBase
	{

        private static float MAX_NEEDS_VALUE; 
        private static float MIN_NEEDS_VALUE;
        private static float HUNGRY_WHEN;
        private static float THIRSTY_WHEN;
        private static float THIRST_PER_DAY;
        private static float HUNGER_PER_DAY;
        private static float DAMAGE_SPEED_HUNGER;
        private static float DAMAGE_SPEED_THIRST;
        private static float DEFAULT_MODIFIER;
        private static float FLYING_MODIFIER;
        private static float RUNNING_MODIFIER;
        private static float SPRINTING_MODIFIER;
        private static float NO_MODIFIER;
        private static float CRAP_AMOUNT;
        private static float CROSS_CRAP_AMOUNT;
        private static float DEATH_RECOVERY;
        private static bool FATIGUE_ENABLED;
        private static float FATIGUE_SITTING;
        private static float FATIGUE_CROUCHING;
        private static float FATIGUE_STANDING;
        private static float FATIGUE_WALKING;
        private static float FATIGUE_RUNNING;
        private static float FATIGUE_SPRINTING;
        private static float FATIGUE_FLYING;
        private static float EXTRA_THIRST_FROM_FATIGUE;
        private static float FATIGUE_LEVEL_NOHEALING;
        private static float FATIGUE_LEVEL_FORCEWALK;
        private static float FATIGUE_LEVEL_FORCECROUCH;
        private static float FATIGUE_LEVEL_HELMET;
        private static float FATIGUE_LEVEL_HEARTATTACK;
        private static String STIMULANT_STRING;
        private static String CHICKEN_SOUP_STRING;

        //Determines bonus values.
        private static float FOOD_BONUS;
        private static float DRINK_BONUS;
        private static float REST_BONUS;

        //Determines starting values for new game/charecter.
        private static float STARTING_HUNGER;
        private static float STARTING_THIRST;
        private static float STARTING_FATIGUE;

        //Determines clone sickness re-spawn values.
        private static float RESPAWN_HUNGER;
        private static float RESPAWN_THIRST;
        private static float RESPAWN_FATIGUE;

        //HUD Values
        private static float HUNGER_ICON_POSITION_X;
        private static float HUNGER_ICON_POSITION_Y;
        private static float THIRST_ICON_POSITION_X;
        private static float THIRST_ICON_POSITION_Y;
        private static float FATIGUE_ICON_POSITION_X;
        private static float FATIGUE_ICON_POSITION_Y;

        private static bool CREATIVETOOLS_NODECAY;

        private int food_logic_skip = 0; // internal counter, init at 0
        private const int FOOD_LOGIC_SKIP_TICKS = 60 * 1; // Updates in realtime every second

        private float mHungerPerMinute;
		private float mThirstPerMinute;
        private bool dead = false;
        private bool IsAutohealingOn = false;
		private float dayLen = 120f;
		private bool config_get = false;
        private static bool decayEnabled = true;

		//private static Config mConfig = Config.Load("hatm.cfg");
		private static PlayerDataStore mPlayerDataStore = new PlayerDataStore();
		private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
		private static List<IMyPlayer> mPlayers = new List<IMyPlayer>();
		private static Dictionary<string, float> mFoodTypes = new Dictionary<string, float>();
		private static Dictionary<string, float> mBeverageTypes = new Dictionary<string, float>();
		private const string OBJECT_BUILDER_PREFIX = "ObjectBuilder_";
		private static bool modStarted = false;

        private static MyEntity3DSoundEmitter soundEmitter = new MyEntity3DSoundEmitter(null)
        {
            CustomMaxDistance = 30f,
        };

        private static MySoundPair EATING_SOUND = new MySoundPair("Eating");
        private static MySoundPair DRINKING_SOUND = new MySoundPair("Drinking");

        private void Init()
		{
			mPlayerDataStore.Load();
			mConfigDataStore.Load();
            
			MAX_NEEDS_VALUE = mConfigDataStore.get_MAX_NEEDS_VALUE();
			MIN_NEEDS_VALUE = mConfigDataStore.get_MIN_NEEDS_VALUE();
			HUNGRY_WHEN = mConfigDataStore.get_HUNGRY_WHEN();
			THIRSTY_WHEN = mConfigDataStore.get_THIRSTY_WHEN();
			THIRST_PER_DAY = mConfigDataStore.get_THIRST_PER_DAY();
			HUNGER_PER_DAY = mConfigDataStore.get_HUNGER_PER_DAY();
			DAMAGE_SPEED_HUNGER = mConfigDataStore.get_DAMAGE_SPEED_HUNGER();
			DAMAGE_SPEED_THIRST = mConfigDataStore.get_DAMAGE_SPEED_THIRST();
			DEFAULT_MODIFIER = mConfigDataStore.get_DEFAULT_MODIFIER();
			FLYING_MODIFIER = mConfigDataStore.get_FLYING_MODIFIER();
			RUNNING_MODIFIER = mConfigDataStore.get_RUNNING_MODIFIER();
			SPRINTING_MODIFIER = mConfigDataStore.get_SPRINTING_MODIFIER();
			NO_MODIFIER = mConfigDataStore.get_NO_MODIFIER();
			CRAP_AMOUNT = mConfigDataStore.get_CRAP_AMOUNT();
			CROSS_CRAP_AMOUNT = mConfigDataStore.get_CROSS_CRAP_AMOUNT();
			DEATH_RECOVERY = mConfigDataStore.get_DEATH_RECOVERY();
			
			FATIGUE_ENABLED = mConfigDataStore.get_FATIGUE_ENABLED();
			FATIGUE_SITTING = mConfigDataStore.get_FATIGUE_SITTING();
			FATIGUE_CROUCHING = mConfigDataStore.get_FATIGUE_CROUCHING();
			FATIGUE_STANDING = mConfigDataStore.get_FATIGUE_STANDING();
            FATIGUE_WALKING = mConfigDataStore.get_FATIGUE_WALKING();
            FATIGUE_RUNNING = mConfigDataStore.get_FATIGUE_RUNNING();
			FATIGUE_SPRINTING = mConfigDataStore.get_FATIGUE_SPRINTING();
            FATIGUE_FLYING = mConfigDataStore.get_FATIGUE_FLYING();
            EXTRA_THIRST_FROM_FATIGUE = mConfigDataStore.get_EXTRA_THIRST_FROM_FATIGUE();

			FATIGUE_LEVEL_NOHEALING = mConfigDataStore.get_FATIGUE_LEVEL_NOHEALING();
			FATIGUE_LEVEL_FORCEWALK = mConfigDataStore.get_FATIGUE_LEVEL_FORCEWALK();
			FATIGUE_LEVEL_FORCECROUCH = mConfigDataStore.get_FATIGUE_LEVEL_FORCECROUCH();
			FATIGUE_LEVEL_HELMET = mConfigDataStore.get_FATIGUE_LEVEL_HELMET();
			FATIGUE_LEVEL_HEARTATTACK = mConfigDataStore.get_FATIGUE_LEVEL_HEARTATTACK();

            STIMULANT_STRING = mConfigDataStore.get_STIMULANT_STRING();
            CHICKEN_SOUP_STRING = mConfigDataStore.get_CHICKEN_SOUP_STRING();

            FOOD_BONUS = mConfigDataStore.get_CRAP_AMOUNT();
            DRINK_BONUS = mConfigDataStore.get_CRAP_AMOUNT();
            REST_BONUS = mConfigDataStore.get_CRAP_AMOUNT();

            STARTING_HUNGER = mConfigDataStore.get_STARTING_HUNGER();
            STARTING_THIRST = mConfigDataStore.get_STARTING_THIRST();
            STARTING_FATIGUE = mConfigDataStore.get_STARTING_FATIGUE();

            RESPAWN_HUNGER = mConfigDataStore.get_RESPAWN_HUNGER();
            RESPAWN_THIRST = mConfigDataStore.get_RESPAWN_THIRST();
            RESPAWN_FATIGUE = mConfigDataStore.get_RESPAWN_FATIGUE();

            CREATIVETOOLS_NODECAY = mConfigDataStore.get_CREATIVETOOLS_NODECAY();
           
            // Minimum of 2h, because it's unplayable under....
            IsAutohealingOn = MyAPIGateway.Session.SessionSettings.AutoHealing; dayLen = Math.Max(MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes, 120f);
            mThirstPerMinute = THIRST_PER_DAY / dayLen;
            mHungerPerMinute = HUNGER_PER_DAY / dayLen;
			mConfigDataStore.Save();

			if (Utils.isDev())
				MyAPIGateway.Utilities.ShowMessage("SERVER", "INIT");

			MyAPIGateway.Multiplayer.RegisterMessageHandler(1338, AdminCommandHandler);
			MyAPIGateway.Utilities.RegisterMessageHandler(1339, NeedsApiHandler);
			NeedsApi api = new NeedsApi();

            // TODO un-hardcode these - move an xml file maybe?
            // Any negative means that it will only refill to 50% of the MAX_NEEDS_VALUE as defined in the config file.
            // *** REGISTERING DRINKS ITEM ***
            api.RegisterDrinkableItem("EmergencyWater_DNSK", -10f); // emergency ration, takes ages to make and only available in emergency rations block, reduces thirst by 25 but wont affect thirst levels above 50% of Max.
            api.RegisterDrinkableItem("Water_DNSK", 25f);
            api.RegisterDrinkableItem("Coffee_DNSK", 50f); 
            api.RegisterDrinkableItem("HotChocolate_DNSK", 50f); 
            
            // *** REGISTERING FOODS ITEMS ***
            api.RegisterEdibleItem("EmergencyFood_DNSK", -10f); // emergency ration, takes ages to make and only available in emergency rations block, reduces hunger by 10 but wont affect hunger levels above 50% of Max.
            api.RegisterEdibleItem("SpaceBar_DNSK", 25f);
            api.RegisterEdibleItem("SimpleMeal_DNSK", 50f);
            api.RegisterEdibleItem("FineMeal_DNSK", 100f);
            api.RegisterEdibleItem("LuxuryMeal_DNSK", 150f);
            api.RegisterEdibleItem("ProteinShake_DNSK", 25f);
		}

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;

            // Food logic is desactivated in creative mode
            if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative)
                return;
            
            try
            {
                if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer)
                {
                    if (!modStarted)
                    {
                        modStarted = true;
                        Init();

                        food_logic_skip = FOOD_LOGIC_SKIP_TICKS;
                    }

                    if (++food_logic_skip >= FOOD_LOGIC_SKIP_TICKS)// && !MyAPIGateway.Session.HasCreativeRights)
                    {
                        food_logic_skip = 0;

                        UpdatePlayerList();
                        UpdateFoodLogic();
                    }
                }
            }
            catch (Exception e)
            {
                //MyApiGateway.Utilities.ShowMessage("ERROR", "Logger error: " + e.Message + "\n" + e.StackTrace);

                Logging.Instance.WriteLine(("(FoodSystem) Server UpdateSimulation Error: " + e.Message + "\n" + e.StackTrace));
            }
        }

        private void UpdateFoodLogic()
        {
            bool ChangedStance = false;
            MyObjectBuilder_Character character;
            
            MyCharacterMovementEnum curmove = MyCharacterMovementEnum.Sitting;

            foreach (IMyPlayer player in mPlayers)
            {
                if (player.Controller != null && player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity != null && player.Controller.ControlledEntity.Entity.DisplayName != "")
                {
                    PlayerData playerData = mPlayerDataStore.get(player);
                    //Logging.Instance.WriteLine(playerData.ToString() + "Loaded to Server");

                    IMyEntity controlledEnt = player.Controller.ControlledEntity.Entity;
                    controlledEnt = GetCharacterEntity(controlledEnt);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "Character: " + entity.DisplayName); // gets me player name

                    float CurrentModifier = 1f;
                    float FatigueRate = 0f;

                    bool ForceEating = false;
                    float RecycleBonus = 1f;
                    bool FatigueBonus = false;
                    bool HungerBonus = false;
                    bool ThirstBonus = false;

                    // if we were under the effects of a bonus, keep it until we no longer are
                    if (playerData.fatigue > MAX_NEEDS_VALUE)
                        FatigueBonus = true;
                    if (playerData.thirst > MAX_NEEDS_VALUE)
                        ThirstBonus = true;
                    if (playerData.hunger > MAX_NEEDS_VALUE)
                        HungerBonus = true;

                    if (controlledEnt is IMyCharacter)
                    {
                        character = controlledEnt.GetObjectBuilder(false) as MyObjectBuilder_Character;
                        //MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState);

                        // Checks for a datastore for the player.
                        if (playerData.entity == null || playerData.entity.Closed || playerData.entity.EntityId != controlledEnt.EntityId)
                        {
                            bool newPlayerOrNeedsReset = false;

                            if (!playerData.loaded)
                            {
                                newPlayerOrNeedsReset = true;
                                playerData.loaded = true;
                            }
                            else if ((playerData.entity != null) && (playerData.entity != controlledEnt))
                            {
                                newPlayerOrNeedsReset = true;
                            }

                            // Determines what values you start a new game / playerDataStore with.
                            if (newPlayerOrNeedsReset)
                            {
                                playerData.hunger = STARTING_HUNGER;
                                playerData.thirst = STARTING_THIRST;
                                playerData.fatigue = STARTING_FATIGUE;
                            }

                            playerData.entity = controlledEnt;
                        }

                        // Determines what values you re-spawn with.
                        if (dead)
                        {
                            playerData.hunger = RESPAWN_HUNGER;
                            playerData.thirst = RESPAWN_THIRST;
                            playerData.fatigue = RESPAWN_FATIGUE;
                            dead = false;

                        }

                        //MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState + ":" + playerData.lastmovement);
                        ChangedStance = playerData.lastmovement != character.MovementState;
                        curmove = character.MovementState;

                        switch (character.MovementState)
                        {

                            // Check if player is 'sitting'.
                            case MyCharacterMovementEnum.Sitting:
                                IMyCubeBlock cb = player.Controller.ControlledEntity.Entity as IMyCubeBlock;
                                
                                CurrentModifier = DEFAULT_MODIFIER;
                                FatigueRate = FATIGUE_SITTING;

                                // Case-Switch: Check if interacting with a bed, a lunchroom seat or a treadmill.
                                // cb.DisplayNameText is name of individual block.
                                // cb.DefinitionDisplayNameText is name of block type.
                                
                                String seatBlockName = cb.DisplayNameText.ToLower();
                                String seatBlockType = cb.DefinitionDisplayNameText.ToLower();

                                if (seatBlockType.Contains("cryo")) // Checks if player is in a Cryopd - practically freezes stats.
                                {
                                    CurrentModifier = 0.0000125f;
                                    FatigueRate = 0.0000125f;
                                }
                                
                                else if (seatBlockType.Contains("treadmill"))
                                {
                                    CurrentModifier = RUNNING_MODIFIER; // jog...
                                    FatigueRate = FATIGUE_RUNNING / 2.5f; // but pace yourself
                                }

                                else if (seatBlockType.Contains("bed") || seatBlockType.Contains("bunk"))
                                {
                                    CurrentModifier = DEFAULT_MODIFIER / 2f; // nap time! Needs are reduced.
                                    FatigueRate = FATIGUE_SITTING * 3f; //  nap time! Rest is greatly sped up.
                                    FatigueBonus |= !ChangedStance; // longer nap? OK, allow for extra resting
                                }

                                else if (seatBlockType.Contains("toilet") && ChangedStance)
                                {
                                    ForceEating = true; // also forces crapping, so this makes sense. but use changedstance to do it only once.
                                    RecycleBonus = 1.5f;
                                }

                                else if (seatBlockType.Contains("bathroom") && ChangedStance)
                                {
                                    ForceEating = true; // also forces crapping, so this makes sense. but use changedstance to do it only once.
                                    RecycleBonus = 1.5f;
                                }

                                else if (seatBlockName.Contains("noms"))
                                {
                                    ForceEating = true; // also forces crapping, fortunately the suit takes care of it. Eat continuously while sitting.
                                    HungerBonus |= playerData.hunger > MAX_NEEDS_VALUE * 0.99; // get to 100% first, then apply bonus.
                                    ThirstBonus |= playerData.thirst > MAX_NEEDS_VALUE * 0.99; // get to 100% first, then apply bonus.
                                }

                                break;

                            case MyCharacterMovementEnum.Flying:
                                CurrentModifier = FLYING_MODIFIER;
                                FatigueRate = FATIGUE_FLYING; // operating a jetpack is surprisingly hard
                                break;

                            case MyCharacterMovementEnum.Falling:
                                CurrentModifier = FLYING_MODIFIER;
                                FatigueRate = FATIGUE_WALKING; // change nothing for the first iteration (prevents jump exploit)
                                if (!ChangedStance)
                                    FatigueRate = FATIGUE_STANDING; // freefall is actually relaxing when you are used to it. A professional space engineer would be.
                                break;

                            case MyCharacterMovementEnum.Crouching:
                            case MyCharacterMovementEnum.CrouchRotatingLeft:
                            case MyCharacterMovementEnum.CrouchRotatingRight:
                                CurrentModifier = DEFAULT_MODIFIER;
                                FatigueRate = FATIGUE_CROUCHING;
                                break;

                            case MyCharacterMovementEnum.Standing:
                            case MyCharacterMovementEnum.RotatingLeft:
                            case MyCharacterMovementEnum.RotatingRight:
                                CurrentModifier = DEFAULT_MODIFIER;
                                FatigueRate = FATIGUE_STANDING;
                                break;

                            case MyCharacterMovementEnum.CrouchWalking:
                            case MyCharacterMovementEnum.CrouchBackWalking:
                            case MyCharacterMovementEnum.CrouchStrafingLeft:
                            case MyCharacterMovementEnum.CrouchStrafingRight:
                            case MyCharacterMovementEnum.CrouchWalkingRightFront:
                            case MyCharacterMovementEnum.CrouchWalkingRightBack:
                            case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                            case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                                CurrentModifier = RUNNING_MODIFIER;
                                FatigueRate = FATIGUE_RUNNING; // doing the duckwalk is more tiring than walking: try it if you don't believe me
                                break;



                            case MyCharacterMovementEnum.Walking:
                            case MyCharacterMovementEnum.BackWalking:
                            case MyCharacterMovementEnum.WalkStrafingLeft:
                            case MyCharacterMovementEnum.WalkStrafingRight:
                            case MyCharacterMovementEnum.WalkingRightFront:
                            case MyCharacterMovementEnum.WalkingRightBack:
                            case MyCharacterMovementEnum.WalkingLeftFront:
                            case MyCharacterMovementEnum.WalkingLeftBack:
                                CurrentModifier = DEFAULT_MODIFIER;
                                FatigueRate = FATIGUE_WALKING;
                                break;

                            case MyCharacterMovementEnum.LadderUp:
                                CurrentModifier = RUNNING_MODIFIER;
                                FatigueRate = FATIGUE_RUNNING;
                                break;

                            case MyCharacterMovementEnum.LadderDown:
                                CurrentModifier = DEFAULT_MODIFIER;
                                FatigueRate = FATIGUE_WALKING;
                                break;

                            case MyCharacterMovementEnum.Running:
                            case MyCharacterMovementEnum.Backrunning:
                            case MyCharacterMovementEnum.RunStrafingLeft:
                            case MyCharacterMovementEnum.RunStrafingRight:
                            case MyCharacterMovementEnum.RunningRightFront:
                            case MyCharacterMovementEnum.RunningRightBack:
                            case MyCharacterMovementEnum.RunningLeftBack:
                            case MyCharacterMovementEnum.RunningLeftFront:
                                CurrentModifier = RUNNING_MODIFIER;
                                FatigueRate = FATIGUE_RUNNING;
                                break;

                            case MyCharacterMovementEnum.Sprinting:
                            case MyCharacterMovementEnum.Jump:
                                CurrentModifier = SPRINTING_MODIFIER;
                                FatigueRate = FATIGUE_SPRINTING;
                                break;

                            case MyCharacterMovementEnum.Died:
                                CurrentModifier = DEFAULT_MODIFIER; // unused, but let's have them
                                FatigueRate = FATIGUE_STANDING; // unused, but let's have them
                                dead = true; // for death recovery logic
                                break;

                        }
                        playerData.lastmovement = character.MovementState; // track delta

                    }
                    else if (playerData.entity != null || !playerData.entity.Closed)
                        controlledEnt = playerData.entity;

                    // Sanity checks
                    if (HungerBonus)
                    {
                        if (playerData.hunger > MAX_NEEDS_VALUE * FOOD_BONUS)
                            playerData.hunger = MAX_NEEDS_VALUE * FOOD_BONUS;
                    }
                    else
                    {
                        if (playerData.hunger > MAX_NEEDS_VALUE)
                            playerData.hunger = MAX_NEEDS_VALUE;
                    }

                    if (ThirstBonus)
                    {
                        if (playerData.thirst > MAX_NEEDS_VALUE * DRINK_BONUS)
                            playerData.thirst = MAX_NEEDS_VALUE * DRINK_BONUS;
                    }
                    else
                    {
                        if (playerData.thirst > MAX_NEEDS_VALUE)
                            playerData.thirst = MAX_NEEDS_VALUE;
                    }

                    // Check if Creative Tools no decay enabled in config and enable/disable behaviour.
                    if (CREATIVETOOLS_NODECAY)
                    {
                        if (MyAPIGateway.Session.EnableCopyPaste)
                        {
                            decayEnabled = false;
                        }
                        else
                        {
                            decayEnabled = true;
                        }
                    }
                    
                    // Process fatigue needs
                    if (FATIGUE_ENABLED && decayEnabled)
                    {
                        playerData.fatigue += (FatigueRate * CurrentModifier * FOOD_LOGIC_SKIP_TICKS / 60 * 20);// / 15);
                        playerData.fatigue = Math.Max(playerData.fatigue, MIN_NEEDS_VALUE);
                        if (FatigueBonus)
                            playerData.fatigue = Math.Min(playerData.fatigue, MAX_NEEDS_VALUE * REST_BONUS);
                        else
                            playerData.fatigue = Math.Min(playerData.fatigue, MAX_NEEDS_VALUE);

                    }
                    else
                        playerData.fatigue = 9001f;

                    if (playerData.fatigue <= 0)
                    {

                        // fatigue consequences
                        // at 0, start causing extra thirst
                        // at specified, force walk instead of run (unless overriding by sprinting)
                        // at specified, force crouch, and do damage flashes
                        // at specified, breathing reflex / mess with helmet, and do a bit of actual damage (just in case thirst isn't already causing it)
                        // at specified, cause heart attack

                        if (playerData.fatigue <= (0.00f * MIN_NEEDS_VALUE))
                        {
                            if (EXTRA_THIRST_FROM_FATIGUE > 0)
                            {
                                // positive: pile on to thirst, per second
                                playerData.thirst -= (EXTRA_THIRST_FROM_FATIGUE * FOOD_LOGIC_SKIP_TICKS / 60);
                            }
                            else
                            { // negative: multiply modifier
                                CurrentModifier *= -EXTRA_THIRST_FROM_FATIGUE;
                            }
                        }

                        if (playerData.fatigue <= (FATIGUE_LEVEL_FORCEWALK * MIN_NEEDS_VALUE))
                        { // force player to walk if they were running
                            switch (curmove)
                            {
                                case MyCharacterMovementEnum.Sprinting:
                                case MyCharacterMovementEnum.Running:
                                case MyCharacterMovementEnum.Backrunning:
                                case MyCharacterMovementEnum.RunStrafingLeft:
                                case MyCharacterMovementEnum.RunStrafingRight:
                                case MyCharacterMovementEnum.RunningRightFront:
                                case MyCharacterMovementEnum.RunningRightBack:
                                case MyCharacterMovementEnum.RunningLeftBack:
                                case MyCharacterMovementEnum.RunningLeftFront:
                                    VRage.Game.ModAPI.Interfaces.IMyControllableEntity controlledEntity = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                                    controlledEntity.SwitchWalk();
                                    break;
                            }
                        }

                        if (playerData.fatigue <= (FATIGUE_LEVEL_FORCECROUCH * MIN_NEEDS_VALUE))
                        {
                            bool iscrouching = false;
                            switch (curmove)
                            {
                                case MyCharacterMovementEnum.Crouching:
                                case MyCharacterMovementEnum.CrouchWalking:
                                case MyCharacterMovementEnum.CrouchBackWalking:
                                case MyCharacterMovementEnum.CrouchStrafingLeft:
                                case MyCharacterMovementEnum.CrouchStrafingRight:
                                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                                    iscrouching = true;
                                    break;
                            }
                            if (!iscrouching)
                            {
                                VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                                ce.Crouch(); // force player to crouch
                            }
                        }

                        if (playerData.fatigue <= (FATIGUE_LEVEL_HELMET * MIN_NEEDS_VALUE))
                        {
                            VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                            ce.SwitchHelmet(); // force player to switch helmet, panic reaction from trying to catch breath

                            var destroyable = controlledEnt as IMyDestroyableObject;
                            destroyable.DoDamage(0.001f, MyStringHash.GetOrCompute("Fatigue"), true); // starting to hurt
                        }

                        if (playerData.fatigue <= (FATIGUE_LEVEL_NOHEALING * MIN_NEEDS_VALUE))
                        {
                            var destroyable = controlledEnt as IMyDestroyableObject;
                            destroyable.DoDamage(0.001f, MyStringHash.GetOrCompute("Fatigue"), true); // starting to hurt
                            if (IsAutohealingOn) // fatigued? no autohealing, either.
                            {
                                const float HealthTick = 100f / 240f * FOOD_LOGIC_SKIP_TICKS / 60f;
                                destroyable.DoDamage(0.0f, MyStringHash.GetOrCompute("Testing"), false);
                            }

                        }

                        if (playerData.fatigue <= (FATIGUE_LEVEL_HEARTATTACK * MIN_NEEDS_VALUE))
                        {
                            var destroyable = controlledEnt as IMyDestroyableObject;
                            destroyable.DoDamage(1000f, MyStringHash.GetOrCompute("Fatigue"), true); // sudden, but very avoidable, heart attack ;)
                        }
                    }

                    // Process thirst needs
                    if (playerData.thirst > MIN_NEEDS_VALUE && decayEnabled)
                    {
                        // We update every second - at default values this is 100 / 120 / 60 * 1 = 0.0138 thirst per second
                        // Config value / Default Day Length / Sixty Seconds * Multiplier = Thirst per second
                        playerData.thirst -= mThirstPerMinute / 60 * CurrentModifier; 
                        playerData.thirst = Math.Max(playerData.thirst, MIN_NEEDS_VALUE);
                    }

                    // Process hunger needs
                    if (playerData.hunger > MIN_NEEDS_VALUE && decayEnabled)
                    {
                        // We update every second - at default values this is 50 / 120 / 60 * 1 = 0.0069 hunger per second 
                        // Config value / Default Day Length / Sixty Seconds * Multiplier = Hunger per second
                        playerData.hunger -= mHungerPerMinute / 60 * CurrentModifier;
                        playerData.hunger = Math.Max(playerData.hunger, MIN_NEEDS_VALUE);
                    }

                    // Try to meet needs
                    if (playerData.hunger < (MAX_NEEDS_VALUE * HUNGRY_WHEN) || ForceEating)
                        playerEatSomething(controlledEnt, playerData, HungerBonus ? MAX_NEEDS_VALUE * 1.25f : MAX_NEEDS_VALUE, RecycleBonus);

                    if (playerData.thirst < (MAX_NEEDS_VALUE * THIRSTY_WHEN) || ForceEating)
                        playerDrinkSomething(controlledEnt, playerData, ThirstBonus ? MAX_NEEDS_VALUE * 1.25f : MAX_NEEDS_VALUE, RecycleBonus);

                    // Cause damage if needs are unmet
                    if (playerData.thirst <= 0)
                    {
                        var destroyable = controlledEnt as IMyDestroyableObject;
                        if (DAMAGE_SPEED_THIRST > 0)
                            destroyable.DoDamage((IsAutohealingOn ? (DAMAGE_SPEED_THIRST + 1f) : DAMAGE_SPEED_THIRST), MyStringHash.GetOrCompute("Thirst"), true);
                        else
                            destroyable.DoDamage(((IsAutohealingOn ? (-DAMAGE_SPEED_THIRST + 1f) : -DAMAGE_SPEED_THIRST) + DAMAGE_SPEED_THIRST * playerData.thirst), MyStringHash.GetOrCompute("Thirst"), true);
                    }

                    if (playerData.hunger <= 0)
                    {
                        var destroyable = controlledEnt as IMyDestroyableObject;
                        if (DAMAGE_SPEED_HUNGER > 0)
                            destroyable.DoDamage((IsAutohealingOn ? (DAMAGE_SPEED_HUNGER + 1f) : DAMAGE_SPEED_HUNGER), MyStringHash.GetOrCompute("Hunger"), true);
                        else
                            destroyable.DoDamage(((IsAutohealingOn ? (-DAMAGE_SPEED_HUNGER + 1f) : -DAMAGE_SPEED_HUNGER) + DAMAGE_SPEED_HUNGER * playerData.hunger), MyStringHash.GetOrCompute("Hunger"), true);
                    }

                    /*
					character = entity.GetObjectBuilder(false) as MyObjectBuilder_Character;
					if (character.Health == null) // ok, so the variable exists, but it's always null for some reason?
						CurPlayerHealth = 101f;
					else
						CurPlayerHealth = (float) (character.Health);

					if (IsAutohealingOn && CurPlayerHealth < 70f)
					{
						const float HealthTick = 100f / 240f * FOOD_LOGIC_SKIP_TICKS / 60f;
						var destroyable = entity as IMyDestroyableObject;
						destroyable.DoDamage(HealthTick, MyStringHash.GetOrCompute("Testing"), false);
					}
					 */

                    if (dead && DEATH_RECOVERY > 0.0)
                    {
                        MyInventoryBase inventory = ((MyEntity)controlledEnt).GetInventoryBase();
                        if (playerData.hunger > 0)
                            inventory.AddItems((MyFixedPoint)((1f / MAX_NEEDS_VALUE) * DEATH_RECOVERY * (playerData.hunger)), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                        if (playerData.thirst > 0)
                            inventory.AddItems((MyFixedPoint)((1f / MAX_NEEDS_VALUE) * DEATH_RECOVERY * (playerData.thirst)), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater" });
                    }

                    //Sends data from Server.cs to Client.cs
                    string message = MyAPIGateway.Utilities.SerializeToXML<PlayerData>(playerData);
                    //Logging.Instance.WriteLine(("Message sent from Server.cs to Client.cs: " + message));
                    MyAPIGateway.Multiplayer.SendMessageTo(
                        1337,
                        Encoding.Unicode.GetBytes(message),
                        player.SteamUserId
                    );
                }
            }
        }

        // Update the player list
        private void UpdatePlayerList()
        {
            mPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(mPlayers);
        }

        private static bool playerEatSomething(IMyEntity entity, PlayerData playerData, float maxval_cap, float crapbonus)
        {
            MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
            var items = inventory.GetItems();

            foreach (IMyInventoryItem item in items)
            {
                float result;

                // Getting the item type
                string szItemContent = item.Content.ToString();
                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

                // Type verification
                if (!szTypeName.Equals("Ingot"))
                    continue;

                if (mFoodTypes.TryGetValue(item.Content.SubtypeName, out result))
                {
                                    
                    float canConsumeNum = 0f;

                    // if a food is registered as negative, reduce the maximum value. Useful for low nutrition meals.
                    if (result < 0)
                    {
                        result = Math.Abs(result);
                        canConsumeNum = Math.Min((((maxval_cap / 2f) - playerData.hunger) / result), (float)item.Amount);
                    }
                    else
                    {
                        canConsumeNum = Math.Min(((maxval_cap - playerData.hunger) / result), (float)item.Amount);
                    }

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "canEat: " + canConsumeNum);

                    if (canConsumeNum > 0)
                    {
                        // Play eating sound
                        //MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("Eating", playerData.entity.PositionComp.WorldAABB.Matrix.Forward * 2.0);
                        soundEmitter.Entity = (MyEntity) entity;
                        soundEmitter.PlaySound(EATING_SOUND);
                        
                        inventory.Remove(item, (MyFixedPoint)canConsumeNum);
                        playerData.hunger += result * (float)canConsumeNum;
                        if (item.Content.SubtypeName.Contains("Shake")) // TODO parametrize this
                            playerData.thirst += Math.Max(0f, Math.Min(result * (float)canConsumeNum, maxval_cap - playerData.thirst)); // TODO parametrize this
                            
                        foreach (var player in mPlayers)
                        {
                            if (playerData.steamid == player.SteamUserId && item.Content.SubtypeName.Contains("SuitEnergy"))
                            {
                                // Replenish suit energy by food amount value or up until max whichever is the lesser value.
                                var playerEnergy = player.Character.SuitEnergyLevel;
                                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(player.IdentityId, playerEnergy += Math.Max(0f, Math.Min(result * (float)canConsumeNum, 1 - playerEnergy)));
                            }
                        }

                        // Waste management line
                        if (CRAP_AMOUNT > 0.0)
                        {
                            inventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                            if (CROSS_CRAP_AMOUNT > 0.0)
                                inventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater_DNSK" });
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool playerDrinkSomething(IMyEntity entity, PlayerData playerData, float maxval_cap, float crapbonus)
        {
            MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
            var items = inventory.GetItems();

            foreach (IMyInventoryItem item in items)
            {
                float result;

                // Getting the item type
                string szItemContent = item.Content.ToString();

                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "szItemContent: " + item.Content.SubtypeName);
                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

                // Type verification
                if (!szTypeName.Equals("Ingot"))
                    continue;

                if (mBeverageTypes.TryGetValue(item.Content.SubtypeName, out result))
                {
                    float canConsumeNum = 0f;

                    // if a drink is registered as negative, reduce the maximum value. Useful for low nutrition drinks.
                    if (result < 0)
                    {
                        result = Math.Abs(result);
                        canConsumeNum = Math.Min((((maxval_cap / 2f) - playerData.thirst) / result), (float)item.Amount);
                    }
                    else
                    {
                        canConsumeNum = Math.Min(((maxval_cap - playerData.thirst) / result), (float)item.Amount);
                    }

                    // Debug check for values.
                    /*
                    var info1 = string.Format("Max: {0} -> Thirst: {1} -> Result: {2} -> ItemAmount: {3} -> CanConsumeNum: {4}", maxval_cap.ToString(),
                        playerData.thirst.ToString(), result.ToString(), (float) item.Amount, canConsumeNum.ToString());
                    MyVisualScriptLogicProvider.SendChatMessage(info1);
                    MyVisualScriptLogicProvider.SendChatMessage("CanConsumNum: " + canConsumeNum.ToString());
                    */
                    

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "canDrink: " + canConsumeNum);
                    if (canConsumeNum > 0)
                    {
                        soundEmitter.Entity = (MyEntity)entity;
                        soundEmitter.PlaySound(DRINKING_SOUND);

                        inventory.Remove(item, (MyFixedPoint)canConsumeNum);

                        playerData.thirst += result * (float)canConsumeNum;

                        if (item.Content.SubtypeName.Contains("Coffee") || item.Content.SubtypeName.Contains("HotChocolate")) // TODO parametrize this
                            playerData.fatigue += 25.0f; // TODO parametrize this

                        else if (item.Content.SubtypeName.Contains(STIMULANT_STRING)) // TODO parametrize this
                            playerData.fatigue = MAX_NEEDS_VALUE; // TODO parametrize this

                        if (item.Content.SubtypeName.Contains("ouillon")) // TODO parametrize this
                            playerData.hunger += Math.Max(0f, Math.Min(result * (float)canConsumeNum, maxval_cap - playerData.hunger)); // TODO parametrize this

                        else if (item.Content.SubtypeName.Contains(CHICKEN_SOUP_STRING)) // TODO parametrize this
                            playerData.hunger += Math.Max(0f, Math.Min(result * (float)canConsumeNum, maxval_cap - playerData.hunger)); // TODO parametrize this

                        // waste management line
                        if (CRAP_AMOUNT > 0.0)
                        {
                            inventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater_DNSK" });
                            if (CROSS_CRAP_AMOUNT > 0.0)
                                inventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                        }
                        return true;
                    }
                }
            }

            return false;
        }

		private IMyEntity GetCharacterEntity(IMyEntity entity)
		{
			if (entity is MyCockpit)
				return (entity as MyCockpit).Pilot as IMyEntity;

			if (entity is MyRemoteControl)
				return (entity as MyRemoteControl).Pilot as IMyEntity;

			//TODO: Add more pilotable entities
			return entity;
		}

		public void AdminCommandHandler(byte[] data)
		{
			//Keen why do you not pass the steamId? :/
			Command command = MyAPIGateway.Utilities.SerializeFromXML<Command>(Encoding.Unicode.GetString(data));

			/*if (Utils.isAdmin(command.sender)) {
                var words = command.content.Trim().ToLower().Replace("/", "").Split(' ');
                if (words.Length > 0 && words[0] == "hatm") {
                    switch (words[1])
                    {
                        case "blacklist":
                            IMyPlayer player = mPlayers.Find(p => words[2] == p.DisplayName);
                            mConfig.BlacklistAdd(player.SteamUserId);
                            break;
                    }
                }
            }*/
		}

		public void NeedsApiHandler(object data)
		{
			//mFoodTypes.Add(szItemName, hungerValue);
			//mBeverageTypes.Add(szItemName, thirstValue);

			NeedsApi.Event e = (NeedsApi.Event)data;

			if (e.type == NeedsApi.Event.Type.RegisterEdibleItem) {
				NeedsApi.RegisterEdibleItemEvent edibleItemEvent = (NeedsApi.RegisterEdibleItemEvent)e.payload;
				//MyAPIGateway.Utilities.ShowMessage("DEBUG", "EdibleItem " + edibleItemEvent.item + "(" +  edibleItemEvent.value + ") registered");
				mFoodTypes.Add(edibleItemEvent.item, edibleItemEvent.value);
			} else if (e.type == NeedsApi.Event.Type.RegisterDrinkableItem) {
				NeedsApi.RegisterDrinkableItemEvent drinkableItemEvent = (NeedsApi.RegisterDrinkableItemEvent)e.payload;
				//MyAPIGateway.Utilities.ShowMessage("DEBUG", "DrinkableItem " + drinkableItemEvent.item + "(" +  drinkableItemEvent.value + ") registered");
				mBeverageTypes.Add(drinkableItemEvent.item, drinkableItemEvent.value);
			}
		}

		// Saving datas when requested
		public override void SaveData()
		{
			mPlayerDataStore.Save();
			mConfigDataStore.Save();
		}

		protected override void UnloadData()
		{
			modStarted = false;
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(1338, AdminCommandHandler);
			MyAPIGateway.Utilities.UnregisterMessageHandler(1339, NeedsApiHandler);
			mPlayers.Clear();
			mFoodTypes.Clear();
			mBeverageTypes.Clear();
			mPlayerDataStore.clear();
			mConfigDataStore.clear();
			Logging.Instance.Close();
		}
	}
}