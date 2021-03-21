using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.ModAPI;
using Sandbox.ModAPI;
using VRageMath;
using Sandbox.Game.Entities;
using System;
using Sandbox.Game;
using Sandbox.Definitions;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using Sandbox.Game.Lights;
using Sandbox.Game.EntityComponents;
using Stollie.DailyNeeds;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using Digi;

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "LargeIceRefinery", "SmallIceRefinery", "MiniIceRefinery")]
    public class IceRefinery : MyGameLogicComponent
    {
        public readonly IceRefineryBlockSettings Settings = new IceRefineryBlockSettings();

        public const string CONTROLS_PREFIX = "IceRefinery.";
        private static Guid SETTINGS_GUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");
        public const int SETTINGS_CHANGED_COUNTDOWN = (60 * 1) / 10; // div by 10 because it runs in update10
        public const float RATIO_MIN = -100.0f;
        public const float RATIO_MAX = 100.0f;

        private int syncCountdown;
        private MyLight _light;
        private Dictionary<string, MyEntitySubpart> subparts;
        
        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private bool AUTOMATIC_BLOCK_COLOR;

        IMyCubeBlock block = null;
        private static IMyTerminalControl ratioControl = null;
        Server Mod => Server.Instance;
        public float refineRatio
        {
            get { return Settings.refineRatio; }
            set
            {
                Settings.refineRatio = MathHelper.Clamp(value, RATIO_MIN, RATIO_MAX);
                SettingsChanged();
                block?.Components?.Get<MyResourceSinkComponent>()?.Update();
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            SetupTerminalControls<IMyRefinery>();
            block = (IMyRefinery)Entity;

            if (block.CubeGrid?.Physics == null)
                return;
            
            mConfigDataStore.Load();
            AUTOMATIC_BLOCK_COLOR = mConfigDataStore.get_AUTOMATIC_BLOCK_COLOR();

            subparts = (block as MyEntity).Subparts;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            if (AUTOMATIC_BLOCK_COLOR == true && Settings.colorChanged == false)
            {
                Settings.colorChanged = true;
                block.CubeGrid.ColorBlocks(block.Min, block.Max,
                    new Color(new Vector3(0.1f, 0.3f, 0.45f)).ColorToHSVDX11());
            }

            Settings.refineRatio = 0.0f;
            LoadSettings();
            SaveSettings();
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                SyncSettings();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                AdjustIceBlueprint();

                if (MyAPIGateway.Session == null)
                    return;

                var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                             MyAPIGateway.Multiplayer.IsServer;

                var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

                if (isDedicatedHost)
                    return;
                
                if (block.IsWorking)
                {
                    var lightColorRed = Color.DeepSkyBlue.R;
                    var lightColorGreen = Color.DeepSkyBlue.G;
                    var lightColorBlue = Color.DeepSkyBlue.B;
                    var lightColorAlpha = Color.DeepSkyBlue.A;
                    var lightAdjustment = block.WorldMatrix.Backward * 0.2;

                    var emEmissiveness = 0.5f;
                    var emColorRed = 0f;
                    var emColorGreen = 0.3f;
                    var emColorBlue = 0.4f;
                    var emColorAlpha = 1.0f;

                    RotateTurbine();

                    CreateLight((MyEntity)block, Color.DeepSkyBlue);
                    if (_light != null)
                    {
                        _light.LightOn = true;
                        _light.UpdateLight();
                    }
                    if (subparts != null)
                    {
                        foreach (var subpart in subparts)
                        {
                            MyCubeBlockEmissive.SetEmissiveParts(subpart.Value as MyEntity, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);
                        }
                    }
                }
                else
                {
                    if (_light != null)
                    {
                        _light.LightOn = false;
                        _light.UpdateLight();
                    }
                    
                    if (subparts != null)
                    {
                        foreach (var subpart in subparts)
                        {
                            if (subpart.Key == "WaterRecyclingSystem_Turbine")
                            {
                                subpart.Value.SetEmissiveParts("Emissive", Color.Red, 1.0f);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }
        void SyncSettings()
        {
            if (syncCountdown > 0 && --syncCountdown <= 0)
            {
                SaveSettings();

                Mod.CachedPacketSettings.Send(block.EntityId, Settings);
            }
        }

        public override bool IsSerialized()
        {
            // called when the game iterates components to check if they should be serialized, before they're actually serialized.
            // this does not only include saving but also streaming and blueprinting.
            // NOTE for this to work reliably the MyModStorageComponent needs to already exist in this block with at least one element.

            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return base.IsSerialized();
        }

        static void SetupTerminalControls<T>()
        {
            var mod = Server.Instance;

            if (mod.ControlsCreated)
                return;

            mod.ControlsCreated = true;

            var ratioSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(CONTROLS_PREFIX + "RefineRatio");
            ratioSlider.Title = MyStringId.GetOrCompute("Refine Ratio");
            ratioSlider.Tooltip = MyStringId.GetOrCompute("Refine Tooltip");
            ratioSlider.Visible = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<IceRefinery>();
                return logic != null;
            };
            ratioSlider.SupportsMultipleBlocks = true;
            ratioSlider.SetLimits(RATIO_MIN, RATIO_MAX);
            ratioSlider.Getter = (b) =>
            {
                var logic = b?.GameLogic?.GetAs<IceRefinery>();
                return logic == null ? 0 : logic.refineRatio;
            };
            ratioSlider.Setter = (b, v) =>
            {
                var logic = b?.GameLogic?.GetAs<IceRefinery>();
                if (logic != null)
                {
                    logic.refineRatio = (int)Math.Floor(v);
                }
            };
            ratioSlider.Writer = (block, sb) =>
            {
                var logic = block?.GameLogic?.GetAs<IceRefinery>();
                if (logic != null) sb.Append(logic.refineRatio.ToString() + "%");
            };
            MyAPIGateway.TerminalControls.AddControl<T>(ratioSlider);
        }

        void LoadSettings()
        {
            if (block.Storage == null)
                return;

            string rawData;
            if (!block.Storage.TryGetValue(SETTINGS_GUID, out rawData))
                return;

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<IceRefineryBlockSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.refineRatio = loadedSettings.refineRatio;
                    Settings.colorChanged = loadedSettings.colorChanged;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading settings!\n{e}");
            }
        }

        void SaveSettings()
        {
            if (block == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; modInstance={Server.Instance != null}");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; modInstance={Server.Instance != null}");

            if (block.Storage == null)
                block.Storage = new MyModStorageComponent();

            block.Storage.SetValue(SETTINGS_GUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        void SettingsChanged()
        {
            if (syncCountdown == 0)
                syncCountdown = SETTINGS_CHANGED_COUNTDOWN;
        }
        void AdjustIceBlueprint()
        {
            // Adjust RefineIce blueprint
            MyBlueprintDefinitionBase iceRefineryBlueprint = MyDefinitionManager.Static.GetBlueprintDefinition(
                    new MyDefinitionId(typeof(MyObjectBuilder_BlueprintDefinition), "RefineIce"));
            var outputs = iceRefineryBlueprint.Results;
            // TODO: Pull original values and store them so this isnt hard coded. 
            var refineRatioCalc = (refineRatio + 100) / 200f;
            var iceGasOutput = (MyFixedPoint)MathHelper.Lerp(0.8f, 0f, MathHelper.Clamp(refineRatioCalc, 0, 1));
            var iceDrinkOutput = (MyFixedPoint)MathHelper.Lerp(0f, 0.8f, MathHelper.Clamp(refineRatioCalc, 0, 1));

            for (var i = 0; i < outputs.Length; i++)
            {
                // This is the Ice Gas (Vanilla Ice) output
                if (i == 0)
                {
                    outputs[i].Amount = iceGasOutput;
                }
                // This is the Ice Drink ratio output
                if (i == 1)
                {
                    // TODO: Pull original values and store them so this isnt hard coded.
                    outputs[i].Amount = iceDrinkOutput;
                }
            }
        }

        public void RotateTurbine()
        {
            try
            {
                if (subparts != null)
                {
                    foreach (var subpart in subparts)
                    {
                        if (subpart.Key == "WaterRecyclingSystem_Turbine")
                        {
                            var rotation = 0.005f;
                            var initialMatrix = subpart.Value.PositionComp.LocalMatrix;
                            var rotationMatrix = MatrixD.CreateRotationY(rotation);
                            var matrix = rotationMatrix * initialMatrix;
                            subpart.Value.PositionComp.LocalMatrix = matrix;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Animation Error" + e, 2500, "Red");
            }
        }

        public void CreateLight(MyEntity entity, Color color)
        {
            //These control the light settings on spawn.
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            var lightAdjustment = block.WorldMatrix.Backward * 0.2;
            var lightPosition = entity.WorldMatrix.Translation + lightAdjustment; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.

            if (block.BlockDefinition.SubtypeName.Contains("Small"))
            {
                lightRange = 0.4f;
                //lightFalloff = 0.5f; //Light falloff
                lightAdjustment = block.WorldMatrix.Forward * 0.05;
            }

            if (_light == null)//Ignore - checks if there is a light and if not makes it.
            {
                _light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                _light.Start(lightPosition, color.ToVector4(), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                //_light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                _light.LightOn = true; //Ignore - turns light on
            }
            else
            {
                _light.Position = entity.WorldMatrix.Translation + lightAdjustment; //Updates the lights position constantly. You'll need help if you want it somewhere else.
                _light.UpdateLight(); //Ignore - tells the game to update the light.
            }
        }

        public override void Close()
        {
            if (_light != null)
            {
                MyLights.RemoveLight(_light);
                _light = null;
                block = null;
            }
        }
    }
}
