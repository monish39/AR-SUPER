using System;
using UnityEngine;
using DisasterResponse.Hazards;
using DisasterResponse.Victims;
using DisasterResponse.AudioVFX;

namespace DisasterResponse.Tools
{
    public enum ResponderToolType
    {
        ThermalScanner = 0,
        FireExtinguisher = 1,
        RescueSpreader = 2,
        MedicalKit = 3
    }

    public class ToolController : MonoBehaviour
    {
        public static ToolController Instance { get; private set; }

        [Header("Current Tool")]
        [SerializeField] private ResponderToolType activeTool = ResponderToolType.ThermalScanner;

        [Header("Tool Sub-Components")]
        [SerializeField] private ThermalScannerTool scannerTool;
        [SerializeField] private ExtinguisherTool extinguisherTool;
        [SerializeField] private RescueSpreaderTool spreaderTool;
        [SerializeField] private MedicalKitTool medicalKitTool;

        [Header("Selected Triage Tag To Apply")]
        [SerializeField] private TriageCategory selectedTriageTag = TriageCategory.Unassigned;

        public ResponderToolType ActiveTool => activeTool;
        public TriageCategory SelectedTriageTag => selectedTriageTag;

        public event Action<ResponderToolType> OnToolChanged;
        public event Action<TriageCategory> OnTriageTagSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (scannerTool == null) scannerTool = GetComponentInChildren<ThermalScannerTool>();
            if (extinguisherTool == null) extinguisherTool = GetComponentInChildren<ExtinguisherTool>();
            if (spreaderTool == null) spreaderTool = GetComponentInChildren<RescueSpreaderTool>();
            if (medicalKitTool == null) medicalKitTool = GetComponentInChildren<MedicalKitTool>();

            SelectTool(activeTool);
        }

        public void SelectTool(ResponderToolType newTool)
        {
            activeTool = newTool;

            if (scannerTool != null) scannerTool.SetActive(activeTool == ResponderToolType.ThermalScanner);
            if (extinguisherTool != null) extinguisherTool.SetActive(activeTool == ResponderToolType.FireExtinguisher);
            if (spreaderTool != null) spreaderTool.SetActive(activeTool == ResponderToolType.RescueSpreader);
            if (medicalKitTool != null) medicalKitTool.SetActive(activeTool == ResponderToolType.MedicalKit);

            OnToolChanged?.Invoke(activeTool);
            DisasterAudioEngine.Instance?.PlayToolEquipSound();
        }

        public void SetSelectedTriageTag(TriageCategory tag)
        {
            selectedTriageTag = tag;
            OnTriageTagSelected?.Invoke(selectedTriageTag);
        }

        public void PerformToolAction(RaycastHit hitInfo, bool isContinuousHold, float deltaTime)
        {
            switch (activeTool)
            {
                case ResponderToolType.ThermalScanner:
                    if (scannerTool != null) scannerTool.ExecuteScan(hitInfo);
                    break;
                case ResponderToolType.FireExtinguisher:
                    if (extinguisherTool != null) extinguisherTool.ExecuteExtinguish(hitInfo, isContinuousHold, deltaTime);
                    break;
                case ResponderToolType.RescueSpreader:
                    if (spreaderTool != null) spreaderTool.ExecuteSpread(hitInfo, isContinuousHold, deltaTime);
                    break;
                case ResponderToolType.MedicalKit:
                    if (medicalKitTool != null) medicalKitTool.ExecuteMedical(hitInfo, selectedTriageTag, isContinuousHold, deltaTime);
                    break;
            }
        }

        public void StopContinuousToolAction()
        {
            if (extinguisherTool != null) extinguisherTool.StopExtinguishing();
            if (spreaderTool != null) spreaderTool.StopSpreading();
            if (medicalKitTool != null) medicalKitTool.StopMedicalAction();
        }
    }
}
