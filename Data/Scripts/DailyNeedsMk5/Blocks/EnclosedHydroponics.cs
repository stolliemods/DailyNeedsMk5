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

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "LargeHydroponics", "SmallHydroponics", "MiniHydroponics")]
    public class EnclosedHydroponics : MyGameLogicComponent
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
        IMyCubeBlock enclosedHydroponics = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                var _light = new MyLight();
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                enclosedHydroponics = Entity as IMyCubeBlock;
                if (enclosedHydroponics.Storage == null)
                {
                    enclosedHydroponics.Storage = new MyModStorageComponent();
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

                if (AUTOMATIC_BLOCK_COLOR == true && !enclosedHydroponics.Storage.ContainsKey(ColorCheckStorageGUID))
                {
                    enclosedHydroponics.Storage[ColorCheckStorageGUID] = "ColorChanged";
                    enclosedHydroponics.CubeGrid.ColorBlocks(enclosedHydroponics.Min, enclosedHydroponics.Max, new Color(new Vector3(0.0f, 0.45f, 0.0f)).ColorToHSVDX11());
                }

                subparts = (enclosedHydroponics as MyEntity).Subparts;
                if (enclosedHydroponics.IsWorking)
                {
                    var lightColorRed = Color.YellowGreen.R;
                    var lightColorGreen = Color.YellowGreen.G;
                    var lightColorBlue = Color.YellowGreen.B;
                    var lightColorAlpha = Color.YellowGreen.A;

                    var emEmissiveness = 0.5f;
                    var emColorRed = 0.604f;
                    var emColorGreen = 0.804f;
                    var emColorBlue = 0.196f;
                    var emColorAlpha = 1.0f;

                    //CreateLight((MyEntity)enclosedHydroponics, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha);
                    CreateLight((MyEntity)enclosedHydroponics, Color.YellowGreen);
                    MyCubeBlockEmissive.SetEmissiveParts(enclosedHydroponics as MyEntity, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);
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
                    
                    enclosedHydroponics.SetEmissiveParts("Emissive", Color.Red, 1.0f);

                    if (subparts != null)
                    {
                        foreach (var subpart in subparts)
                        {
                            subpart.Value.SetEmissiveParts("Emissive", Color.Red, 1.0f);
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
                var subpart = enclosedHydroponics.GetSubpart("EnclosedHydroponics_Leaves");
                var rotation = 0.003f;
                var initialMatrix = subpart.PositionComp.LocalMatrix;
                var rotationMatrix = MatrixD.CreateRotationY(rotation);
                var matrix = rotationMatrix * initialMatrix;
                if (!subpart.Closed)
                {
                    subpart.PositionComp.LocalMatrix = matrix;
                    AnimationLoopWRS++;
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
            var lightPosition = entity.WorldMatrix.Translation; //Sets the light to the center of the block you are spawning it on, if you need it elsewhere you will need help.
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            //var lightOffset = 0.5f; //Light offset

            if (enclosedHydroponics.BlockDefinition.SubtypeName.Contains("Small"))
            {
                lightRange = 0.4f;
            }

            // Ignore - checks if there is a light and if not makes it.
            if (_light == null) 
            {
                _light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                _light.Start(lightPosition, color.ToVector4(), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                //_light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                _light.LightOn = true; //Ignore - turns light on
            }

            // Updates the lights position if it exists.
            else
            {
                _light.Position = lightPosition; //Updates the lights position constantly. You'll need help if you want it somewhere else.
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