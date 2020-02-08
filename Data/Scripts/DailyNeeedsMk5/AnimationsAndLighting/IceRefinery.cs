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
using Rek.FoodSystem;

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "LargeIceRefinery", "SmallIceRefinery", "MiniIceRefinery")]
    public class IceRefinery : MyGameLogicComponent
    {
        private int RotationTimeWRS = 0;
        private int AnimationLoopWRS = 0;
        private bool playAnimation = true;
        private MyLight _light;
        public Dictionary<string, MyEntitySubpart> subparts;
        private static Guid ColorCheckStorageGUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");

        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private bool AUTOMATIC_BLOCK_COLOR;

        MyObjectBuilder_EntityBase objectBuilder = null;
        IMyCubeBlock waterRecyclingSystem = null;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                var _light = new MyLight();
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                waterRecyclingSystem = Entity as IMyCubeBlock;
                if (waterRecyclingSystem.Storage == null)
                {
                    waterRecyclingSystem.Storage = new MyModStorageComponent();
                }

                mConfigDataStore.Load();
                AUTOMATIC_BLOCK_COLOR = mConfigDataStore.get_AUTOMATIC_BLOCK_COLOR();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Init Error" + e, 10000, "Red");
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return objectBuilder;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MyAPIGateway.Session == null)
                    return;

                var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                             MyAPIGateway.Multiplayer.IsServer;

                var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

                if (isDedicatedHost)
                    return;

                if (AUTOMATIC_BLOCK_COLOR == true && !waterRecyclingSystem.Storage.ContainsKey(ColorCheckStorageGUID))
                {
                    waterRecyclingSystem.Storage[ColorCheckStorageGUID] = "ColorChanged";
                    waterRecyclingSystem.CubeGrid.ColorBlocks(waterRecyclingSystem.Min, waterRecyclingSystem.Max,
                        new Color(new Vector3(0.1f, 0.3f, 0.45f)).ColorToHSVDX11());
                    
                }

                subparts = (waterRecyclingSystem as MyEntity).Subparts;
                if (waterRecyclingSystem.IsWorking)
                {
                    var lightColorRed = Color.DeepSkyBlue.R;
                    var lightColorGreen = Color.DeepSkyBlue.G;
                    var lightColorBlue = Color.DeepSkyBlue.B;
                    var lightColorAlpha = Color.DeepSkyBlue.A;
                    var lightAdjustment = waterRecyclingSystem.WorldMatrix.Backward * 0.2;

                    var emEmissiveness = 0.5f;
                    var emColorRed = 0f;
                    var emColorGreen = 0.3f;
                    var emColorBlue = 0.4f;
                    var emColorAlpha = 1.0f;

                    //CreateLight((MyEntity)waterRecyclingSystem, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha);
                    CreateLight((MyEntity)waterRecyclingSystem, Color.DeepSkyBlue);
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

                    RotateTurbine();
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
                            AnimationLoopWRS++;
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
            var lightAdjustment = waterRecyclingSystem.WorldMatrix.Backward * 0.2;
            var lightPosition = entity.WorldMatrix.Translation + lightAdjustment; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.

            if (waterRecyclingSystem.BlockDefinition.SubtypeName.Contains("Small"))
            {
                lightRange = 0.4f;
                //lightFalloff = 0.5f; //Light falloff
                lightAdjustment = waterRecyclingSystem.WorldMatrix.Forward * 0.05;
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
            }
        }
    }
}
