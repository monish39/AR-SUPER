using UnityEngine;
using DisasterResponse.Victims;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Hazards
{
    public class RubbleObstacle : HazardBase
    {
        [Header("Obstacle Properties")]
        [SerializeField] private float requiredClearanceEffort = 3.0f; // seconds of spreader tool application
        [SerializeField] private VictimController pinnedVictim;
        [SerializeField] private Rigidbody debrisRigidbody;
        [SerializeField] private ParticleSystem dustParticles;

        private float currentEffort = 0f;
        private bool isCleared = false;

        protected override void Start()
        {
            hazardName = "Heavy Concrete Debris / Structural Collapse";
            base.Start();

            if (debrisRigidbody == null) debrisRigidbody = GetComponent<Rigidbody>();
            if (pinnedVictim != null)
            {
                pinnedVictim.SetPinned(true);
            }
        }

        public void BindVictim(VictimController victim)
        {
            pinnedVictim = victim;
            if (pinnedVictim != null)
            {
                pinnedVictim.SetPinned(true);
            }
        }

        public override void Mitigate(float amount)
        {
            if (isCleared) return;

            currentEffort += amount;
            hazardIntensity = Mathf.Clamp01(1f - (currentEffort / requiredClearanceEffort));

            // Dynamically lift and tilt the heavy pillar as jaws spread
            transform.localPosition += Vector3.up * (amount * 0.12f);
            transform.Rotate(Vector3.forward, amount * 8f, Space.Self);

            if (dustParticles != null && !dustParticles.isPlaying)
            {
                dustParticles.Play();
            }

            DisasterAudioEngine.Instance?.PlaySpreaderWorkSound();

            if (currentEffort >= requiredClearanceEffort)
            {
                ClearRubble();
            }
        }

        private void ClearRubble()
        {
            isCleared = true;
            hazardIntensity = 0f;

            if (pinnedVictim != null)
            {
                pinnedVictim.SetPinned(false);
            }

            if (debrisRigidbody != null)
            {
                debrisRigidbody.isKinematic = false;
                debrisRigidbody.AddForce(Vector3.up * 3f + Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
                debrisRigidbody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            else
            {
                // Smoothly lift and fade/shift
                transform.position += Vector3.up * 0.6f;
            }

            MarkContained();
            DisasterAudioEngine.Instance?.PlayRubbleClearedSound();
        }
    }
}
