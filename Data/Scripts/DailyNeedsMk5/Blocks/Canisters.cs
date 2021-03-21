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

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FloatingObject), true)]
    public class Canisters : MyGameLogicComponent
    {

        MyObjectBuilder_FloatingObject floatingobject = null;
        MyObjectBuilder_InventoryItem item = null;

        private MyLight _light;
        private bool initalized = false;

        private int AnimationLoop = 0;
        private int RotationTime = 0;

        public Dictionary<string, MyEntitySubpart> subparts;

        public void Init()
        {
            try
            {
                initalized = true;
                //var _light = new MyLight();
                floatingobject = (MyObjectBuilder_FloatingObject)Entity.GetObjectBuilder();
                item = floatingobject.Item;

                string itemName = item.PhysicalContent.SubtypeName;
                // MyVisualScriptLogicProvider.SendChatMessage(itemName);

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Init Error" + e, 10000, "Red");
            }
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

                if (!initalized)
                    Init();

                floatingobject = (MyObjectBuilder_FloatingObject)Entity.GetObjectBuilder();
                item = floatingobject.Item;
                
                if (item.PhysicalContent.SubtypeName.Contains("_DNSK"))
                {
                    //CreateLight(Entity as MyEntity, Color.Brown);

                    subparts = (Entity as MyEntity).Subparts;
                    RotateInnerCylinder();
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 10000, "Red");
            }
        }

        public void CreateLight(MyEntity entity, Color color)
        {
            // These control the light settings on spawn.
            var lightPosition = entity.WorldMatrix.Translation; // Sets the light to the center of the block you are spawning it on, if you need it elsehwere you will need help.
            var lightRange = 0.5f; // Range of light
            var lightIntensity = 1.0f; // Light intensity
            var lightFalloff = 0.5f; // Light falloff
            // var lightOffset = 0.5f; // Light offset

            if (_light == null) // Ignore - checks if there is a light and if not makes it.
            {
                _light = MyLights.AddLight(); // Ignore - adds the light to the games lighting system
                _light.Start(entity.WorldMatrix.Translation, color.ToVector4(), lightRange, ""); // Ignore- Determines the lights position, initial color and initial range.
                _light.Intensity = lightIntensity; // Ignore - sets light intensity from above values.
                _light.Falloff = lightFalloff; // Ignore - sets light fall off from above values.
                //_light.PointLightOffset = lightOffset; // Ignore - sets light offset from above values.
                _light.LightOn = true; // Ignore - turns light on
            }
            else
            {
                _light.Position = entity.WorldMatrix.Translation; // Updates the lights position constantly. You'll need help if you want it somewhere else.
                _light.UpdateLight(); // Ignore - tells the game to update the light.
            }
        }

        public void RotateInnerCylinder()
        {
            try
            {
                foreach (var subpart in subparts)
                {
                    if (subparts.Count == 0)
                        return;
                    
                    var initialMatrix = subpart.Value.PositionComp.LocalMatrix;

                    double rotationX = 0.0f;
                    double rotationY = 0.003f;
                    double rotationZ = 0.0;

                    if (AnimationLoop == 200) AnimationLoop = 0;
                    if (AnimationLoop == 0) RotationTime = -1;
                    if (AnimationLoop == 100) RotationTime = 1;

                    var rotationMatrix = MatrixD.CreateRotationX(rotationX) * MatrixD.CreateRotationY(rotationY) * MatrixD.CreateRotationZ(rotationZ);
                    var matrix = rotationMatrix * initialMatrix;
                    subpart.Value.PositionComp.LocalMatrix = matrix;
                    AnimationLoop++;
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Animation Error" + e, 2500, "Red");
            }
        }

        public override void OnRemovedFromScene()
        {
            if (_light != null) MyLights.RemoveLight(_light);
            base.OnRemovedFromScene();
        }
    }
}