using UnityEngine;
using DisasterResponse.Hazards;
using DisasterResponse.AudioVFX;
using DisasterResponse.UI;

namespace DisasterResponse.Tools
{
    public class ExtinguisherTool : MonoBehaviour
    {
        [Header("Extinguisher Properties")]
        [SerializeField] private ParticleSystem foamStreamParticles;
        [SerializeField] private float extinguishRate = 42f; // fuel per second
        [SerializeField] private AudioSource sprayAudioSource;

        private bool isActive = false;
        private bool isSpraying = false;

        public void SetActive(bool active)
        {
            isActive = active;
            if (!isActive) StopExtinguishing();
        }

        public void ExecuteExtinguish(RaycastHit hit, bool isHolding, float deltaTime)
        {
            if (!isActive) return;

            if (foamStreamParticles != null)
            {
                if (!foamStreamParticles.isPlaying) foamStreamParticles.Play();
                foamStreamParticles.transform.LookAt(hit.point);
            }

            if (!isSpraying)
            {
                isSpraying = true;
                DisasterAudioEngine.Instance?.StartExtinguisherLoop();
            }

            float effort = isHolding ? (extinguishRate * deltaTime) : 15f;

            var fire = hit.collider.GetComponentInParent<FireHazard>();
            if (fire != null && !fire.IsContained)
            {
                fire.Mitigate(effort);

                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#00CCFF>[SUPPRESSION IN PROGRESS]</color> Extinguishing thermal hazard... Fuel at {fire.HazardIntensity * 100f:F0}%");
            }
        }

        public void StopExtinguishing()
        {
            isSpraying = false;
            if (foamStreamParticles != null && foamStreamParticles.isPlaying)
            {
                foamStreamParticles.Stop();
            }
            DisasterAudioEngine.Instance?.StopExtinguisherLoop();
        }
    }
}
