using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DisasterResponse.Core;
using DisasterResponse.AR;

namespace DisasterResponse.UI
{
    public class ScenarioSelectUI : MonoBehaviour
    {
        [Header("Scenario Buttons")]
        [SerializeField] private Button earthquakeScenarioButton;
        [SerializeField] private Button fireGasScenarioButton;
        [SerializeField] private Button hospitalScenarioButton;

        [Header("Mode & Controls")]
        [SerializeField] private Toggle tabletopToggle;
        [SerializeField] private Button repositionZoneButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button webcamToggleButton;
        [SerializeField] private Button switchCameraButton;
        [SerializeField] private Button thermalVisionButton;

        private void Start()
        {
            if (thermalVisionButton != null)
            {
                thermalVisionButton.onClick.AddListener(() =>
                {
                    Tools.FLIRThermalVisionController.Instance?.ToggleThermalVision();
                });
            }
            if (earthquakeScenarioButton != null)
            {
                earthquakeScenarioButton.onClick.AddListener(() => ChangeScenario(DisasterScenarioType.EarthquakeCollapse));
            }

            if (fireGasScenarioButton != null)
            {
                fireGasScenarioButton.onClick.AddListener(() => ChangeScenario(DisasterScenarioType.GasLeakAndFire));
            }

            if (hospitalScenarioButton != null)
            {
                hospitalScenarioButton.onClick.AddListener(() => ChangeScenario(DisasterScenarioType.HospitalMassCasualty));
            }

            if (tabletopToggle != null)
            {
                tabletopToggle.isOn = DisasterSimulationManager.Instance != null && DisasterSimulationManager.Instance.IsTabletopMode;
                tabletopToggle.onValueChanged.AddListener((isOn) =>
                {
                    DisasterSimulationManager.Instance?.SetTabletopMode(isOn);
                });
            }

            if (repositionZoneButton != null)
            {
                repositionZoneButton.onClick.AddListener(RepositionZone);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartMission);
            }

            if (webcamToggleButton != null)
            {
                webcamToggleButton.onClick.AddListener(() =>
                {
                    ARWebCamBackground.Instance?.ToggleWebCam();
                });
            }

            if (switchCameraButton != null)
            {
                switchCameraButton.onClick.AddListener(() =>
                {
                    ARWebCamBackground.Instance?.SwitchCamera();
                });
            }
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                // [C] Toggle WebCam feed
                if (kb.cKey.wasPressedThisFrame)
                {
                    ARWebCamBackground.Instance?.ToggleWebCam();
                }

                // [V] Toggle FLIR Thermal Vision
                if (kb.vKey.wasPressedThisFrame)
                {
                    Tools.FLIRThermalVisionController.Instance?.ToggleThermalVision();
                }
            }
        }

        private void ChangeScenario(DisasterScenarioType scenario)
        {
            DisasterSimulationManager.Instance?.SelectScenario(scenario);
        }

        private void RepositionZone()
        {
            if (DisasterSimulationManager.Instance != null)
            {
                DisasterSimulationManager.Instance.RestartCurrentScenario();
            }
        }

        private void RestartMission()
        {
            DisasterSimulationManager.Instance?.RestartCurrentScenario();
        }
    }
}
