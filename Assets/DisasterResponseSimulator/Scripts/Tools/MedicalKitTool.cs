using UnityEngine;
using DisasterResponse.Victims;
using DisasterResponse.AudioVFX;
using DisasterResponse.UI;

namespace DisasterResponse.Tools
{
    public class MedicalKitTool : MonoBehaviour
    {
        [Header("Medical Kit Properties")]
        [SerializeField] private ParticleSystem healingParticles;

        private bool isActive = false;
        private VictimController currentTreatingVictim;
        private float lastInterventionTime = 0f;

        public void SetActive(bool active)
        {
            isActive = active;
            if (!isActive) StopMedicalAction();
        }

        public void ExecuteMedical(RaycastHit hit, TriageCategory selectedTag, bool isHolding, float deltaTime)
        {
            if (!isActive) return;

            var victim = hit.collider.GetComponentInParent<VictimController>();
            if (victim == null) return;

            currentTreatingVictim = victim;

            // 1. If victim is pinned under rubble, alert responder immediately
            if (victim.Vitals.isPinnedUnderRubble)
            {
                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner("<color=#FF4444>[TRAUMA ALERT]</color> Casualty is pinned under concrete collapse! Use <b>[3] Rescue Spreader</b> to lift rubble first!");
                DisasterAudioEngine.Instance?.PlayWarningBuzzer();
                return;
            }

            // 2. If already stabilized, click triggers AI EMS evacuation!
            if (victim.Vitals.isStabilized && !victim.Vitals.isEvacuated && !isHolding)
            {
                victim.Evacuate();
                return;
            }

            // 3. Assign or update triage category if user selected a tag
            if (selectedTag != TriageCategory.Unassigned && victim.Vitals.assignedCategory != selectedTag)
            {
                victim.AssignTriageTag(selectedTag);
            }

            // 4. Clinical Emergency Interventions (Airway & Hemorrhage) - Immediate response on click!
            if (victim.Vitals.isAirwayObstructed)
            {
                victim.Vitals.isAirwayObstructed = false;
                victim.Vitals.bloodOxygen = Mathf.Min(98f, victim.Vitals.bloodOxygen + 12f);
                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#00FF88>[AIRWAY CLEARED]</color> Jaw-thrust performed on {victim.Vitals.victimName}. Patent airway established!");
                DisasterAudioEngine.Instance?.PlayStabilizedSound();
                lastInterventionTime = Time.time;
            }
            else if (victim.Vitals.hasActiveBleeding)
            {
                victim.Vitals.hasActiveBleeding = false;
                victim.Vitals.systolicBP = Mathf.Min(120, victim.Vitals.systolicBP + 18);
                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#00FF88>[TOURNIQUET APPLIED]</color> Combat windlass tourniquet secured on {victim.Vitals.victimName}. Hemorrhage arrested!");
                DisasterAudioEngine.Instance?.PlayStabilizedSound();
                lastInterventionTime = Time.time;
            }

            // 5. Continuous Stabilization Treatment
            if (!victim.Vitals.isStabilized && !victim.Vitals.isDeceased)
            {
                // Each click or hold tick advances treatment
                float treatmentStep = isHolding ? deltaTime : 0.5f;
                victim.ApplyMedicalTreatment(treatmentStep);

                if (healingParticles != null)
                {
                    healingParticles.transform.position = hit.point + Vector3.up * 0.2f;
                    if (!healingParticles.isPlaying) healingParticles.Play();
                }

                if (!victim.Vitals.isStabilized)
                {
                    var hud = FindFirstObjectByType<ICSHUDController>();
                    hud?.UpdatePromptBanner($"<color=#00CCFF>[TREATMENT IN PROGRESS]</color> Hold Left-Click on {victim.Vitals.victimName} to stabilize ({victim.StabilizationProgress * 100f:F0}%)");
                }
            }
            else if (victim.Vitals.isStabilized && !victim.Vitals.isEvacuated)
            {
                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#00FF88>[PATIENT STABILIZED]</color> Click {victim.Vitals.victimName} once more with [4] MedKit to deploy AI EMS transport gurney!");
            }
        }

        public void StopMedicalAction()
        {
            currentTreatingVictim = null;
            if (healingParticles != null && healingParticles.isPlaying)
            {
                healingParticles.Stop();
            }
        }
    }
}
