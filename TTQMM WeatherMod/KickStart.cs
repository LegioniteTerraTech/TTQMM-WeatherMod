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
        public static bool randomRainActive = true; //Let's make the rain happen randomly, like actual rain!
        public static float dailyRainChance = 0.5f; //Chance of it raining on a start of a new day
        public static float totalRainFrequency = 1.0f; //Total Rain Frequency
        //Control the chance it rains throughout that lucky day
        public static bool UseAltDateFormat = false; //Change the date format to Y M D (requested by Exund)

        //Legacy
        public static bool RainToggledOn = true; //Keep teh rain Actives
        public static bool KeepRainActive = false; //Keep teh rain Actives
        public static float RainIntensity = 0.1f; //Variable for the intensity of rain

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
            RainGUI.Initiate();

            try
            {
                WaterOptions.SetupOptionsAndConfig();
            }
            catch
            {
                Debug.Log("WeatherMod: NativeOptions and/or ConfigHelper failed to load.  Are they unavailable?");
            }
        }

    }
    public class WaterOptions
    {
        public static string ModName => KickStart.ModName;

        //Make a Config File to store user preferences
        public static ModConfig _thisModConfig;

        public static OptionKey GUIMenuHotKey;

        public static OptionToggle RainEnabled;
        public static OptionToggle RandomRain;
        public static OptionToggle RainActive;
        public static OptionToggle AltDateFormat;
        public static OptionRange DailyRainChance;
        public static OptionRange TRainFrequency;
        public static OptionRange RainStrength;

        public static void Save()
        {
            _thisModConfig.WriteConfigJsonFile();
        }
        public static void SetupOptionsAndConfig()
        {
            ///*
            //Create and Edit Config
            Debug.Log("\nWeatherMod: Config Loading");
            ModConfig thisModConfig = new ModConfig();
            Debug.Log("WeatherMod: Config Loaded.");

            thisModConfig.BindConfig<KickStart>(null, "keyInt");
            KickStart.hotKey = (KeyCode)KickStart.keyInt;

            thisModConfig.BindConfig<KickStart>(null, "RainToggledOn");
            thisModConfig.BindConfig<KickStart>(null, "randomRainActive");
            thisModConfig.BindConfig<KickStart>(null, "dailyRainChance");
            thisModConfig.BindConfig<KickStart>(null, "totalRainFrequency");
            thisModConfig.BindConfig<KickStart>(null, "KeepRainActive");
            thisModConfig.BindConfig<KickStart>(null, "RainIntensity");
            thisModConfig.BindConfig<KickStart>(null, "UseAltDateFormat");
            _thisModConfig = thisModConfig;

            Debug.Log("WeatherMod: Config Binder Loaded.");

            //Options Menu Support
            var WeatherProperties = ModName + " - Weather Settings";
            GUIMenuHotKey = new OptionKey("GUI Menu button", WeatherProperties, KickStart.hotKey);
            GUIMenuHotKey.onValueSaved.AddListener(() => { KickStart.keyInt = (int)(KickStart.hotKey = GUIMenuHotKey.SavedValue); WeatherCommander.Save(); });

            RainEnabled = new OptionToggle("Raining Enabled", WeatherProperties, KickStart.RainToggledOn);
            RainEnabled.onValueSaved.AddListener(() => { KickStart.RainToggledOn = RainEnabled.SavedValue; });
            RainActive = new OptionToggle("Keep Raining", WeatherProperties, KickStart.KeepRainActive);
            RainActive.onValueSaved.AddListener(() => { KickStart.KeepRainActive = RainActive.SavedValue; });
            RandomRain = new OptionToggle("Natural Rain Enabled", WeatherProperties, KickStart.randomRainActive);
            RandomRain.onValueSaved.AddListener(() => { KickStart.randomRainActive = RandomRain.SavedValue; });
            DailyRainChance = new OptionRange("Daily Rain Chance", WeatherProperties, KickStart.dailyRainChance, 0f, 1f, 0.1f);
            DailyRainChance.onValueSaved.AddListener(() => { KickStart.dailyRainChance = DailyRainChance.SavedValue; });
            TRainFrequency = new OptionRange("Natural Rain Chance", WeatherProperties, KickStart.totalRainFrequency, 0f, 2f, 0.2f);
            TRainFrequency.onValueSaved.AddListener(() => { KickStart.totalRainFrequency = TRainFrequency.SavedValue; });
            RainStrength = new OptionRange("Rain Intensity", WeatherProperties, KickStart.RainIntensity, 0f, 1f, 0.1f);
            RainStrength.onValueSaved.AddListener(() => { KickStart.RainIntensity = RainStrength.SavedValue; });

            AltDateFormat = new OptionToggle("Y/M/D Format", WeatherProperties, KickStart.UseAltDateFormat);
            AltDateFormat.onValueSaved.AddListener(() => { KickStart.UseAltDateFormat = AltDateFormat.SavedValue; });
            NativeOptionsMod.onOptionsSaved.AddListener(() => { Save(); });
            Debug.Log("WeatherMod: NativeOptions Set.\n");
        }

    }
    public class RainGUI : MonoBehaviour
    {
        static private RainGUI inst = null;
        static private bool ShowGUI = false;
        static private Rect Window = new Rect(100, 0, 220, 240);
        static public GameObject GUIDisp;
        public static int rainDayDisplay = 0;
        public static int rainChanceDisplay = 0;
        public static int rainIntensityDisplay = 0;
        public static bool IsItRaining;
        public static bool lastRainState;
        public static bool RainStateUpdate;

        //Time-display handling
        public static int month = 0;
        public static int year = 0;

        //private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        static private void GUIWindow(int ID)
        {
            //Toggle if the rain is running
            KickStart.RainToggledOn = GUI.Toggle(new Rect(20, 40, 100, 20), KickStart.RainToggledOn, "Rain Enabled?");

            KickStart.randomRainActive = GUI.Toggle(new Rect(20, 60, 100, 20), KickStart.randomRainActive, "Natural Rain?");

            KickStart.KeepRainActive = GUI.Toggle(new Rect(20, 120, 100, 20), KickStart.KeepRainActive, "Make it Rain!");

            KickStart.RainIntensity = GUI.HorizontalSlider(new Rect(20, 100, 160, 15), KickStart.RainIntensity, 0f, 1f);
            rainIntensityDisplay = (int)(KickStart.RainIntensity * 100);
            GUI.Label(new Rect(20, 80, 120, 20), "Rain Intensity: " + rainIntensityDisplay.ToString() + "%");

            RainMaker.VisualizeRainSpawnerCenter.enabled = GUI.Toggle(new Rect(20, 140, 100, 20), RainMaker.VisualizeRainSpawnerCenter.enabled, "ShowRainCenter");

            //Day/hour tracker - very rough but I'm not going overkill to be precise
            month = (int)Mathf.Repeat((Singleton.Manager<ManTimeOfDay>.inst.GameDay / 32), 13);
            year = (int)(Singleton.Manager<ManTimeOfDay>.inst.GameDay / 365) + 2021;
            if (KickStart.UseAltDateFormat)
                GUI.Label(new Rect(20, 170, 160, 30), "Year/Month/Day: " + year + "/" + month  + "/" + Mathf.Repeat((int)Singleton.Manager<ManTimeOfDay>.inst.GameDay, 32));
            else
                GUI.Label(new Rect(20, 170, 160, 30), "Month/Day/Year: " + month + "/" + Mathf.Repeat((int)Singleton.Manager<ManTimeOfDay>.inst.GameDay, 32) + "/" + year);
            GUI.Label(new Rect(20, 190, 100, 30), "Hour: " + Singleton.Manager<ManTimeOfDay>.inst.TimeOfDay);
            string displayForecast;
            if (WeatherCommander.isRainyDay == true)
                displayForecast = "Rainy";
            else
                displayForecast = "Sunny";
            GUI.Label(new Rect(20, 210, 100, 30), "Forecast: " + displayForecast);
            GUI.DragWindow();


            //GUI.Label(new Rect(0, 140, 100, 20), "Fog Weight");
            //GUI.Button(new Rect(0, 160, 100, 20), "Unavailable");
            //RainMaker.FogWeight = GUI.HorizontalSlider(new Rect(0, 180, 100, 15), RainMaker.FogWeight, 0f, 1f);

        }

        private void Update()
        {
            if (Input.GetKeyDown(KickStart.hotKey))
            {
                ShowGUI = !ShowGUI;
                GUIDisp.SetActive(ShowGUI);
                if (!ShowGUI)
                {
                    //Debug.Log("\nWeatherMod: Writing to Config...");
                    try
                    {
                        WaterOptions.Save();
                    }
                    catch
                    {
                        Debug.Log("\nWeatherMod: Writing to Config failed, NativeOptions and/or ConfigHelper unavailable/broken");
                    }
                }
            }

            if (KickStart.RainToggledOn == true)
            {
                WeatherCommander.WeatherUpdate();
            }
            else 
                RainMaker.IsRaining = false;

            //Now handle network if possible
            try
            {
                if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost == true)
                {
                    if (IsItRaining != lastRainState || RainStateUpdate == true)////NetworkHandler.ServerWeatherStrength != Class1.RainIntensity
                    {
                        lastRainState = IsItRaining;
                        RainStateUpdate = false;

                        //If it's not raining then keep intensity zero!
                        if (!IsItRaining)
                        {
                            NetworkHandler.ServerWeatherStrength = 0f;
                            //Debug.Log("Turning off rain with " + Class1.RainIntensity);
                        }
                        else
                        {
                            NetworkHandler.ServerWeatherStrength = KickStart.RainIntensity;
                            //Debug.Log("Turning on rain with " + Class1.RainIntensity);
                        }
                        if (RainMaker.IsRaining == true)
                            //Debug.Log("It's confirmed raining");

                        Debug.Log("WeatherMod: Updating Server rain state to " + (float)NetworkHandler.ServerWeatherStrength);
                    }
                }
            }
            catch { }
            //We don't have to flag Deathmatch for now as it can't affect gameplay other than some slight visability issues.
            // Water mod automatically disables water height changes for now as well
            //    - and thunder/lighting and hail isn't in yet.
        }

        public static void Initiate()
        {
            inst = new GameObject("RainGUI").AddComponent<RainGUI>();
            inst.gameObject.SetActive(true);
            GUIDisp = new GameObject();
            GUIDisp.AddComponent<GUIDisplay>();
            GUIDisp.SetActive(false);
        }
        internal class GUIDisplay : MonoBehaviour
        {
            private void OnGUI()
            {
                if (ShowGUI)
                {
                    Window = GUI.Window(1, Window, GUIWindow, "Rain Settings");
                }
            }
        }
    }

}
