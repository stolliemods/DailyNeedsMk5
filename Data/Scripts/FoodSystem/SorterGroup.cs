using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using System;
using Sandbox.Game;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Collections;

namespace Stollie.DailyNeeds
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false)]
    public class SorterGroupTwo : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase objectBuilder = null;
        IMyConveyorSorter sorter = null;

        private bool filterset = false;
        private int tick = 0;

        private List<Sandbox.ModAPI.Ingame.MyInventoryItemFilter> dailyNeedsItems = new List<Sandbox.ModAPI.Ingame.MyInventoryItemFilter>();
        private ListReader<MyDefinitionBase> definitionsList = new ListReader<MyDefinitionBase>();

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                this.objectBuilder = objectBuilder;
                sorter = Entity as IMyConveyorSorter;

                definitionsList = MyDefinitionManager.Static.GetInventoryItemDefinitions();
                foreach (var definition in definitionsList)
                {
                    if (definition.Id.SubtypeName != null && definition.Id.SubtypeName.Contains("_DNSK"))
                    {
                        //Logging.Instance.WriteLine(definition.Id.SubtypeName);
                        dailyNeedsItems.Add(definition.Id);
                    }
                }

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Init Error" + e, 10000, "Red");
            }
        }

        public override void UpdateBeforeSimulation()
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

                tick++;

                if (sorter.DisplayNameText.Contains("_DNSK") && filterset == false)
                {
                    filterset = true;
                    ApplyFilter();
                    tick = 0;
                }

                if (tick > 1000 && filterset == true)
                {
                    ApplyFilter();
                    tick = 0;
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.ShowNotificationToAll("Update Error" + e, 2500, "Red");
            }
        }

        public void ApplyFilter()
        {
            sorter.SetFilter(Sandbox.ModAPI.Ingame.MyConveyorSorterMode.Whitelist, dailyNeedsItems);
            sorter.DrainAll = true;
            TriggerTerminalRefresh((MyCubeBlock)sorter);
        }

        public static void TriggerTerminalRefresh(MyCubeBlock block)
        {
            MyOwnershipShareModeEnum shareMode;
            long ownerId;
            if (block.IDModule != null)
            {
                ownerId = block.IDModule.Owner;
                shareMode = block.IDModule.ShareMode;
            }
            else
            {
                return;
            }
            block.ChangeOwner(ownerId, shareMode == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            block.ChangeOwner(ownerId, shareMode);
        }

    }
}