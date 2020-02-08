using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game;
using VRage.Game.ModAPI;
using Sandbox.Game.Lights;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRageRender;
using VRageRender.Lights;
using Rek.FoodSystem;

namespace Stollie.DailyNeeds //Just change this to something for yourself like 'namespace Kreeg.something'
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), //This controls the type so you could make it like 'MyObjectBuilder_LargeGatlingTurret' or 'MyObjectBuilder_LargeMissileTurret' 
    useEntityUpdate: false, entityBuilderSubTypeNames: "LargeCompost")] //This is just SubtypeId from the .SBC file.
    public class SoilTrayAnimation : MyGameLogicComponent
    {
        private IMyCubeBlock soilTray = null; //You have to specify the type in this variable, so like 'IMyLargeGatlingTurret',
                                              //'IMySmallGatlingGun', 'IMyLargeMissileTurret', 'IMySmallMissileLauncher', 'IMySmallGatlingGun'
                                              //and just give it a name of your choice to replace 'soilTray', just do a find and replace as thats the main reference point.
        MyObjectBuilder_EntityBase objectBuilder = null; //Ignore
        private MyLight _light; //Ignore - variable to assign light too.
        private static Guid ColorCheckStorageGUID = new Guid("0A9A3146-F8D1-40FD-A664-D0B9D071B0AC");

        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private bool AUTOMATIC_BLOCK_COLOR;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var _light = new MyLight(); //Ignore - this makes a new light and associates it to the light variable above.
            base.Init(objectBuilder); //Ignore
            this.objectBuilder = objectBuilder; //Ignore
            soilTray = Entity as IMyCubeBlock; //Have to do the same as above and change this to the right type, i.e. 'IMyLargeMissileTurret',
                                               //this is how the program associates the block type to the variable you put above.
            if (soilTray.Storage == null)
            {
                soilTray.Storage = new MyModStorageComponent();
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
            if (MyAPIGateway.Session == null)
                return;

            var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                         MyAPIGateway.Multiplayer.IsServer;

            var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

            if (isDedicatedHost)
                return;

            if (AUTOMATIC_BLOCK_COLOR == true && !soilTray.Storage.ContainsKey(ColorCheckStorageGUID))
            {
                soilTray.Storage[ColorCheckStorageGUID] = "ColorChanged";
                soilTray.CubeGrid.ColorBlocks(soilTray.Min, soilTray.Max, new Color(new Vector3(1.0f, 0.2f, 0.0f)).ColorToHSVDX11());
            }

            if (soilTray.IsWorking) //This checks if the block is powered and above integrity etc. essentially is it working?
            {
                var lightColorRed = 1.0f;
                var lightColorGreen = 0.55f;
                var lightColorBlue = 0.0f;
                var lightColorAlpha = 1.0f;
                var lightAdjustment = soilTray.WorldMatrix.Up * 0.5;

                var emEmissiveness = 0.5f;
                var emColorRed = 0.2f;
                var emColorGreen = 0.01f;
                var emColorBlue = 0.0f;
                var emColorAlpha = 1.0f;

                CreateLight((MyEntity)soilTray, lightColorRed, lightColorGreen, lightColorBlue, lightColorAlpha, lightAdjustment); //Ignore - This calls the method lower down to create the actual light.
                MyCubeBlockEmissive.SetEmissiveParts((MyEntity)soilTray, emEmissiveness, Color.FromNonPremultiplied(new Vector4(emColorRed, emColorGreen, emColorBlue, emColorAlpha)), Color.White);

                if (_light != null)
                {
                    _light.LightOn = true; //Ignore - this turns the light on if the block is working.
                    _light.UpdateLight(); //Ignore - tells the game to update the light state.
                }
            }
            else
            {
                if (_light != null)
                {
                    _light.LightOn = false; //Ignore - this turns the light off if the block is NOT working.
                    _light.UpdateLight(); //Ignore - tells the game to update the light state.
                }
                MyCubeBlockEmissive.SetEmissiveParts(soilTray as MyEntity, 1f, Color.FromNonPremultiplied(new Vector4(1.0f, 0.0f, 0.0f, 1f)), Color.White); //Changes the emissives of the main
            }
        }

        /*
         * This method creates the actual light in game.
         */
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
