using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
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

        //Brief calculations
        public static float RainIntensityProcessed = 0f;
        public static float RainIntensityProcessedLast = 0f;
        //public static float RainIntensityLerp = 0f;//Fade controller for the rain when entering and leaving - WIP


        public static void Save()
        {
            Debug.Log("\nWeatherMod: Writing to Config...");
            Class1._thisModConfig.WriteConfigJsonFile();
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

        public static void WeatherUpdate()
        {
            //Try to get day
            try
            {
                //Fetch the day
                int cDay = Singleton.Manager<ManTimeOfDay>.inst.GameDay;
                if (cDay > lDay && Class1.randomRainActive == true)
                {
                    Debug.Log("\nWeatherMod:");
                    Debug.Log("It's a new day!  Rolling chance of rain...");
                    Debug.Log("  Last Day: " + lDay);
                    Debug.Log("  Current Day: " + cDay);
                    lDay = cDay;
                    RandomChance = UnityEngine.Random.Range(0f, 1f);
                    Debug.Log("  Roll: " + RandomChance);
                    Debug.Log("  MaxVal: " + Class1.dailyRainChance);
                    if (RandomChance <= Class1.dailyRainChance)
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
                if (cHour > lHour && isRainyDay == true && Class1.randomRainActive == true)
                {
                    Debug.Log("\nClunk! New hour!  Rolling chance of rain...");
                    Debug.Log("  Last Hour: " + lHour);
                    Debug.Log("  Current Hour: " + cHour);
                    lHour = cHour;
                    RandomChance = UnityEngine.Random.Range(0f, 1f);
                    float cRainFreq = Mathf.Clamp(0.5f * Class1.totalRainFrequency, 0, 1); //Calculate Rain Frequency
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
                    RainMaker.RainWeight = NetworkHandler.ServerWeatherStrength;
                    RainMaker.IsRaining = true;//The intensity will control the rain now
                                               //Debug.Log("\nWeatherMod: Decoupled from local player controls!");
                    NetUpdate();
                    return;//End it here right and now.  Do not pass go.  Do not collect 2000.
                }
                else // process the rain
                {
                    //Single-Player/MP-Host handling
                    if (Class1.RainToggledOn)
                    {
                        RainIntensityProcessed = Class1.RainIntensity;
                        RainMaker.RainWeight = Class1.RainIntensity;
                        if (Class1.KeepRainActive)
                        {
                            RainMaker.IsRaining = true;
                            RainGUI.IsItRaining = true;
                        }
                        else if (isCurrentlyRaining && Class1.randomRainActive)
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
