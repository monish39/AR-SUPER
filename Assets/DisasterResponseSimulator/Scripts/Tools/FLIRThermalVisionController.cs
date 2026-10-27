using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DisasterResponse.Tools
{
    public class FLIRThermalVisionController : MonoBehaviour
    {
        public static FLIRThermalVisionController Instance { get; private set; }

        [Header("Thermal Vision Settings")]
        [SerializeField] private Material thermalMaterial;
        [SerializeField] private bool isThermalActive = false;

        [Header("HUD Overlay")]
        [SerializeField] private GameObject thermalHUD;
        [SerializeField] private TextMeshProUGUI telemetryText;
        [SerializeField] private TextMeshProUGUI tempReadoutText;

        private Camera targetCamera;
        private RenderTexture thermalRT;

        public bool IsThermalActive => isThermalActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            targetCamera = Camera.main;
            EnsureThermalMaterial();
            CreateThermalHUD();
        }

        private void EnsureThermalMaterial()
        {
            if (thermalMaterial == null)
            {
                var shader = Shader.Find("DisasterResponse/FLIRThermalVision");
                if (shader != null)
                {
                    thermalMaterial = new Material(shader);
                }
            }
        }

        private void CreateThermalHUD()
        {
            var canvasGo = GameObject.Find("ICS_Tactical_HUD_Canvas");
            if (canvasGo == null) return;

            thermalHUD = new GameObject("FLIR_Thermal_Overlay");
            thermalHUD.transform.SetParent(canvasGo.transform, false);

            var rect = thermalHUD.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Header Telemetry
            var headerGo = new GameObject("ThermalHeader");
            headerGo.transform.SetParent(thermalHUD.transform, false);
            telemetryText = headerGo.AddComponent<TextMeshProUGUI>();
            telemetryText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            telemetryText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            telemetryText.rectTransform.anchoredPosition = new Vector2(0, -75);
            telemetryText.rectTransform.sizeDelta = new Vector2(500, 30);
            telemetryText.fontSize = 15;
            telemetryText.alignment = TextAlignmentOptions.Center;
            telemetryText.fontStyle = FontStyles.Bold;
            telemetryText.color = new Color(1f, 0.85f, 0.1f);
            telemetryText.text = "FLIR INFRARED SENSOR: ACTIVE [BAND: 8-14 um] | PRESS [V] TO TOGGLE";

            // Center Crosshair Temp Readout
            var tempGo = new GameObject("TempReadout");
            tempGo.transform.SetParent(thermalHUD.transform, false);
            tempReadoutText = tempGo.AddComponent<TextMeshProUGUI>();
            tempReadoutText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            tempReadoutText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tempReadoutText.rectTransform.anchoredPosition = new Vector2(0, -45);
            tempReadoutText.rectTransform.sizeDelta = new Vector2(300, 26);
            tempReadoutText.fontSize = 14;
            tempReadoutText.alignment = TextAlignmentOptions.Center;
            tempReadoutText.color = Color.cyan;
            tempReadoutText.text = "TARGET: 21.5 C [AMBIENT]";

            thermalHUD.SetActive(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.vKey.wasPressedThisFrame)
            {
                ToggleThermalVision();
            }
#endif
            if (isThermalActive)
            {
                UpdateCenterThermalTelemetry();
            }
        }

        public void ToggleThermalVision()
        {
            SetThermalVision(!isThermalActive);
        }

        public void SetThermalVision(bool active)
        {
            isThermalActive = active;
            if (thermalHUD != null) thermalHUD.SetActive(active);

            AudioVFX.DisasterAudioEngine.Instance?.PlayScannerBeep();
        }

        private void UpdateCenterThermalTelemetry()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || tempReadoutText == null) return;

            Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var victim = hit.collider.GetComponentInParent<Victims.VictimController>();
                var hazard = hit.collider.GetComponentInParent<Hazards.HazardBase>();

                if (victim != null)
                {
                    if (victim.Vitals.isDeceased)
                    {
                        tempReadoutText.text = "<color=#8888FF>TARGET: 22.0 C [COLD / NON-VIABLE]</color>";
                    }
                    else
                    {
                        float temp = 36.8f + (victim.Vitals.heartRate > 100 ? 0.8f : -0.5f);
                        tempReadoutText.text = $"<color=#FFAA00>TARGET: {temp:F1} C [HUMAN BIOMETRIC DETECTED]</color>";
                    }
                }
                else if (hazard != null)
                {
                    if (hazard is Hazards.FireHazard)
                    {
                        tempReadoutText.text = "<color=#FFFFFF>TARGET: > 420 C [CRITICAL COMBUSTION HAZARD]</color>";
                    }
                    else
                    {
                        tempReadoutText.text = "<color=#00FFFF>TARGET: -12 C [CRYOGENIC GAS EXPANSION]</color>";
                    }
                }
                else
                {
                    tempReadoutText.text = "TARGET: 21.4 C [AMBIENT DEBRIS]";
                }
            }
            else
            {
                tempReadoutText.text = "SCANNING AMBIENT ATMOSPHERE...";
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (isThermalActive && thermalMaterial != null)
            {
                Graphics.Blit(src, dest, thermalMaterial);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
}
