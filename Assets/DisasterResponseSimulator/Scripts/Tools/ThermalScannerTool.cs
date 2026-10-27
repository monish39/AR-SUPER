using UnityEngine;
using DisasterResponse.Victims;
using DisasterResponse.Hazards;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Tools
{
    public class ThermalScannerTool : MonoBehaviour
    {
        [Header("Scanner Visuals")]
        [SerializeField] private LineRenderer scannerBeam;
        [SerializeField] private GameObject scannerHUDPanel;

        private bool isActive = false;
        private float beamTimer = 0f;

        private void Awake()
        {
            EnsureBeamRenderer();
        }

        private void EnsureBeamRenderer()
        {
            if (scannerBeam == null)
            {
                scannerBeam = gameObject.AddComponent<LineRenderer>();
                scannerBeam.positionCount = 2;
                scannerBeam.startWidth = 0.02f;
                scannerBeam.endWidth = 0.005f;

                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                var mat = new Material(shader) { color = new Color(0.2f, 1f, 0.5f, 0.9f) };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.2f, 1f, 0.5f, 0.9f));
                scannerBeam.material = mat;
                scannerBeam.enabled = false;
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (scannerBeam != null) scannerBeam.enabled = false;
            if (scannerHUDPanel != null) scannerHUDPanel.SetActive(active);
        }

        private void Update()
        {
            if (beamTimer > 0f)
            {
                beamTimer -= Time.deltaTime;
                if (beamTimer <= 0f && scannerBeam != null)
                {
                    scannerBeam.enabled = false;
                }
            }
        }

        public void ExecuteScan(RaycastHit hit)
        {
            if (!isActive) return;

            // Draw laser line from tool/camera to target
            if (scannerBeam != null)
            {
                scannerBeam.enabled = true;
                scannerBeam.SetPosition(0, transform.position + Vector3.down * 0.1f);
                scannerBeam.SetPosition(1, hit.point);
                beamTimer = 0.25f;
            }

            // Expanding Holographic Sonar Ping across the floor
            ProceduralVFXBuilder.CreateSonarPingEffect(null, hit.point);

            // Victim scanning
            var victim = hit.collider.GetComponentInParent<VictimController>();
            if (victim != null)
            {
                victim.Discover();
                var vitals = victim.Vitals;
                if (vitals != null)
                {
                    var hud = FindFirstObjectByType<UI.ICSHUDController>();

                    if (vitals.isDeceased)
                    {
                        DisasterAudioEngine.Instance?.PlayFlatlineSound();
                        hud?.UpdatePromptBanner($"<color=#888888>[BIOMETRIC SCAN: {vitals.victimName}]</color> Core Temp: 31.2°C (Hypothermic) | HR: 0 bpm | SpO2: 0% | <color=#FF4444>APNEA - NON-VIABLE (BLACK)</color>");
                    }
                    else
                    {
                        float pulsePitch = Mathf.Clamp(vitals.heartRate / 80f, 0.6f, 1.8f);
                        DisasterAudioEngine.Instance?.PlayHeartbeatSound(pulsePitch);
                        if (vitals.isAirwayObstructed || vitals.respiratoryRate > 24)
                        {
                            DisasterAudioEngine.Instance?.PlayBreathingSound();
                        }

                        float coreTemp = 36.8f - (vitals.hasActiveBleeding ? 1.4f : 0f);
                        string statusTag = vitals.isStabilized ? "<color=#00FF88>STABILIZED</color>" : 
                            (vitals.isAirwayObstructed ? "<color=#FF2222>AIRWAY OCCLUSION</color>" : 
                            (vitals.hasActiveBleeding ? "<color=#FF3333>HEMORRHAGE</color>" : "<color=#FFAA00>ALERT</color>"));

                        hud?.UpdatePromptBanner($"<color=#00FFCC>[BIOMETRIC SCAN: {vitals.victimName}]</color> Temp: {coreTemp:F1}°C | HR: <b>{vitals.heartRate} bpm</b> | SpO2: <b>{vitals.bloodOxygen:F0}%</b> | RR: <b>{vitals.respiratoryRate} bpm</b> | Status: {statusTag}");
                    }
                }
                else
                {
                    DisasterAudioEngine.Instance?.PlayScannerBeep();
                }
                return;
            }

            // Hazard scanning
            var hazard = hit.collider.GetComponentInParent<HazardBase>();
            if (hazard != null)
            {
                DisasterAudioEngine.Instance?.PlayScannerBeep();
                var hud = FindFirstObjectByType<UI.ICSHUDController>();

                if (hazard is FireHazard fire)
                {
                    hud?.UpdatePromptBanner($"<color=#FF5500>[THERMAL SIGNATURE DETECTED]</color> Core Flame Temp: <b>680°C</b> | Intensity: {fire.HazardIntensity * 100f:F0}% | <color=#00CCFF>Use [2] Extinguisher to Douse</color>");
                }
                else if (hazard is GasLeakHazard gas)
                {
                    hud?.UpdatePromptBanner($"<color=#FFAA00>[TOXIC VAPOR TELEMETRY]</color> Methane Concentration: <b>{(int)(gas.HazardIntensity * 950)} PPM</b> | Explosive Hazard! | <color=#00CCFF>Use [3] Spreader to Seal Valve</color>");
                }
                else if (hazard is RubbleObstacle rubble)
                {
                    hud?.UpdatePromptBanner($"<color=#FFCC00>[STRUCTURAL COLLAPSE SCAN]</color> Reinforced Concrete Debris | Mass: <b>540 kg</b> | <color=#00CCFF>Hold [3] Rescue Spreader to Lift</color>");
                }
                return;
            }

            // General Surface Scan
            DisasterAudioEngine.Instance?.PlayScannerBeep();
            var generalHud = FindFirstObjectByType<UI.ICSHUDController>();
            generalHud?.UpdatePromptBanner($"<color=#00AAFF>[TACTICAL SONAR PING]</color> Surface elevation verified. No active bio-signals at coordinate.");
        }
    }
}
