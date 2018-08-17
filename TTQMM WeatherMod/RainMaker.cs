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
        public static Material spriteMaterial;
        private static GameObject FXFolder;
        public static GameObject oRain;
        public static GameObject oRainHit;
        public static Transform RainSpawnerCenter;
        public static MeshRenderer VisualizeRainSpawnerCenter;
        public static ParticleSystem FXRain;
        public static ParticleSystem FXRainHit;
        public static bool isRaining = true;
        public static float RainWeight = 0.1f;
        public static bool WaterModExists = false;
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
                if (WaterModExists)
                {
                    if (WaterMod.QPatch.WaterHeight > RainSpawnerCenter.position.y)
                        return false;
                }
                return isRaining;
            }
            set
            {
                if (value == true)
                {
                    FXRain.Play();
                }
                else
                {
                    FXRain.Stop();
                }
                isRaining = value;
            }
        }

        public static void Initiate()
        {
            FXFolder = new GameObject("WeatherModFX");
            oRain = new GameObject("Rain");
            oRainHit = new GameObject("RainHit");

            oRain.transform.parent = FXFolder.transform;
            oRainHit.transform.parent = oRain.transform;

            CollisionLayers = LayerMask.GetMask("Default", "Water", "Tank", "Terrain", "Landmarks", "Scenery", "ShieldBulletFilter");

            CreateBlurredSprite();
            CreateSpriteMaterial();
            CreateRainHit();
            CreateRain();

            IsRaining = isRaining;
            Debug.Log("Finished, Created Rain");
            if (ModExists("WaterMod"))
            {
                Debug.Log("Found WaterMod!");
                WaterModExists = true;
            }
        }
        static void CreateSpriteMaterial()
        {
            var shader = Shader.Find("Particles/Additive");
            spriteMaterial = new Material(shader);

            blurredMat = new Material(shader);
            blurredMat.mainTexture = blurredSprite;
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
        
        static void CreateRain()
        {
            GameObject center = new GameObject("RainCenter");
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject.Destroy(vis.GetComponent<CapsuleCollider>());
            vis.transform.parent = center.transform;
            RainSpawnerCenter = center.transform;
            RainSpawnerCenter.parent = oRain.transform;
            VisualizeRainSpawnerCenter = vis.GetComponent<MeshRenderer>();
            VisualizeRainSpawnerCenter.enabled = false;

            var ps = oRain.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startSize = 0.01f;
            m.startLifetime = 1f;
            m.playOnAwake = false;
            m.maxParticles = 5000;
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.y = -20f;
            var e = ps.emission;
            e.rateOverTime = 1000f;
            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Cone;
            s.angle = 0f;
            s.radius = 30f;
            s.rotation = Vector3.right * 90f;
            s.position = Vector3.up * 10f;
            RainSpawnerCenter.localPosition = Vector3.up * 10f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.cameraVelocityScale = 0.15f;
            r.velocityScale = 0.05f;
            r.lengthScale = 2f;
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
            b.enabled = true;
            b.AddSubEmitter(FXRainHit, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritRotation);
            FXRain = ps;
            oRain.AddComponent<RainScript>();
        }

        private static void CreateRainHit()
        {
            var ps = oRainHit.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startSize = 0.075f;
            m.startLifetime = 0.25f;
            m.gravityModifier = .5f;
            var e = ps.emission;
            e.rateOverTime = 0f;
            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Cone;
            s.angle = 25f;
            s.radius = 0.1f;
            s.rotation = Vector3.left * 90f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.cameraVelocityScale = 0.05f;
            r.velocityScale = 0.2f;
            r.lengthScale = 0.2f;
            r.material = blurredMat;

            FXRainHit = ps;
        }

        public static void UpdateFog()
        {
            
        }

        private class RainScript : MonoBehaviour
        {
            Vector3 lastcampos;
            private void Update()
            {
                if (isRaining)
                {
                    oRain.transform.position = Camera.main.transform.position * 2 + (Camera.main.transform.rotation * Vector3.forward * 10f) - lastcampos;
                    oRain.transform.rotation = Quaternion.LookRotation((Camera.main.transform.position - lastcampos), Vector3.up) * Quaternion.Euler(90, 0, 0);
                    if (IsRaining)
                    {
                        var e = FXRain.emission;
                        e.rateOverTime = 5000f * RainWeight;
                    }
                    else
                    {
                        var e = FXRain.emission;
                        e.rateOverTime = 0f;
                    }
                }
            }
            private void FixedUpdate()
            {
                lastcampos = Camera.main.transform.position + Vector3.down * 0.75f;
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
