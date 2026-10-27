using System;
using UnityEngine;

namespace DisasterResponse.Victims
{
    public enum STARTAssessmentStep
    {
        AmbulationCheck = 0,
        AirwayAssessment = 1,
        RespiratoryRateCheck = 2,
        PerfusionCapillaryCheck = 3,
        MentalStatusCheck = 4,
        EvaluationComplete = 5
    }

    public static class STARTTriageSystem
    {
        /// <summary>
        /// Evaluates a victim against the official FEMA Simple Triage and Rapid Treatment (START) algorithm.
        /// Returns the clinically correct Triage Category according to START protocols.
        /// </summary>
        public static TriageCategory DetermineCorrectCategory(VictimVitals vitals)
        {
            // 1. Spontaneous Breathing / Non-viable Check
            if (vitals.isDeceased)
            {
                return TriageCategory.Expectant; // BLACK: Non-viable / Expectant
            }

            // 2. Can the casualty ambulate (walk)?
            if (vitals.primaryInjury == InjuryType.MinorLacerations && !vitals.isPinnedUnderRubble && vitals.consciousness == ConsciousnessLevel.Alert)
            {
                return TriageCategory.Minimal; // GREEN: Walking wounded
            }

            // 3. Airway compromised? (Requires immediate positional maneuver / jaw-thrust)
            if (vitals.isAirwayObstructed)
            {
                return TriageCategory.Immediate; // RED: Airway compromise
            }

            // 4. Respiratory Rate critical threshold (> 30 bpm or < 10 bpm)
            if (vitals.respiratoryRate > 30 || vitals.respiratoryRate < 10)
            {
                return TriageCategory.Immediate; // RED: Tachypnea / Respiratory Failure
            }

            // 5. Perfusion & Hemorrhage Assessment (Active arterial bleeding, decompensating shock, low SpO2)
            if (vitals.hasActiveBleeding || vitals.systolicBP < 90 || vitals.bloodOxygen < 88f)
            {
                return TriageCategory.Immediate; // RED: Hemorrhagic Shock
            }

            // 6. Neurological / Mental Status Assessment (Inability to follow simple commands / altered sensorium)
            if (vitals.consciousness != ConsciousnessLevel.Alert)
            {
                return TriageCategory.Immediate; // RED: Altered Mental Status
            }

            // 7. Non-ambulatory due to serious fracture/crush, but normal breathing (10-30), pulse intact, alert
            return TriageCategory.Delayed; // YELLOW: Delayed care
        }

        /// <summary>
        /// Validates assigned category and returns clinical feedback on Under-Triage or Over-Triage.
        /// </summary>
        public static (bool isAccurate, string clinicalFeedback) ValidateTriageDecision(TriageCategory assigned, VictimVitals vitals)
        {
            TriageCategory correct = DetermineCorrectCategory(vitals);

            if (assigned == correct)
            {
                string rationale = GetCategoryRationale(correct, vitals);
                return (true, $"ACCURATE [{correct.ToString().ToUpper()}]: {rationale}");
            }

            if ((int)assigned > (int)correct)
            {
                // Note: Immediate is 1, Delayed is 2, Minimal is 3, Expectant is 4.
                // If assigned = Delayed (2) but correct = Immediate (1), then assigned > correct => UNDER-TRIAGE!
                return (false, $"UNDER-TRIAGE DANGER: Assigned {assigned} but patient clinically qualifies for {correct}! Delay in critical intervention could cause fatal deterioration.");
            }
            else
            {
                return (false, $"OVER-TRIAGE: Assigned {assigned} but clinical indicators warrant {correct}. Resource consumption may deprive more critical casualties.");
            }
        }

        private static string GetCategoryRationale(TriageCategory cat, VictimVitals v)
        {
            switch (cat)
            {
                case TriageCategory.Immediate:
                    if (v.isAirwayObstructed) return "Compromised airway requires immediate jaw-thrust / opening.";
                    if (v.hasActiveBleeding) return "Severe arterial hemorrhage requires immediate tourniquet.";
                    if (v.respiratoryRate > 30 || v.respiratoryRate < 10) return $"Abnormal respiratory rate ({v.respiratoryRate} bpm) indicates acute shock.";
                    if (v.bloodOxygen < 88f) return $"Severe hypoxia (SpO2 {v.bloodOxygen:F0}%) requires high-flow O2.";
                    return "Immediate life-saving resuscitation required.";

                case TriageCategory.Delayed:
                    return "Stable respiration, pulse present, mental status intact; non-ambulatory due to major trauma.";

                case TriageCategory.Minimal:
                    return "Ambulatory walking wounded with minor injuries; can await delayed care.";

                case TriageCategory.Expectant:
                    return "No spontaneous respiration or signs of life after airway intervention.";

                default:
                    return "Triage evaluation incomplete.";
            }
        }
    }
}

