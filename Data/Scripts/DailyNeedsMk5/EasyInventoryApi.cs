using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace Draygo.API
{
	public class EasyInventoryAPI
	{
		public static readonly long MODID = 646796262;
		static EasyInventoryAPI instance;
		private Action m_onRegisteredAction;
		private bool registered = false;
		private Action<string, Func<MyItemType, bool>> RegisterMethod;

		public EasyInventoryAPI(Action onRegisteredAction = null)
		{
			if (instance != null)
			{

				return;
			}
			instance = this;
			m_onRegisteredAction += onRegisteredAction;
			MyAPIGateway.Utilities.RegisterMessageHandler(MODID, RegisterComponents);
		}


		public void RegisterComponents(object obj)
		{
			if (registered)
				return;
			if (obj is MyTuple<Action<string, Func<MyItemType, bool>>>)
            {
				var Methods = (MyTuple<Action<string, Func<MyItemType, bool>>>)obj;
				RegisterMethod = Methods.Item1;
				registered = true;
            }
			if(m_onRegisteredAction != null)
			{
				m_onRegisteredAction();
			}
		}

		public void RegisterEasyFilter(string Name, Func<MyItemType, bool> Filter)
		{
			if(!registered)
			{
				m_onRegisteredAction += () =>
				{
					RegisterEasyFilter(Name, Filter);
                };
			}
			RegisterMethod(Name, Filter);
		}

        public void Close()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MODID, RegisterComponents);
        }
    }
}
