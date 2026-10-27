using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DisasterResponse.AudioVFX
{
    public class ARGraphicsPostProcessingManager : MonoBehaviour
    {
        public static ARGraphicsPostProcessingManager Instance { get; private set; }

        private Volume globalVolume;
        private Light keySunLight;
        private Light fillSkyLight;
        private Light emergencyStrobeLight;

        private float strobeTimer = 0f;
        private bool strobeState = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ConfigureCamera();
            CreatePostProcessingStack();
            SetupTacticalLighting();
        }

        private void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cam.allowHDR = true;
            cam.allowMSAA = true;

            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;
        }

        private void CreatePostProcessingStack()
        {
            if (globalVolume != null) return;

            var volumeGo = new GameObject("Global_AR_PostProcessing_Volume");
            volumeGo.transform.SetParent(transform, false);

            globalVolume = volumeGo.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 100f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "AR_Tactical_VolumeProfile";
            globalVolume.profile = profile;

            // 1. Bloom - Generates photorealistic radiant glow on fire, lasers, beacons, and holographic HUD
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.8f);
            bloom.scatter.Override(0.70f);
            bloom.tint.Override(new Color(1f, 0.96f, 0.92f));

            // 2. Tonemapping - ACES Film curve for high dynamic range colors
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 3. Color Adjustments - Rich tactical contrast and emergency vibrancy
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.contrast.Override(14f);
            colorAdj.saturation.Override(12f);
            colorAdj.postExposure.Override(0.15f);

            // 4. Vignette - Subtle optical attenuation simulating tactical AR visor optics
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.02f, 0.05f, 0.08f));

            // 5. Chromatic Aberration - High-tech holographic edge dispersion
            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0.10f);
        }

        private void SetupTacticalLighting()
        {
            // 1. Key Directional Sun Light (High dynamic contrast with soft shadows)
            var existingLight = FindFirstObjectByType<Light>();
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                keySunLight = existingLight;
            }
            else
            {
                var lightGo = new GameObject("Tactical_KeyLight");
                lightGo.transform.SetParent(transform, false);
                keySunLight = lightGo.AddComponent<Light>();
                keySunLight.type = LightType.Directional;
            }

            keySunLight.color = new Color(1f, 0.96f, 0.90f);
            keySunLight.intensity = 1.35f;
            keySunLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            keySunLight.shadows = LightShadows.Soft;
            keySunLight.shadowStrength = 0.65f;

            // 2. Cool Sky Fill Light (Softens harsh pitch-black shadows)
            if (fillSkyLight == null)
            {
                var fillGo = new GameObject("Tactical_SkyFillLight");
                fillGo.transform.SetParent(transform, false);
                fillSkyLight = fillGo.AddComponent<Light>();
                fillSkyLight.type = LightType.Directional;
                fillSkyLight.color = new Color(0.35f, 0.52f, 0.75f); // Cool atmospheric sky tint
                fillSkyLight.intensity = 0.40f;
                fillSkyLight.transform.rotation = Quaternion.Euler(-35f, 148f, 0f);
                fillSkyLight.shadows = LightShadows.None;
            }

            // 3. Emergency Incident Command Strobe Beacon (Pulsing tactical warning light)
            if (emergencyStrobeLight == null)
            {
                var strobeGo = new GameObject("Emergency_Incident_Strobe");
                strobeGo.transform.SetParent(transform, false);
                strobeGo.transform.position = new Vector3(0f, 3.5f, 0f);
                emergencyStrobeLight = strobeGo.AddComponent<Light>();
                emergencyStrobeLight.type = LightType.Point;
                emergencyStrobeLight.range = 14f;
                emergencyStrobeLight.color = new Color(1.0f, 0.15f, 0.15f);
                emergencyStrobeLight.intensity = 0f;
                emergencyStrobeLight.shadows = LightShadows.None;
            }
        }

        private void Update()
        {
            // Tactical emergency vehicle double-flash strobe simulation (Red / Amber)
            if (emergencyStrobeLight != null)
            {
                strobeTimer += Time.deltaTime;
                float cycle = strobeTimer % 1.2f;

                // Double flash pulse
                bool flash = (cycle > 0.05f && cycle < 0.12f) || (cycle > 0.22f && cycle < 0.30f);
                if (flash != strobeState)
                {
                    strobeState = flash;
                    emergencyStrobeLight.intensity = flash ? 2.5f : 0.0f;
                    emergencyStrobeLight.color = (cycle < 0.6f) ? new Color(1.0f, 0.15f, 0.15f) : new Color(1.0f, 0.65f, 0.05f);
                }
            }
        }
    }
}
