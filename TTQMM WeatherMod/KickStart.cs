using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Nuterra.NativeOptions;
using WaterMod;

#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif

namespace TTQMM_WeatherMod
{
    public class KickStart
    {
        public const string ModName = "Weather Mod";

        //PLAYER INPUT
        //Random Rain
        public static bool randomWeatherActive = true; //Let's make the rain happen randomly, like actual weather!
        public static float dailyWeatherChance = 0.5f; //Chance of it raining on a start of a new day
        public static float totalWeatherFrequency = 1.0f; //Total Rain Frequency
        //Control the chance it rains throughout that lucky day
        public static bool UseAltDateFormat = false; //Change the date format to Y M D (requested by Exund)

        //Legacy
        /// <summary> Enable rain </summary>
        public static bool WeatherToggledOn = true;
        /// <summary> Player-Set: Keep teh rain Actives </summary>
        public static bool KeepWeatherActive = false; //Keep teh rain Actives
        /// <summary> Player-Set rain intensity </summary>
        public static float WeatherIntensity = 0.1f; //Variable for the intensity of rain

        // --- Below is experimental ---
        // will instead use cloud coverage to determine rain chance from hooks gained from Biomes mod
        /*
        public static float RainGrass = 0.4f,//Grasslands Biome Rain chance
        RainDesert = 0.05f,//Desert Biome Rain chance
        RainMount = 0.25f,//Mountains Biome Rain chance
        RainIce = 0.8f,//Ice Flats Biome Rain chance
        RainSalt = 0.125f,//Salt Flats Biome Rain chance
        RainPillars = 0.6;//Pillars Biome Rain chance

        // - Below is the calculations for experimental
        public float RainGrass = 0.4f,//Grasslands Biome Rain chance
        RainDesert = 0.05f,//Desert Biome Rain chance
        RainMount = 0.25f,//Mountains Biome Rain chance
        RainIce = 0.8f,//Ice Flats Biome Rain chance
        RainSalt = 0.125f,//Salt Flats Biome Rain chance
        RainPillars = 0.6;//Pillars Biome Rain chance
        */

        //The saved variables
        public static KeyCode hotKey;
        public static int keyInt = 47;//default to be slash
        public static void Main()
        {

            var harmony = new Harmony("aceba1.fx.weather.core");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            RainMaker.Initiate();
            WeatherGUI.Initiate();
            WeatherCommander.Init();

            try
            {
                WaterOptions.SetupOptionsAndConfig();
            }
            catch 
            {
                DebugWeather.Log("WeatherMod: NativeOptions and/or ConfigHelper failed to load.  Are they unavailable?");
            }
        }

    }
    public class WaterOptions
    {
        public static string ModName => KickStart.ModName;

        //Make a Config File to store user preferences
        public static ModConfig _thisModConfig;

        private static OptionKey GUIMenuHotKey;

        private static OptionToggle WeatherEnabled;
        private static OptionToggle RandomWeather;
        private static OptionToggle WeatherActive;
        private static OptionToggle AltDateFormat;
        private static OptionRange DailyWeatherChance;
        private static OptionRange TWeatherFrequency;
        private static OptionRange WeatherStrength;

        public static void Save()
        {
            _thisModConfig.WriteConfigJsonFile();
        }
        public static void SetupOptionsAndConfig()
        {
            ///*
            //Create and Edit Config
            DebugWeather.Log("\nWeatherMod: Config Loading");
            ModConfig thisModConfig = new ModConfig();
            DebugWeather.Log("WeatherMod: Config Loaded.");

            thisModConfig.BindConfig<KickStart>(null, "keyInt");
            KickStart.hotKey = (KeyCode)KickStart.keyInt;

            thisModConfig.BindConfig<KickStart>(null, "WeatherToggledOn");
            thisModConfig.BindConfig<KickStart>(null, "randomWeatherActive");
            thisModConfig.BindConfig<KickStart>(null, "dailyWeatherChance");
            thisModConfig.BindConfig<KickStart>(null, "totalWeatherFrequency");
            thisModConfig.BindConfig<KickStart>(null, "KeepWeatherActive");
            thisModConfig.BindConfig<KickStart>(null, "WeatherIntensity");
            thisModConfig.BindConfig<KickStart>(null, "UseAltDateFormat");
            _thisModConfig = thisModConfig;

            DebugWeather.Log("WeatherMod: Config Binder Loaded.");

            //Options Menu Support
            var WeatherProperties = ModName + " - Weather Settings";
            GUIMenuHotKey = new OptionKey("GUI Menu button", WeatherProperties, KickStart.hotKey);
            GUIMenuHotKey.onValueSaved.AddListener(() => { KickStart.keyInt = (int)(KickStart.hotKey = GUIMenuHotKey.SavedValue); WeatherCommander.Save(); });

            WeatherEnabled = new OptionToggle("Weather Effects Enabled", WeatherProperties, KickStart.WeatherToggledOn);
            WeatherEnabled.onValueSaved.AddListener(() => { KickStart.WeatherToggledOn = WeatherEnabled.SavedValue; });
            WeatherActive = new OptionToggle("Keep Weather Active", WeatherProperties, KickStart.KeepWeatherActive);
            WeatherActive.onValueSaved.AddListener(() => { KickStart.KeepWeatherActive = WeatherActive.SavedValue; });
            RandomWeather = new OptionToggle("Natural Weather Enabled", WeatherProperties, KickStart.randomWeatherActive);
            RandomWeather.onValueSaved.AddListener(() => { KickStart.randomWeatherActive = RandomWeather.SavedValue; });
            DailyWeatherChance = new OptionRange("Daily Weather Chance", WeatherProperties, KickStart.dailyWeatherChance, 0f, 1f, 0.1f);
            DailyWeatherChance.onValueSaved.AddListener(() => { KickStart.dailyWeatherChance = DailyWeatherChance.SavedValue; });
            TWeatherFrequency = new OptionRange("Weather Day Intensity", WeatherProperties, KickStart.totalWeatherFrequency, 0f, 2f, 0.2f);
            TWeatherFrequency.onValueSaved.AddListener(() => { KickStart.totalWeatherFrequency = TWeatherFrequency.SavedValue; });
            WeatherStrength = new OptionRange("Manual Weather Intensity", WeatherProperties, KickStart.WeatherIntensity, 0f, 1f, 0.1f);
            WeatherStrength.onValueSaved.AddListener(() => { KickStart.WeatherIntensity = WeatherStrength.SavedValue; });

            AltDateFormat = new OptionToggle("Y/M/D Format", WeatherProperties, KickStart.UseAltDateFormat);
            AltDateFormat.onValueSaved.AddListener(() => { KickStart.UseAltDateFormat = AltDateFormat.SavedValue; });
            NativeOptionsMod.onOptionsSaved.AddListener(() => { Save(); });
            DebugWeather.Log("WeatherMod: NativeOptions Set.\n");
        }

    }
}
