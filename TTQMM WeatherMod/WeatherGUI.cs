using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TTQMM_WeatherMod
{
    public class WeatherGUI : MonoBehaviour
    {
        static private WeatherGUI inst = null;
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
            KickStart.WeatherToggledOn = GUI.Toggle(new Rect(20, 40, 100, 20), KickStart.WeatherToggledOn, "Rain Enabled?");

            KickStart.randomWeatherActive = GUI.Toggle(new Rect(20, 60, 100, 20), KickStart.randomWeatherActive, "Natural Rain?");

            KickStart.KeepWeatherActive = GUI.Toggle(new Rect(20, 120, 100, 20), KickStart.KeepWeatherActive, "Make it Rain!");

            KickStart.WeatherIntensity = GUI.HorizontalSlider(new Rect(20, 100, 160, 15), KickStart.WeatherIntensity, 0f, 1f);
            rainIntensityDisplay = (int)(KickStart.WeatherIntensity * 100);
            GUI.Label(new Rect(20, 80, 120, 20), "Rain Intensity: " + rainIntensityDisplay.ToString() + "%");

            RainMaker.VisualizeRainSpawnerCenter.enabled = GUI.Toggle(new Rect(20, 140, 100, 20), RainMaker.VisualizeRainSpawnerCenter.enabled, "ShowRainCenter");

            //Day/hour tracker - very rough but I'm not going overkill to be precise
            month = (int)Mathf.Repeat((Singleton.Manager<ManTimeOfDay>.inst.GameDay / 32), 13);
            year = (int)(Singleton.Manager<ManTimeOfDay>.inst.GameDay / 365) + 2021;
            if (KickStart.UseAltDateFormat)
                GUI.Label(new Rect(20, 170, 160, 30), "Year/Month/Day: " + year + "/" + month + "/" + Mathf.Repeat((int)Singleton.Manager<ManTimeOfDay>.inst.GameDay, 32));
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

        internal void Update()
        {
            if (Input.GetKeyDown(KickStart.hotKey))
            {
                ShowGUI = !ShowGUI;
                GUIDisp.SetActive(ShowGUI);
                if (!ShowGUI)
                {
                    //DebugWeather.Log("\nWeatherMod: Writing to Config...");
                    try
                    {
                        WaterOptions.Save();
                    }
                    catch
                    {
                        DebugWeather.Log("\nWeatherMod: Writing to Config failed, NativeOptions and/or ConfigHelper unavailable/broken");
                    }
                }
            }

            if (KickStart.WeatherToggledOn == true)
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
                    if (IsItRaining != lastRainState || RainStateUpdate == true)////NetworkHandler.ServerWeatherStrength != KickStart.RainIntensity
                    {
                        lastRainState = IsItRaining;
                        RainStateUpdate = false;

                        //If it's not raining then keep intensity zero!
                        if (!IsItRaining)
                        {
                            NetworkHandler.ServerWeatherStrength = 0f;
                            //DebugWeather.Log("Turning off rain with " + KickStart.RainIntensity);
                        }
                        else
                        {
                            NetworkHandler.ServerWeatherStrength = WeatherCommander.WorldWeatherIntensity;// KickStart.RainIntensity;
                            //DebugWeather.Log("Turning on rain with " + KickStart.RainIntensity);
                        }
                        if (RainMaker.IsRaining == true)
                        {
                            //DebugWeather.Log("It's confirmed raining"); 
                        }

                        DebugWeather.Log("WeatherMod: Updating Server rain state to " + (float)NetworkHandler.ServerWeatherStrength);
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
            inst = new GameObject("RainGUI").AddComponent<WeatherGUI>();
            inst.gameObject.SetActive(true);
            GUIDisp = new GameObject();
            GUIDisp.AddComponent<GUIDisplay>();
            GUIDisp.SetActive(false);
        }
        internal class GUIDisplay : MonoBehaviour
        {
            internal void OnGUI()
            {
                if (ShowGUI)
                {
                    Window = GUI.Window(1, Window, GUIWindow, "Rain Settings");
                }
            }
        }
    }

}
