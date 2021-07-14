using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using ModHelper.Config;
using HarmonyLib;
using Nuterra.NativeOptions;

namespace TTQMM_WeatherMod
{
    public class Class1
    {
        const string ModName = "WeatherMod";

        //Make a Config File to store user preferences
        public static ModConfig _thisModConfig;

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
        public static OptionKey GUIMenuHotKey;

        public static OptionToggle RainEnabled;
        public static OptionToggle RandomRain;
        public static OptionToggle RainActive;
        public static OptionToggle AltDateFormat;
        public static OptionRange DailyRainChance;
        public static OptionRange TRainFrequency;
        public static OptionRange RainStrength;

        public static void Main()
        {

            var harmony = new Harmony("aceba1.fx.weather.core");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            RainMaker.Initiate();
            RainGUI.Initiate();

            ///*
            //Create and Edit Config
            Debug.Log("\nWeatherMod: Config Loading");
            ModConfig thisModConfig = new ModConfig();
            Debug.Log("WeatherMod: Config Loaded.");

            thisModConfig.BindConfig<Class1>(null, "keyInt");
            hotKey = (KeyCode)keyInt;

            thisModConfig.BindConfig<Class1>(null, "RainToggledOn");
            thisModConfig.BindConfig<Class1>(null, "randomRainActive");
            thisModConfig.BindConfig<Class1>(null, "dailyRainChance");
            thisModConfig.BindConfig<Class1>(null, "totalRainFrequency");
            thisModConfig.BindConfig<Class1>(null, "KeepRainActive");
            thisModConfig.BindConfig<Class1>(null, "RainIntensity");
            thisModConfig.BindConfig<Class1>(null, "UseAltDateFormat");
            _thisModConfig = thisModConfig;

            Debug.Log("WeatherMod: Config Binder Loaded.");

            //Options Menu Support
            var WeatherProperties = ModName + " - Weather Settings";
            GUIMenuHotKey = new OptionKey("GUI Menu button", WeatherProperties, hotKey);
            GUIMenuHotKey.onValueSaved.AddListener(() => { keyInt = (int)(hotKey = GUIMenuHotKey.SavedValue); WeatherCommander.Save(); });

            RainEnabled = new OptionToggle("Raining Enabled", WeatherProperties, RainToggledOn);
            RainEnabled.onValueSaved.AddListener(() => { RainToggledOn = RainEnabled.SavedValue; });
            RainActive = new OptionToggle("Keep Raining", WeatherProperties, KeepRainActive);
            RainActive.onValueSaved.AddListener(() => { KeepRainActive = RainActive.SavedValue; });
            RandomRain = new OptionToggle("Natural Rain Enabled", WeatherProperties, randomRainActive);
            RandomRain.onValueSaved.AddListener(() => { randomRainActive = RandomRain.SavedValue; });
            DailyRainChance = new OptionRange("Daily Rain Chance", WeatherProperties, dailyRainChance, 0f, 1f, 0.1f);
            DailyRainChance.onValueSaved.AddListener(() => { dailyRainChance = DailyRainChance.SavedValue; });
            TRainFrequency = new OptionRange("Natural Rain Chance", WeatherProperties, totalRainFrequency, 0f, 2f, 0.2f);
            TRainFrequency.onValueSaved.AddListener(() => { totalRainFrequency = TRainFrequency.SavedValue; });
            RainStrength = new OptionRange("Rain Intensity", WeatherProperties, RainIntensity, 0f, 1f, 0.1f);
            RainStrength.onValueSaved.AddListener(() => { RainIntensity = RainStrength.SavedValue; });

            AltDateFormat = new OptionToggle("Y/M/D Format", WeatherProperties, UseAltDateFormat);
            AltDateFormat.onValueSaved.AddListener(() => { UseAltDateFormat = AltDateFormat.SavedValue; });
            

            Debug.Log("WeatherMod: NativeOptions Set.\n");
        }

    }
    public class RainGUI : MonoBehaviour
    {
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
            Class1.RainToggledOn = GUI.Toggle(new Rect(20, 40, 100, 20), Class1.RainToggledOn, "Rain Enabled?");

            Class1.randomRainActive = GUI.Toggle(new Rect(20, 60, 100, 20), Class1.randomRainActive, "Natural Rain?");

            Class1.KeepRainActive = GUI.Toggle(new Rect(20, 120, 100, 20), Class1.KeepRainActive, "Make it Rain!");

            Class1.RainIntensity = GUI.HorizontalSlider(new Rect(20, 100, 160, 15), Class1.RainIntensity, 0f, 1f);
            rainIntensityDisplay = (int)(Class1.RainIntensity * 100);
            GUI.Label(new Rect(20, 80, 120, 20), "Rain Intensity: " + rainIntensityDisplay.ToString() + "%");

            RainMaker.VisualizeRainSpawnerCenter.enabled = GUI.Toggle(new Rect(20, 140, 100, 20), RainMaker.VisualizeRainSpawnerCenter.enabled, "ShowRainCenter");

            //Day/hour tracker - very rough but I'm not going overkill to be precise
            month = (int)Mathf.Repeat((Singleton.Manager<ManTimeOfDay>.inst.GameDay / 32), 13);
            year = (int)(Singleton.Manager<ManTimeOfDay>.inst.GameDay / 365) + 2021;
            if (Class1.UseAltDateFormat)
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
            if (Input.GetKeyDown(Class1.hotKey))
            {
                ShowGUI = !ShowGUI;
                GUIDisp.SetActive(ShowGUI);
                if (!ShowGUI)
                {
                    //Debug.Log("\nWeatherMod: Writing to Config...");
                    Class1._thisModConfig.WriteConfigJsonFile();
                }
            }

            if (Class1.RainToggledOn == true)
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
                            NetworkHandler.ServerWeatherStrength = Class1.RainIntensity;
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
            new GameObject("RainGUI").AddComponent<RainGUI>();
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
