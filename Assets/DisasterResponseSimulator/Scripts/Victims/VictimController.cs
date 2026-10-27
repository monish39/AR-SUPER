using System;
using UnityEngine;
using DisasterResponse.Core;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Victims
{
    [RequireComponent(typeof(Collider))]
    public class VictimController : MonoBehaviour
    {
        [Header("Vitals & Profile")]
        [SerializeField] private VictimVitals vitals = new VictimVitals();

        [Header("UI Reference")]
        [SerializeField] private WorldSpaceVitalsUI vitalsUI;

        [Header("Visual Indicators")]
        [SerializeField] private GameObject triageMarkerVisual;
        [SerializeField] private Renderer bodyRenderer;

        private float stabilizationProgress = 0f;
        private const float RequiredStabilizationTime = 4.0f; // seconds of continuous treatment
        private bool isDiscovered = false;

        public VictimVitals Vitals => vitals;
        public float StabilizationProgress => stabilizationProgress / RequiredStabilizationTime;
        public bool IsDiscovered => isDiscovered;

        public event Action<VictimController> OnVictimDiscovered;
        public event Action<VictimController, TriageCategory> OnTriageCategoryAssigned;
        public event Action<VictimController> OnVictimStabilized;
        public event Action<VictimController> OnVictimDeceased;
        public event Action<VictimController> OnVictimEvacuated;

        private void Start()
        {
            if (vitalsUI == null) vitalsUI = GetComponentInChildren<WorldSpaceVitalsUI>();
            if (vitalsUI != null)
            {
                vitalsUI.BindVictim(this);
                // Display floating AR vitals HUD so responder sees immediate triage status
                vitalsUI.SetVisible(true);
            }
        }

        private void Update()
        {
            if (vitals.isDeceased || vitals.isEvacuated) return;

            // Health decay rate based on injury severity
            float decayRate = 0f;
            if (vitals.hasActiveBleeding) decayRate += 2.2f;
            if (vitals.isAirwayObstructed) decayRate += 1.8f;
            if (vitals.isPinnedUnderRubble) decayRate += 0.8f;

            if (decayRate > 0f && !vitals.isStabilized)
            {
                vitals.healthPercentage -= decayRate * Time.deltaTime;
                
                // Adjust SpO2 and Heart Rate dynamically
                if (vitals.isAirwayObstructed)
                {
                    vitals.bloodOxygen = Mathf.Max(60f, vitals.bloodOxygen - 1.5f * Time.deltaTime);
                }

                if (vitals.hasActiveBleeding)
                {
                    vitals.heartRate = Mathf.Min(160, vitals.heartRate + (int)(3 * Time.deltaTime));
                    vitals.systolicBP = Mathf.Max(65, vitals.systolicBP - (int)(2 * Time.deltaTime));
                }

                if (vitals.healthPercentage <= 0f)
                {
                    Die();
                }
            }
        }

        public void Discover()
        {
            if (!isDiscovered)
            {
                isDiscovered = true;
                if (vitalsUI != null) vitalsUI.SetVisible(true);
                OnVictimDiscovered?.Invoke(this);
                IncidentCommandSystem.Instance?.RegisterVictimDiscovered(this);
                DisasterAudioEngine.Instance?.PlayDiscoveryChime();
            }
        }

        public void ApplyMedicalTreatment(float deltaTime)
        {
            if (vitals.isDeceased || vitals.isEvacuated || vitals.isStabilized) return;

            Discover();

            if (vitals.isPinnedUnderRubble)
            {
                // Can't fully stabilize while pinned
                DisasterAudioEngine.Instance?.PlayWarningBuzzer();
                return;
            }

            stabilizationProgress += deltaTime;
            vitals.healthPercentage = Mathf.Min(100f, vitals.healthPercentage + 5f * deltaTime);

            if (stabilizationProgress >= RequiredStabilizationTime)
            {
                Stabilize();
            }
        }

        public void AssignTriageTag(TriageCategory category)
        {
            vitals.assignedCategory = category;
            Discover();
            if (vitalsUI != null) vitalsUI.UpdateDisplay();

            // Validate against clinical FEMA START Triage algorithm
            var (isAccurate, feedback) = STARTTriageSystem.ValidateTriageDecision(category, vitals);
            OnTriageCategoryAssigned?.Invoke(this, category);
            IncidentCommandSystem.Instance?.RegisterTriageDecision(this, category, isAccurate);

            // Audio & Radio Dispatch feedback
            DisasterAudioEngine.Instance?.PlayTriageConfirmSound();
            var hud = FindFirstObjectByType<UI.ICSHUDController>();
            if (hud != null)
            {
                hud.UpdatePromptBanner(isAccurate ? $"<color=#00FF88>[START TRIAGE VALIDATED]</color> {feedback}" : $"<color=#FF4444>[TRIAGE DEVIATION]</color> {feedback}");
            }
        }

        public void SetPinned(bool pinned)
        {
            vitals.isPinnedUnderRubble = pinned;
            if (vitalsUI != null) vitalsUI.UpdateDisplay();
        }

        public void Stabilize()
        {
            vitals.isStabilized = true;
            vitals.hasActiveBleeding = false;
            vitals.isAirwayObstructed = false;
            vitals.bloodOxygen = 98f;
            vitals.heartRate = 80;
            vitals.systolicBP = 115;
            
            if (vitalsUI != null) vitalsUI.UpdateDisplay();
            OnVictimStabilized?.Invoke(this);
            IncidentCommandSystem.Instance?.RegisterVictimStabilized(this);
            DisasterAudioEngine.Instance?.PlayStabilizedSound();

            TacticalRadioCommsAudio.Instance?.DispatchRadioTransmission($"Casualty {vitals.victimName} stabilized. Airway patent, bleeding controlled.");
        }

        public void Evacuate()
        {
            if (vitals.isEvacuated) return;
            vitals.isEvacuated = true;
            if (vitalsUI != null) vitalsUI.SetVisible(false);

            OnVictimEvacuated?.Invoke(this);
            IncidentCommandSystem.Instance?.RegisterVictimEvacuated(this);

            // Deploy AI Paramedic Stretcher Gurney Team from landing zone
            var landingZone = GameObject.Find("EvacuationLandingZone");
            Vector3 lzPos = landingZone != null ? landingZone.transform.position : Vector3.zero;
            AIEMSStretcherTeam.DeployEvacuationTeam(this, lzPos);
        }

        private void Die()
        {
            vitals.isDeceased = true;
            vitals.healthPercentage = 0f;
            vitals.heartRate = 0;
            vitals.bloodOxygen = 0f;
            vitals.systolicBP = 0;

            if (vitalsUI != null) vitalsUI.UpdateDisplay();
            OnVictimDeceased?.Invoke(this);
            IncidentCommandSystem.Instance?.RegisterVictimDeceased(this);
            DisasterAudioEngine.Instance?.PlayFlatlineSound();
        }
    }
}
