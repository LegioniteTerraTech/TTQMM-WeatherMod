using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using TerraTechETCUtil;

namespace TTQMM_WeatherMod
{
    public static class Patches
    {
        [HarmonyPatch(typeof(ModuleLight), "EnableLights")]
        [HarmonyPriority(-9001)]
        static class LightsWhenDark
        {
            static void Prefix(ModuleLight __instance, ref bool enable)
            {
                if (__instance.block?.tank && WeatherCommander.RainIntensityCurrent >= 0.25f)
                    enable = true;
            }
        }
        [HarmonyPatch(typeof(ManTimeOfDay), "LerpCloudData")]
        [HarmonyPriority(-9001)]
        static class CloudsEffect
        {
            private static FieldInfo cloudflare = typeof(ManTimeOfDay).GetField("m_TargetCloudParams", BindingFlags.Instance | BindingFlags.NonPublic);
            static void Prefix(ManTimeOfDay __instance)
            {
                if (WeatherCommander.RainIntensityCurrent > 0 && (ManNetwork.IsNetworked || RainMaker.IsRaining))
                {
                    TOD_CloudParameters cloudsInst = (TOD_CloudParameters)cloudflare.GetValue(__instance);
                    float rainintense = WeatherCommander.RainIntensityCurrent;
                    float cloudintense = WeatherCommander.RainIntensityCurrent * 4;
                    cloudsInst.Opacity += rainintense;
                    cloudsInst.Coverage += rainintense;
                    if (WeatherCommander.ThunderNLightning)
                    {
                        cloudsInst.Attenuation += cloudintense;
                        cloudsInst.Saturation += cloudintense;
                        cloudsInst.Brightness = 0.05f;
                    }
                    else
                    {
                        cloudsInst.Attenuation += cloudintense / 2f;
                        cloudsInst.Saturation += cloudintense / 2f;
                    }
                }
            }
        }

        //---------------------------------------------
        //                  Networking
        //---------------------------------------------
        [HarmonyPatch(typeof(NetPlayer), "OnRecycle")]
        static class OnRecycle
        {
            static void Postfix(NetPlayer __instance)
            {
                if (__instance.isServer || __instance.isLocalPlayer)
                {
                    NetworkHandler.serverWeatherStrength = 0f;
                    Debug.Log("\nDiscarded " + __instance.netId.ToString() + " and reset server weather strength level");
                    NetworkHandler.HostExists = false;
                }
            }
        }

        [HarmonyPatch(typeof(NetPlayer), "OnStartClient")]
        static class OnStartClient
        {
            static void Postfix(NetPlayer __instance)
            {
                Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, 
                    NetworkHandler.WeatherChange, new ManNetwork.MessageHandler(NetworkHandler.OnClientChangeWeatherStrength));
                Debug.Log("\nSubscribed " + __instance.netId.ToString() + " to weather strength updates from host. Sending current weather strength...");
                NetworkHandler.TryBroadcastNewStrength(NetworkHandler.serverWeatherStrength);
            }
        }

        [HarmonyPatch(typeof(NetPlayer), "OnStartServer")]
        static class OnStartServer
        {
            static void Postfix(NetPlayer __instance)
            {
                if (!NetworkHandler.HostExists)
                {
                    //Singleton.Manager<ManNetwork>.inst.SubscribeToServerMessage(__instance.netId, WeatherChange, new ManNetwork.MessageHandler(OnServerChangeWeatherStrength));
                    Debug.Log("\nHost started, hooked weather strength broadcasting to " + __instance.netId.ToString());
                    NetworkHandler.Host = __instance.netId;
                    NetworkHandler.HostExists = true;
                }
            }
        }
    }
}
