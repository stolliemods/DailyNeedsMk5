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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), false, "LargeKitchen_DNSK", "SmallKitchen_DNSK", "MiniKitchen_DNSK")]
    public class Kitchen : MyGameLogicComponent
    {
        private int TranslationTimeLeftArm = 0;
        private int TranslationTimeRightArm = 0;

        private int RotationTimeLeftArm = 0;
        private int RotationTimeRightArm = 0;
        private int RotationTimeCutter = 0;

        private int AnimationLoopLeftArm = 0;
        private int AnimationLoopRightArm = 0;
        private int AnimationLoopCutter = 0;

        private bool playAnimation = true;
        private MyLight _light;
        public Dictionary<string, MyEntitySubpart> subparts;
        private static Guid ColorCheckStorageGUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");

        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private bool AUTOMATIC_BLOCK_COLOR;

        MyObjectBuilder_EntityBase objectBuilder = null;
        IMyCubeBlock foodProteinResequencer = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                var _light = new MyLight();
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                foodProteinResequencer = Entity as IMyCubeBlock;
                if (foodProteinResequencer.Storage == null)
                {
                    foodProteinResequencer.Storage = new MyModStorageComponent();
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

                if (AUTOMATIC_BLOCK_COLOR == true && !foodProteinResequencer.Storage.ContainsKey(ColorCheckStorageGUID))
                {
                    foodProteinResequencer.Storage[ColorCheckStorageGUID] = "ColorChanged";
                    foodProteinResequencer.CubeGrid.ColorBlocks(foodProteinResequencer.Min, foodProteinResequencer.Max, new Color(new Vector3(1.0f, 1.0f, 1.0f)).ColorToHSVDX11());
                }

                subparts = (foodProteinResequencer as MyEntity).Subparts;
                if (foodProteinResequencer.IsWorking)
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

                    //CreateLight((MyEntity)foodProteinResequencer, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha);
                    CreateLight((MyEntity)foodProteinResequencer, Color.White);
                    MyCubeBlockEmissive.SetEmissiveParts(foodProteinResequencer as MyEntity, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);
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

                    MoveLeftArm();
                    MoveRightArm();
                    RotateCutter();
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
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }
        
        public void CreateLight(MyEntity entity, Color color)
        {
            //These control the light settings on spawn.
            var lightPosition = entity.WorldMatrix.Translation; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
                                     //var lightOffset = 0.5f; //Light offset

            if (foodProteinResequencer.BlockDefinition.SubtypeName.Contains("Small"))
            {
                lightRange = 0.4f;
            }

            if (_light == null)//Ignore - checks if there is a light and if not makes it.
            {
                _light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                _light.Start(entity.WorldMatrix.Translation, color.ToVector4(), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                //_light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                _light.LightOn = true; //Ignore - turns light on
            }
            else
            {
                _light.Position = entity.WorldMatrix.Translation; //Updates the lights position constantly. You'll need help if you want it somewhere else.
                _light.UpdateLight(); //Ignore - tells the game to update the light.
            }
        }
       
        public void MoveLeftArm()
        {
            try
            {
                var subpart = Entity.GetSubpart("FoodProteinResequencer_LeftArm");
                //double rotation = 0.002f;
                var initialMatrix = subpart.PositionComp.LocalMatrix;
                double rotationX = 0.001f;
                double rotationY = 0.001f;

                if (AnimationLoopLeftArm == 200) AnimationLoopLeftArm = 0;
                if (AnimationLoopLeftArm == 0) TranslationTimeLeftArm = -1;
                if (AnimationLoopLeftArm == 100) TranslationTimeLeftArm = 1;

                var rotationMatrix = MatrixD.CreateRotationX(rotationX * TranslationTimeLeftArm) * MatrixD.CreateRotationY(rotationY * TranslationTimeLeftArm);
                var matrix = rotationMatrix * initialMatrix;
                subpart.PositionComp.LocalMatrix = matrix;
                AnimationLoopLeftArm++;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }

        public void MoveRightArm()
        {
            try
            {
                var subpart = Entity.GetSubpart("FoodProteinResequencer_RightArm");
                var rotation = -0.001f;
                var initialMatrix = subpart.PositionComp.LocalMatrix;

                if (AnimationLoopRightArm == 500) AnimationLoopRightArm = 0;
                if (AnimationLoopRightArm == 0) TranslationTimeRightArm = -1;
                if (AnimationLoopRightArm == 250) TranslationTimeRightArm = 1;

                var rotationMatrix = MatrixD.CreateRotationY(rotation * TranslationTimeRightArm);
                var matrix = rotationMatrix * initialMatrix;
                subpart.PositionComp.LocalMatrix = matrix;
                AnimationLoopRightArm++;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }

        public void RotateCutter()
        {
            try
            {
                var subpart = Entity.GetSubpart("FoodProteinResequencer_RightArm");
                var cutterSubpart = subpart.GetSubpart("FoodProteinResequencer_Grinder");
                var initialMatrix = cutterSubpart.PositionComp.LocalMatrix;

                double rotationX = 0.0f;
                double rotationY = 0.0f;
                double rotationZ = 0.1;

                if (AnimationLoopCutter == 200) AnimationLoopCutter = 0;
                if (AnimationLoopCutter == 0) RotationTimeCutter = -1;
                if (AnimationLoopCutter == 100) RotationTimeCutter = 1;

                var rotationMatrix = MatrixD.CreateRotationX(rotationX) * MatrixD.CreateRotationY(rotationY) * MatrixD.CreateRotationZ(rotationZ);
                var matrix = rotationMatrix * initialMatrix;
                cutterSubpart.PositionComp.LocalMatrix = matrix;
                AnimationLoopCutter++;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
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