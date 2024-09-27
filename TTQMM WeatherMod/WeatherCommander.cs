using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;
using HarmonyLib;

namespace TTQMM_WeatherMod
{
    internal class WeatherCommander
    {
        //TIME MANAGEMENT
        public static int lDay = 0; //the last day
        public static int lHour = 0; //the last hour
        public static float RandomChance = 0f; //Rolling the dice
        public static bool isRainyDay = false; //is it supposed to rain today?
        public static bool isCurrentlyRaining = false; //is it raining right now
        public static float RainIntensityCurrent = 0f;


        //Brief calculations
        public static bool ThunderNLightning = false;
        public static float RainIntensityProcessed = 0f;
        public static float RainIntensityProcessedLast = 0f;
        //public static float RainIntensityLerp = 0f;//Fade controller for the rain when entering and leaving - WIP


        public static void Save()
        {
            Debug.Log("\nWeatherMod: Writing to Config...");
            try
            {
                WaterOptions.Save();
            }
            catch
            {
                Debug.Log("\nWeatherMod: Writing to Config failed, NativeOptions and/or ConfigHelper unavailable/broken");
            }
        }
        public static void NetUpdate()
        {
            if (RainIntensityProcessed != RainIntensityProcessedLast)
            {
                RainIntensityProcessedLast = RainIntensityProcessed;
                //toggle signal
                RainGUI.RainStateUpdate = true;
            }
        }

        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);

        static Color darkColor = new Color(0f, 0f, 0f, 1f);

        private static bool CamRain = false;
        static Gradient underWaterSkyColors = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            },

            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(darkColor, 0f),
                new GradientColorKey(darkColor, 1f),
            }
        };
        private static void MakeDark()
        {
            var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 40f;

            Color abyssColor;
            abyssColor = darkColor * RainIntensityProcessed;
            abyssColor.a = 1f;
            float depthWatch = Mathf.Clamp01((RainIntensityProcessed / 2) - (ManTimeOfDay.inst.NightTime ? 0f : 0.5f));
            Color darker = abyssColor * (1f - depthWatch);
            RenderSettings.fogDensity = 160f;
            RenderSettings.fogColor = darker;
            RenderSettings.ambientLight = darker;
            RenderSettings.ambientGroundColor = darker;
            RenderSettings.ambientIntensity = 1 - RainIntensityProcessed;
            var keys = underWaterSkyColors.colorKeys;
            keys[0].color = keys[1].color = darker;
            underWaterSkyColors.colorKeys = keys;

            sky.Day.AmbientColor = sky.Night.AmbientColor = underWaterSkyColors;
            sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underWaterSkyColors;
        }
        public static void WeatherUpdate()
        {
            //Try to get day
            try
            {
                //Fetch the day
                int cDay = Singleton.Manager<ManTimeOfDay>.inst.GameDay;
                if (cDay > lDay && KickStart.randomRainActive == true)
                {
                    Debug.Log("\nWeatherMod:");
                    Debug.Log("It's a new day!  Rolling chance of rain...");
                    Debug.Log("  Last Day: " + lDay);
                    Debug.Log("  Current Day: " + cDay);
                    lDay = cDay;
                    RandomChance = UnityEngine.Random.Range(0f, 1f);
                    Debug.Log("  Roll: " + RandomChance);
                    Debug.Log("  MaxVal: " + KickStart.dailyRainChance);
                    if (RandomChance <= KickStart.dailyRainChance)
                    {
                        isRainyDay = true;
                        Debug.Log("  Rain!");
                    }
                    else
                    {
                        isCurrentlyRaining = false;
                        isRainyDay = false;
                        Debug.Log("  No Rain.");
                    }
                }
                else if (cDay < lDay)
                {
                    Debug.Log("\nTime was changed backwards, resyncing LastDay...");
                    lDay = -1 + cDay;
                }

            }
            catch //Ooop couldn't fetch the day
            {
                //Debug.Log("\nWEATHERMOD HAS ENCOUNTERED A SERIOUS ERROR! - Could not fetch current day!");
            }

            //Now try to get hour
            try
            {        
                //Fetch the hour
                int cHour = Singleton.Manager<ManTimeOfDay>.inst.TimeOfDay;
                if (cHour > lHour && isRainyDay == true && KickStart.randomRainActive == true)
                {
                    Debug.Log("\nClunk! New hour!  Rolling chance of rain...");
                    Debug.Log("  Last Hour: " + lHour);
                    Debug.Log("  Current Hour: " + cHour);
                    lHour = cHour;
                    RandomChance = UnityEngine.Random.Range(0f, 1f);
                    float cRainFreq = Mathf.Clamp(0.5f * KickStart.totalRainFrequency, 0, 1); //Calculate Rain Frequency
                    Debug.Log("  Roll: " + RandomChance);
                    Debug.Log("  MaxVal: " + cRainFreq);
                    if (RandomChance <= cRainFreq)
                    {
                        isCurrentlyRaining = true;
                        Debug.Log("  It's pouring!\n");
                    }
                    else 
                    {
                        isCurrentlyRaining = false;
                        Debug.Log("  No rain this time.\n");
                    }
                }
                else if (cHour < lHour)
                {
                    Debug.Log("\nTime was changed backwards, resyncing LastHour...");
                    lHour = cHour;
                }
                if (RainIntensityProcessed > 0)
                {
                    if (ManNetwork.IsNetworked)
                    {
                        RainIntensityCurrent = Mathf.Lerp(RainIntensityCurrent, NetworkHandler.ServerWeatherStrength, 0.12f);
                        CamRain = true;
                        ManTimeOfDayExt.SetState("WeM", 0, MakeDark);
                    }
                    else if (RainMaker.IsRaining)
                    {
                        RainIntensityCurrent = Mathf.Lerp(RainIntensityCurrent, RainIntensityProcessed, 0.12f);
                        CamRain = true;
                        ManTimeOfDayExt.SetState("WeM", 0, MakeDark);
                    }
                    else
                    {
                        if (RainIntensityCurrent < 0.05f)
                            RainIntensityCurrent = 0;
                        else
                            RainIntensityCurrent = Mathf.Lerp(RainIntensityCurrent, 0, 0.3f);
                        if (CamRain)
                        {   // OUTSIDE the rain
                            CamRain = false;
                            ManTimeOfDayExt.RemoveState("WeM");
                        }
                    }
                }
                else
                {
                    if (RainIntensityCurrent < 0.05f)
                        RainIntensityCurrent = 0;
                    else
                        RainIntensityCurrent = Mathf.Lerp(RainIntensityCurrent, 0, 0.3f);
                    if (CamRain)
                    {   // OUTSIDE the rain
                        CamRain = false;
                        ManTimeOfDayExt.RemoveState("WeM");
                    }
                } 

            }
            catch //Ooop couldn't fetch the hour
            {
                //Debug.Log("WEATHERMOD HAS ENCOUNTERED A SERIOUS ERROR! - Could not fetch current hour!");
            }


            //Now to test teh rain
            //NETCODE HANDLING RECIEVER - Decouple controls when not host
            try
            {
                if (ManNetwork.inst.IsMultiplayer() == true && ManNetwork.IsHost == false)
                {
                    //Decoupled!
                    RainMaker.RainWeight = RainIntensityCurrent;
                    RainMaker.IsRaining = true;//The intensity will control the rain now
                                               //Debug.Log("\nWeatherMod: Decoupled from local player controls!");
                    NetUpdate();
                    return;//End it here right and now.  Do not pass go.  Do not collect 2000.
                }
                else // process the rain
                {
                    //Single-Player/MP-Host handling
                    if (KickStart.RainToggledOn)
                    {
                        RainMaker.RainWeight = RainIntensityCurrent = KickStart.RainIntensity;
                        if (KickStart.KeepRainActive)
                        {
                            RainMaker.IsRaining = true;
                            RainGUI.IsItRaining = true;
                        }
                        else if (isCurrentlyRaining && KickStart.randomRainActive)
                        {
                            RainMaker.IsRaining = true;
                            RainGUI.IsItRaining = true;
                        }
                        else
                        {
                            RainMaker.IsRaining = false;
                            RainGUI.IsItRaining = false;
                        }
                    }
                    else
                        RainMaker.IsRaining = false;
                    NetUpdate();
                }
            }
            catch { }

        }
    }
}
