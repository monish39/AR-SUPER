using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DisasterResponse.Victims
{
    public class WorldSpaceVitalsUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private TextMeshProUGUI victimNameText;
        [SerializeField] private TextMeshProUGUI vitalsText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image triageBadge;
        [SerializeField] private TextMeshProUGUI triageBadgeText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider stabilizationSlider;
        [SerializeField] private Image pulseIcon;

        private Transform targetCamera;
        private VictimController boundVictim;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            FindCamera();
        }

        private void FindCamera()
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
        }

        public void BindVictim(VictimController victim)
        {
            boundVictim = victim;
            UpdateDisplay();
        }

        private void LateUpdate()
        {
            if (targetCamera == null) FindCamera();

            if (targetCamera != null)
            {
                // Clean billboarding to match camera orientation perfectly
                transform.rotation = targetCamera.rotation;
            }

            // Pulse effect for heart rate
            if (pulseIcon != null && boundVictim != null && !boundVictim.Vitals.isDeceased)
            {
                float bpm = Mathf.Max(30f, boundVictim.Vitals.heartRate);
                float scale = 1f + 0.25f * Mathf.Sin(Time.time * (bpm / 60f) * Mathf.PI * 2f);
                pulseIcon.transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (boundVictim != null)
            {
                UpdateDisplay();
            }
        }

        public void UpdateDisplay()
        {
            if (boundVictim == null) return;
            var vitals = boundVictim.Vitals;

            if (victimNameText != null)
            {
                victimNameText.text = vitals.victimName;
            }

            if (vitalsText != null)
            {
                if (vitals.isDeceased)
                {
                    vitalsText.text = "<color=#888888>NO VITALS DETECTED</color>";
                }
                else
                {
                    vitalsText.text = $"<b>HR:</b> {vitals.heartRate} bpm   <b>SpO2:</b> {vitals.bloodOxygen:F0}%   <b>BP:</b> {vitals.systolicBP}/78";
                }
            }

            if (statusText != null)
            {
                if (vitals.isDeceased)
                {
                    statusText.text = "<color=#666666>STATUS: DECEASED (NON-VIABLE)</color>";
                }
                else if (vitals.isPinnedUnderRubble)
                {
                    statusText.text = "<color=#FF4444><b>[!] PINNED UNDER CONCRETE</b> - Lift with [3] Spreader</color>";
                }
                else if (vitals.isAirwayObstructed)
                {
                    statusText.text = "<color=#FF2222><b>[!] AIRWAY BLOCKED</b> - Click with [4] MedKit to open</color>";
                }
                else if (vitals.hasActiveBleeding)
                {
                    statusText.text = "<color=#FF2222><b>[!] ARTERIAL BLEEDING</b> - Click with [4] MedKit for tourniquet</color>";
                }
                else if (vitals.isStabilized)
                {
                    statusText.text = "<color=#44FF88><b>STABILIZED</b> - Click with [4] MedKit to deploy EMS Evac</color>";
                }
                else
                {
                    statusText.text = $"INJURY: {vitals.GetInjuryDescription()} (Hold [4] to Treat)";
                }
            }

            if (healthSlider != null)
            {
                healthSlider.value = vitals.healthPercentage / 100f;
            }

            if (stabilizationSlider != null)
            {
                stabilizationSlider.value = boundVictim.StabilizationProgress;
                stabilizationSlider.gameObject.SetActive(!vitals.isStabilized && !vitals.isDeceased);
            }

            UpdateTriageBadge(vitals.assignedCategory);
        }

        private void UpdateTriageBadge(TriageCategory cat)
        {
            if (triageBadge == null) return;

            Color badgeColor;
            string label;

            switch (cat)
            {
                case TriageCategory.Immediate:
                    badgeColor = new Color(0.9f, 0.15f, 0.15f, 0.95f);
                    label = "RED / IMMEDIATE";
                    break;
                case TriageCategory.Delayed:
                    badgeColor = new Color(0.95f, 0.75f, 0.1f, 0.95f);
                    label = "YELLOW / DELAYED";
                    break;
                case TriageCategory.Minimal:
                    badgeColor = new Color(0.15f, 0.85f, 0.25f, 0.95f);
                    label = "GREEN / MINIMAL";
                    break;
                case TriageCategory.Expectant:
                    badgeColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
                    label = "BLACK / EXPECTANT";
                    break;
                default:
                    badgeColor = new Color(0.3f, 0.35f, 0.45f, 0.7f);
                    label = "NOT TRIAGED";
                    break;
            }

            triageBadge.color = badgeColor;
            if (triageBadgeText != null) triageBadgeText.text = label;
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null) canvas.enabled = visible;
            else gameObject.SetActive(visible);
        }
    }
}
