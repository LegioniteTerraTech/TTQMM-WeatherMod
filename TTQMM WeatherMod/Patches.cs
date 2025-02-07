using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using TerraTechETCUtil;
using static Singleton;
using static ManUpdate;

namespace TTQMM_WeatherMod
{
    public static class Patches
    {
        [HarmonyPatch(typeof(ModuleLight), "EnableLights")]
        [HarmonyPriority(-9001)]
         private static class LightsWhenDark2
        {
            internal static void Prefix(ModuleLight __instance, ref bool enable)
            {
                if (__instance.block?.tank && WeatherCommander.WeatherIntensityByPlayer >= 0.25f)
                    enable = true;
            }
        }
        
        [HarmonyPatch(typeof(TOD_Sky), "UpdateCelestials")]
        [HarmonyPriority(-9001)]
        private static class DarknessEffect2
        {
            internal static void Postfix()
            {
                WeatherCommander.RemoteUpdate();
            }
        }

        [HarmonyPatch(typeof(ManTimeOfDay), "LerpCloudData")]
        [HarmonyPriority(-9001)]
        private static class CloudsEffect
        {
            private static FieldInfo sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.Instance | BindingFlags.NonPublic);
            private static FieldInfo clouds = typeof(ManTimeOfDay).GetField("m_CloudData", BindingFlags.Instance | BindingFlags.NonPublic);
            private static FieldInfo cloudflare = typeof(ManTimeOfDay).GetField("m_TargetCloudParams", BindingFlags.Instance | BindingFlags.NonPublic);
            private static FieldInfo cloudsNow = typeof(ManTimeOfDay).GetField("m_BlendImmediately", BindingFlags.Instance | BindingFlags.NonPublic);
            internal static void Prefix(ManTimeOfDay __instance)
            {
                if (WeatherCommander.WeatherIntensityByPlayer > 0 && (ManNetwork.IsNetworked || RainMaker.IsRaining))
                {
                    TOD_CloudParameters cloudsInst = (TOD_CloudParameters)cloudflare.GetValue(__instance);
                    float weatherClamped = WeatherCommander.WeatherIntensityByPlayer;
                    float rainintense = weatherClamped;
                    float cloudintense = weatherClamped * 4;
                    cloudsInst.Opacity += rainintense;
                    cloudsInst.Coverage += rainintense;
                    if (WeatherCommander.ThunderNLightning)
                    {
                        //cloudsInst.Attenuation += cloudintense;
                        //cloudsInst.Saturation += cloudintense;
                        cloudsInst.Brightness = 0.05f;
                    }
                    else
                    {
                        //cloudsInst.Attenuation += cloudintense / 2f;
                        //cloudsInst.Saturation += cloudintense / 2f;
                    }
                    if ((bool)cloudsNow.GetValue(__instance))
                    {
                        TOD_CloudParameters cloudsInstMain = ((TOD_Sky)sky.GetValue(__instance)).Clouds;
                        var cloudDat = (List<ManTimeOfDay.BiomeCloudData>)clouds.GetValue(__instance);
                        if (cloudDat.Count > 0)
                        {
                            cloudsInstMain.Size = 0;
                            cloudsInstMain.Opacity = 0;
                            cloudsInstMain.Coverage = 0;
                            cloudsInstMain.Sharpness = 0;
                            cloudsInstMain.Attenuation = 0;
                            cloudsInstMain.Saturation = 0;
                            cloudsInstMain.Scattering = 0;
                            cloudsInstMain.Brightness = 0;

                            for (int k = 0; k < cloudDat.Count; k++)
                            {
                                float weight = cloudDat[k].m_Weight;
                                TOD_CloudParameters cloudData = cloudDat[k].m_CloudData;
                                cloudsInstMain.Size += cloudData.Size * weight;
                                cloudsInstMain.Opacity += cloudData.Opacity * weight;
                                cloudsInstMain.Coverage += cloudData.Coverage * weight;
                                cloudsInstMain.Sharpness += cloudData.Sharpness * weight;
                                cloudsInstMain.Attenuation += cloudData.Attenuation * weight;
                                cloudsInstMain.Saturation += cloudData.Saturation * weight;
                                cloudsInstMain.Scattering += cloudData.Scattering * weight;
                                cloudsInstMain.Brightness += cloudData.Brightness * weight;
                            }
                            cloudsInstMain.Opacity += rainintense;
                            cloudsInstMain.Coverage += rainintense;
                            if (WeatherCommander.ThunderNLightning)
                            {
                                //cloudsInstMain.Attenuation += cloudintense;
                                //cloudsInstMain.Saturation += cloudintense;
                                cloudsInstMain.Brightness = 0.05f;
                            }
                            else
                            {
                                //cloudsInstMain.Attenuation += cloudintense / 2f;
                                //cloudsInstMain.Saturation += cloudintense / 2f;
                            }
                        }
                    }
                }
            }
        }

        /*
        [HarmonyPatch(typeof(ManTimeOfDay), "UpdateBiomeColours")]
        [HarmonyPriority(-9001)]
        private static class FORCE_DARKNESS
        {
            internal static void Postfix(ref DayNightColours dayColours, ref DayNightColours nightColours)
            {
                if (WeatherCommander.RainIntensityByPlayer > 0 && (ManNetwork.IsNetworked || RainMaker.IsRaining))
                {
                    WeatherCommander.TEMP_BypassEdit(dayColours, nightColours);
                }
            }
        }
        */

        //---------------------------------------------
        //                  Networking
        //---------------------------------------------
        [HarmonyPatch(typeof(NetPlayer), "OnRecycle")]
        private static class OnRecycle
        {
            internal static void Postfix(NetPlayer __instance)
            {
                if (__instance.isServer || __instance.isLocalPlayer)
                {
                    NetworkHandler.serverWeatherStrength = 0f;
                    DebugWeather.Log("\nDiscarded " + __instance.netId.ToString() + " and reset server weather strength level");
                    NetworkHandler.HostExists = false;
                }
            }
        }

        [HarmonyPatch(typeof(NetPlayer), "OnStartClient")]
        private static class OnStartClient
        {
            internal static void Postfix(NetPlayer __instance)
            {
                Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, 
                    NetworkHandler.WeatherChange, new ManNetwork.MessageHandler(NetworkHandler.OnClientChangeWeatherStrength));
                DebugWeather.Log("\nSubscribed " + __instance.netId.ToString() + " to weather strength updates from host. Sending current weather strength...");
                NetworkHandler.TryBroadcastNewStrength(NetworkHandler.serverWeatherStrength);
            }
        }

        [HarmonyPatch(typeof(NetPlayer), "OnStartServer")]
        private static class OnStartServer
        {
            internal static void Postfix(NetPlayer __instance)
            {
                if (!NetworkHandler.HostExists)
                {
                    //Singleton.Manager<ManNetwork>.inst.SubscribeToServerMessage(__instance.netId, WeatherChange, new ManNetwork.MessageHandler(OnServerChangeWeatherStrength));
                    DebugWeather.Log("\nHost started, hooked weather strength broadcasting to " + __instance.netId.ToString());
                    NetworkHandler.Host = __instance.netId;
                    NetworkHandler.HostExists = true;
                }
            }
        }
    }
}
