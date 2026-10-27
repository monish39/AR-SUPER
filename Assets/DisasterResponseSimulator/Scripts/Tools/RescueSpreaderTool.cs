using UnityEngine;
using DisasterResponse.Hazards;
using DisasterResponse.AudioVFX;
using DisasterResponse.UI;

namespace DisasterResponse.Tools
{
    public class RescueSpreaderTool : MonoBehaviour
    {
        [Header("Spreader Properties")]
        [SerializeField] private float clearanceSpeed = 1.4f; // units per second
        [SerializeField] private ParticleSystem sparkParticles;

        private bool isActive = false;
        private float lastSoundTime = 0f;

        public void SetActive(bool active)
        {
            isActive = active;
            if (!isActive) StopSpreading();
        }

        public void ExecuteSpread(RaycastHit hit, bool isHolding, float deltaTime)
        {
            if (!isActive) return;

            float effort = isHolding ? (clearanceSpeed * deltaTime) : 0.45f;

            // 1. Heavy Rubble / Collapsed Pillar Obstacle
            var rubble = hit.collider.GetComponentInParent<RubbleObstacle>();
            if (rubble != null && !rubble.IsContained)
            {
                rubble.Mitigate(effort);

                if (sparkParticles != null)
                {
                    sparkParticles.transform.position = hit.point;
                    if (!sparkParticles.isPlaying) sparkParticles.Play();
                }

                if (Time.time - lastSoundTime > 0.4f)
                {
                    lastSoundTime = Time.time;
                    DisasterAudioEngine.Instance?.PlaySpreaderWorkSound();
                }

                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#FFAA00>[HYDRAULIC JAWS ENGAGED]</color> Lifting structural concrete collapse... Hold to extract victim!");
                return;
            }

            // 2. Gas Leak Rupture Valve Handle
            var gasLeak = hit.collider.GetComponentInParent<GasLeakHazard>();
            if (gasLeak != null && !gasLeak.IsContained)
            {
                gasLeak.Mitigate(effort * 0.9f);

                if (Time.time - lastSoundTime > 0.35f)
                {
                    lastSoundTime = Time.time;
                    DisasterAudioEngine.Instance?.PlayValveTurnSound();
                }

                var hud = FindFirstObjectByType<ICSHUDController>();
                hud?.UpdatePromptBanner($"<color=#FFAA00>[VALVE SHUTOFF]</color> Turning industrial isolation wheel... Hold to seal gas line!");
                return;
            }
        }

        public void StopSpreading()
        {
            if (sparkParticles != null && sparkParticles.isPlaying)
            {
                sparkParticles.Stop();
            }
        }
    }
}
