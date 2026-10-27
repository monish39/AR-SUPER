using System;
using UnityEngine;
using DisasterResponse.Core;

namespace DisasterResponse.Hazards
{
    public abstract class HazardBase : MonoBehaviour
    {
        [Header("Hazard Attributes")]
        [SerializeField] protected string hazardName = "Hazard";
        [SerializeField] protected float threatRadius = 3.0f;
        [SerializeField] protected float hazardIntensity = 1.0f; // 0 (contained) to 1.0 (full threat)
        [SerializeField] protected bool isContained = false;

        public string HazardName => hazardName;
        public float ThreatRadius => threatRadius;
        public float HazardIntensity => hazardIntensity;
        public bool IsContained => isContained;

        public event Action<HazardBase> OnHazardContained;
        public event Action<HazardBase> OnHazardIntensified;

        protected virtual void Start()
        {
            IncidentCommandSystem.Instance?.RegisterHazard(this);
        }

        protected virtual void OnDestroy()
        {
            IncidentCommandSystem.Instance?.UnregisterHazard(this);
        }

        public abstract void Mitigate(float amount);

        protected virtual void MarkContained()
        {
            if (!isContained)
            {
                isContained = true;
                hazardIntensity = 0f;
                OnHazardContained?.Invoke(this);
                IncidentCommandSystem.Instance?.RegisterHazardContained(this);
            }
        }

        protected virtual void TriggerIntensified()
        {
            OnHazardIntensified?.Invoke(this);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = isContained ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, threatRadius);
        }
    }
}
