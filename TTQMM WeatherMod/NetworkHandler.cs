using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace TTQMM_WeatherMod
{
    static class NetworkHandler
    {
        //Handles very much like the Water Mod's netcode, just a tad bit different.
        internal static NetworkInstanceId Host;
        internal static bool HostExists = false;

        internal const TTMsgType WeatherChange = (TTMsgType)275;

        internal static float serverWeatherStrength = KickStart.RainIntensity;

        // For now we will handle it simply with the strength value coming from WeatherCommander to minimise comms strain
        //private static bool serverWeatherActive = false;

        public static float ServerWeatherStrength
        {
            get { return serverWeatherStrength; }
            set
            {
                serverWeatherStrength = value;
                TryBroadcastNewStrength(serverWeatherStrength);
            }
        }

        public class WeatherChangeMessage : MessageBase
        {
            public WeatherChangeMessage() { }

            public WeatherChangeMessage(float strength)
            {
                Strength = strength;
            }

            public override void Deserialize(NetworkReader reader)
            {
                Strength = reader.ReadSingle();
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(Strength);
            }

            public float Strength;
        }

        public static void TryBroadcastNewStrength(float Strength)
        {
            if (HostExists)
            {
                try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients(WeatherChange, new WeatherChangeMessage(Strength), Host);
                    Debug.Log("\nSent new rain strength and rain state to all");
                }
                catch { Debug.Log("\nFailed to send new rain strength and rain state..."); }
            }
        }

        public static void OnClientChangeWeatherStrength(NetworkMessage netMsg)
        {
            //Since the host is the one calling the shots, this must not fire for the host at all costs or SPAM COMMS.
            if (ManNetwork.IsHost == false)
            {
                var reader = new WeatherChangeMessage();
                netMsg.ReadMessage(reader);
                serverWeatherStrength = reader.Strength;
                Debug.Log("\nReceived new rain strength, changing to " + serverWeatherStrength.ToString());
            }
        }
    }
}
