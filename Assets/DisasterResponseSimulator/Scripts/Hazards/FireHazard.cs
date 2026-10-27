using UnityEngine;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Hazards
{
    public class FireHazard : HazardBase
    {
        [Header("Fire Properties")]
        [SerializeField] private float fuelRemaining = 100f;
        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private ParticleSystem fireParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private Light fireLight;
        [SerializeField] private AudioSource fireAudioSource;

        private float initialLightIntensity = 2.5f;

        protected override void Start()
        {
            hazardName = "Class A/B Structural Fire";
            base.Start();

            if (fireLight != null)
            {
                initialLightIntensity = fireLight.intensity;
            }

            if (fireAudioSource == null)
            {
                fireAudioSource = GetComponent<AudioSource>();
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (isContained) return;

            // Organic Perlin noise firelight flicker
            if (fireLight != null)
            {
                float flicker = Mathf.PerlinNoise(Time.time * 11f, transform.position.x) * 0.55f + 0.72f;
                fireLight.intensity = initialLightIntensity * hazardIntensity * flicker;
                fireLight.range = 4.5f + Mathf.PerlinNoise(transform.position.z, Time.time * 7f) * 1.5f;
            }
        }

        public override void Mitigate(float amount)
        {
            if (isContained) return;

            fuelRemaining -= amount;
            hazardIntensity = Mathf.Clamp01(fuelRemaining / maxFuel);
            UpdateVisuals();

            DisasterAudioEngine.Instance?.PlayFireSizzleSound();

            if (fuelRemaining <= 0f)
            {
                Extinguish();
            }
        }

        private void UpdateVisuals()
        {
            if (fireParticles != null)
            {
                var main = fireParticles.main;
                main.startSizeMultiplier = Mathf.Max(0.1f, hazardIntensity);
                var emission = fireParticles.emission;
                emission.rateOverTimeMultiplier = Mathf.Max(1f, 30f * hazardIntensity);
            }

            if (fireLight != null)
            {
                fireLight.intensity = initialLightIntensity * hazardIntensity;
                fireLight.enabled = hazardIntensity > 0.05f;
            }

            if (fireAudioSource != null)
            {
                fireAudioSource.volume = hazardIntensity * 0.7f;
            }
        }

        private void Extinguish()
        {
            fuelRemaining = 0f;
            hazardIntensity = 0f;
            UpdateVisuals();

            if (fireParticles != null) fireParticles.Stop();
            if (fireLight != null) fireLight.enabled = false;
            if (fireAudioSource != null) fireAudioSource.Stop();

            MarkContained();
            DisasterAudioEngine.Instance?.PlayExtinguishedSound();
        }
    }
}
