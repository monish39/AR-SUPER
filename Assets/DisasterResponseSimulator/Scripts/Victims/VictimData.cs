using System;
using UnityEngine;

namespace DisasterResponse.Victims
{
    public enum TriageCategory
    {
        Unassigned = 0,
        Immediate = 1,   // RED: Critical / High priority
        Delayed = 2,     // YELLOW: Serious, temporarily stable
        Minimal = 3,     // GREEN: Minor / Walking wounded
        Expectant = 4    // BLACK: Non-viable / Deceased
    }

    public enum ConsciousnessLevel
    {
        Alert,
        Verbal,
        Pain,
        Unresponsive
    }

    public enum InjuryType
    {
        SevereHemorrhage,
        SmokeInhalation,
        CrushInjury,
        SevereBurns,
        BluntTrauma,
        Fracture,
        MinorLacerations
    }

    [System.Serializable]
    public class VictimVitals
    {
        public string victimName = "Casualty";
        public int heartRate = 85;               // bpm
        public float bloodOxygen = 96f;          // SpO2 %
        public int systolicBP = 118;             // mmHg
        public int respiratoryRate = 18;         // breaths/min
        public ConsciousnessLevel consciousness = ConsciousnessLevel.Alert;
        public InjuryType primaryInjury = InjuryType.MinorLacerations;
        
        public bool isPinnedUnderRubble = false;
        public bool isAirwayObstructed = false;
        public bool hasActiveBleeding = false;

        public TriageCategory trueCorrectCategory = TriageCategory.Minimal;
        public TriageCategory assignedCategory = TriageCategory.Unassigned;

        public float healthPercentage = 100f;    // 0 to 100
        public bool isDeceased = false;
        public bool isStabilized = false;
        public bool isEvacuated = false;

        public string GetInjuryDescription()
        {
            switch (primaryInjury)
            {
                case InjuryType.SevereHemorrhage:
                    return "Arterial Bleeding - Rapid Blood Loss";
                case InjuryType.SmokeInhalation:
                    return "Smoke Inhalation - Airway Compromised";
                case InjuryType.CrushInjury:
                    return "Crush Syndrome - Trapped Extremity";
                case InjuryType.SevereBurns:
                    return "2nd/3rd Degree Thermal Burns";
                case InjuryType.BluntTrauma:
                    return "Severe Blunt Trauma / Torso Impact";
                case InjuryType.Fracture:
                    return "Compound Limb Fracture";
                case InjuryType.MinorLacerations:
                default:
                    return "Superficial Abrasions & Shock";
            }
        }
    }
}
