using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.Library.Utils;	// For MyGameModeEnum
using Draygo.API;
using VRageMath;
using VRageRender;
using VRage.Utils;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Rek.FoodSystem
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Client : MySessionComponentBase
	{
        #region HUDAPI Setup
        private bool mStarted = false;
	    private bool eventsReady = false;
	    private bool textHUDInit = false;

        private IMyHudNotification mNotify = null;
		private PlayerData mPlayerData = null;

	    private HudAPIv2 TextAPI;
        private HudAPIv2.HUDMessage hunger_Hud_Message = null;
	    private HudAPIv2.HUDMessage thirst_Hud_Message = null;
	    private HudAPIv2.HUDMessage fatigue_Hud_Message = null;
        private HudAPIv2.HUDMessage juiced_Hud_Message = null;

        private HudAPIv2.BillBoardHUDMessage hunger_Icon_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage hunger_Bar25_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage hunger_Bar50_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage hunger_Bar75_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage hunger_Bar100_Billboard_Message = null;

        private HudAPIv2.BillBoardHUDMessage thirst_Icon_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage thirst_Bar25_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage thirst_Bar50_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage thirst_Bar75_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage thirst_Bar100_Billboard_Message = null;

	    private HudAPIv2.BillBoardHUDMessage fatigue_Icon_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage fatigue_Bar25_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage fatigue_Bar50_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage fatigue_Bar75_Billboard_Message = null;
	    private HudAPIv2.BillBoardHUDMessage fatigue_Bar100_Billboard_Message = null;

        private HudAPIv2.BillBoardHUDMessage juiced_Icon_Billboard_Message = null;

        private StringBuilder hunger_Hud_StringBuilder = new StringBuilder();
	    private StringBuilder thirst_Hud_StringBuilder = new StringBuilder();
	    private StringBuilder fatigue_Hud_StringBuilder = new StringBuilder();
        private StringBuilder juiced_Hud_StringBuilder = new StringBuilder();
        #endregion

        #region HUD Icon String References
        private static readonly MyStringId ThirstIcon = MyStringId.GetOrCompute("ThirstIcon_LightBlue");
	    private static readonly MyStringId ThirstIconRed = MyStringId.GetOrCompute("ThirstIcon_Red");
	    private static readonly MyStringId ThirstIconGreen = MyStringId.GetOrCompute("ThirstIcon_Green");

        private static readonly MyStringId HungerIcon = MyStringId.GetOrCompute("HungerIcon_LightBlue");
	    private static readonly MyStringId HungerIconRed = MyStringId.GetOrCompute("HungerIcon_Red");
	    private static readonly MyStringId HungerIconGreen = MyStringId.GetOrCompute("HungerIcon_Green");

        private static readonly MyStringId FatigueIcon = MyStringId.GetOrCompute("FatigueIcon_LightBlue");
	    private static readonly MyStringId FatigueIconRed = MyStringId.GetOrCompute("FatigueIcon_Red");
	    private static readonly MyStringId FatigueIconGreen = MyStringId.GetOrCompute("FatigueIcon_Green");

        private static readonly MyStringId JuicedIcon = MyStringId.GetOrCompute("JuicedIcon");

        private static readonly MyStringId TwentyFivePercentHudIconFull = MyStringId.GetOrCompute("25PercentFull");
	    private static readonly MyStringId TwentyFivePercentHudIconRed = MyStringId.GetOrCompute("25PercentProgressBarRed");
	    private static readonly MyStringId FiftyPercentHudIconHudIconFull = MyStringId.GetOrCompute("50PercentFull");
	    private static readonly MyStringId FiftyPercentProgressBarAmber = MyStringId.GetOrCompute("50PercentProgressBarAmber");
	    private static readonly MyStringId SeventyFivePercentHudIconFull = MyStringId.GetOrCompute("75PercentFull");
	    private static readonly MyStringId OneHundredPercentHudIconFull = MyStringId.GetOrCompute("100PercentFull");
        #endregion

        #region HUD Default Position Values
        private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
        private static float HUNGER_ICON_POSITION_X = -0.941f;
        private static float HUNGER_ICON_POSITION_Y = 0.90f;
        private static float THIRST_ICON_POSITION_X = -0.941f;
        private static float THIRST_ICON_POSITION_Y = 0.85f;
        private static float FATIGUE_ICON_POSITION_X = -0.941f;
        private static float FATIGUE_ICON_POSITION_Y = 0.80f;
        private static float JUICED_ICON_POSITION_X = 0.93f;
        private static float JUICED_ICON_POSITION_Y = 0.70f;

        private float hungerIconPositionX = -0.941f;
        private float hungerIconPositionY = 0.90f;
        private float thirstIconPositionX = -0.941f;
        private float thirstIconPositionY = 0.85f;
        private float fatigueIconPositionX = -0.941f;
        private float fatigueIconPositionY = 0.80f;
        private float juicedIconPositionX = -0.93f;
        private float juicedIconPositionY = 0.70f;
        #endregion

        private float tick;

        private void Init()
		{
            TextAPI = new HudAPIv2();
            
            if (Utils.isDev())
			{
				MyAPIGateway.Utilities.ShowMessage("CLIENT", "INIT");
                Logging.Instance.WriteLine("CLIENT: INIT");
			}

			MyAPIGateway.Utilities.MessageEntered += onMessageEntered;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(1337, FoodUpdateMsgHandler);

            mConfigDataStore.Load();
            HUNGER_ICON_POSITION_X = mConfigDataStore.get_HUNGER_ICON_POSITION_X();
            HUNGER_ICON_POSITION_Y = mConfigDataStore.get_HUNGER_ICON_POSITION_Y();
            THIRST_ICON_POSITION_X = mConfigDataStore.get_THIRST_ICON_POSITION_X();
            THIRST_ICON_POSITION_Y = mConfigDataStore.get_THIRST_ICON_POSITION_Y();
            FATIGUE_ICON_POSITION_X = mConfigDataStore.get_FATIGUE_ICON_POSITION_X();
            FATIGUE_ICON_POSITION_Y = mConfigDataStore.get_FATIGUE_ICON_POSITION_Y();

            hungerIconPositionX = HUNGER_ICON_POSITION_X;
            hungerIconPositionY = HUNGER_ICON_POSITION_Y;
            thirstIconPositionX = THIRST_ICON_POSITION_X;
            thirstIconPositionY = THIRST_ICON_POSITION_Y;
            fatigueIconPositionX = FATIGUE_ICON_POSITION_X;
            fatigueIconPositionY = FATIGUE_ICON_POSITION_Y;
        }

	    public override void UpdateAfterSimulation()
	    {
	        if (MyAPIGateway.Session == null)
	            return;

	        try
	        {
	            var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
	                         MyAPIGateway.Multiplayer.IsServer;

	            var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

	            if (isDedicatedHost)
	                return;

	            if (!mStarted)
	            {
	                mStarted = true;
	                Init();
	            }
                
                if (tick > 600) tick = 0;
                tick++;
            }

            catch (Exception e)
	        {
	            Logging.Instance.WriteLine(("(FoodSystem) UpdateSimulation Error: " + e.Message + "\n" + e.StackTrace));
	        }
	    }

        private void ShowNotification(string text, MyFontEnum color)
		{
			if (mNotify == null)
			{
				mNotify = MyAPIGateway.Utilities.CreateNotification(text, 10000, MyFontEnum.Red);
			}
			else
			{
				mNotify.Text = text;
				mNotify.ResetAliveTime();
			}

			mNotify.Show();
		}

	    private void FoodUpdateMsgHandler(byte[] data)
	    {
	        try
	        {
	            //MyAPIGateway.Utilities.ShowMessage("Debug", "Heartbeat: " + mHud.Heartbeat);
	            mPlayerData = MyAPIGateway.Utilities.SerializeFromXML<PlayerData>(Encoding.Unicode.GetString(data));
	            //Logging.Instance.WriteLine(mPlayerData.ToString() + "Loaded to Client");

                //MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "%");

                if (TextAPI.Heartbeat == false && tick >= 600)
                {
                    MyAPIGateway.Utilities.ShowNotification("DAILY NEEDS - HUDAPI MOD MISSING - PLEASE ENABLE", 5000, "Red");
                }

                if (mPlayerData != null && TextAPI.Heartbeat)
	            {
                    if (mPlayerData.thirst <= 1 && mPlayerData.hunger <= 1)
                    {
                        ShowNotification(
                            "Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%) and Hungry (" +
                            Math.Floor(mPlayerData.hunger) + "%)", MyFontEnum.Red);
                    }
                    else if (mPlayerData.thirst <= 1)
                    {
                        ShowNotification("Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%)",
                            MyFontEnum.Red);
                    }
                    else if (mPlayerData.hunger <= 1)
                    {
                        ShowNotification("Warning: You are Hungry (" + Math.Floor(mPlayerData.hunger) + "%)",
                            MyFontEnum.Red);
                    }

                    if (!textHUDInit)
                    {
                        textHUDInit = true;

                        var twentyFivePercentAlignmentValue = 0.035f;
                        var fiftyPercentAlignmentValue = 0.073f;
                        var seventyPercentAlignmentValue = 0.1115f;
                        var oneHundredPercentAlignmentValue = 0.15f;
                        var numberTextAlignmentValue = 0.175f;

                        #region HungerInitialisation

                        //Actual Icon
                        hunger_Icon_Billboard_Message =
                            new HudAPIv2.BillBoardHUDMessage(HungerIcon, new Vector2D(hungerIconPositionX, hungerIconPositionY), Color.White);
                        hunger_Icon_Billboard_Message.Height = 1.65f;
                        hunger_Icon_Billboard_Message.Scale = 0.02d;
                        hunger_Icon_Billboard_Message.Rotation = 0f;
                        hunger_Icon_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //25 Percent Bar
                        hunger_Bar25_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(TwentyFivePercentHudIconFull,
                            new Vector2D(hungerIconPositionX + twentyFivePercentAlignmentValue, hungerIconPositionY), Color.White,
                            null, -1, 1D, 1F, 1F, 0F, true, true, BlendTypeEnum.PostPP);
                        hunger_Bar25_Billboard_Message.Height = 1.85f;
                        hunger_Bar25_Billboard_Message.Scale = 0.07f;
                        hunger_Bar25_Billboard_Message.Rotation = 0f;
                        hunger_Bar25_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //50 Percent Bar
                        hunger_Bar50_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(FiftyPercentHudIconHudIconFull,
                            new Vector2D(hungerIconPositionX + fiftyPercentAlignmentValue, hungerIconPositionY), Color.White);
                        hunger_Bar50_Billboard_Message.Height = 1.85f;
                        hunger_Bar50_Billboard_Message.Scale = 0.07f;
                        hunger_Bar50_Billboard_Message.Rotation = 0f;
                        hunger_Bar50_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //75 Percent Bar
                        hunger_Bar75_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(SeventyFivePercentHudIconFull,
                            new Vector2D(hungerIconPositionX + seventyPercentAlignmentValue, hungerIconPositionY), Color.White);
                        hunger_Bar75_Billboard_Message.Height = 1.85f;
                        hunger_Bar75_Billboard_Message.Scale = 0.07f;
                        hunger_Bar75_Billboard_Message.Rotation = 0f;
                        hunger_Bar75_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //100 Percent Bar
                        hunger_Bar100_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(OneHundredPercentHudIconFull,
                            new Vector2D(hungerIconPositionX + oneHundredPercentAlignmentValue, hungerIconPositionY), Color.White);
                        hunger_Bar100_Billboard_Message.Height = 1.85f;
                        hunger_Bar100_Billboard_Message.Scale = 0.07f;
                        hunger_Bar100_Billboard_Message.Rotation = 0f;
                        hunger_Bar100_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //Number Text Component with slight float adjustment to line up with bars
                        hunger_Hud_Message = new HudAPIv2.HUDMessage(hunger_Hud_StringBuilder, new Vector2D(hungerIconPositionX + numberTextAlignmentValue, hungerIconPositionY + 0.02),
                            Scale: 1.2d, Blend: BlendTypeEnum.PostPP);

                        #endregion

                        #region ThirstInitialisation

                        thirst_Icon_Billboard_Message =
                            new HudAPIv2.BillBoardHUDMessage(ThirstIcon, new Vector2D(thirstIconPositionX, thirstIconPositionY), Color.White);
                        thirst_Icon_Billboard_Message.Height = 1.55f;
                        thirst_Icon_Billboard_Message.Scale = 0.02d;
                        thirst_Icon_Billboard_Message.Rotation = 0f;
                        thirst_Icon_Billboard_Message.Blend = BlendTypeEnum.PostPP;
                        
                        thirst_Bar25_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(TwentyFivePercentHudIconFull,
                            new Vector2D(thirstIconPositionX + twentyFivePercentAlignmentValue, thirstIconPositionY), Color.White);
                        thirst_Bar25_Billboard_Message.Height = 1.85f;
                        thirst_Bar25_Billboard_Message.Scale = 0.07f;
                        thirst_Bar25_Billboard_Message.Rotation = 0f;
                        thirst_Bar25_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        thirst_Bar50_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(FiftyPercentHudIconHudIconFull,
                            new Vector2D(thirstIconPositionX + fiftyPercentAlignmentValue, thirstIconPositionY), Color.White);
                        thirst_Bar50_Billboard_Message.Height = 1.85f;
                        thirst_Bar50_Billboard_Message.Scale = 0.07f;
                        thirst_Bar50_Billboard_Message.Rotation = 0f;
                        thirst_Bar50_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        thirst_Bar75_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(SeventyFivePercentHudIconFull,
                            new Vector2D(thirstIconPositionX  + seventyPercentAlignmentValue, thirstIconPositionY), Color.White);
                        thirst_Bar75_Billboard_Message.Height = 1.85f;
                        thirst_Bar75_Billboard_Message.Scale = 0.07f;
                        thirst_Bar75_Billboard_Message.Rotation = 0f;
                        thirst_Bar75_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        thirst_Bar100_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(OneHundredPercentHudIconFull,
                            new Vector2D(thirstIconPositionX + oneHundredPercentAlignmentValue, thirstIconPositionY), Color.White);
                        thirst_Bar100_Billboard_Message.Height = 1.85f;
                        thirst_Bar100_Billboard_Message.Scale = 0.07f;
                        thirst_Bar100_Billboard_Message.Rotation = 0f;
                        thirst_Bar100_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        thirst_Hud_Message = new HudAPIv2.HUDMessage(thirst_Hud_StringBuilder,
                            new Vector2D(thirstIconPositionX + numberTextAlignmentValue, thirstIconPositionY + 0.02f), Scale: 1.2d, Blend: BlendTypeEnum.PostPP);
                        #endregion

                        #region FatigueInitialisation

                        fatigue_Icon_Billboard_Message =
                            new HudAPIv2.BillBoardHUDMessage(FatigueIcon, new Vector2D(fatigueIconPositionX, fatigueIconPositionY), Color.White);
                        fatigue_Icon_Billboard_Message.Height = 1.55f;
                        fatigue_Icon_Billboard_Message.Scale = 0.02d;
                        fatigue_Icon_Billboard_Message.Rotation = 0f;
                        fatigue_Icon_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        fatigue_Bar25_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(TwentyFivePercentHudIconFull,
                            new Vector2D(fatigueIconPositionX + twentyFivePercentAlignmentValue, fatigueIconPositionY), Color.White);
                        fatigue_Bar25_Billboard_Message.Height = 2.0f;
                        fatigue_Bar25_Billboard_Message.Scale = 0.07f;
                        fatigue_Bar25_Billboard_Message.Rotation = 0f;
                        fatigue_Bar25_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        fatigue_Bar50_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(FiftyPercentHudIconHudIconFull,
                            new Vector2D(fatigueIconPositionX + fiftyPercentAlignmentValue, fatigueIconPositionY), Color.White);
                        fatigue_Bar50_Billboard_Message.Height = 1.85f;
                        fatigue_Bar50_Billboard_Message.Scale = 0.07f;
                        fatigue_Bar50_Billboard_Message.Rotation = 0f;
                        fatigue_Bar50_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        fatigue_Bar75_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(SeventyFivePercentHudIconFull,
                            new Vector2D(fatigueIconPositionX + seventyPercentAlignmentValue, fatigueIconPositionY), Color.White);
                        fatigue_Bar75_Billboard_Message.Height = 1.85f;
                        fatigue_Bar75_Billboard_Message.Scale = 0.07f;
                        fatigue_Bar75_Billboard_Message.Rotation = 0f;
                        fatigue_Bar75_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        fatigue_Bar100_Billboard_Message = new HudAPIv2.BillBoardHUDMessage(OneHundredPercentHudIconFull,
                            new Vector2D(fatigueIconPositionX + oneHundredPercentAlignmentValue, fatigueIconPositionY), Color.White);
                        fatigue_Bar100_Billboard_Message.Height = 1.85f;
                        fatigue_Bar100_Billboard_Message.Scale = 0.07f;
                        fatigue_Bar100_Billboard_Message.Rotation = 0f;
                        fatigue_Bar100_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        fatigue_Hud_Message = new HudAPIv2.HUDMessage(fatigue_Hud_StringBuilder,
                            new Vector2D(fatigueIconPositionX + numberTextAlignmentValue, fatigueIconPositionY + 0.02f), Scale: 1.2d, Blend: BlendTypeEnum.PostPP);
                        #endregion

                        #region JuiceInitialisation
                        juiced_Icon_Billboard_Message =
                            new HudAPIv2.BillBoardHUDMessage(JuicedIcon, new Vector2D(juicedIconPositionX, juicedIconPositionY), Color.White);
                        juiced_Icon_Billboard_Message.Height = 1.55f;
                        juiced_Icon_Billboard_Message.Scale = 0.04d;
                        juiced_Icon_Billboard_Message.Rotation = 0f;
                        juiced_Icon_Billboard_Message.Blend = BlendTypeEnum.PostPP;

                        //Number Text Component with slight adjustments to line up with Icon
                        juiced_Hud_Message = new HudAPIv2.HUDMessage(juiced_Hud_StringBuilder, new Vector2D(juicedIconPositionX - 0.007f, juicedIconPositionY - 0.05f),
                                Scale: 1.2d, Blend: BlendTypeEnum.PostPP);
                         juiced_Hud_Message.Offset = new Vector2D(0.0f);
                            
                        #endregion
                    }

                    if (textHUDInit)
                    {
                        #region HungerUpdate
                        //Hunger Text
                        hunger_Hud_StringBuilder.Clear();
                        if (Math.Floor(mPlayerData.hunger) > 30)
                            hunger_Hud_StringBuilder.AppendFormat("<color=white>{0}", Math.Floor(mPlayerData.hunger));

                        if (Math.Floor(mPlayerData.hunger) <= 30 && Math.Floor(mPlayerData.hunger) > 5)
                            hunger_Hud_StringBuilder.AppendFormat("<color=yellow>{0}", Math.Floor(mPlayerData.hunger));

                        if (Math.Floor(mPlayerData.hunger) <= 5)
                            hunger_Hud_StringBuilder.AppendFormat("<color=red>{0}", Math.Floor(mPlayerData.hunger));

                        //Hunger Main Icon
                        if (Math.Floor(mPlayerData.hunger) > 100)
                        {
                            hunger_Icon_Billboard_Message.Material = HungerIconGreen;
                        }

                        if (Math.Floor(mPlayerData.hunger) > 0 && Math.Floor(mPlayerData.hunger) <= 100)
                        {
                            hunger_Icon_Billboard_Message.Material = HungerIcon;
                        }

                        if (Math.Floor(mPlayerData.hunger) <= 0)
                        {
                            hunger_Icon_Billboard_Message.Material = HungerIconRed;
                        }

                        //Hunger Bars
                        if (Math.Floor(mPlayerData.hunger) > 75)
                        {

                            hunger_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            hunger_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            hunger_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            hunger_Bar100_Billboard_Message.Material = OneHundredPercentHudIconFull;
                            hunger_Bar25_Billboard_Message.Visible = true;
                            hunger_Bar50_Billboard_Message.Visible = true;
                            hunger_Bar75_Billboard_Message.Visible = true;
                            hunger_Bar100_Billboard_Message.Visible = true;
                        }

                        if (Math.Floor(mPlayerData.hunger) < 76)
                        {
                            hunger_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            hunger_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            hunger_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            hunger_Bar25_Billboard_Message.Visible = true;
                            hunger_Bar50_Billboard_Message.Visible = true;
                            hunger_Bar75_Billboard_Message.Visible = true;
                            hunger_Bar100_Billboard_Message.Visible = false;

                        }

                        if (Math.Floor(mPlayerData.hunger) < 51 && Math.Floor(mPlayerData.hunger) > 25)
                        {
                            hunger_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            hunger_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            hunger_Bar25_Billboard_Message.Visible = true;
                            hunger_Bar50_Billboard_Message.Visible = true;
                            hunger_Bar75_Billboard_Message.Visible = false;
                            hunger_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.hunger) < 26 && Math.Floor(mPlayerData.hunger) > 0)
                        {
                            hunger_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconRed;
                            hunger_Bar25_Billboard_Message.Visible = true;
                            hunger_Bar50_Billboard_Message.Visible = false;
                            hunger_Bar75_Billboard_Message.Visible = false;
                            hunger_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.hunger) <= 0)
                        {
                            hunger_Bar25_Billboard_Message.Visible = false;
                            hunger_Bar50_Billboard_Message.Visible = false;
                            hunger_Bar75_Billboard_Message.Visible = false;
                            hunger_Bar100_Billboard_Message.Visible = false;
                        }

                        #endregion

                        #region ThirstUpdate

                        //Thirst Text
                        thirst_Hud_StringBuilder.Clear();
                        if (Math.Floor(mPlayerData.thirst) > 30)
                            thirst_Hud_StringBuilder.AppendFormat("<color=white>{0}", Math.Floor(mPlayerData.thirst));

                        if (Math.Floor(mPlayerData.thirst) <= 30 && Math.Floor(mPlayerData.thirst) > 5)
                            thirst_Hud_StringBuilder.AppendFormat("<color=yellow>{0}", Math.Floor(mPlayerData.thirst));

                        if (Math.Floor(mPlayerData.thirst) <= 5)
                            thirst_Hud_StringBuilder.AppendFormat("<color=red>{0}", Math.Floor(mPlayerData.thirst));

                        //Thirst Main Icon
                        if (Math.Floor(mPlayerData.thirst) > 100)
                        {
                            thirst_Icon_Billboard_Message.Material = ThirstIconGreen;
                        }

                        if (Math.Floor(mPlayerData.thirst) > 0 && Math.Floor(mPlayerData.thirst) <= 100)
                        {
                            thirst_Icon_Billboard_Message.Material = ThirstIcon;
                        }

                        if (Math.Floor(mPlayerData.thirst) <= 0)
                        {
                            thirst_Icon_Billboard_Message.Material = ThirstIconRed;
                        }

                        //Thirst Bars
                        if (Math.Floor(mPlayerData.thirst) > 75)
                        {
                            thirst_Bar100_Billboard_Message.Material = OneHundredPercentHudIconFull;
                            thirst_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            thirst_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            thirst_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            thirst_Bar25_Billboard_Message.Visible = true;
                            thirst_Bar50_Billboard_Message.Visible = true;
                            thirst_Bar75_Billboard_Message.Visible = true;
                            thirst_Bar100_Billboard_Message.Visible = true;
                        }

                        if (Math.Floor(mPlayerData.thirst) < 76)
                        {
                            thirst_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            thirst_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            thirst_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            thirst_Bar25_Billboard_Message.Visible = true;
                            thirst_Bar50_Billboard_Message.Visible = true;
                            thirst_Bar75_Billboard_Message.Visible = true;
                            thirst_Bar100_Billboard_Message.Visible = false;

                        }

                        if (Math.Floor(mPlayerData.thirst) < 51 && Math.Floor(mPlayerData.thirst) > 25)
                        {
                            thirst_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            thirst_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            thirst_Bar25_Billboard_Message.Visible = true;
                            thirst_Bar50_Billboard_Message.Visible = true;
                            thirst_Bar75_Billboard_Message.Visible = false;
                            thirst_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.thirst) < 26 && Math.Floor(mPlayerData.thirst) > 0)
                        {
                            thirst_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconRed;
                            thirst_Bar25_Billboard_Message.Visible = true;
                            thirst_Bar50_Billboard_Message.Visible = false;
                            thirst_Bar75_Billboard_Message.Visible = false;
                            thirst_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.thirst) <= 0)
                        {
                            thirst_Bar25_Billboard_Message.Visible = false;
                            thirst_Bar50_Billboard_Message.Visible = false;
                            thirst_Bar75_Billboard_Message.Visible = false;
                            thirst_Bar100_Billboard_Message.Visible = false;
                        }

                        #endregion

                        #region FatigueUpdate
                        //Fatigue
                        fatigue_Hud_StringBuilder.Clear();
                        if (Math.Floor(mPlayerData.fatigue) > 30)
                            fatigue_Hud_StringBuilder.AppendFormat("<color=white>{0}", Math.Floor(mPlayerData.fatigue));

                        if (Math.Floor(mPlayerData.fatigue) <= 30 && Math.Floor(mPlayerData.fatigue) > 5)
                            fatigue_Hud_StringBuilder.AppendFormat("<color=yellow>{0}", Math.Floor(mPlayerData.fatigue));

                        if (Math.Floor(mPlayerData.fatigue) <= 5)
                            fatigue_Hud_StringBuilder.AppendFormat("<color=red>{0}", Math.Floor(mPlayerData.fatigue));

                        //Fatigue Main Icon
                        if (Math.Floor(mPlayerData.fatigue) > 100)
                        {
                            fatigue_Icon_Billboard_Message.Material = FatigueIconGreen;
                        }

                        if (Math.Floor(mPlayerData.fatigue) > 0 && Math.Floor(mPlayerData.fatigue) <= 100)
                        {
                            fatigue_Icon_Billboard_Message.Material = FatigueIcon;
                        }

                        if (Math.Floor(mPlayerData.fatigue) <= 0)
                        {
                            fatigue_Icon_Billboard_Message.Material = FatigueIconRed;
                        }

                        //Fatigue Bars
                        if (Math.Floor(mPlayerData.fatigue) > 75)
                        {
                            fatigue_Bar100_Billboard_Message.Material = OneHundredPercentHudIconFull;
                            fatigue_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            fatigue_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            fatigue_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            fatigue_Bar25_Billboard_Message.Visible = true;
                            fatigue_Bar50_Billboard_Message.Visible = true;
                            fatigue_Bar75_Billboard_Message.Visible = true;
                            fatigue_Bar100_Billboard_Message.Visible = true;
                        }

                        if (Math.Floor(mPlayerData.fatigue) < 76)
                        {
                            fatigue_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            fatigue_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            fatigue_Bar75_Billboard_Message.Material = SeventyFivePercentHudIconFull;
                            fatigue_Bar25_Billboard_Message.Visible = true;
                            fatigue_Bar50_Billboard_Message.Visible = true;
                            fatigue_Bar75_Billboard_Message.Visible = true;
                            fatigue_Bar100_Billboard_Message.Visible = false;

                        }

                        if (Math.Floor(mPlayerData.fatigue) < 51 && Math.Floor(mPlayerData.fatigue) > 25)
                        {
                            fatigue_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconFull;
                            fatigue_Bar50_Billboard_Message.Material = FiftyPercentHudIconHudIconFull;
                            fatigue_Bar25_Billboard_Message.Visible = true;
                            fatigue_Bar50_Billboard_Message.Visible = true;
                            fatigue_Bar75_Billboard_Message.Visible = false;
                            fatigue_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.fatigue) < 26 && Math.Floor(mPlayerData.fatigue) > 0)
                        {
                            fatigue_Bar25_Billboard_Message.Material = TwentyFivePercentHudIconRed;
                            fatigue_Bar25_Billboard_Message.Visible = true;
                            fatigue_Bar50_Billboard_Message.Visible = false;
                            fatigue_Bar75_Billboard_Message.Visible = false;
                            fatigue_Bar100_Billboard_Message.Visible = false;
                        }

                        if (Math.Floor(mPlayerData.fatigue) <= 0)
                        {
                            fatigue_Bar25_Billboard_Message.Visible = false;
                            fatigue_Bar50_Billboard_Message.Visible = false;
                            fatigue_Bar75_Billboard_Message.Visible = false;
                            fatigue_Bar100_Billboard_Message.Visible = false;
                        }

                        #endregion

                        #region JuiceUpdate
                        juiced_Hud_StringBuilder.Clear();
                        if (Math.Floor(mPlayerData.juice) > 0 && Math.Floor(mPlayerData.juice) < 10)
                        {
                            juiced_Hud_StringBuilder.AppendFormat("<color=white>{0}", Math.Floor(mPlayerData.juice));
                            juiced_Hud_Message.Offset = new Vector2D(0.0f);
                        }
                        else if (Math.Floor(mPlayerData.juice) > 9)
                        {
                            juiced_Hud_StringBuilder.AppendFormat("<color=white>{0}", Math.Floor(mPlayerData.juice));
                            juiced_Hud_Message.Offset = new Vector2D(-0.005f, 0.00f);
                        }

                                if (Math.Floor(mPlayerData.juice) > 0)
                        {
                            juiced_Icon_Billboard_Message.Visible = true;
                            juiced_Icon_Billboard_Message.Material = JuicedIcon;
                        }

                        if (Math.Floor(mPlayerData.juice) <= 0)
                        {
                            juiced_Icon_Billboard_Message.Visible = false;
                        }
                        #endregion
                    }
                }
            }
	        catch (Exception e)
	        {
	            Logging.Instance.WriteLine(("(FoodSystem) FoodUpdateMsg Error: " + e.Message + "\n" + e.StackTrace));
            }
        }

	    private void onMessageEntered(string messageText, ref bool sendToOthers)
        {
            sendToOthers = true;

            if (!messageText.StartsWith("/")) return;

            var words = messageText.Trim().ToLower().Replace("/", "").Split(' ');

            if (words.Length > 0)
            {
                switch (words[0])
                {
                    case "food":
                        if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative) MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger, thirst and fatigue are disabled in creative mode.");
                        else if (mPlayerData != null)
                        {
                            if (mPlayerData.fatigue < 9000)
                            {
                                if (words.Length > 1 && words[1] == "detail") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + mPlayerData.hunger + "% Thirst: " + mPlayerData.thirst + "% Fatigue: " + mPlayerData.fatigue + "%");
                                else MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "% Fatigue: " + Math.Floor(mPlayerData.fatigue) + "%");
                            }
                            else
                            {
                                if (words.Length > 1 && words[1] == "detail") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + mPlayerData.hunger + "% Thirst: " + mPlayerData.thirst + "%");
                                else MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "%");
                            }
                        }
                        break;

                    /*
                case "debug":
                    if (words.Length > 1 && words[1] == "sun") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Sun rotation interval: " + MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
                    else if (words.Length > 1 && words[1] == "world") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "World name: " + MyAPIGateway.Session.Name);
                    break;
                     */

                    case "needs":
                        sendToOthers = false;
                        if (words.Length > 1)
                        {
                            Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, words[1]);

                            string message = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                            MyAPIGateway.Multiplayer.SendMessageToServer(
                                1338,
                                Encoding.Unicode.GetBytes(message)
                            );
                        }
                        break;
                }
            }
        }

        protected override void UnloadData() // will act up without the try-catches. yes it's ugly and slow. it only gets called on disconnect so we don't care
		{
		    try
		    {
		        try
		        {
		            TextAPI.Close();
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            mStarted = false;
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1337, FoodUpdateMsgHandler);
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
		        try
		        {
		            MyAPIGateway.Utilities.MessageEntered -= onMessageEntered;
		        }
		        catch (Exception e)
		        {
		            MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
		        }

		        ;
			try
			{
			Logging.Instance.Close();
			}
			catch (Exception e)
			{
			MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
			}

			;
		    }
		    catch (Exception e)
		    {
		        Logging.Instance.WriteLine(("(FoodSystem) Client Unload Data Error: " + e.Message + "\n" + e.StackTrace));
            };
		}
	}
}
 