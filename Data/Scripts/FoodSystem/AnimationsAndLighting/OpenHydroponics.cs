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
using VRage.Game.Entity;
using Sandbox.Game.Lights;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Weapons;
using VRage.Game.ModAPI.Ingame;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyEntity = VRage.ModAPI.IMyEntity;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Collections;
using MyEngineerToolBaseDefinition = Sandbox.Definitions.MyEngineerToolBaseDefinition;

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenGenerator), false, "LargeOpenHydroponics")]
    public class OpenHydroAnimation : MyGameLogicComponent
    {
        private int RotationTimeWRS = 0;
        private int AnimationLoopWRS = 0;
        private bool playAnimation = true;
        private MyLight _light;
        public Dictionary<string, MyEntitySubpart> subparts;
        private static Guid ColorCheckStorageGUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");
        private bool init = false;

        MyObjectBuilder_EntityBase objectBuilder = null;
        IMyCubeBlock openHydroponics = null;
        private int tick = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                openHydroponics = Entity as IMyCubeBlock;
                if (openHydroponics.Storage == null)
                {
                    openHydroponics.Storage = new MyModStorageComponent();
                }
                
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
            if (MyAPIGateway.Session == null)
                return;

            var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                         MyAPIGateway.Multiplayer.IsServer;

            var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

            if (isDedicatedHost)
                return;

           try
            {
                if (!openHydroponics.Storage.ContainsKey(ColorCheckStorageGUID))
                {
                    openHydroponics.Storage[ColorCheckStorageGUID] = "ColorChanged";
                    openHydroponics.CubeGrid.ColorBlocks(openHydroponics.Min, openHydroponics.Max, new Color(new Vector3(0.6f, 0.3f, 0.0f)).ColorToHSVDX11());
                }

                subparts = (openHydroponics as MyEntity).Subparts;
                if (openHydroponics.IsWorking)
                {
                    var lightColorRed = Color.YellowGreen.R;
                    var lightColorGreen = Color.YellowGreen.G;
                    var lightColorBlue = Color.YellowGreen.B;
                    var lightColorAlpha = Color.YellowGreen.A;

                    var emEmissiveness = 0.5f;
                    var emColorRed = 0.6f;
                    var emColorGreen = 0.81f;
                    var emColorBlue = 0.2f;
                    var emColorAlpha = 1.0f;

                    //CreateLight((MyEntity)openHydroponics, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha);
                    CreateLight((MyEntity)openHydroponics, Color.YellowGreen);
                    MyCubeBlockEmissive.SetEmissiveParts(openHydroponics as MyEntity, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);

                    if (_light != null)
                    {
                        _light.LightOn = true;
                        _light.UpdateLight();
                    }
                    
                    if (subparts != null)
                    {
                        foreach (var subpart in subparts)
                        {
                            MyCubeBlockEmissive.SetEmissiveParts(subpart.Value as MyEntity, 1f, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);
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
                            subpart.Value.SetEmissiveParts("Emissive", Color.Red, 1.0f);
                        }
                    }
                }

                tick = 0;
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
                var subpart = openHydroponics.GetSubpart("OpenHydroponics_Rotor");
                if (subpart == null)
                    return;

                var rotation = 0.003f;
                var initialMatrix = subpart.PositionComp.LocalMatrix;
                var rotationMatrix = MatrixD.CreateRotationZ(rotation);
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

        public void CreateLight(MyEntity entity, float lightColorRed, float lightColorGreen, float lightColorBlue, float lightColorAlpha)
        {
            //These control the light settings on spawn.
            var lightPosition = entity.WorldMatrix.Translation; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            //var lightOffset = 0.5f; //Light offset
            
            if (_light == null)//Ignore - checks if there is a light and if not makes it.
            {
                _light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                _light.Start(entity.WorldMatrix.Translation, new Vector4(lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                //_light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                _light.LightOn = true; //Ignore - turns light on
            }
            else
            {
                _light.Position = entity.WorldMatrix.Translation + entity.WorldMatrix.Up * 0.2; //Updates the lights position constantly. You'll need help if you want it somewhere else.
                _light.UpdateLight(); //Ignore - tells the game to update the light.
            }
        }

        public void CreateLight(MyEntity entity, Color color)
        {
            //These control the light settings on spawn.
            var lightPosition = entity.WorldMatrix.Translation; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            var lightOffset = 0.5f; //Light offset

            if (_light == null)//Ignore - checks if there is a light and if not makes it.
            {
                _light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                _light.Start(entity.WorldMatrix.Translation, color.ToVector4(), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                _light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                _light.LightOn = true; //Ignore - turns light on
            }
            else
            {
                _light.Position = entity.WorldMatrix.Translation; //Updates the lights position constantly. You'll need help if you want it somewhere else.
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