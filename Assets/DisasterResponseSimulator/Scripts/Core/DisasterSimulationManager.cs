using System;
using UnityEngine;
using DisasterResponse.Environment;

namespace DisasterResponse.Core
{
    public enum DisasterScenarioType
    {
        EarthquakeCollapse = 0,
        GasLeakAndFire = 1,
        HospitalMassCasualty = 2
    }

    public class DisasterSimulationManager : MonoBehaviour
    {
        public static DisasterSimulationManager Instance { get; private set; }

        [Header("Current Configuration")]
        [SerializeField] private DisasterScenarioType activeScenario = DisasterScenarioType.EarthquakeCollapse;
        [SerializeField] private bool isTabletopMode = false;
        [SerializeField] private float tabletopScale = 0.5f;
        [SerializeField] private float roomScale = 1.0f;

        [Header("Scene References")]
        [SerializeField] private DisasterScenarioBuilder scenarioBuilder;
        [SerializeField] private Transform disasterZoneRoot;

        private bool isZonePlaced = false;
        private Vector3 currentAnchorPosition = Vector3.zero;
        private Quaternion currentAnchorRotation = Quaternion.identity;

        public DisasterScenarioType ActiveScenario => activeScenario;
        public bool IsTabletopMode => isTabletopMode;
        public bool IsZonePlaced => isZonePlaced;
        public Transform DisasterZoneRoot => disasterZoneRoot;

        public event Action<DisasterScenarioType> OnScenarioChanged;
        public event Action<Vector3> OnDisasterZonePlaced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (disasterZoneRoot == null)
            {
                var existing = GameObject.Find("DisasterZoneRoot");
                if (existing != null)
                {
                    disasterZoneRoot = existing.transform;
                }
                else
                {
                    var newRoot = new GameObject("DisasterZoneRoot");
                    disasterZoneRoot = newRoot.transform;
                }
            }

            if (scenarioBuilder == null)
            {
                scenarioBuilder = GetComponentInChildren<DisasterScenarioBuilder>();
                if (scenarioBuilder == null)
                {
                    scenarioBuilder = gameObject.AddComponent<DisasterScenarioBuilder>();
                }
            }
        }

        public void PlaceDisasterZone(Vector3 worldPosition, Quaternion rotation)
        {
            EnsureInitialized();
            currentAnchorPosition = worldPosition;
            currentAnchorRotation = rotation;

            if (disasterZoneRoot != null)
            {
                disasterZoneRoot.position = worldPosition;
                disasterZoneRoot.rotation = rotation;
                float targetScale = isTabletopMode ? tabletopScale : roomScale;
                disasterZoneRoot.localScale = Vector3.one * targetScale;
            }

            isZonePlaced = true;
            BuildActiveScenario();

            IncidentCommandSystem.Instance?.StartMission();
            OnDisasterZonePlaced?.Invoke(worldPosition);
        }

        public void SetTabletopMode(bool tabletop)
        {
            isTabletopMode = tabletop;
            if (disasterZoneRoot != null && isZonePlaced)
            {
                float targetScale = isTabletopMode ? tabletopScale : roomScale;
                disasterZoneRoot.localScale = Vector3.one * targetScale;
            }
        }

        public void SelectScenario(DisasterScenarioType newScenario)
        {
            EnsureInitialized();
            activeScenario = newScenario;
            OnScenarioChanged?.Invoke(activeScenario);

            if (isZonePlaced)
            {
                BuildActiveScenario();
                IncidentCommandSystem.Instance?.StartMission();
            }
        }

        public void BuildActiveScenario()
        {
            EnsureInitialized();
            if (scenarioBuilder != null && disasterZoneRoot != null)
            {
                IncidentCommandSystem.Instance?.ResetMission();
                scenarioBuilder.BuildScenario(activeScenario, disasterZoneRoot);
            }
        }

        public void RestartCurrentScenario()
        {
            if (isZonePlaced)
            {
                BuildActiveScenario();
                IncidentCommandSystem.Instance?.StartMission();
            }
        }
    }
}
