using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace Stollie.DailyNeeds

{

	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]

	public class IceRefineryControlsAdjustment : MySessionComponentBase
	{

		public override void LoadData()
		{

			MyAPIGateway.TerminalControls.CustomControlGetter += AdjustTerminalControls;

		}

		protected override void UnloadData()
		{

			MyAPIGateway.TerminalControls.CustomControlGetter -= AdjustTerminalControls;

		}

		public void AdjustTerminalControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
		{

			if (block as IMyRefinery != null)
			{
				for (int i = controls.Count - 1; i >= 0; i--)
				{
					if (controls[i].Id == "RefineRatio")
					{

						var newLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyTerminalBlock>("ECB_Separator");
						var newTitle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyTerminalBlock>("ECB_Label");
						newTitle.Label = MyStringId.GetOrCompute("Refine Ratio");
						controls.Move(i, 0);
						//controls.Insert(0, newLabel);
						//controls.Insert(0, newTitle);
						return;
					}
				}
			}
		}
	}
}