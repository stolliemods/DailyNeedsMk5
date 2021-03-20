using System;
using System.Collections.Generic;
using System.Linq;
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
using Sandbox.Game;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.ModAPI.Weapons;
using Sandbox.Definitions;
using VRage.Game.ModAPI.Ingame;
using VRage.Input;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyEntity = VRage.ModAPI.IMyEntity;
using IMyInventoryItem = VRage.Game.ModAPI.IMyInventoryItem;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Components;
using Draygo.API;
using VRage.ModAPI;
using VRage.Profiler;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;
using DailyNeedsMk5.Data.Scripts.DailyNeedsMk5;

namespace Rek.FoodSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Server : MySessionComponentBase
    {
        // Various consumption config file modifier settings
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

        // Determines bonus values.
        private static float FOOD_BONUS;
        private static float DRINK_BONUS;
        private static float FATIGUE_BONUS;

        // Determines starting values for new game/charecter.
        private static float STARTING_HUNGER;
        private static float STARTING_THIRST;
        private static float STARTING_FATIGUE;

        // Determines clone sickness re-spawn values.
        private static float RESPAWN_HUNGER;
        private static float RESPAWN_THIRST;
        private static float RESPAWN_FATIGUE;

        // Misc. Settings including update tick timer.
        private static bool CREATIVETOOLS_NODECAY;
        private static bool EATING_AND_DRINKING_REQUIRES_PRESSURISATION;
        private const int FOOD_LOGIC_SKIP_TICKS = 60 * 1; // Updates in realtime every second

        // Player Data Stores
        //private static Config mConfig = Config.Load("hatm.cfg");
        private static PlayerDataStore mPlayerDataStore = new PlayerDataStore();
        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private static List<IMyPlayer> mPlayers = new List<IMyPlayer>();
        private static Dictionary<IMyPlayer, float> healingPlayers = new Dictionary<IMyPlayer, float>();

        // Item Type Dictionaries
        private static Dictionary<string, float> mFoodTypes = new Dictionary<string, float>();
        private static Dictionary<string, Drinkables_Struct> mBeverageTypes = new Dictionary<string, Drinkables_Struct>();
        private static Dictionary<string, float> mDrugTypes = new Dictionary<string, float>();

        // Sound Emitters
        private static MyEntity3DSoundEmitter soundEmitter = new MyEntity3DSoundEmitter(null, true, 1.0f);
        private static MySoundPair EATING_SOUND = new MySoundPair("Eating");
        private static MySoundPair DRINKING_SOUND = new MySoundPair("Drinking");
        private static MySoundPair FASTHEARTWITHBEEP_SOUND_FADEOUT = new MySoundPair("FastHeartBeatWithBeep_Fadeout");
        private static MySoundPair FASTHEARTWITHBEEP_SOUND = new MySoundPair("FastHeartBeatWithBeep");
        private bool fatigueRecoverySoundPlayed;

        // Easy Inventory API
        private EasyInventoryAPI EasyAPI;

        // Misc. class variables.
        private float mHungerPerMinute;
        private float mThirstPerMinute;
        private bool IsAutohealingOn = false;
        private float dayLen = 120f;
        private bool config_get = false;
        private static bool decayEnabled = true;
        private int currentFatigue;
        private const string OBJECT_BUILDER_PREFIX = "ObjectBuilder_";
        private static bool modStarted = false;
        private int food_logic_skip = 0; // internal counter, init at 0
        private static bool healPlayer = false;
        private static int healPlayerTick = 0;
        private bool eventHandlersActive = false;
        private static List<IMyPlayer> playersToRemove = new List<IMyPlayer>();
        private static int testingTick = 0;

        private static Dictionary<IMyCubeGrid, long> upgradedGrids = new Dictionary<IMyCubeGrid, long>();
        private static HashSet<IMyCharacter> juicedPilots = new HashSet<IMyCharacter>();

        private static StopWatch stopwatch = new StopWatch();

        /// <summary>
        /// Converts the Mined Ore param for all definitions containing subtypeId 'Ice'  to 'DirtyIce'.
        /// <param name=""></param>
        /// <returns></returns>
        /// </summary>
        public override void LoadData()
        {
            var allVoxelMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions();
            foreach (var def in allVoxelMaterials)
            {
                if (def.Id.SubtypeName.Contains("Ice"))
                {
                    def.MinedOre = "Ice_Dirty";
                }
            }
        }

        private void Init_Main()
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

            FOOD_BONUS = mConfigDataStore.get_FOOD_BONUS();
            DRINK_BONUS = mConfigDataStore.get_DRINK_BONUS();
            FATIGUE_BONUS = mConfigDataStore.get_FATIGUE_BONUS();

            STARTING_HUNGER = mConfigDataStore.get_STARTING_HUNGER();
            STARTING_THIRST = mConfigDataStore.get_STARTING_THIRST();
            STARTING_FATIGUE = mConfigDataStore.get_STARTING_FATIGUE();

            RESPAWN_HUNGER = mConfigDataStore.get_RESPAWN_HUNGER();
            RESPAWN_THIRST = mConfigDataStore.get_RESPAWN_THIRST();
            RESPAWN_FATIGUE = mConfigDataStore.get_RESPAWN_FATIGUE();

            CREATIVETOOLS_NODECAY = mConfigDataStore.get_CREATIVETOOLS_NODECAY();
            EATING_AND_DRINKING_REQUIRES_PRESSURISATION = mConfigDataStore.get_EATING_AND_DRINKING_REQUIRES_PRESSURISATION();

            // Minimum of 2h, because it's unplayable under....
            IsAutohealingOn = MyAPIGateway.Session.SessionSettings.AutoHealing;
            dayLen = Math.Max(MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes, 120f);
            mThirstPerMinute = THIRST_PER_DAY / dayLen;
            mHungerPerMinute = HUNGER_PER_DAY / dayLen;
            mConfigDataStore.Save();

            /*
            if (MyAPIGateway.Utilities.GamePaths.ModScopeName.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("LnNibQ=="))) == true &&
                (MyAPIGateway.Utilities.GamePaths.ModScopeName.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("MTk1NzU4Mjc1OQ=="))) == false))
            {
                return;
            }
            */

            if (Utils.isDev())
            {
                MyAPIGateway.Utilities.ShowMessage("SERVER", "INIT");
                Logging.Instance.WriteLine("SERVER: INIT");
            }

            MyAPIGateway.Multiplayer.RegisterMessageHandler(1338, AdminCommandHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(1339, NeedsApiHandler);

            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
            MyAPIGateway.Entities.OnEntityRemove += EntityRemoved;

            EasyAPI = new EasyInventoryAPI(registerCallback);
        }

        // Maintain entity list additions.
        public void EntityAdded(IMyEntity entity)
        {
        }

        // Maintain entity list removals.
        public void EntityRemoved(IMyEntity entity)
        {
        }

        private void registerCallback()
        {
            EasyAPI.RegisterEasyFilter("DNEIF", EasyAPIRegister);
        }

        private void Init_RegisterConsumableItems()
        {
            NeedsApi needsApi = new NeedsApi();

            // TODO un-hardcode these - move to an xml file maybe?
            // Any negative means that it will only refill to 50% of the MAX_NEEDS_VALUE as defined in the config file.
            // *** REGISTERING DRINKS ITEM ***
            needsApi.RegisterDrinkableItem("EmergencyWater_DNSK", 0.0f, -5f, 0.0f);
            needsApi.RegisterDrinkableItem("Water_DNSK", 0.0f, 20f, 0.0f);
            needsApi.RegisterDrinkableItem("Coffee_DNSK", 5.0f, 20f, 50.0f);
            needsApi.RegisterDrinkableItem("HotChocolate_DNSK", 5.0f, 25f, 50.0f);
            needsApi.RegisterDrinkableItem("Vodka", -5.0f, 20f, 50.0f);

            // *** REGISTERING FOODS ITEMS ***
            needsApi.RegisterEdibleItem("EmergencyFood_DNSK", -10f);
            needsApi.RegisterEdibleItem("SpaceBar_DNSK", 25f);
            needsApi.RegisterEdibleItem("SimpleMeal_DNSK_50", 50f);
            needsApi.RegisterEdibleItem("FineMeal_DNSK", 100f);
            needsApi.RegisterEdibleItem("LuxuryMeal_DNSK", 150f);
            needsApi.RegisterEdibleItem("ProteinShake_DNSK", 25f);

            needsApi.RegisterDrugItem("HealthRestoreDrug_DNSK", 25.0f);
            needsApi.RegisterDrugItem("EnergyRestoreDrug_DNSK", 25.0f);
            needsApi.RegisterDrugItem("JuiceDrug_DNSK", 20.0f);
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
                if (!MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
                    return;

                if (!modStarted)
                {
                    modStarted = true;
                    Init_Main();
                    Init_RegisterConsumableItems();
                    food_logic_skip = FOOD_LOGIC_SKIP_TICKS;
                }

                // Update every 1 second real-time (60 ticks)
                if (++food_logic_skip >= FOOD_LOGIC_SKIP_TICKS)
                {
                    food_logic_skip = 0;

                    UpdatePlayerList();
                    UpdateNeedsLogic();
                    CheckUpgradedGrids();
                }

                if (testingTick == 200)
                    testingTick = 0;

                testingTick++;

            }
            catch (Exception e)
            {
                //MyApiGateway.Utilities.ShowMessage("ERROR", "Logger error: " + e.Message + "\n" + e.StackTrace);

                Logging.Instance.WriteLine(("(FoodSystem) Server UpdateSimulation Error: " + e.Message + "\n" + e.StackTrace));
            }
        }

        /// <summary>
        /// Tells EasyInventory to ignore item from deposit functions.
        /// </summary>
        private bool EasyAPIRegister(MyItemType item)
        {
            if (item.SubtypeId.Contains("_DNSK"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the player list every time FOOD_LOGIC_SKIP_TICKS is processed in UpdateAfterSimulation(), just prior to UpdateNeedsLogic().
        /// </summary>
        private void UpdatePlayerList()
        {
            mPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(mPlayers);
        }

        /// <summary>
        /// Run the major needs logic components.
        /// Called everytime FOOD_LOGIC_SKIP_TICKS is processed in UpdateAfterSimulation() and after the playerlist is updated by UpdatePlayerList().
        /// </summary>
        private void UpdateNeedsLogic()
        {
            try
            {
                //stopwatch.Start("Needs Logic");
                foreach (IMyPlayer player in mPlayers)
                {
                    if (player.Controller != null && player.Controller.ControlledEntity != null &&
                        player.Controller.ControlledEntity.Entity != null &&
                        player.Controller.ControlledEntity.Entity.DisplayName != "")
                    {
                        PlayerData playerData = mPlayerDataStore.get(player);
                        //Logging.Instance.WriteLine(playerData.ToString() + " Loaded to Server");
                        //MyAPIGateway.Utilities.ShowMessage("DEBUG", "Character: " + controlledEntity.DisplayName); // gets players name
                        
                        bool HungerBonus = false;
                        bool ThirstBonus = false;
                        bool FatigueBonus = false;
                        bool ChangedStance = false;

                        // Check if player is under the effects of a bonus, keep it until they no longer are.
                        if (playerData.hunger > MAX_NEEDS_VALUE) HungerBonus = true;
                        if (playerData.thirst > MAX_NEEDS_VALUE) ThirstBonus = true;
                        if (playerData.fatigue > MAX_NEEDS_VALUE) FatigueBonus = true;

                        // Sanity checks - make sure playerStore values dont exceed max config values.
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

                        if (FatigueBonus)
                        {
                            if (playerData.fatigue > MAX_NEEDS_VALUE * FATIGUE_BONUS)
                                playerData.fatigue = MAX_NEEDS_VALUE * FATIGUE_BONUS;
                        }
                        else
                        {
                            if (playerData.fatigue > MAX_NEEDS_VALUE)
                                playerData.fatigue = MAX_NEEDS_VALUE;
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

                        IMyEntity controlledEnt = player.Controller.ControlledEntity.Entity;
                        controlledEnt = GetCharacterEntity(controlledEnt);

                        // Checks for a datastore for the player and sets initial needs.
                        if (controlledEnt is IMyCharacter)
                        {
                            MyObjectBuilder_Character character =
                                controlledEnt.GetObjectBuilder(false) as MyObjectBuilder_Character;
                            //MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState); //Check Character state

                            if (playerData.entity == null || playerData.entity.Closed ||
                                playerData.entity.EntityId != controlledEnt.EntityId)
                            {
                                bool newPlayerOrNeedsReset = false;
                                // Checks if player data was loaded on gameload, see playerDataStore.cs
                                if (!playerData.loaded)
                                {
                                    newPlayerOrNeedsReset = true;
                                    playerData.loaded = true;
                                }
                                // If player data was loaded but the entities aren't matching, rest the datastore.
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
                                    playerData.entity = controlledEnt;
                                }
                            }

                            // Reset death bool and set clone sickness respawn values if the player was dead.
                            if (playerData.dead)
                            {
                                playerData.hunger = RESPAWN_HUNGER;
                                playerData.thirst = RESPAWN_THIRST;
                                playerData.fatigue = RESPAWN_FATIGUE;
                                playerData.dead = false;
                            }
                        }
                        else if (playerData.entity != null || !playerData.entity.Closed)
                            controlledEnt = playerData.entity;

                        #region Movement State Effects
                        float CurrentModifier = 1f;
                        float FatigueRate = 0f;
                        float RecycleBonus = 1f;
                        bool ForceEating = false;
                        MyCharacterMovementEnum currentMovementState = MyCharacterMovementEnum.Sitting;
                        if (controlledEnt is IMyCharacter)
                        {
                            MyObjectBuilder_Character character =
                                controlledEnt.GetObjectBuilder(false) as MyObjectBuilder_Character;
                            ChangedStance = playerData.lastmovement != character.MovementState;
                            currentMovementState = character.MovementState;
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

                                    if (seatBlockType.Contains("cryo")
                                    ) // Checks if player is in a Cryopod - practically freezes stats.
                                    {
                                        CurrentModifier = 0.0000125f;
                                        FatigueRate = 0.0000125f;
                                    }

                                    else if (seatBlockType.Contains("treadmill"))
                                    {
                                        CurrentModifier = RUNNING_MODIFIER; // jog...
                                        FatigueRate = FATIGUE_RUNNING / 2.5f; // but pace yourself
                                    }

                                    else if (seatBlockType.Contains("bed") || seatBlockType.Contains("bunk") ||
                                             seatBlockType.Contains("stateroom"))
                                    {
                                        CurrentModifier = DEFAULT_MODIFIER / 2f; // nap time! Needs are reduced.
                                        FatigueRate = FATIGUE_SITTING * 3f; //  nap time! Rest is greatly sped up.
                                        FatigueBonus |= !ChangedStance; // longer nap? OK, allow for extra resting
                                    }

                                    else if (seatBlockType.Contains("toilet") && ChangedStance)
                                    {
                                        ForceEating =
                                            true; // also forces crapping, so this makes sense. but use changedstance to do it only once.
                                        RecycleBonus = 1.5f;
                                    }

                                    else if (seatBlockType.Contains("bathroom") && ChangedStance)
                                    {
                                        ForceEating =
                                            true; // also forces crapping, so this makes sense. but use changedstance to do it only once.
                                        RecycleBonus = 1.5f;
                                    }

                                    else if (seatBlockName.Contains("noms"))
                                    {
                                        ForceEating =
                                            true; // also forces crapping, fortunately the suit takes care of it. Eat continuously while sitting.
                                        HungerBonus |=
                                            playerData.hunger >
                                            MAX_NEEDS_VALUE * 0.99; // get to 100% first, then apply bonus.
                                        ThirstBonus |=
                                            playerData.thirst >
                                            MAX_NEEDS_VALUE * 0.99; // get to 100% first, then apply bonus.
                                    }

                                    break;

                                
                                case MyCharacterMovementEnum.Flying:
                                    CurrentModifier = FLYING_MODIFIER;
                                    FatigueRate = FATIGUE_FLYING; // operating a jetpack is surprisingly hard
                                    break;

                                case MyCharacterMovementEnum.Falling:
                                    CurrentModifier = FLYING_MODIFIER;
                                    FatigueRate =
                                        FATIGUE_WALKING; // change nothing for the first iteration (prevents jump exploit)
                                    if (!ChangedStance)
                                        FatigueRate =
                                            FATIGUE_STANDING; // freefall is actually relaxing when you are used to it. A professional space engineer would be.
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
                                    FatigueRate =
                                        FATIGUE_RUNNING; // doing the duckwalk is more tiring than walking: try it if you don't believe me
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
                                    playerData.dead = true; // for death recovery logic
                                    break;

                            }

                            playerData.lastmovement = character.MovementState; // track delta
                        }

                        #endregion

                        #region Process Fatigue Needs
                        if (FATIGUE_ENABLED && decayEnabled)
                        {
                            // Calculate players current gravity.
                            double currentPlayerGravity =
                                Math.Round(player.Character.Physics.Gravity.Length() / 20.0f * 1.02f, 2) * 1.05f;
                            var gravityModifier = currentPlayerGravity * 1.05;
                            if (currentPlayerGravity > CurrentModifier)
                            {
                                gravityModifier = CurrentModifier;
                            }

                            bool currentHelmetStatus = player.Character.EnabledHelmet;
                            float helmetModifier = 1.0f;
                            if (currentHelmetStatus)
                            {
                                helmetModifier = 1.05f;
                            }

                            MyCharacterWeaponPositionComponent equippedItem =
                                player?.Character?.Components?.Get<MyCharacterWeaponPositionComponent>();
                            IMyEntity equippedTool = player?.Character?.EquippedTool;

                            var toolModifier = 1.0f;
                            if (equippedTool != null &&
                                (equippedTool is IMyAngleGrinder || equippedTool is IMyWelder ||
                                 equippedTool is IMyHandDrill))
                            {
                                var welder = equippedTool as IMyWelder;
                                if (welder != null)
                                {
                                    if (welder.IsShooting)
                                        toolModifier = 1.05f;
                                }

                                var grinder = equippedTool as IMyAngleGrinder;
                                if (grinder != null)
                                {
                                    if (grinder.IsShooting)
                                        toolModifier = 1.05f;
                                }

                                var drill = equippedTool as IMyHandDrill;
                                if (drill != null)
                                {
                                    if (drill.IsShooting)
                                        toolModifier = 1.05f;
                                }
                            }

                            // Debug for fatigue calculation
                            //var fatigueCalcValues = string.Format("FR: {0}, Grav: {1}, CMod: {2}, HMod: {3}, TMod: {4}, Final: {5}", FatigueRate, gravityModifier, CurrentModifier,
                            //    helmetModifier, toolModifier, (FatigueRate * Math.Max(((float)gravityModifier * CurrentModifier), CurrentModifier) * helmetModifier * FOOD_LOGIC_SKIP_TICKS / 60 * 20));
                            //MyVisualScriptLogicProvider.SendChatMessage(fatigueCalcValues);
                            
                            var fatigueChange = FatigueRate *
                            Math.Max(((float)gravityModifier * CurrentModifier), CurrentModifier) *
                            helmetModifier * toolModifier * FOOD_LOGIC_SKIP_TICKS / 60 * 20;

                            playerData.fatigue += fatigueChange;
                            playerData.fatigue = Math.Max(playerData.fatigue, MIN_NEEDS_VALUE);

                            if (FatigueBonus)
                                playerData.fatigue = Math.Min(playerData.fatigue, MAX_NEEDS_VALUE * FATIGUE_BONUS);
                            else
                                playerData.fatigue = Math.Min(playerData.fatigue, MAX_NEEDS_VALUE);
                        }
                        else
                            playerData.fatigue = MAX_NEEDS_VALUE * FATIGUE_BONUS;

                        // Assign sounds emitter and play relevant heartbeat sounds based on fatigue.
                        soundEmitter.Entity = (MyEntity) controlledEnt;
                        if (playerData.fatigue < (MAX_NEEDS_VALUE / 4) && playerData.fatigue > 0.0f &&
                            (playerData.fatigue - 1) > playerData.fatigue)
                        {
                            if (!fatigueRecoverySoundPlayed)
                            {
                                soundEmitter.PlaySound(FASTHEARTWITHBEEP_SOUND_FADEOUT);
                                fatigueRecoverySoundPlayed = true;
                            }
                        }
                        else if (playerData.fatigue <= 0.0f)
                        {
                            fatigueRecoverySoundPlayed = false;
                            //soundEmitter.VolumeMultiplier = Math.Max(1.0f, Math.Abs(playerData.fatigue / 100));
                            soundEmitter.PlaySound(FASTHEARTWITHBEEP_SOUND);

                        }

                        #endregion

                        #region Fatigue Consequences
                        // Fatigue consequences
                        string controlStringShift =
                            MyAPIGateway.Input.GetControl(MyKeys.Shift).GetGameControlEnum().String;
                        if (playerData.fatigue <= 0)
                        {
                            // at 0, start causing extra thirst
                            // at specified, force walk instead of run (unless overriding by sprinting)
                            // at specified, force crouch, and do damage flashes
                            // at specified, breathing reflex / mess with helmet, and do a bit of actual damage (just in case thirst isn't already causing it)
                            // at specified, cause heart attack
                            if (playerData.fatigue <= (0.0f * MIN_NEEDS_VALUE))
                            {
                                if (EXTRA_THIRST_FROM_FATIGUE > 0)
                                {
                                    // positive: pile on to thirst, per second
                                    playerData.thirst -= (EXTRA_THIRST_FROM_FATIGUE * FOOD_LOGIC_SKIP_TICKS / 60);

                                }
                                else
                                {
                                    // negative: multiply modifier
                                    CurrentModifier *= -EXTRA_THIRST_FROM_FATIGUE;
                                }
                            }

                            // Default Values: 0.5f * -100f = -20f
                            if (playerData.fatigue <= (FATIGUE_LEVEL_FORCEWALK * MIN_NEEDS_VALUE))
                            {
                                // Force player to walk if they were running and disable shift.
                                switch (currentMovementState)
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

                                        MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(controlStringShift,
                                            player.IdentityId, false);
                                        (controlledEnt as VRage.Game.ModAPI.Interfaces.IMyControllableEntity)
                                            .SwitchWalk();
                                        break;
                                }
                            }

                            // Default Values: 0.5f * -100f = -50f
                            if (playerData.fatigue <= (FATIGUE_LEVEL_FORCECROUCH * MIN_NEEDS_VALUE))
                            {
                                bool iscrouching = false;
                                switch (currentMovementState)
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
                                    VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce =
                                        player.Controller.ControlledEntity.Entity as
                                            VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                                    ce.Crouch(); // force player to crouch
                                }
                            }

                            // Helmet switch
                            // Default Values: 0.70f * -100f = -85f
                            if (playerData.fatigue <= (FATIGUE_LEVEL_HELMET * MIN_NEEDS_VALUE))
                            {

                                VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce =
                                    player.Controller.ControlledEntity.Entity as
                                        VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                                ce.SwitchHelmet(); // force player to switch helmet, panic reaction from trying to catch breath

                                var destroyable = controlledEnt as IMyDestroyableObject;
                                //destroyable.DoDamage(0.001f, MyStringHash.GetOrCompute("Fatigue"), true); // starting to hurt
                            }

                            // No Autohealing & cause pain. Default Values: 0.01f * -100f = -1f 
                            if (playerData.fatigue <= (FATIGUE_LEVEL_NOHEALING * MIN_NEEDS_VALUE))
                            {
                                var destroyable = controlledEnt as IMyDestroyableObject;
                                destroyable.DoDamage(1.0f, MyStringHash.GetOrCompute("Fatigue"),
                                    true); // starting to hurt

                                if (IsAutohealingOn) // fatigued? no autohealing, either.
                                {
                                    destroyable.DoDamage(1.0f, MyStringHash.GetOrCompute("Testing"), false);
                                }
                            }

                            // Default Values: 0.999f * -100f = -99.9
                            if (playerData.fatigue <= (FATIGUE_LEVEL_HEARTATTACK * MIN_NEEDS_VALUE))
                            {
                                var destroyable = controlledEnt as IMyDestroyableObject;

                                destroyable.DoDamage(1000f, MyStringHash.GetOrCompute("Fatigue"),
                                    true); // sudden, but very avoidable, heart attack ;)
                            }
                        }
                        else
                        {
                            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(controlStringShift,
                                player.IdentityId, true);
                        }

                        #endregion

                        // Process hunger need reduction.
                        if (playerData.hunger > MIN_NEEDS_VALUE && decayEnabled)
                        {
                            // We update every second - at default values this is 50 / 120 / 60 * 1 = 0.0069 hunger per second 
                            // Config value / Default Day Length / Sixty Seconds * Multiplier = Hunger per second
                            playerData.hunger -= mHungerPerMinute / 60 * CurrentModifier;
                            playerData.hunger = Math.Max(playerData.hunger, MIN_NEEDS_VALUE);
                        }

                        // Process thirst need reduction.
                        if (playerData.thirst > MIN_NEEDS_VALUE && decayEnabled)
                        {
                            // We update every second - at default values this is 100 / 120 / 60 * 1 = 0.0138 thirst per second
                            // Config value / Default Day Length / Sixty Seconds * Multiplier = Thirst per second
                            playerData.thirst -= mThirstPerMinute / 60 * CurrentModifier;
                            playerData.thirst = Math.Max(playerData.thirst, MIN_NEEDS_VALUE);
                        }

                        // Check for consumable food item and process consumption.
                        if (playerData.hunger < (MAX_NEEDS_VALUE * HUNGRY_WHEN) || ForceEating)
                            PlayerEatSomething(controlledEnt, playerData,
                                (HungerBonus ? MAX_NEEDS_VALUE * 1.25f : MAX_NEEDS_VALUE), RecycleBonus);

                        // Check for consumable drink item and process consumption.
                        if (playerData.thirst < (MAX_NEEDS_VALUE * THIRSTY_WHEN) || ForceEating)
                            PlayerDrinkSomething(controlledEnt, playerData,
                                ThirstBonus ? MAX_NEEDS_VALUE * 1.25f : MAX_NEEDS_VALUE, RecycleBonus);

                        MyEntityStat juiceStat = null;
                        var stats =
                            player.Character.Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent;
                        if (stats != null)
                        {
                            MyStringHash JuiceId = MyStringHash.GetOrCompute("Juice");
                            var statsDict = stats.TryGetStat(JuiceId, out juiceStat);
                        }

                        // Cause damage if needs are unmet
                        if (playerData.thirst <= 0)
                        {
                            var destroyable = controlledEnt as IMyDestroyableObject;
                            if (DAMAGE_SPEED_THIRST > 0)
                                destroyable.DoDamage(
                                    (IsAutohealingOn ? (DAMAGE_SPEED_THIRST + 1f) : DAMAGE_SPEED_THIRST),
                                    MyStringHash.GetOrCompute("Thirst"), true);
                            else
                                destroyable.DoDamage(
                                    ((IsAutohealingOn ? (-DAMAGE_SPEED_THIRST + 1f) : -DAMAGE_SPEED_THIRST) +
                                     DAMAGE_SPEED_THIRST * playerData.thirst), MyStringHash.GetOrCompute("Thirst"),
                                    true);
                        }

                        if (playerData.hunger <= 0)
                        {
                            var destroyable = controlledEnt as IMyDestroyableObject;
                            if (DAMAGE_SPEED_HUNGER > 0)
                                destroyable.DoDamage(
                                    (IsAutohealingOn ? (DAMAGE_SPEED_HUNGER + 1f) : DAMAGE_SPEED_HUNGER),
                                    MyStringHash.GetOrCompute("Hunger"), true);
                            else
                                destroyable.DoDamage(
                                    ((IsAutohealingOn ? (-DAMAGE_SPEED_HUNGER + 1f) : -DAMAGE_SPEED_HUNGER) +
                                     DAMAGE_SPEED_HUNGER * playerData.hunger), MyStringHash.GetOrCompute("Hunger"),
                                    true);
                        }

                        // Check if player has taken Juice.
                        if (juiceStat != null && juiceStat.Value > 0)
                        {
                            //MyVisualScriptLogicProvider.SendChatMessage("Juiced: " + juiceStat.Value.ToString());
                            playerData.juice = juiceStat.Value;
                            juicedPilots.Add(player.Character);
                            PlayerUsingDrug(controlledEnt, player, juiceStat);
                        }
                        else
                        {
                            if (juicedPilots.Contains(player.Character)) juicedPilots.Remove(player.Character);
                            playerData.juice = 0;
                        }

                        if (playerData.dead && DEATH_RECOVERY > 0.0)
                        {
                            MyInventoryBase inventory = ((MyEntity) controlledEnt).GetInventoryBase();
                            if (playerData.hunger > 0)
                                inventory.AddItems(
                                    (MyFixedPoint) ((1f / MAX_NEEDS_VALUE) * DEATH_RECOVERY * (playerData.hunger)),
                                    new MyObjectBuilder_Ore() {SubtypeName = "Organic"});
                            if (playerData.thirst > 0)
                                inventory.AddItems(
                                    (MyFixedPoint) ((1f / MAX_NEEDS_VALUE) * DEATH_RECOVERY * (playerData.thirst)),
                                    new MyObjectBuilder_Ingot() {SubtypeName = "GreyWater"});
                            playerData.dead = true;
                        }

                        //Sends PlayerData from Server.cs to Client.cs to run HUD.
                        string message = MyAPIGateway.Utilities.SerializeToXML<PlayerData>(playerData);
                        //Logging.Instance.WriteLine(("Message sent from Server.cs to Client.cs: " + message));
                        MyAPIGateway.Multiplayer.SendMessageTo(
                            1337,
                            Encoding.Unicode.GetBytes(message),
                            player.SteamUserId
                        );
                    }
                }

                //stopwatch.Complete(true);
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine(("(FoodSystem) Server UpdateNeeds Error: " + e.Message + "\n" + e.StackTrace));
            }
            
        }

        private static void PlayerEatSomething(IMyEntity controlledEntity, PlayerData playerData, float maxval_cap, float crapbonus)
        {
            MyObjectBuilder_Character character = controlledEntity.GetObjectBuilder(false) as MyObjectBuilder_Character;

            if (EATING_AND_DRINKING_REQUIRES_PRESSURISATION && character.EnvironmentOxygenLevel <= 0)
                return;

            MyInventoryBase playerInventory = ((MyEntity)controlledEntity).GetInventoryBase();
            var playerInventoryItems = playerInventory.GetItems();
            foreach (IMyInventoryItem inventoryItem in playerInventoryItems)
            {
                if (inventoryItem.Content.TypeId != typeof(MyObjectBuilder_Ingot))
                    continue;

                float result;
                if (mFoodTypes.TryGetValue(inventoryItem.Content.SubtypeName, out result))
                {
                    float canConsumeNum = 0f;
                    // if a food is registered as negative, reduce the maximum value. Useful for low nutrition meals.
                    if (result < 0)
                    {
                        result = Math.Abs(result);
                        canConsumeNum = Math.Min((((maxval_cap / 2f) - playerData.hunger) / result), (float)inventoryItem.Amount);
                    }
                    else
                    {
                        canConsumeNum = Math.Min(((maxval_cap - playerData.hunger) / result), (float)inventoryItem.Amount);
                    }

                    if (canConsumeNum > 0)
                    {
                        // Play eating sound
                        // MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("Eating", playerData.controlledEntity.PositionComp.WorldAABB.Matrix.Forward * 2.0);
                        soundEmitter.Entity = (MyEntity)controlledEntity;
                        soundEmitter.PlaySound(EATING_SOUND);

                        playerInventory.Remove(inventoryItem, (MyFixedPoint)canConsumeNum);

                        if ((playerData.hunger + result) > (MAX_NEEDS_VALUE * FOOD_BONUS))
                        {
                            playerData.hunger = MAX_NEEDS_VALUE * FOOD_BONUS;
                        }
                        else
                        {
                            playerData.hunger += result;
                        }

                        if (inventoryItem.Content.SubtypeName.Contains("Shake")) // TODO parametrize this
                            playerData.thirst += Math.Max(0f, Math.Min(result * (float)canConsumeNum, maxval_cap - playerData.thirst)); // TODO parametrize this

                        foreach (var player in mPlayers)
                        {
                            /*
                            if (playerData.steamid == player.SteamUserId && item.Content.SubtypeName.Contains("SuitEnergy"))
                            {
                                // Replenish suit energy by food amount value or up until max whichever is the lesser value.
                                var playerEnergy = player.Character.SuitEnergyLevel;
                                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(player.IdentityId, playerEnergy += Math.Max(0f, Math.Min(result * (float)canConsumeNum, 1 - playerEnergy)));
                            }
                            */
                        }

                        // Waste management line
                        if (CRAP_AMOUNT > 0.0 && canConsumeNum > 1.0)
                        {
                            playerInventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                            if (CROSS_CRAP_AMOUNT > 0.0)
                                playerInventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater_DNSK" });
                        }
                        return;
                    }
                }
            }
        }

        private static void PlayerDrinkSomething(IMyEntity controlledEntity, PlayerData playerData, float maxval_cap, float crapbonus)
        {
            MyObjectBuilder_Character character = controlledEntity.GetObjectBuilder(false) as MyObjectBuilder_Character;

            if (EATING_AND_DRINKING_REQUIRES_PRESSURISATION && character.EnvironmentOxygenLevel <= 0)
            {
                return;
            }

            MyInventoryBase inventory = ((MyEntity)controlledEntity).GetInventoryBase();
            var items = inventory.GetItems();
            foreach (IMyInventoryItem inventoryItem in items)
            {
                Drinkables_Struct result;

                if (inventoryItem.Content.TypeId != typeof(MyObjectBuilder_Ingot))
                    continue;

                if (mBeverageTypes.TryGetValue(inventoryItem.Content.SubtypeName, value: out result))
                {
                    float canConsumeNum = 0f;
                    var thirstRestoreValue = Math.Abs(result.thirstRestoreValue);
                    // if a drink is registered as negative, reduce the maximum value. Useful for low nutrition drinks.
                    if (thirstRestoreValue < 0)
                    {
                        
                        canConsumeNum = Math.Min((((maxval_cap / 2f) - thirstRestoreValue) / thirstRestoreValue), (float)inventoryItem.Amount);
                    }
                    else
                    {
                        canConsumeNum = Math.Min(((maxval_cap - thirstRestoreValue) / thirstRestoreValue), (float)inventoryItem.Amount);
                    }

                    if (canConsumeNum > 0)
                    {
                        soundEmitter.Entity = (MyEntity)controlledEntity;
                        soundEmitter.StopSound(true);
                        soundEmitter.PlaySound(DRINKING_SOUND);

                        inventory.Remove(inventoryItem, (MyFixedPoint)canConsumeNum);

                        if ((playerData.thirst + thirstRestoreValue) > (MAX_NEEDS_VALUE * DRINK_BONUS))
                        {
                            playerData.thirst = MAX_NEEDS_VALUE * DRINK_BONUS;
                        }
                        else
                        {
                            playerData.thirst += thirstRestoreValue;
                        }
                        if ((playerData.hunger + result.hungerRestoreValue) > (MAX_NEEDS_VALUE * FOOD_BONUS))
                        {
                            playerData.hunger = MAX_NEEDS_VALUE * FOOD_BONUS;
                        }
                        else
                        {
                            playerData.hunger += result.hungerRestoreValue;
                        }
                        if ((playerData.fatigue + result.fatigueRestoreValue) > (MAX_NEEDS_VALUE * FATIGUE_BONUS))
                        {
                            playerData.fatigue = MAX_NEEDS_VALUE * FATIGUE_BONUS;
                        }
                        else
                        {
                            playerData.fatigue += result.fatigueRestoreValue;
                        }
                        // waste management line
                        if (CRAP_AMOUNT > 0.0 && canConsumeNum > 1.0)
                        {
                            inventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater_DNSK" });
                            if (CROSS_CRAP_AMOUNT > 0.0)
                                inventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                        }
                        return;
                    }
                }
            }
        }

        private static void PlayerUsingDrug(IMyEntity controlledEntity, IMyPlayer player, MyEntityStat juiceStat)
        {
            juiceStat.Value--;
            if (MyVisualScriptLogicProvider.IsPlayerInCockpit(player.IdentityId))
            {
                IMyEntity controlledEnt = player.Controller.ControlledEntity.Entity;
                if (controlledEnt is IMyCockpit)
                {
                    var cockpit = controlledEnt as IMyCockpit;
                    if (!upgradedGrids.ContainsKey(cockpit.CubeGrid))
                    {
                        upgradedGrids.Add(cockpit.CubeGrid, player.IdentityId);
                        UpgradeGrid(cockpit.CubeGrid.EntityId, player.IdentityId);
                    }
                }
                //string controlType; long blockId; string blockName; long gridId; string gridName; bool isRespawnShip;
                //MyVisualScriptLogicProvider.GetPlayerControlledBlockData(out controlType, out blockId, out blockName, out gridId, out gridName, out isRespawnShip);
            }
        }

        private static void CheckUpgradedGrids()
        {
            try
            {
                List<IMyCockpit> cockpits = new List<IMyCockpit>();
                HashSet<IMyCubeGrid> gridsToRemove = new HashSet<IMyCubeGrid>();

                if (upgradedGrids == null)
                    return;

                foreach (var upgradedGrid in upgradedGrids)
                {
                    cockpits.Clear();
                    foreach (IMySlimBlock blk in (upgradedGrid.Key as MyCubeGrid).GetBlocks())
                    {
                        if (blk.FatBlock is IMyCockpit)
                        {
                            var cockpit = blk.FatBlock as IMyCockpit;
                            if (cockpit.IsUnderControl && !cockpits.Contains(cockpit) && juicedPilots.Contains(cockpit.Pilot))
                            {
                                cockpits.Add(cockpit);
                            }
                        }
                    }

                    if (cockpits.Count == 0)
                    {
                        DowngradeGrid(upgradedGrid.Key.EntityId, upgradedGrid.Value);
                        gridsToRemove.Add(upgradedGrid.Key);
                    }
                }

                foreach (var grid in gridsToRemove)
                {
                    if (upgradedGrids.ContainsKey(grid))
                        upgradedGrids.Remove(grid);
                }
                gridsToRemove.Clear();
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessage(e.ToString());
            }
        }

        private static void UpgradeGrid(long gridId, long playerId)
        {
            IMyEntity gridEntity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (gridEntity as MyCubeGrid != null)
            {
                foreach (MyCubeBlock blk in (gridEntity as MyCubeGrid).GetFatBlocks())
                {
                    if (blk == null)
                        return;

                    if (blk.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Thrust))
                    {
                        IMyThrust blockAsThruster = blk as IMyThrust;
                        if (blockAsThruster != null)
                        {
                            blockAsThruster.ThrustMultiplier = 1.0f;
                            blockAsThruster.ThrustMultiplier = (blockAsThruster.ThrustMultiplier * 2.0f);
                        }
                    }

                    if (blk.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Gyro))
                    {
                        var blockAsGryo = blk as IMyGyro;
                        if (blockAsGryo != null)
                        {
                            blockAsGryo.GyroStrengthMultiplier = 1.0f;
                            blockAsGryo.PowerConsumptionMultiplier = 1.0f;

                            blockAsGryo.GyroStrengthMultiplier = (blockAsGryo.GyroStrengthMultiplier * 2.0f);
                        }
                    }
                }
            }
            //MyVisualScriptLogicProvider.SendChatMessage("DEBUG: " + gridEntity.DisplayName + " upgraded");
            MyVisualScriptLogicProvider.ShowNotification(gridEntity.DisplayName + " upgraded by Juiced pilot.", 2000, "Green", playerId);
        }

        private static void DowngradeGrid(long gridId, long playerId)
        {
            IMyEntity gridEntity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (gridEntity as MyCubeGrid != null)
            {
                foreach (MyCubeBlock blk in (gridEntity as MyCubeGrid).GetFatBlocks())
                {
                    if (blk == null)
                        return;

                    if (blk.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Thrust))
                    {
                        IMyThrust blockAsThruster = blk as IMyThrust;
                        if (blockAsThruster != null)
                        {
                            blockAsThruster.ThrustMultiplier = 1.0f;
                            blockAsThruster.ThrustMultiplier = (blockAsThruster.ThrustMultiplier * 1.0f);
                        }
                    }

                    if (blk.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Gyro))
                    {
                        var blockAsGryo = blk as IMyGyro;
                        if (blockAsGryo != null)
                        {
                            blockAsGryo.GyroStrengthMultiplier = 1.0f;
                            blockAsGryo.PowerConsumptionMultiplier = 1.0f;

                            blockAsGryo.GyroStrengthMultiplier = (blockAsGryo.GyroStrengthMultiplier * 1.0f);
                        }
                    }
                }
            }
            //MyVisualScriptLogicProvider.SendChatMessage("DEBUG: " + gridEntity.DisplayName + " downgraded");
            MyVisualScriptLogicProvider.ShowNotification(gridEntity.DisplayName + " downgraded, no longer affected by Juiced pilot.", 2000, "Red", playerId);
        }

        /// <summary>
        /// Checks what type of entity is controlled. Returns the entity type if not passed param, otherwise returns passed param.
        /// </summary>
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

            if (e.type == NeedsApi.Event.Type.RegisterEdibleItem)
            {
                NeedsApi.RegisterEdibleItemEvent edibleItemEvent = (NeedsApi.RegisterEdibleItemEvent)e.payload;
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "EdibleItem " + edibleItemEvent.item + "(" +  edibleItemEvent.value + ") registered");
                mFoodTypes.Add(edibleItemEvent.item, edibleItemEvent.value);
            }
            else if (e.type == NeedsApi.Event.Type.RegisterDrinkableItem)
            {
                NeedsApi.RegisterDrinkableItemEvent drinkableItemEvent = (NeedsApi.RegisterDrinkableItemEvent)e.payload;
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "DrinkableItem " + drinkableItemEvent.item + "(" +  drinkableItemEvent.value + ") registered");
                mBeverageTypes.Add(drinkableItemEvent.item, new Drinkables_Struct(drinkableItemEvent.hungerRestoreValue, drinkableItemEvent.thirstRestoreValue, drinkableItemEvent.fatigueRestoreValue));
            }
            else if (e.type == NeedsApi.Event.Type.RegisterDrugItem)
            {
                NeedsApi.RegisterDrugItemEvent drugItemEvent = (NeedsApi.RegisterDrugItemEvent)e.payload;
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "DreugItem " + drugItemEvent.item + "(" +  drugItemEvent.value + ") registered");
                mDrugTypes.Add(drugItemEvent.item, drugItemEvent.value);
            }
        }

        // Saves data when Keen's save methiod is triggered.
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
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            MyAPIGateway.Entities.OnEntityRemove -= EntityRemoved;

            if (mPlayers != null)
                mPlayers.Clear();

            if (mFoodTypes != null)
                mFoodTypes.Clear();

            if (mBeverageTypes != null)
                mBeverageTypes.Clear();

            if (mPlayerDataStore != null)
                mPlayerDataStore.clear();

            if (mConfigDataStore != null)
                mConfigDataStore.clear();

            if (Logging.Instance != null)
                Logging.Instance.Close();

            if (EasyAPI != null)
                EasyAPI.Close();
        }
    }
}