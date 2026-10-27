using UnityEngine;

namespace DisasterResponse.Victims
{
    public class VictimChestBreathing : MonoBehaviour
    {
        private VictimController victim;
        private Transform torsoTransform;
        private Vector3 initialTorsoScale;
        private Vector3 initialTorsoPos;

        private void Start()
        {
            victim = GetComponent<VictimController>();
            torsoTransform = transform.Find("Torso");
            if (torsoTransform != null)
            {
                initialTorsoScale = torsoTransform.localScale;
                initialTorsoPos = torsoTransform.localPosition;
            }
        }

        private void Update()
        {
            if (victim == null || torsoTransform == null) return;
            if (victim.Vitals.isDeceased || victim.Vitals.isEvacuated)
            {
                torsoTransform.localScale = initialTorsoScale;
                return;
            }

            // Frequency based on real-time breaths per minute
            float bpm = Mathf.Max(6f, victim.Vitals.respiratoryRate);
            float freq = (bpm / 60f) * Mathf.PI * 2f;

            // Chest expansion simulation
            float expansion = Mathf.Sin(Time.time * freq) * 0.08f;
            if (victim.Vitals.isAirwayObstructed)
            {
                // Shallow, labored, irregular breathing
                expansion *= 0.4f;
            }

            torsoTransform.localScale = new Vector3(
                initialTorsoScale.x * (1f + expansion * 0.5f),
                initialTorsoScale.y,
                initialTorsoScale.z * (1f + expansion));

            torsoTransform.localPosition = initialTorsoPos + Vector3.up * (expansion * 0.02f);
        }
    }
}
