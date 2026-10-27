using System;
using System.Collections.Generic;
using UnityEngine;
using DisasterResponse.Victims;
using DisasterResponse.Hazards;

namespace DisasterResponse.Core
{
    public class IncidentCommandSystem : MonoBehaviour
    {
        public static IncidentCommandSystem Instance { get; private set; }

        [Header("Mission Tracking")]
        private List<VictimController> registeredVictims = new List<VictimController>();
        private List<HazardBase> registeredHazards = new List<HazardBase>();

        // Per-victim triage decisions: maps victim to (AssignedCategory, IsAccurate)
        private Dictionary<VictimController, (TriageCategory category, bool isAccurate)> victimTriageDecisions 
            = new Dictionary<VictimController, (TriageCategory, bool)>();

        private int discoveredVictimsCount = 0;
        private int stabilizedVictimsCount = 0;
        private int evacuatedVictimsCount = 0;
        private int deceasedVictimsCount = 0;

        private float missionStartTime = 0f;
        private float missionElapsedTime = 0f;
        private bool isMissionActive = false;

        public int TotalVictims => registeredVictims.Count;
        public int DiscoveredVictims => discoveredVictimsCount;
        public int TriagedVictims => victimTriageDecisions.Count;
        public int CorrectTriageDecisions
        {
            get
            {
                int count = 0;
                foreach (var kvp in victimTriageDecisions)
                {
                    if (kvp.Value.isAccurate) count++;
                }
                return count;
            }
        }

        public int StabilizedVictims => stabilizedVictimsCount;
        public int EvacuatedVictims => evacuatedVictimsCount;
        public int DeceasedVictims => deceasedVictimsCount;

        public int TotalHazards => registeredHazards.Count;
        public int ContainedHazards
        {
            get
            {
                int count = 0;
                for (int i = 0; i < registeredHazards.Count; i++)
                {
                    if (registeredHazards[i] != null && registeredHazards[i].IsContained) count++;
                }
                return count;
            }
        }

        public float ContainmentPercentage
        {
            get
            {
                if (TotalHazards == 0) return 100f;
                return ((float)ContainedHazards / TotalHazards) * 100f;
            }
        }

        public float MissionElapsedTime => missionElapsedTime;
        public bool IsMissionActive => isMissionActive;

        public event Action OnMissionDataUpdated;
        public event Action<MissionDebriefData> OnMissionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (isMissionActive)
            {
                missionElapsedTime = Time.time - missionStartTime;
                CheckMissionCompletion();
            }
        }

        public void StartMission()
        {
            isMissionActive = true;
            missionStartTime = Time.time;
            missionElapsedTime = 0f;
            discoveredVictimsCount = 0;
            stabilizedVictimsCount = 0;
            evacuatedVictimsCount = 0;
            deceasedVictimsCount = 0;
            victimTriageDecisions.Clear();

            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterVictim(VictimController victim)
        {
            if (!registeredVictims.Contains(victim))
            {
                registeredVictims.Add(victim);
                OnMissionDataUpdated?.Invoke();
            }
        }

        public void UnregisterVictim(VictimController victim)
        {
            registeredVictims.Remove(victim);
            victimTriageDecisions.Remove(victim);
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterHazard(HazardBase hazard)
        {
            if (!registeredHazards.Contains(hazard))
            {
                registeredHazards.Add(hazard);
                OnMissionDataUpdated?.Invoke();
            }
        }

        public void UnregisterHazard(HazardBase hazard)
        {
            registeredHazards.Remove(hazard);
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterVictimDiscovered(VictimController victim)
        {
            if (!victim.IsDiscovered)
            {
                discoveredVictimsCount++;
                OnMissionDataUpdated?.Invoke();
            }
        }

        public void RegisterTriageDecision(VictimController victim, TriageCategory category, bool isAccurate)
        {
            victimTriageDecisions[victim] = (category, isAccurate);
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterVictimStabilized(VictimController victim)
        {
            stabilizedVictimsCount++;
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterVictimEvacuated(VictimController victim)
        {
            evacuatedVictimsCount++;
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterVictimDeceased(VictimController victim)
        {
            deceasedVictimsCount++;
            OnMissionDataUpdated?.Invoke();
        }

        public void RegisterHazardContained(HazardBase hazard)
        {
            OnMissionDataUpdated?.Invoke();
        }

        private void CheckMissionCompletion()
        {
            if (!isMissionActive) return;

            // Check if all hazards are contained and all victims are either evacuated or deceased
            bool allHazardsContained = (ContainedHazards >= TotalHazards);
            bool allVictimsResolved = (evacuatedVictimsCount + deceasedVictimsCount >= TotalVictims && TotalVictims > 0);

            if (allHazardsContained && allVictimsResolved)
            {
                EndMission();
            }
        }

        public void EndMission()
        {
            if (!isMissionActive) return;
            isMissionActive = false;

            MissionDebriefData debrief = CalculateDebrief();
            OnMissionCompleted?.Invoke(debrief);
        }

        public string GetTacticalPriorityGuidance()
        {
            // 1. High Threat Hazard Check (Uncontained gas leaks or roaring fires)
            for (int i = 0; i < registeredHazards.Count; i++)
            {
                var h = registeredHazards[i];
                if (h != null && !h.IsContained)
                {
                    if (h is GasLeakHazard)
                        return "<color=#FFAA00>[PRIORITY 1: GAS RUPTURE]</color> Turn isolation valve using <b>[3] Spreader</b> to prevent explosion!";
                    if (h is FireHazard)
                        return "<color=#FF5533>[PRIORITY 1: THERMAL HAZARD]</color> Douse active fire with <b>[2] Extinguisher</b>!";
                }
            }

            // 2. Trapped Victims Under Rubble
            for (int i = 0; i < registeredVictims.Count; i++)
            {
                var v = registeredVictims[i];
                if (v != null && !v.Vitals.isDeceased && !v.Vitals.isEvacuated && v.Vitals.isPinnedUnderRubble)
                {
                    return $"<color=#FF3333>[PRIORITY 2: TRAPPED CASUALTY]</color> {v.Vitals.victimName} pinned under concrete! Lift debris with <b>[3] Spreader</b>.";
                }
            }

            // 3. Airway or Severe Bleeding Crisis
            for (int i = 0; i < registeredVictims.Count; i++)
            {
                var v = registeredVictims[i];
                if (v != null && !v.Vitals.isDeceased && !v.Vitals.isEvacuated && !v.Vitals.isStabilized)
                {
                    if (v.Vitals.isAirwayObstructed)
                        return $"<color=#FF4444>[PRIORITY 3: AIRWAY OCCLUSION]</color> {v.Vitals.victimName} is asphyxiating! Clear airway with <b>[4] MedKit</b>.";
                    if (v.Vitals.hasActiveBleeding)
                        return $"<color=#FF2222>[PRIORITY 3: ARTERIAL HEMORRHAGE]</color> {v.Vitals.victimName} bleeding rapidly! Apply tourniquet with <b>[4] MedKit</b>.";
                }
            }

            // 4. Untriaged Casualties
            for (int i = 0; i < registeredVictims.Count; i++)
            {
                var v = registeredVictims[i];
                if (v != null && !v.Vitals.isDeceased && !v.Vitals.isEvacuated && v.Vitals.assignedCategory == TriageCategory.Unassigned)
                {
                    return $"<color=#00CCFF>[PRIORITY 4: START TRIAGE]</color> Evaluate and tag {v.Vitals.victimName} ([R]ed, [Y]ellow, [G]reen, [B]lack).";
                }
            }

            // 5. Unstabilized Casualties
            for (int i = 0; i < registeredVictims.Count; i++)
            {
                var v = registeredVictims[i];
                if (v != null && !v.Vitals.isDeceased && !v.Vitals.isEvacuated && !v.Vitals.isStabilized)
                {
                    return $"<color=#00FF88>[PRIORITY 5: STABILIZATION]</color> Hold Left-Click on {v.Vitals.victimName} with <b>[4] MedKit</b> to stabilize.";
                }
            }

            // 6. Evacuation of Stabilized Casualties
            for (int i = 0; i < registeredVictims.Count; i++)
            {
                var v = registeredVictims[i];
                if (v != null && !v.Vitals.isDeceased && !v.Vitals.isEvacuated && v.Vitals.isStabilized)
                {
                    return $"<color=#00FF88>[EVACUATION READY]</color> Click {v.Vitals.victimName} with <b>[4] MedKit</b> to deploy AI EMS gurney transport!";
                }
            }

            if (ContainedHazards < TotalHazards)
            {
                return "<color=#FFAA00>[HAZARD CONTROL]</color> Neutralize remaining environmental hazards to secure the scene.";
            }

            return "<color=#00FF88>[MISSION COMPLETE]</color> All casualties evacuated and hazards contained. Generating AAR...";
        }

        public MissionDebriefData CalculateDebrief()
        {
            var debrief = new MissionDebriefData();
            debrief.elapsedTimeSeconds = missionElapsedTime;
            debrief.totalVictims = TotalVictims;
            debrief.evacuatedVictims = evacuatedVictimsCount;
            debrief.deceasedVictims = deceasedVictimsCount;
            debrief.totalHazards = TotalHazards;
            debrief.containedHazards = ContainedHazards;

            // Scoring based on unique casualty triage decisions
            int totalTriaged = victimTriageDecisions.Count;
            int correctTriage = CorrectTriageDecisions;
            float triageAccuracy = (totalTriaged > 0) ? ((float)correctTriage / totalTriaged) * 100f : 0f;
            float survivalRate = (TotalVictims > 0) ? ((float)evacuatedVictimsCount / TotalVictims) * 100f : 100f;
            float containmentRate = ContainmentPercentage;
            float timeScore = Mathf.Clamp01(1f - (missionElapsedTime / 300f)) * 100f;

            debrief.triageAccuracyPercent = triageAccuracy;
            debrief.overallScore = Mathf.RoundToInt((triageAccuracy * 0.35f) + (survivalRate * 0.35f) + (containmentRate * 0.20f) + (timeScore * 0.10f));

            if (debrief.overallScore >= 92 && debrief.deceasedVictims == 0) debrief.ratingGrade = "S";
            else if (debrief.overallScore >= 80) debrief.ratingGrade = "A";
            else if (debrief.overallScore >= 68) debrief.ratingGrade = "B";
            else if (debrief.overallScore >= 50) debrief.ratingGrade = "C";
            else debrief.ratingGrade = "F";

            debrief.debriefSummary = $"Incident resolved in {missionElapsedTime:F1}s. Rescued {evacuatedVictimsCount}/{TotalVictims} casualties with {triageAccuracy:F0}% triage accuracy. All active hazards neutralized.";

            return debrief;
        }

        public void ResetMission()
        {
            registeredVictims.Clear();
            registeredHazards.Clear();
            victimTriageDecisions.Clear();
            isMissionActive = false;
            missionElapsedTime = 0f;
            discoveredVictimsCount = 0;
            stabilizedVictimsCount = 0;
            evacuatedVictimsCount = 0;
            deceasedVictimsCount = 0;
            OnMissionDataUpdated?.Invoke();
        }
    }

    [System.Serializable]
    public class MissionDebriefData
    {
        public float elapsedTimeSeconds;
        public int overallScore;
        public string ratingGrade = "A";
        public float triageAccuracyPercent;
        public int totalVictims;
        public int evacuatedVictims;
        public int deceasedVictims;
        public int totalHazards;
        public int containedHazards;
        public string debriefSummary;
    }
}

