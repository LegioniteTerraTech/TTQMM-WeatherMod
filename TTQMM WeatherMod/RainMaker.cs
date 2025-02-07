using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TTQMM_WeatherMod
{
    public static class RainMaker
    {
        public static Texture2D blurredSprite;
        public static Material blurredMat;
        //public static Material spriteMaterial;
        public static Material mainMaterial;
        private static GameObject FXFolder;
        public const int rainInstances = 16;
        public static GameObject[] oRain;
        public static GameObject[] oRainHit;
        public static Transform RainMain;
        public static Transform RainSpawnerCenter;
        public static MeshRenderer VisualizeRainSpawnerCenter;
        public static ParticleSystem[] FXRain;
        public static ParticleSystem[] FXRainHit;
        public static bool isRaining = true;
        public static bool WaterModExists = false;
        public static float RainWeight;
        public static float WaterModHeight;
        public static LayerMask CollisionLayers;


        public static ParticleSystem.MinMaxGradient WaterGradient = new ParticleSystem.MinMaxGradient(
                new Gradient()
                {
                    alphaKeys = new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.48f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                    },
                    colorKeys = new GradientColorKey[] {
                    new GradientColorKey(new Color(0.561f, 0.937f, 0.875f), 0.5f),
                    new GradientColorKey(new Color(0f, 0.69f, 1f), 1f)
                    },
                    mode = GradientMode.Blend
                });

        public static bool IsRaining
        {
            get
            {
                try
                {
                    if (WaterModExists)
                    {
                        if (WaterMod.WaterHeight > RainSpawnerCenter.position.y)
                            return false;
                    }
                }
                catch { }
                return isRaining;
            }
            set
            {
                if (isRaining != value)
                {
                    if (value)
                    {
                        for (int i = 0; i < rainInstances; i++)
                            FXRain[i].Play();
                    }
                    else
                    {
                        for (int i = 0; i < rainInstances; i++)
                            FXRain[i].Stop();
                    }
                    ManTimeOfDay.inst.DayNightChangedEvent.Send(ManTimeOfDay.inst.NightTime);
                }
                isRaining = value;
            }
        }

        public static void Initiate()//Startup sequence
        {
            FXFolder = new GameObject("WeatherModFX");
            oRain = new GameObject[rainInstances];
            oRainHit = new GameObject[rainInstances];
            FXRain = new ParticleSystem[rainInstances];
            FXRainHit = new ParticleSystem[rainInstances];
            for (int i = 0; i < rainInstances; i++)
            {
                oRain[i] = new GameObject("Rain" + i);
                oRainHit[i] = new GameObject("RainHit" + i);

                oRain[i].transform.parent = FXFolder.transform;
                oRainHit[i].transform.parent = oRain[i].transform;
            }


            CollisionLayers = LayerMask.GetMask("Default", "Water", "Tank", "Terrain", "Landmarks", "Scenery", "ShieldBulletFilter");

            CreateBlurredSprite();
            
            CreateSpriteMaterial();
            CreateRainHit();
            CreateRain();

            IsRaining = isRaining;
            DebugWeather.Log("WeatherMod: Created Rain Effects");
            if (ModExists("WaterMod"))
            {
                DebugWeather.Log("Found WaterMod!");
                WaterModExists = true;
            }
        }

        static void CreateBlurredSprite()
        {
            int radius = 4;
            blurredSprite = new Texture2D(radius * 2, radius * 2);
            for (int y = 0; y < radius * 2; y++)
            {
                for (int x = 0; x < radius * 2; x++)
                {
                    blurredSprite.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.8f - Mathf.Sqrt((y - radius) * (y - radius) + (x - radius) * (x - radius)) / radius)));
                }
            }
            blurredSprite.Apply();
        }
        internal static Shader InsureGetShader(string name)
        {
            //var shader = Shader.Find("Standard");
            Shader shader = Shader.Find(name);
            //var shader = Shader.Find("Shield");
            //var shader = Shader.Find("Unlit/Transparent");
            //var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (shader == null)
            {
                IEnumerable<Shader> shaders = Resources.FindObjectsOfTypeAll<Shader>();
                /*
                foreach (var item in shaders)
                {
                    if (item && !item.name.NullOrEmpty())
                        DebugWater.Log(item.name);
                }
                */
                shaders = shaders.Where(s => s.name == name); ////Standard
                shader = shaders.ElementAt(0);
                if (shader == null)
                    DebugWeather.Log("Water Mod: failed to get shader");
            }
            return shader;
        }
        static void CreateSpriteMaterial()
        {

            //var shader = Shader.Find("Standard");
            //var shader = Shader.Find("Legacy Shaders/Particles/Additive (Soft)");
            Shader shader = InsureGetShader("Legacy Shaders/Particles/Alpha Blended");
            blurredMat = new Material(shader)
            {
                renderQueue = 3500,
                color = new Color(0.2f, 0.8f, 0.75f, 0.8f)
            };
            blurredMat.mainTexture = blurredSprite;
            blurredMat.EnableKeyword("_ALPHATEST_ON");
            blurredMat.EnableKeyword("_ALPHABLEND_ON");
            blurredMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            blurredMat.DisableKeyword("_EMISSION");
            blurredMat.SetColor("_Color", new Color(0.2f, 0.8f, 0.75f, 0.8f));
            blurredMat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 0.75f, 0.2f));

            //blurredMat.SetFloat("_Mode", 2f);
            //blurredMat.SetFloat("_Metallic", 0.6f);
            //blurredMat.SetFloat("_Glossiness", 0.9f);
            //blurredMat.SetInt("_SrcBlend", 5);
            //blurredMat.SetInt("_DstBlend", 10);
            //blurredMat.SetInt("_ZWrite", 0);
            //blurredMat.DisableKeyword("_ALPHATEST_ON");
            //blurredMat.EnableKeyword("_ALPHABLEND_ON");
            //blurredMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        
        static void CreateRain()
        {
            GameObject mainTrans = new GameObject("RainMain");
            RainMain = mainTrans.transform;
            mainTrans.AddComponent<RainScript>();

            GameObject center = new GameObject("RainCenter");
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject.Destroy(vis.GetComponent<CapsuleCollider>());
            vis.transform.parent = center.transform;
            RainSpawnerCenter = center.transform;
            RainSpawnerCenter.parent = RainMain;

            VisualizeRainSpawnerCenter = vis.GetComponent<MeshRenderer>();
            VisualizeRainSpawnerCenter.enabled = false;

            for (int i = 0; i < oRain.Length; i++)
            {
                var ps = oRain[i].AddComponent<ParticleSystem>();
                var m = ps.main;
                m.simulationSpace = ParticleSystemSimulationSpace.World;
                m.startSize = 0.015f;
                m.startLifetime = 1.85f;
                m.playOnAwake = false;
                m.maxParticles = 50000;//5000;
                m.gravityModifier = 1f;
                //m.startColor = WaterGradient; - May need some tweaks to be enabled
                var v = ps.velocityOverLifetime;
                v.enabled = true;
                v.space = ParticleSystemSimulationSpace.World;
                v.y = -26;//-16f;
                var e = ps.emission;
                e.rateOverTime = 1000f;
                var s = ps.shape;
                s.shapeType = ParticleSystemShapeType.Cone;
                //s.shapeType = ParticleSystemShapeType.Circle;
                s.angle = 0f;
                s.radius = 180f;
                s.rotation = Vector3.right * 90f;
                s.position = Vector3.up * 12.5f;
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                //r.cameraVelocityScale = 0;
                r.cameraVelocityScale = 0.15f;
                r.velocityScale = 0.05f;
                r.lengthScale = 2f;
                r.maxParticleSize = 0.5f;
                r.material = blurredMat;
                var c = ps.collision;
                c.enabled = true;
                c.type = ParticleSystemCollisionType.World;
                c.quality = ParticleSystemCollisionQuality.High;
                c.enableDynamicColliders = true;
                c.collidesWith = CollisionLayers;
                c.maxKillSpeed = 0;
                c.minKillSpeed = 0;
                var b = ps.subEmitters;
                Transform trans = oRain[i].transform;
                trans.parent = RainMain;
                trans.localScale = Vector3.one;
                trans.localPosition = Vector3.zero;
                trans.localRotation = Quaternion.identity;
                b.enabled = true;
                b.AddSubEmitter(FXRainHit[i], ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritRotation);
                FXRain[i] = ps;
            }
            RainSpawnerCenter.localPosition = Vector3.up * 12.5f;
        }

        private static void CreateRainHit()
        {
            for (int i = 0; i < oRain.Length; i++)
            {
                var ps = oRainHit[i].AddComponent<ParticleSystem>();
                var m = ps.main;
                m.simulationSpace = ParticleSystemSimulationSpace.World;
                m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.125f);
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                m.gravityModifier = .5f;
                m.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                var e = ps.emission;
                e.rateOverTime = 0f;
                e.burstCount = 1;
                e.SetBurst(0, new ParticleSystem.Burst(0, 1, 3));
                var s = ps.shape;
                s.shapeType = ParticleSystemShapeType.Cone;
                s.angle = 25f;
                s.radius = 0.001f;
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.cameraVelocityScale = 0.05f;
                r.velocityScale = 0.2f;
                r.lengthScale = 0.2f;
                r.material = blurredMat;

                FXRainHit[i] = ps;
            }
        }

        public static void UpdateFog()
        {
            
        }

        private class RainScript : MonoBehaviour
        {
            Vector3 lastcampos;
            internal void Update()
            {
                try
                {
                    if (isRaining)//Updater
                    {
                        if ((lastcampos - Camera.main.transform.position).sqrMagnitude > 10000)
                        {
                            lastcampos = Camera.main.transform.position;
                            return;
                        }
                        RainMain.position = Camera.main.transform.position * 2 + (Camera.main.transform.rotation * Vector3.forward * 17.5f) - lastcampos;
                        RainMain.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - lastcampos, Vector3.up) * Quaternion.Euler(90, 0, 0);
                        for (int i = 0; i < rainInstances; i++)
                        {
                            if (IsRaining) // main
                            {
                                var e = FXRain[i].emission;
                                e.rateOverTime = 2500f * RainWeight;
                                var s = FXRain[i].main;
                                s.startSize = 0.015f + RainWeight * 0.05f;
                                s.gravityModifier = 1f + RainWeight * 0.25f;//* 0.25f;
                                //var r = FXRain[i].GetComponent<ParticleSystemRenderer>();
                                //r.lengthScale = 2f + RainWeight * 0.2f;

                                var d = oRainHit[i].GetComponent<ParticleSystem>().shape;
                                d.radius = 0.0015f + RainWeight * 0.003f;
                                var m = oRainHit[i].GetComponent<ParticleSystem>().main;
                                m.startSpeedMultiplier = 1f + RainWeight * 0.2f;
                            }
                            else
                            {
                                var e = FXRain[i].emission;
                                e.rateOverTime = 0f;
                            }
                        }
                    }
                    /*
                    else
                    {
                        var e = FXRain.emission;
                        e.rateOverTime = 0;
                    }
                    */

                }
                catch { }
            }
            internal void FixedUpdate()
            {
                try
                {
                    lastcampos = Camera.main.transform.position + Vector3.down * 0.5f;
                }
                catch { }
            }
        }

        public static bool ModExists(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
