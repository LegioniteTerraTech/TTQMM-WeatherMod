using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TTQMM_WeatherMod
{
    public class Class1
    {
        public static void Main()
        {
            //var harmony = HarmonyInstance.Create("aceba1.fx.weather.core");
            //harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            RainMaker.Initiate();
            RainGUI.Initiate();
        }
    }
    public class RainGUI : MonoBehaviour
    {
        private bool ShowGUI = false;
        private Rect Window = new Rect(100, 0, 100, 140);

        private void GUIWindow(int ID)
        {
            GUI.Label(new Rect(0, 20, 100, 20), "RainWeight");
            RainMaker.RainWeight = GUI.HorizontalSlider(new Rect(0, 40, 100, 15), RainMaker.RainWeight, 0f, 1f);

            RainMaker.IsRaining = GUI.Toggle(new Rect(0, 60, 100, 20), RainMaker.isRaining, "IsRaining");
            GUI.Label(new Rect(0, 80, 100, 20), "FogWeight");
            GUI.Button(new Rect(0, 100, 100, 20), "Unavailable");
            //RainMaker.FogWeight = GUI.HorizontalSlider(new Rect(0, 100, 100, 15), RainMaker.FogWeight, 0f, 1f);
            RainMaker.VisualizeRainSpawnerCenter.enabled = GUI.Toggle(new Rect(0, 120, 100, 20), RainMaker.VisualizeRainSpawnerCenter.enabled, "ShowRainCenter");
            GUI.DragWindow();
        }

        private void OnGUI()
        {
            if (ShowGUI)
            {
                Window = GUI.Window(1, Window, GUIWindow, "RainSettings");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                ShowGUI = !ShowGUI;
            }
        }
        public static void Initiate()
        {
            new GameObject("RainGUI").AddComponent<RainGUI>();
        }
    }
}
