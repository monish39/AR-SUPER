using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DisasterResponse.Core;

namespace DisasterResponse.UI
{
    public class DebriefingModalUI : MonoBehaviour
    {
        [Header("Modal Panel")]
        [SerializeField] private GameObject modalRoot;

        [Header("Debriefing Texts")]
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI casualtyText;
        [SerializeField] private TextMeshProUGUI triageAccuracyText;
        [SerializeField] private TextMeshProUGUI hazardContainmentText;

        [Header("Actions")]
        [SerializeField] private Button restartMissionButton;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (modalRoot != null) modalRoot.SetActive(false);

            if (IncidentCommandSystem.Instance != null)
            {
                IncidentCommandSystem.Instance.OnMissionCompleted += ShowDebriefing;
            }

            if (restartMissionButton != null)
            {
                restartMissionButton.onClick.AddListener(RestartMission);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseModal);
            }
        }

        private void OnDestroy()
        {
            if (IncidentCommandSystem.Instance != null)
            {
                IncidentCommandSystem.Instance.OnMissionCompleted -= ShowDebriefing;
            }
        }

        public void ShowDebriefing(MissionDebriefData debrief)
        {
            if (modalRoot == null) return;
            modalRoot.SetActive(true);

            if (gradeText != null)
            {
                gradeText.text = debrief.ratingGrade;
                gradeText.color = GetGradeColor(debrief.ratingGrade);
            }

            if (scoreText != null) scoreText.text = $"Tactical Efficiency: <b>{debrief.overallScore}%</b>";
            if (summaryText != null) summaryText.text = debrief.debriefSummary;
            
            int min = (int)(debrief.elapsedTimeSeconds / 60);
            int sec = (int)(debrief.elapsedTimeSeconds % 60);
            if (timeText != null) timeText.text = $"Time: <b>{min:00}:{sec:00}</b>";

            if (casualtyText != null)
            {
                casualtyText.text = $"Rescued: <b>{debrief.evacuatedVictims}/{debrief.totalVictims}</b> | Fatalities: <b>{debrief.deceasedVictims}</b>";
            }

            if (triageAccuracyText != null)
            {
                triageAccuracyText.text = $"Triage Accuracy: <b>{debrief.triageAccuracyPercent:F0}%</b>";
            }

            if (hazardContainmentText != null)
            {
                hazardContainmentText.text = $"Hazards Contained: <b>{debrief.containedHazards}/{debrief.totalHazards}</b>";
            }
        }

        private Color GetGradeColor(string grade)
        {
            switch (grade)
            {
                case "S": return new Color(1f, 0.85f, 0.2f);
                case "A": return new Color(0.2f, 0.9f, 0.3f);
                case "B": return new Color(0.2f, 0.7f, 1f);
                case "C": return new Color(1f, 0.6f, 0.1f);
                default: return new Color(0.9f, 0.2f, 0.2f);
            }
        }

        private void RestartMission()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            DisasterSimulationManager.Instance?.RestartCurrentScenario();
        }

        private void CloseModal()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }
    }
}
