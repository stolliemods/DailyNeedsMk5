using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game;
using VRage.Game.ModAPI;
using Sandbox.Game.Lights;
using VRage.Game.Entity;
using Rek.FoodSystem;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using VRageRender;

namespace Stollie.DailyNeeds //Just change this to something for yourself like 'namespace Kreeg.something'
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), //This controls the type so you could make it like 'MyObjectBuilder_LargeGatlingTurret' or 'MyObjectBuilder_LargeMissileTurret' 
    useEntityUpdate: false, entityBuilderSubTypeNames: "Manufacturing")] //This is just SubtypeId from the .SBC file.
    public class SoilTrayAnimation : MyGameLogicComponent
    {
        private IMyCubeBlock cubeBlock = null; //You have to specify the type in this variable, so like 'IMyLargeGatlingTurret',
                                              //'IMySmallGatlingGun', 'IMyLargeMissileTurret', 'IMySmallMissileLauncher', 'IMySmallGatlingGun'
                                              //and just give it a name of your choice to replace 'cubeBlock', just do a find and replace as thats the main reference point.
        MyObjectBuilder_EntityBase objectBuilder = null; //Ignore
        private MyLight _light; //Ignore - variable to assign light too.
        private static Guid ColorCheckStorageGUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");

        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private bool AUTOMATIC_BLOCK_COLOR;

        public Dictionary<string, MyEntitySubpart> subparts;
        private int RotationTime = 0;
        private int AnimationLoop = 0;
        private bool playAnimation = true;

        private Vector3D lazerOriginVector = new Vector3D();
        private Vector3D lazerDestinationVector = new Vector3D();

        private MyStringId material = MyStringId.GetOrCompute("DrugLaser");
        private Random rnd = new Random();
        private float LazerLoop = 0.0f;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var _light = new MyLight(); //Ignore - this makes a new light and associates it to the light variable above.
            cubeBlock = Entity as IMyCubeBlock; //Have to do the same as above and change this to the right type, i.e. 'IMyLargeMissileTurret',
                                               //this is how the program associates the block type to the variable you put above.
            if (cubeBlock.Storage == null)
            {
                cubeBlock.Storage = new MyModStorageComponent();
            }

            mConfigDataStore.Load();
            AUTOMATIC_BLOCK_COLOR = mConfigDataStore.get_AUTOMATIC_BLOCK_COLOR();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME; //Ignore - determines update speed and before/after physics.
        }

        /*
         * This is a method that just controls updating the game each frame - after simulation means after Physics simulations, there is a before as well but then you have to change the update period above.
         */
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

                if (AUTOMATIC_BLOCK_COLOR == true && !cubeBlock.Storage.ContainsKey(ColorCheckStorageGUID))
                {
                    cubeBlock.Storage[ColorCheckStorageGUID] = "ColorChanged";
                    cubeBlock.CubeGrid.ColorBlocks(cubeBlock.Min, cubeBlock.Max, new Color(new Vector3(0.9, 0.71f, 0.14f)).ColorToHSVDX11());
                }

                subparts = (cubeBlock as MyEntity).Subparts;

                if (cubeBlock.IsWorking) //This checks if the block is powered and above integrity etc. essentially is it working?
                {
                    var lightColorRed = 1.0f;
                    var lightColorGreen = 0.55f;
                    var lightColorBlue = 0.0f;
                    var lightColorAlpha = 1.0f;
                    var lightAdjustment = cubeBlock.WorldMatrix.Up * 0.5;

                    var emEmissiveness = 0.5f;
                    var emColorRed = 0.0f;
                    var emColorGreen = 0.5f;
                    var emColorBlue = 0.0f;
                    var emColorAlpha = 1.0f;

                    CreateLight((MyEntity) cubeBlock, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha,
                        lightAdjustment); //Ignore - This calls the method lower down to create the actual light.
                    //MyCubeBlockEmissive.SetEmissiveParts((MyEntity)cubeBlock, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);

                    if (_light != null)
                    {
                        _light.LightOn = true; //Ignore - this turns the light on if the block is working.
                        _light.UpdateLight(); //Ignore - tells the game to update the light state.
                    }

                    if (subparts != null)
                    {
                        foreach (var subpart in subparts)
                        {
                            MyCubeBlockEmissive.SetEmissiveParts(subpart.Value as MyEntity, emEmissiveness,
                                Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);
                        }
                    }

                    RotateRings();
                    RotateCentrifuge();

                    if (AnimationLoop > 150) AnimationLoop = 0;
                }
                else
                {
                    if (_light != null)
                    {
                        _light.LightOn = false; //Ignore - this turns the light off if the block is NOT working.
                        _light.UpdateLight(); //Ignore - tells the game to update the light state.
                    }

                    MyCubeBlockEmissive.SetEmissiveParts(cubeBlock as MyEntity, 1f, Color.FromNonPremultiplied(new Vector4(1.0f, 0.0f, 0.0f, 1f)), Color.White); //Changes the emissives of the main
                }
                UpdateParticleEffect();

            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessage("DrugLab UpdateSimulation Method Error!" + e.ToString());
            }
        }

        private MyParticleEffect _weldingEffect;
        private Dictionary<IMyModelDummy, MyParticleEffect> gasParticleEffects = new Dictionary<IMyModelDummy, MyParticleEffect>();
        private Dictionary<MyEntitySubpart, MyParticleEffect> lazerParticleEffects = new Dictionary<MyEntitySubpart, MyParticleEffect>();
        private HashSet<MyEntitySubpart> lazerSubparts = new HashSet<MyEntitySubpart>();

        private void UpdateParticleEffect()
        {
            if (cubeBlock.IsWorking)
            {
                MyEntitySubpart subpart_Ring1 = Entity.GetSubpart("Ring1");
                MyEntitySubpart subpart_Ring2 = Entity.GetSubpart("Ring2");
                MyEntitySubpart subpart_Ring3 = Entity.GetSubpart("Ring3");
                MyEntitySubpart subpart_Centrifuge = Entity.GetSubpart("Centrifuge");
                IMyModelDummy dummy_WelderEffect = null;

                Dictionary<string, IMyModelDummy> manufacturingDummyList = new Dictionary<string, IMyModelDummy>();
                (Entity.Model as IMyModel)?.GetDummies(manufacturingDummyList);
                
                foreach (var dummy in manufacturingDummyList)
                {
                    if (dummy.Key.Contains("LazerEndPoint"))
                        dummy_WelderEffect = dummy.Value;

                }

                MatrixD weldingEffectMatrix = dummy_WelderEffect.Matrix * Entity.WorldMatrix;
                Vector3D weldingEffectPosition = weldingEffectMatrix.Translation;

                Dictionary<string, IMyModelDummy> centrifugeDummyList = new Dictionary<string, IMyModelDummy>();
                (subpart_Centrifuge.Model as IMyModel)?.GetDummies(centrifugeDummyList);

                #region GasParticles
                // START OF GAS PARTICLES
                foreach (var dummy in centrifugeDummyList)
                {
                    if (dummy.Key.Contains("Gas"))
                    {
                        MatrixD dummyMatrix = dummy.Value.Matrix * subpart_Centrifuge.WorldMatrix;
                        Vector3D dummyPosition = dummy.Value.Matrix.Translation;
                        MyParticleEffect _gasEffect = null;
                        
                        if (!gasParticleEffects.ContainsKey(dummy.Value))
                        {
                            MyParticlesManager.TryCreateParticleEffect("Canister_Gas", ref dummyMatrix, ref dummyPosition, uint.MaxValue, out _gasEffect);
                            _gasEffect.UserScale = 0.5f;
                            //_gasEffect.Loop = true;                            
                            gasParticleEffects.Add(dummy.Value, _gasEffect);
                        }
                        else
                        {
                            gasParticleEffects.TryGetValue(dummy.Value, out _gasEffect);
                            _gasEffect.WorldMatrix = dummyMatrix;
                        }
                    }
                    
                }
                // END OF GAS PARTICLES
                #endregion

                #region LazerParticles
                if (_weldingEffect == null)
                {
                    MyParticlesManager.TryCreateParticleEffect("CanisterContactPoint", ref weldingEffectMatrix, ref weldingEffectPosition, uint.MaxValue, out _weldingEffect);
                    _weldingEffect.UserScale = 0.3f;
                    //_weldingEffect.Loop = true;
                    
                }
                else
                {
                    //_weldingEffect.Enabled = true;
                }

                foreach (var subpart in subpart_Ring1.Subparts)
                {
                    if (subpart.Key.Contains("Lazer"))
                    {
                        lazerSubparts.Add(subpart.Value);
                    }
                }

                foreach (var subpart in subpart_Ring2.Subparts)
                {
                    if (subpart.Key.Contains("Lazer"))
                    {
                        lazerSubparts.Add(subpart.Value);
                    }
                }

                foreach (var subpart in subpart_Ring3.Subparts)
                {
                    if (subpart.Key.Contains("Lazer"))
                    {
                        lazerSubparts.Add(subpart.Value);
                    }
                }

                foreach (var subpart in lazerSubparts)
                {
                    MatrixD dummyMatrix = subpart.WorldMatrix;
                    Vector3D dummyPosition = subpart.PositionComp.GetPosition();
                    MyParticleEffect _lazerParticle = null;
                    lazerSubparts.Add(subpart);

                    if (!lazerParticleEffects.ContainsKey(subpart))
                    {
                        MyParticlesManager.TryCreateParticleEffect("LazerPulse", ref dummyMatrix, ref dummyPosition, uint.MaxValue, out _lazerParticle);
                        lazerParticleEffects.Add(subpart, _lazerParticle);
                    }
                    else
                    {
                        Vector3D newVector = Vector3D.Lerp(dummyPosition, weldingEffectPosition, (float)LazerLoop / 100);
                        lazerParticleEffects.TryGetValue(subpart, out _lazerParticle);
                        DrawLine(Color.Green, dummyPosition, weldingEffectPosition, length: 1.0f, thickness: 0.01f);
                        _lazerParticle.WorldMatrix = MatrixD.CreateTranslation(newVector);
                    }
                }

                LazerLoop++;
                if (LazerLoop == 100) LazerLoop = 0;
                #endregion
            }
            else
            {
                _weldingEffect?.Stop();
                if (_weldingEffect != null)
                    MyParticlesManager.RemoveParticleEffect(_weldingEffect);
                _weldingEffect = null;

                foreach (var particle in gasParticleEffects)
                {
                    particle.Value.Stop();
                    MyParticlesManager.RemoveParticleEffect(particle.Value);
                }

                foreach (var particle in lazerParticleEffects)
                {
                    particle.Value.Stop();
                    MyParticlesManager.RemoveParticleEffect(particle.Value);
                }

                gasParticleEffects.Clear();
                lazerParticleEffects.Clear();
            }
        }

        public void RotateRings()
        {
            try
            {
                if (subparts != null)
                {
                    foreach (var subpart in subparts)
                    {
                        if (subpart.Key == "Ring0")
                        {
                            var rotation = 0.005f;
                            var initialMatrix = subpart.Value.PositionComp.LocalMatrix;
                            var rotationMatrix = MatrixD.CreateRotationX(rotation) * MatrixD.CreateRotationZ(rotation);
                            var matrix = rotationMatrix * initialMatrix;
                            subpart.Value.PositionComp.LocalMatrix = matrix;
                            AnimationLoop++;
                        }

                        if (subpart.Key == "Ring1")
                        {
                            var rotation = -0.005f;
                            var initialMatrix = subpart.Value.PositionComp.LocalMatrix;
                            var rotationMatrix = MatrixD.CreateRotationX(rotation) * MatrixD.CreateRotationZ(rotation);
                            var matrix = rotationMatrix * initialMatrix;
                            subpart.Value.PositionComp.LocalMatrix = matrix;
                            AnimationLoop++;
                        }

                        if (subpart.Key == "Ring2")
                        {
                            var rotation = 0.005f;
                            var initialMatrix = subpart.Value.PositionComp.LocalMatrix;
                            var rotationMatrix = MatrixD.CreateRotationZ(rotation) * MatrixD.CreateRotationY(rotation);
                            var matrix = rotationMatrix * initialMatrix;
                            subpart.Value.PositionComp.LocalMatrix = matrix;
                            AnimationLoop++;
                        }

                        if (subpart.Key == "Ring3")
                        {
                            var rotation = -0.005f;
                            var initialMatrix = subpart.Value.PositionComp.LocalMatrix;
                            var rotationMatrix = MatrixD.CreateRotationZ(rotation) * MatrixD.CreateRotationY(rotation);
                            var matrix = rotationMatrix * initialMatrix;
                            subpart.Value.PositionComp.LocalMatrix = matrix;
                            AnimationLoop++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Animation Error" + e, 2500, "Red");
            }
        }

        public void RotateCentrifuge()
        {
            MyEntitySubpart subpart = Entity.GetSubpart("Centrifuge");
            var rotation = 0.005f;
            var initialMatrix = subpart.PositionComp.LocalMatrix;
            var rotationMatrix = MatrixD.CreateRotationY(rotation);
            var matrix = rotationMatrix * initialMatrix;
            subpart.PositionComp.LocalMatrix = matrix;
            AnimationLoop++;
        }

        public void CreateLight(MyEntity entity, float lightColorRed, float lightColorGreen, float lightColorBlue, float lightColorAlpha, Vector3D adjustment)
        {
            //These control the light settings on spawn.
            var lightPosition = entity.WorldMatrix.Translation; //Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.
            var lightRange = 2.0f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            //var lightOffset = 1.0f; //Light offset - Upwards Direction

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
                _light.Position = entity.WorldMatrix.Translation + adjustment; //Updates the lights position constantly. You'll need help if you want it somewhere else.
                _light.UpdateLight(); //Ignore - tells the game to update the light.
            }
        }

        public void DrawLine(Color lineColor, Vector3D lineStartPoint, Vector3D lineHitVectorPoint, float length = 1.0f, float thickness = 0.05f)
        {
            MyTransparentGeometry.AddLineBillboard(material, lineColor, lineStartPoint, lineHitVectorPoint - lineStartPoint, length, thickness);
        }

        public override void Close()
        {
            if (_light != null) MyLights.RemoveLight(_light);
            if (_weldingEffect != null)
            {
                _weldingEffect.Stop();
                MyParticlesManager.RemoveParticleEffect(_weldingEffect);
            }
            foreach (var particle in gasParticleEffects)
            {
                particle.Value.Stop();
                MyParticlesManager.RemoveParticleEffect(particle.Value);
            }

            foreach (var particle in lazerParticleEffects)
            {
                particle.Value.Stop();
                MyParticlesManager.RemoveParticleEffect(particle.Value);
            }
        }
    }
}
