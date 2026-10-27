using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DisasterResponse.Core;
using DisasterResponse.Tools;
using DisasterResponse.Victims;

namespace DisasterResponse.UI
{
    public class ICSHUDController : MonoBehaviour
    {
        [Header("Command Dashboard Texts")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI casualtyStatusText;
        [SerializeField] private TextMeshProUGUI containmentText;
        [SerializeField] private Slider containmentSlider;
        [SerializeField] private TextMeshProUGUI objectiveTickerText;

        [Header("Tool Selector Buttons")]
        [SerializeField] private Button scannerButton;
        [SerializeField] private Button extinguisherButton;
        [SerializeField] private Button spreaderButton;
        [SerializeField] private Button medicalKitButton;

        [Header("Triage Tag Selector Panel & Buttons")]
        [SerializeField] private GameObject triagePanel;
        [SerializeField] private Button tagRedButton;
        [SerializeField] private Button tagYellowButton;
        [SerializeField] private Button tagGreenButton;
        [SerializeField] private Button tagBlackButton;

        [Header("Selected Highlights")]
        [SerializeField] private Image scannerHighlight;
        [SerializeField] private Image extinguisherHighlight;
        [SerializeField] private Image spreaderHighlight;
        [SerializeField] private Image medicalKitHighlight;

        private void Start()
        {
            // Subscribe to ICS updates
            if (IncidentCommandSystem.Instance != null)
            {
                IncidentCommandSystem.Instance.OnMissionDataUpdated += RefreshDashboard;
            }

            // Bind tool buttons
            if (scannerButton != null) scannerButton.onClick.AddListener(() => SelectTool(ResponderToolType.ThermalScanner));
            if (extinguisherButton != null) extinguisherButton.onClick.AddListener(() => SelectTool(ResponderToolType.FireExtinguisher));
            if (spreaderButton != null) spreaderButton.onClick.AddListener(() => SelectTool(ResponderToolType.RescueSpreader));
            if (medicalKitButton != null) medicalKitButton.onClick.AddListener(() => SelectTool(ResponderToolType.MedicalKit));

            // Bind triage tag buttons
            if (tagRedButton != null) tagRedButton.onClick.AddListener(() => SelectTriageTag(TriageCategory.Immediate));
            if (tagYellowButton != null) tagYellowButton.onClick.AddListener(() => SelectTriageTag(TriageCategory.Delayed));
            if (tagGreenButton != null) tagGreenButton.onClick.AddListener(() => SelectTriageTag(TriageCategory.Minimal));
            if (tagBlackButton != null) tagBlackButton.onClick.AddListener(() => SelectTriageTag(TriageCategory.Expectant));

            UpdateToolHighlights(ResponderToolType.ThermalScanner);
            RefreshDashboard();
        }

        private void OnDestroy()
        {
            if (IncidentCommandSystem.Instance != null)
            {
                IncidentCommandSystem.Instance.OnMissionDataUpdated -= RefreshDashboard;
            }
        }

        private float bannerHoldTimer = 0f;

        private void Update()
        {
            if (IncidentCommandSystem.Instance != null && IncidentCommandSystem.Instance.IsMissionActive)
            {
                float time = IncidentCommandSystem.Instance.MissionElapsedTime;
                int minutes = (int)(time / 60);
                int seconds = (int)(time % 60);
                if (timerText != null) timerText.text = $"<b>TIME:</b> {minutes:00}:{seconds:00}";

                // Dynamic Priority Guidance when no temporary banner is holding
                if (bannerHoldTimer > 0f)
                {
                    bannerHoldTimer -= Time.deltaTime;
                }
                else if (objectiveTickerText != null)
                {
                    objectiveTickerText.text = IncidentCommandSystem.Instance.GetTacticalPriorityGuidance();
                }
            }

            // Keyboard shortcuts using New Input System
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                // Tools [1, 2, 3, 4]
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SelectTool(ResponderToolType.ThermalScanner);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SelectTool(ResponderToolType.FireExtinguisher);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SelectTool(ResponderToolType.RescueSpreader);
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SelectTool(ResponderToolType.MedicalKit);

                // Quick Triage Tag Hotkeys: [R]ed, [Y]ellow, [G]reen, [B]lack
                if (ToolController.Instance != null && ToolController.Instance.ActiveTool == ResponderToolType.MedicalKit)
                {
                    if (kb.rKey.wasPressedThisFrame) SelectTriageTag(TriageCategory.Immediate);
                    if (kb.yKey.wasPressedThisFrame) SelectTriageTag(TriageCategory.Delayed);
                    if (kb.gKey.wasPressedThisFrame) SelectTriageTag(TriageCategory.Minimal);
                    if (kb.bKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame) SelectTriageTag(TriageCategory.Expectant);
                }
            }
        }

        private void SelectTool(ResponderToolType tool)
        {
            ToolController.Instance?.SelectTool(tool);
            UpdateToolHighlights(tool);

            // Show triage tag selector only when Medical Kit is selected
            if (triagePanel != null)
            {
                triagePanel.SetActive(tool == ResponderToolType.MedicalKit);
            }
        }

        private void SelectTriageTag(TriageCategory tag)
        {
            ToolController.Instance?.SetSelectedTriageTag(tag);
            UpdatePromptBanner($"<color=#00FFCC>[TAG SELECTED: {tag.ToString().ToUpper()}]</color> Click casualty to validate and assign.");
        }

        private void UpdateToolHighlights(ResponderToolType activeTool)
        {
            if (scannerHighlight != null) scannerHighlight.enabled = (activeTool == ResponderToolType.ThermalScanner);
            if (extinguisherHighlight != null) extinguisherHighlight.enabled = (activeTool == ResponderToolType.FireExtinguisher);
            if (spreaderHighlight != null) spreaderHighlight.enabled = (activeTool == ResponderToolType.RescueSpreader);
            if (medicalKitHighlight != null) medicalKitHighlight.enabled = (activeTool == ResponderToolType.MedicalKit);

            UpdateObjectiveInstructions(activeTool);
        }

        private void UpdateObjectiveInstructions(ResponderToolType tool)
        {
            if (objectiveTickerText == null) return;

            switch (tool)
            {
                case ResponderToolType.ThermalScanner:
                    objectiveTickerText.text = "<b>SCANNER EQUIPPED:</b> Point and click to reveal victim vitals and detect hazards.";
                    break;
                case ResponderToolType.FireExtinguisher:
                    objectiveTickerText.text = "<b>EXTINGUISHER EQUIPPED:</b> Aim and hold mouse/trigger to douse active flames.";
                    break;
                case ResponderToolType.RescueSpreader:
                    objectiveTickerText.text = "<b>RESCUE SPREADER EQUIPPED:</b> Clear heavy rubble off victims and turn shutoff valves.";
                    break;
                case ResponderToolType.MedicalKit:
                    objectiveTickerText.text = "<b>FIRST AID KIT EQUIPPED:</b> Select triage tag, apply treatment, click again to evacuate.";
                    break;
            }
        }

        public void RefreshDashboard()
        {
            var ics = IncidentCommandSystem.Instance;
            if (ics == null) return;

            if (casualtyStatusText != null)
            {
                casualtyStatusText.text = $"<b>CASUALTIES:</b> {ics.EvacuatedVictims}/{ics.TotalVictims} Evacuated  |  {ics.StabilizedVictims} Stabilized  |  {ics.TriagedVictims} Triaged";
            }

            if (containmentText != null)
            {
                containmentText.text = $"<b>HAZARD CONTAINMENT:</b> {ics.ContainmentPercentage:F0}% ({ics.ContainedHazards}/{ics.TotalHazards})";
            }

            if (containmentSlider != null)
            {
                containmentSlider.value = ics.ContainmentPercentage / 100f;
            }
        }

        public void UpdatePromptBanner(string message)
        {
            bannerHoldTimer = 3.5f;
            if (objectiveTickerText != null)
            {
                objectiveTickerText.text = message;
            }
        }
    }
}
