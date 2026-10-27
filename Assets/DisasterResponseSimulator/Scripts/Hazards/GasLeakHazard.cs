using UnityEngine;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Hazards
{
    public class GasLeakHazard : HazardBase
    {
        [Header("Gas Properties")]
        [SerializeField] private float valveTurnsRemaining = 3f;
        [SerializeField] private float maxValveTurns = 3f;
        [SerializeField] private Transform valveHandle;
        [SerializeField] private ParticleSystem gasParticles;
        [SerializeField] private AudioSource hissAudioSource;

        private float rotationProgress = 0f;

        protected override void Start()
        {
            hazardName = "Pressurized Methane / Gas Line Rupture";
            base.Start();

            if (hissAudioSource == null) hissAudioSource = GetComponent<AudioSource>();
            UpdateGasVisuals();
        }

        public override void Mitigate(float amount)
        {
            if (isContained) return;

            valveTurnsRemaining -= amount;
            rotationProgress += amount * 120f;

            if (valveHandle != null)
            {
                valveHandle.Rotate(Vector3.up, amount * 120f, Space.Self);
            }

            hazardIntensity = Mathf.Clamp01(valveTurnsRemaining / maxValveTurns);
            UpdateGasVisuals();

            DisasterAudioEngine.Instance?.PlayValveTurnSound();

            if (valveTurnsRemaining <= 0f)
            {
                ShutoffGas();
            }
        }

        private void UpdateGasVisuals()
        {
            if (gasParticles != null)
            {
                var main = gasParticles.main;
                main.startSizeMultiplier = Mathf.Max(0.1f, hazardIntensity);
                var emission = gasParticles.emission;
                emission.rateOverTimeMultiplier = Mathf.Max(0f, 40f * hazardIntensity);
            }

            if (hissAudioSource != null)
            {
                hissAudioSource.volume = hazardIntensity * 0.8f;
                hissAudioSource.pitch = 0.8f + (hazardIntensity * 0.4f);
            }
        }

        private void ShutoffGas()
        {
            valveTurnsRemaining = 0f;
            hazardIntensity = 0f;
            UpdateGasVisuals();

            if (gasParticles != null) gasParticles.Stop();
            if (hissAudioSource != null) hissAudioSource.Stop();

            MarkContained();
            DisasterAudioEngine.Instance?.PlayValveLockedSound();
        }
    }
}
