using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;
using HarmonyLib;
using WaterMod;
using static Biome;
using static ItemBuildContext;
using static Circuits;

namespace TTQMM_WeatherMod
{
    internal static class DebugWeather
    {
        public static void Log(string info) => Debug.Log(info);
    }

    public enum WeatherType
    {
        /// <summary> No effect other than slight friction reduction </summary>
        Rain,
        /// <summary> Does nothing but look nice </summary>
        Snow,
        /// <summary> Non-laser projectiles may randomly explode </summary>
        Hail,
        /// <summary> Combination of Snow and Hail </summary>
        SnowNHail,
        /// <summary> Interferes with all weapon aiming </summary>
        Sandstorm,
    }
    internal class WeatherCommander
    {
        //TIME MANAGEMENT
        public static int lDay = 0; //the last day
        public static int lHour = 0; //the last hour
        public static float RandomChance = 0f; //Rolling the dice
        public static int RainCoupons = 0;
        public static bool isRainyDay = false; //is it supposed to rain today?
        public static bool isCurrentlyRaining = false; //is it raining right now
        private static float _weatherIntensityByPlayer;
        /// <summary> Current Rain Intensity </summary>
        public static float WeatherIntensityByPlayer
        {
            get => _weatherIntensityByPlayer;
            set
            {
                if (float.IsNaN(value))
                    throw new InvalidProgramException("WeatherIntensityByPlayer was illegally set to NaN");
                _weatherIntensityByPlayer = value;
            }
        }
        public static WeatherType WeatherTypeByPlayer = WeatherType.Rain;

        // Update chart: NaturalWorldRainIntensity > WorldRainIntensity > LocalBiome  > RainIntensityByPlayer

        //Brief calculations
        public static bool ThunderNLightning = false;
        /// <summary> Rain Intensity based on the player's biome, based on biomes and locationing </summary>
        public static float LocalBiomesRainIntensity = 0f;
        public static WeatherType LocalBiomesWeatherType = WeatherType.Rain;
        /// <summary> Rain Intensity the world set </summary>
        public static float NaturalWorldWeatherIntensity = 0f;
        private static float _worldWeatherIntensity;
        public static float WorldWeatherIntensity
        {
            get => _worldWeatherIntensity;
            set
            {
                if (float.IsNaN(value))
                    throw new InvalidProgramException("WorldWeatherIntensity was illegally set to NaN");
                _worldWeatherIntensity = value;
            }
        }

        public static float LastWorldWeatherIntensity = 0f;
        //public static float RainIntensityLerp = 0f;//Fade controller for the rain when entering and leaving - WIP

        internal static void Init()
        {
            ManUpdate.inst.AddAction(ManUpdate.Type.Update, ManUpdate.Order.Last, RemoteUpdate, 1009000);
        }
        internal static void RemoteUpdate()
        {
            if (WorldWeatherIntensity > 0 && (ManNetwork.IsNetworked || RainMaker.IsRaining))
                ManTimeOfDayExt.SetState(orderW);
        }

        internal static void Save()
        {
            DebugWeather.Log("\nWeatherMod: Writing to Config...");
            try
            {
                WaterOptions.Save();
            }
            catch
            {
                DebugWeather.Log("\nWeatherMod: Writing to Config failed, NativeOptions and/or ConfigHelper unavailable/broken");
            }
        }
        private static void NetUpdate()
        {
            if (WorldWeatherIntensity != LastWorldWeatherIntensity)
            {
                LastWorldWeatherIntensity = WorldWeatherIntensity;
                //toggle signal
                WeatherGUI.RainStateUpdate = true;
            }
        }

        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);

        static Color darkColor = new Color(1f, 1f, 1f, 0.65f); //new Color(0f, 0f, 0f, 0.65f);

        private static bool CamRain = false;
        static Gradient darkSkyColors = new Gradient()
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
        private static Color spoopy = new Color(0f, 0f, 0f, 0.65f);
        private static Color CalcSpoopLevel(Color inColor)
        {
            /*
            Color abyssColor;
            abyssColor = darkColor * WeatherIntensityByPlayer * inColor;
            abyssColor.a = 1f;
            float depthWatch = Mathf.Clamp01((WeatherIntensityByPlayer / 2) - (ManTimeOfDay.inst.NightTime ? 0f : 0.5f));
            return (spoopy * depthWatch) + (inColor * (1f - depthWatch));
            // */
            float depthWatch = Mathf.Clamp01((WeatherIntensityByPlayer / 2f) - (ManTimeOfDay.inst.NightTime ? 0f : 0.5f));
            Color sppods = Color.Lerp(spoopy, inColor, depthWatch);
            //DebugWeather.Log("Color out: " + sppods.ToString() + " which is depthWatch " + depthWatch.ToString("0.000"));
            return sppods;
        }
        private static Color MakeDark(Color inColor)
        {
            var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 40f;
            sky.m_UseTerraTechBiomeData = true;
            sky.Fog.Mode = TOD_FogType.None;
            sky.Ambient.Mode = TOD_AmbientType.None;

            Color darker = CalcSpoopLevel(inColor);
            /*
            RenderSettings.fogDensity = 160f;
            RenderSettings.fogColor = darker;
            RenderSettings.ambientLight = darker;
            RenderSettings.ambientGroundColor = darker;
            RenderSettings.ambientIntensity = 1 + RainIntensityProcessed;
            var keys = darkSkyColors.colorKeys;
            keys[0].color = keys[1].color = darker;
            darkSkyColors.colorKeys = keys;

            sky.Day.AmbientColor = sky.Night.AmbientColor = darkSkyColors;
            sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = darkSkyColors;
            */
            return darker;
        }
        private static void KeepDark(DayNightColours dayColours, DayNightColours nightColours)
        {// INSIDE the rainstorm
            ManTimeOfDay.inst.BlendImmediately();
            float depthWatch = (Mathf.Clamp01(1f - WeatherIntensityByPlayer) * 0.8f) + 0.2f;
            dayColours.DustVFXColour = Color.Lerp(spoopy, dayColours.DustVFXColour, depthWatch);
            nightColours.DustVFXColour = Color.Lerp(spoopy, nightColours.DustVFXColour, depthWatch);
            dayColours.LightColour = Color.Lerp(spoopy, dayColours.LightColour, depthWatch);
            nightColours.LightColour = Color.Lerp(spoopy, nightColours.LightColour, depthWatch);
            dayColours.AmbientColour = Color.Lerp(spoopy, dayColours.AmbientColour, depthWatch);
            nightColours.AmbientColour = Color.Lerp(spoopy, nightColours.AmbientColour, depthWatch);
            dayColours.RayColour = Color.Lerp(spoopy, dayColours.RayColour, depthWatch);
            nightColours.RayColour = Color.Lerp(spoopy, nightColours.RayColour, depthWatch);
            dayColours.FogColour = Color.Lerp(spoopy, dayColours.FogColour, depthWatch);
            nightColours.FogColour = Color.Lerp(spoopy, nightColours.FogColour, depthWatch);
            dayColours.SkyColour = Color.Lerp(spoopy, dayColours.SkyColour, depthWatch);
            nightColours.SkyColour = Color.Lerp(spoopy, nightColours.SkyColour, depthWatch);
            dayColours.SunMoonColour = Color.Lerp(spoopy, dayColours.SunMoonColour, depthWatch);
            nightColours.SunMoonColour = Color.Lerp(spoopy, nightColours.SunMoonColour, depthWatch);
        }
        internal static void TEMP_BypassEdit(DayNightColours dayColours, DayNightColours nightColours)
        {// INSIDE the rainstorm
            Color darker = CalcSpoopLevel(new Color(0f, 0f, 0f, 1f));
            dayColours.AmbientColour *= darker;
            nightColours.AmbientColour *= darker;
            dayColours.RayColour *= darker;
            nightColours.RayColour *= darker;
            dayColours.SkyColour *= darker;
            nightColours.SkyColour *= darker;
            dayColours.LightColour *= darker;
            nightColours.LightColour *= darker;
            dayColours.FogColour *= darker;
            nightColours.FogColour *= darker;
            dayColours.DustVFXColour *= darker;
            nightColours.DustVFXColour *= darker;
            dayColours.SunMoonColour *= darker;
            nightColours.SunMoonColour *= darker;
        }
        private static ManTimeOfDayExt.TOD_Ordering orderW = new ManTimeOfDayExt.TOD_Ordering(
            "WeM", 1, MakeDark, KeepDark);



        private static void DetermineIfWeShouldRain()
        {
            //Try to get day
            try
            {
                //Fetch the day
                int cDay = Singleton.Manager<ManTimeOfDay>.inst.GameDay;
                if (cDay > lDay && KickStart.randomWeatherActive == true)
                {
                    DebugWeather.Log("\nWeatherMod:");
                    DebugWeather.Log("It's a new day!  Rolling chance of rain...");
                    DebugWeather.Log("  Last Day: " + lDay);
                    DebugWeather.Log("  Current Day: " + cDay);
                    RainCoupons = 0;
                    lDay = cDay;
                    UnityEngine.Random.State temp = UnityEngine.Random.state;
                    UnityEngine.Random.InitState(cDay);
                    RandomChance = UnityEngine.Random.Range(0f, 1f);
                    UnityEngine.Random.state = temp;
                    DebugWeather.Log("  Roll: " + RandomChance);
                    DebugWeather.Log("  MaxVal: " + KickStart.dailyWeatherChance);
                    if (RandomChance <= KickStart.dailyWeatherChance)
                    {
                        isRainyDay = true;
                        DebugWeather.Log("  Rain!");
                    }
                    else
                    {
                        isCurrentlyRaining = false;
                        isRainyDay = false;
                        DebugWeather.Log("  No Rain.");
                    }
                }
                else if (cDay < lDay)
                {
                    DebugWeather.Log("\nTime was changed backwards, resyncing LastDay...");
                    lDay = -1 + cDay;
                }

            }
            catch //Ooop couldn't fetch the day
            {
                DebugWeather.Log("\nWEATHERMOD HAS ENCOUNTERED A SERIOUS ERROR! - Could not fetch current day!");
            }

            //Now try to get hour
            try
            {
                //Fetch the hour
                int cHour = Singleton.Manager<ManTimeOfDay>.inst.TimeOfDay;
                int cDay = Singleton.Manager<ManTimeOfDay>.inst.GameDay;
                if (cHour > lHour)
                {
                    DebugWeather.Log("\nClunk! New hour!  Rolling chance of rain...");
                    DebugWeather.Log("  Last Hour: " + lHour);
                    DebugWeather.Log("  Current Hour: " + cHour);
                    if (isRainyDay == true && KickStart.randomWeatherActive == true)
                    {
                        lHour = cHour;
                        UnityEngine.Random.State temp = UnityEngine.Random.state;
                        UnityEngine.Random.InitState(cDay * cHour);
                        RandomChance = UnityEngine.Random.Range(0f, 1f);
                        UnityEngine.Random.state = temp;
                        float cRainFreq = Mathf.Clamp(0.5f * KickStart.totalWeatherFrequency, 0, 1); //Calculate Rain Frequency
                        DebugWeather.Log("  Roll: " + RandomChance);
                        DebugWeather.Log("  MaxVal: " + cRainFreq);
                        if (RandomChance <= cRainFreq)
                        {
                            if (RandomChance > 0.675f)
                                RainCoupons++;
                            isCurrentlyRaining = true;
                            NaturalWorldWeatherIntensity = 1 + RainCoupons;
                            DebugWeather.Log("  It's pouring!\n");
                        }
                        else if (RainCoupons > 0)
                        {
                            RainCoupons--;
                            NaturalWorldWeatherIntensity = 1 + RainCoupons;
                            isCurrentlyRaining = true;
                            DebugWeather.Log("  It continues!\n");
                        }
                        else
                        {
                            isCurrentlyRaining = false;
                            DebugWeather.Log("  No rain this time.\n");
                        }
                    }
                }
                else if (cHour < lHour)
                {
                    RainCoupons = 0;
                    DebugWeather.Log("\nTime was changed backwards, resyncing LastHour...");
                    lHour = cHour - 1;
                }
            }
            catch //Ooop couldn't fetch the hour
            {
                DebugWeather.Log("WEATHERMOD HAS ENCOUNTERED A SERIOUS ERROR! - Could not fetch current hour!");
            }
        }
        private static void UpdateWorldRainIntensity()
        {
            try
            {
                if (KickStart.KeepWeatherActive)
                {
                    WorldWeatherIntensity = KickStart.WeatherIntensity;
                }
                else
                {
                    if (ManNetwork.IsNetworked && !ManNetwork.IsHost)
                        WorldWeatherIntensity = NetworkHandler.ServerWeatherStrength;
                    else if (RainMaker.IsRaining)
                        WorldWeatherIntensity = NaturalWorldWeatherIntensity;
                    else
                        WorldWeatherIntensity = 0;
                }

                if (WorldWeatherIntensity > 0)
                {
                    CamRain = true;
                    ManTimeOfDayExt.SetState(orderW);
                }
                else
                {
                    if (CamRain)
                    {   // OUTSIDE the rain
                        CamRain = false;
                        ManTimeOfDayExt.RemoveState("WeM");
                    }
                }
            }
            catch
            {
                DebugWeather.Log("WEATHERMOD HAS ENCOUNTERED A SERIOUS ERROR! - Could not UpdateTargetRainIntensity()!");
            }
        }
        public class BiomeWeather
        {
            public readonly WeatherType type;
            public readonly float AvgTemp;
            public readonly float StrengthMin;
            public readonly float StrengthMax;
            public BiomeWeather(WeatherType dominantWeatherType, float avgTempF, float rainMin, float rainMax) 
            {
                type = dominantWeatherType;
                AvgTemp = avgTempF;
                StrengthMin = rainMin;
                StrengthMax = rainMax;
            }

        }
        /*
Biome #0 - BasicGrasslandBiome_ScaledTrees
Biome #1 - CopseOfTreesSubBiome
Biome #2 - RockyRidgeBiome
Biome #3 - WoodlandValleyBiome
Biome #4 - DesertBiome
Biome #5 - MogulsBiome
Biome #6 - SmallDunesBiome
Biome #7 - LowMesasBiome
Biome #8 - LargeDunesBiome
Biome #9 - FlatsBiome
Biome #10 - MountainsBiome
Biome #11 - TerracedHillsBiome
Biome #12 - PeaksBiome
Biome #13 - CanyonsBiome
Biome #14 - EaglesNestBiome
Biome #15 - GorgesBiome
Biome #16 - StepSlopesBiome
Biome #17 - PillarsBiome
Biome #18 - IceBiome
Biome #19 - LargeCraters_Biome
Biome #20 - MidCraters_Biome
Biome #21 - SmallCraters_Biome
        */
        public static bool TryAddNewBiome(string name, BiomeWeather weather)
        {
            if (WeatherByBiome.ContainsKey(name))
                return false;
            WeatherByBiome.Add(name, weather);
            return true;
        }
        public const string fallbackBiomeName = "WoodlandValleyBiome";
        public const float TempHot = 58.4f;
        public const float TempTemperate = 58.4f;
        public const float TempCold = 25.2f;
        public const float TempFreeze = -10.2f;
        public const float TempSnow = 0f;
        public const float RainFallMinimumForHail = 0.6f;
        private static Dictionary<string, BiomeWeather> WeatherByBiome = new Dictionary<string, BiomeWeather>()
        {
            {"BasicGrasslandBiome_ScaledTrees", new BiomeWeather(WeatherType.Rain, TempTemperate, 0.4f, 1f)},
            {"CopseOfTreesSubBiome",            new BiomeWeather(WeatherType.Rain, TempTemperate, 0.5f, 1f)},
            {"RockyRidgeBiome",                 new BiomeWeather(WeatherType.Rain, TempTemperate, 0f, 0.4f)},
            {"WoodlandValleyBiome",             new BiomeWeather(WeatherType.Rain, TempTemperate, 0f, 1f)},
            {"DesertBiome",                     new BiomeWeather(WeatherType.Sandstorm, TempHot, 0f, 0f)},
            {"MogulsBiome",                     new BiomeWeather(WeatherType.Sandstorm, TempHot, 0f, 0f)},
            {"SmallDunesBiome",                 new BiomeWeather(WeatherType.Sandstorm, TempHot, 0f, 0f)},
            {"LowMesasBiome",                   new BiomeWeather(WeatherType.Sandstorm, TempHot, 0f, 0f)},
            {"LargeDunesBiome",                 new BiomeWeather(WeatherType.Sandstorm, TempHot, 0f, 0f)},
            {"FlatsBiome",                      new BiomeWeather(WeatherType.Rain, TempTemperate, 0f, 0f)},
            {"MountainsBiome",                  new BiomeWeather(WeatherType.Rain, TempCold, 0.25f, 1.25f)},
            {"TerracedHillsBiome",              new BiomeWeather(WeatherType.Rain, TempCold, 0.05f, 0.6f)},
            {"PeaksBiome",                      new BiomeWeather(WeatherType.Rain, TempCold, 0.5f, 2f)},
            {"CanyonsBiome",                    new BiomeWeather(WeatherType.Rain, TempHot, 0f, 0.05f)},
            {"EaglesNestBiome",                 new BiomeWeather(WeatherType.Rain, TempTemperate, 0.1f, 0.35f)},
            {"GorgesBiome",                     new BiomeWeather(WeatherType.Rain, TempTemperate, 0.2f, 0.65f)},
            {"StepSlopesBiome",                 new BiomeWeather(WeatherType.Rain, TempTemperate, 0.1f, 0.15f)},
            {"PillarsBiome",                    new BiomeWeather(WeatherType.Rain, TempCold, 0.245f, 3.5f)},
            {"IceBiome",                        new BiomeWeather(WeatherType.SnowNHail, TempFreeze, 0f, 1f)},
            {"LargeCraters_Biome",              new BiomeWeather(WeatherType.Snow, TempFreeze, 0.45f, 1.5f)},
            {"MidCraters_Biome",                new BiomeWeather(WeatherType.Snow, TempFreeze, 0.45f, 1.5f)},
            {"SmallCraters_Biome",              new BiomeWeather(WeatherType.Snow, TempFreeze, 0.45f, 1.5f)},
        };
        public static BiomeWeather GetBiomeWeather(string biomeName)
        {
            if (WeatherByBiome.TryGetValue(biomeName, out var weather))
                return weather;
            return WeatherByBiome[fallbackBiomeName];
        }
        private static Dictionary<WeatherType, float> weatherTemp = new Dictionary<WeatherType, float>();
        public static bool LogBiomes = false;
        private static List<KeyValuePair<string, float>> biomesContributed = new List<KeyValuePair<string, float>>();
        private static StringBuilder SB = new StringBuilder();
        private static float AddWeatherCaster(string name, BiomeWeather weather, float curWeight)
        {
            float weatherWeight = Mathf.LerpUnclamped(weather.StrengthMin, weather.StrengthMax, curWeight * WorldWeatherIntensity);
            if (weatherWeight > 0)
            {
                WeatherType weatherType = weather.type;
                if (weatherType == WeatherType.SnowNHail)
                    weatherType = (weatherWeight > RainFallMinimumForHail) ? WeatherType.Hail : WeatherType.Snow;
                if (weatherTemp.TryGetValue(weatherType, out float curVal))
                    weatherTemp[weatherType] = weatherWeight + curVal;
                else
                    weatherTemp.Add(weatherType, weatherWeight);
            }
            if (LogBiomes)
                biomesContributed.Add(new KeyValuePair<string, float>(name, weatherWeight));
            return weatherWeight;
        }
        private static void UpdateLocalPlayerRainIntensity()
        {
            if (ManWorld.inst != null && WorldWeatherIntensity > 0)
            {   // Calculate local biome topography
                float weatherStrengthTotal = 0;
                float TotalWeighting = 0;
                //var weights = ManWorld.inst.GetBiomeWeightsAtScenePosition(Singleton.playerPos);
                var curBiomeWeights = ManWorld.inst.GetBiomeWeightsAtScenePosition(Camera.main.transform.position);//ManWorld.inst.CurrentBiomeWeights;
                Biome biomeBest = null;
                float biomeBestVal = -1;
                if (curBiomeWeights.NumWeights > 1)
                {
                    for (int i = curBiomeWeights.NumWeights - 1; i > -1; i--)
                    {
                        Biome biome = curBiomeWeights.Biome(i);
                        if (biome != null)
                        {
                            float curWeight = curBiomeWeights.Weight(i) * (1f - curBiomeWeights.SetPieceBiomeWeight);
                            if (curWeight > 0)
                            {
                                TotalWeighting += curWeight;
                                weatherStrengthTotal += AddWeatherCaster(biome.name, GetBiomeWeather(biome.name), curWeight);
                            }
                            if (biomeBestVal < curWeight)
                            {
                                biomeBest = biome;
                                biomeBestVal = curWeight;
                            }
                        }
                    }
                }
                else
                { // It's just one biome lol
                    biomeBest = curBiomeWeights.Biome(0);
                }
                if (curBiomeWeights.SetPieceBiomeWeight > 0)
                {
                    TotalWeighting += curBiomeWeights.SetPieceBiomeWeight;
                    Biome biome = curBiomeWeights.SetPieceBiome;
                    if (biome != null)
                    {
                        float curWeight = curBiomeWeights.SetPieceBiomeWeight;
                        if (curWeight > 0)
                        {
                            TotalWeighting += curWeight;
                            weatherStrengthTotal += AddWeatherCaster(biome.name, GetBiomeWeather(biome.name), curWeight);
                        }
                        if (biomeBestVal < curWeight)
                        {
                            biomeBest = biome;
                            biomeBestVal = curWeight;
                        }
                    }
                }
                if (biomeBest != null)
                {   // Get the highest biome and add the max val to it
                    weatherStrengthTotal += AddWeatherCaster(biomeBest.name, GetBiomeWeather(biomeBest.name), Mathf.Clamp01(1f - TotalWeighting));
                }
                else
                {   // No biome!?!  We default to yes
                    weatherStrengthTotal += AddWeatherCaster("default", GetBiomeWeather("default"), Mathf.Clamp01(1f - TotalWeighting));
                }

                WeatherType best = WeatherType.Rain;
                float weatherStrengthBest = 0;
                if (biomesContributed.Any())
                {
                    SB.Append("Weather weight: ");
                    SB.Append(weatherStrengthTotal);
                    SB.Append("Biomes: ");
                    foreach (var item in biomesContributed)
                    {
                        SB.Append(item.Key);
                        SB.Append("(");
                        SB.Append(item.Value.ToString("0.000"));
                        SB.Append("), ");
                    }
                    biomesContributed.Clear();
                    DebugWeather.Log(SB.ToString());
                    SB.Clear();
                }
                foreach (var weather in weatherTemp)
                {
                    if (weather.Value > weatherStrengthBest)
                    {
                        best = weather.Key;
                        weatherStrengthBest = weather.Value;
                    }
                }
                weatherTemp.Clear();

                LocalBiomesRainIntensity = weatherStrengthTotal;
                LocalBiomesWeatherType = best;
            }
            else
                LocalBiomesRainIntensity = 0;
            if (WeatherTypeByPlayer != LocalBiomesWeatherType)
            {
                if (WeatherIntensityByPlayer > 0.05f)
                    WeatherIntensityByPlayer = Mathf.Lerp(WeatherIntensityByPlayer, 0, 0.12f);
                else
                    WeatherTypeByPlayer = LocalBiomesWeatherType;
            }
            else
                WeatherIntensityByPlayer = Mathf.Lerp(WeatherIntensityByPlayer, LocalBiomesRainIntensity, 0.12f);
            RainMaker.RainWeight = WeatherIntensityByPlayer;
        }
        private static void UpdateNetcodeHandling()
        {
            //Now to test teh rain
            //NETCODE HANDLING RECIEVER - Decouple controls when not host
            try
            {
                if (!ManNetwork.inst.IsMultiplayer() || ManNetwork.IsHost)
                {   // process the rain
                    //Single-Player/MP-Host handling
                    if (KickStart.WeatherToggledOn)
                    {
                        if (KickStart.KeepWeatherActive)
                        {
                            RainMaker.IsRaining = true;
                            WeatherGUI.IsItRaining = true;
                        }
                        else if (isCurrentlyRaining && KickStart.randomWeatherActive)
                        {
                            RainMaker.IsRaining = true;
                            WeatherGUI.IsItRaining = true;
                        }
                        else
                        {
                            RainMaker.IsRaining = false;
                            WeatherGUI.IsItRaining = false;
                        }
                    }
                    else
                        RainMaker.IsRaining = false;
                    NetUpdate();
                }
                /*
                else
                {
                    //Decoupled!
                    RainMaker.RainWeight = WeatherIntensityByPlayer;
                    RainMaker.IsRaining = true;//The intensity will control the rain now
                                               //DebugWeather.Log("\nWeatherMod: Decoupled from local player controls!");
                    NetUpdate();
                    return;//End it here right and now.  Do not pass go.  Do not collect 2000.
                }
                */
            }
            catch { }

        }

        internal static void WeatherUpdate()
        {
            DetermineIfWeShouldRain();
            UpdateWorldRainIntensity();
            UpdateLocalPlayerRainIntensity();
            UpdateNetcodeHandling();
        }
    }
}
