using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DevCommands;

namespace TTQMM_WeatherMod
{
    public static class AICommands
    {
        [DevCommand(Name = KickStartWeatherMod.modName + ".RainNow", Access = Access.Public, Users = User.Host)]
        public static ManDevCommands.CommandReturn MakeItRain()
        {
            WeatherCommander.isCurrentlyRaining = true;
            return new ManDevCommands.CommandReturn
            {
                message = "It's raining!",
                success = true,
            };
        }
    }
    public class KickStartWeatherMod : ModBase
    {
        public const string modName = "Weather Mod";

        bool isInit = false;
        bool firstInit = false;
        public override bool HasEarlyInit()
        {
            Debug.Log("WeatherMod: CALLED");
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            Debug.Log("WeatherMod: CALLED EARLYINIT");
            if (isInit)
                return;
            try
            {
                TerraTechETCUtil.ModStatusChecker.EncapsulateSafeInit(modName, KickStart.Main);
            }
            catch { }
            isInit = true;
        }
        public override void Init()
        {
            Debug.Log("WeatherMod: CALLED INIT");
            if (isInit)
                return;
            try
            {
                TerraTechETCUtil.ModStatusChecker.EncapsulateSafeInit(modName, KickStart.Main);
            }
            catch { }
            isInit = true;
        }
        public override void DeInit()
        {
            if (!isInit)
                return;
            //isInit = false;
        }
    }
}
