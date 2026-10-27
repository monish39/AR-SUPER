using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DisasterResponse.AR;
using DisasterResponse.Core;
using DisasterResponse.Tools;
using DisasterResponse.UI;
using DisasterResponse.AudioVFX;
using DisasterResponse.Environment;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DisasterResponse.Editor
{
    [InitializeOnLoad]
    public static class DisasterSimulatorSceneSetup
    {
        static DisasterSimulatorSceneSetup()
        {
            EditorApplication.delayCall += CheckAutoSetup;
        }

        private static void CheckAutoSetup()
        {
            if (!System.IO.File.Exists("Assets/Scenes/DisasterResponseSimulator.unity"))
            {
                BuildSceneInternal(false);
            }
        }

        [MenuItem("Disaster Simulator/Setup AR Simulation Scene", false, 10)]
        public static void SetupScene()
        {
            BuildSceneInternal(true);
        }

        public static void BuildSceneInternal(bool showDialog)
        {
            // 1. Setup Camera and AR Rig
            var arRigGo = new GameObject("AR_Simulator_Rig");
            var arRig = arRigGo.AddComponent<ARSimulatorRig>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.SetParent(arRigGo.transform, false);
            cam.transform.localPosition = new Vector3(0, 3.2f, -6.5f);
            cam.transform.localRotation = Quaternion.Euler(24f, 0, 0);

            // Add Reticle (clean GameObject with ARPlacementReticle, no obstructive primitive cylinder)
            var reticleGo = new GameObject("AR_Placement_Reticle");
            var reticleComp = reticleGo.AddComponent<ARPlacementReticle>();
            reticleComp.SetMode(0);

            var reticleField = typeof(ARSimulatorRig).GetField("reticle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (reticleField != null) reticleField.SetValue(arRig, reticleComp);

            var camField = typeof(ARSimulatorRig).GetField("arCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camField != null) camField.SetValue(arRig, cam);

            // Add Interaction Raycaster and WebCam AR Passthrough
            arRigGo.AddComponent<ARInteractionRaycaster>();
            arRigGo.AddComponent<ARWebCamBackground>();

            // 2. Setup Core Managers
            var coreGo = new GameObject("Disaster_Simulation_Core");
            var simManager = coreGo.AddComponent<DisasterSimulationManager>();
            var ics = coreGo.AddComponent<IncidentCommandSystem>();
            var audioEngine = coreGo.AddComponent<DisasterAudioEngine>();
            var scenarioBuilder = coreGo.AddComponent<DisasterScenarioBuilder>();

            // 3. Setup Tool Controller
            var toolsGo = new GameObject("Responder_Tools");
            toolsGo.transform.SetParent(cam.transform, false);
            var toolController = toolsGo.AddComponent<ToolController>();

            var scanner = toolsGo.AddComponent<ThermalScannerTool>();
            var extinguisher = toolsGo.AddComponent<ExtinguisherTool>();
            var spreader = toolsGo.AddComponent<RescueSpreaderTool>();
            var medKit = toolsGo.AddComponent<MedicalKitTool>();

            // Attach foam particles to extinguisher
            var foamPs = ProceduralVFXBuilder.CreateFoamStreamParticles(toolsGo.transform);
            var foamField = typeof(ExtinguisherTool).GetField("foamStreamParticles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (foamField != null) foamField.SetValue(extinguisher, foamPs);

            // 4. Setup Lighting
            var existingLight = Object.FindFirstObjectByType<Light>();
            if (existingLight == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            }

            // 5. Setup UI Canvas & Tactical HUD
            SetupTacticalHUD(simManager, ics, toolController);

            // 6. Setup EventSystem
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }

            // Automatically place and initialize the zone on start!
            simManager.PlaceDisasterZone(new Vector3(0, 0, 1.5f), Quaternion.identity);

            // Save scene as Assets/Scenes/DisasterResponseSimulator.unity
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), "Assets/Scenes/DisasterResponseSimulator.unity");

            // Register scene in Build Settings
            var currentScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in currentScenes)
            {
                if (s.path == "Assets/Scenes/DisasterResponseSimulator.unity")
                {
                    alreadyInBuild = true;
                    break;
                }
            }

            if (!alreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
                for (int i = 0; i < currentScenes.Length; i++) newScenes[i] = currentScenes[i];
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene("Assets/Scenes/DisasterResponseSimulator.unity", true);
                EditorBuildSettings.scenes = newScenes;
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Disaster Simulator Ready",
                    "AR Disaster Response Simulator scene successfully generated and saved to 'Assets/Scenes/DisasterResponseSimulator.unity'!\n\nPress PLAY in the Editor to start. Use buttons or keys 1-4 to equip tools, scan casualties, douse fires, clear rubble, and review the incident debrief.",
                    "OK");
            }

            Debug.Log("[DisasterSimulator] AR Scene setup and saved to Assets/Scenes/DisasterResponseSimulator.unity!");
        }

        private static void SetupTacticalHUD(DisasterSimulationManager simManager, IncidentCommandSystem ics, ToolController tools)
        {
            var canvasGo = new GameObject("Tactical_ICS_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<ICSHUDController>();
            var scenarioUI = canvasGo.AddComponent<ScenarioSelectUI>();
            var debriefUI = canvasGo.AddComponent<DebriefingModalUI>();

            // --- Top Banner (Incident Command System Header) ---
            var topBanner = CreateUIPanel(canvasGo.transform, "TopBanner", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(0, 90), new Color(0.06f, 0.08f, 0.12f, 0.92f));

            var titleText = CreateTextMesh(topBanner.transform, "TitleText", "AR DISASTER RESPONSE SIMULATOR - INCIDENT COMMAND", 20, TextAlignmentOptions.Left, Color.white);
            SetRect(titleText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 15), new Vector2(600, 30));
            titleText.fontStyle = FontStyles.Bold;

            var timerText = CreateTextMesh(topBanner.transform, "TimerText", "<b>TIME:</b> 00:00", 18, TextAlignmentOptions.Right, new Color(0.3f, 0.9f, 1f));
            SetRect(timerText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 15), new Vector2(200, 30));

            var casualtyText = CreateTextMesh(topBanner.transform, "CasualtyText", "<b>CASUALTIES:</b> 0/0 Evacuated  |  0 Stabilized  |  0 Triaged", 15, TextAlignmentOptions.Left, new Color(0.85f, 0.9f, 0.95f));
            SetRect(casualtyText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, -18), new Vector2(500, 26));

            var containmentText = CreateTextMesh(topBanner.transform, "ContainmentText", "<b>HAZARDS:</b> 0% Contained", 15, TextAlignmentOptions.Right, new Color(1f, 0.75f, 0.2f));
            SetRect(containmentText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, -18), new Vector2(350, 26));

            // --- Objective Ticker Banner ---
            var tickerBanner = CreateUIPanel(canvasGo.transform, "ObjectiveBanner", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -92), new Vector2(0, 36), new Color(0.12f, 0.16f, 0.24f, 0.85f));
            var tickerText = CreateTextMesh(tickerBanner.transform, "TickerText", "<b>OBJECTIVE:</b> Locate trapped victims, extinguish active fires, and apply medical triage tags.", 14, TextAlignmentOptions.Center, Color.white);
            SetRect(tickerText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // --- Bottom Responder Tool Bar ---
            var bottomToolbar = CreateUIPanel(canvasGo.transform, "BottomToolbar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(680, 80), new Color(0.08f, 0.10f, 0.15f, 0.94f));

            var btnScanner = CreateButton(bottomToolbar.transform, "BtnScanner", "[1] Scanner", new Vector2(-240, 0), new Vector2(150, 56), new Color(0.15f, 0.45f, 0.75f));
            var btnExtinguisher = CreateButton(bottomToolbar.transform, "BtnExtinguisher", "[2] Extinguisher", new Vector2(-80, 0), new Vector2(150, 56), new Color(0.85f, 0.35f, 0.15f));
            var btnSpreader = CreateButton(bottomToolbar.transform, "BtnSpreader", "[3] Spreader", new Vector2(80, 0), new Vector2(150, 56), new Color(0.75f, 0.65f, 0.2f));
            var btnMedKit = CreateButton(bottomToolbar.transform, "BtnMedKit", "[4] Medical Kit", new Vector2(240, 0), new Vector2(150, 56), new Color(0.2f, 0.7f, 0.35f));

            // --- Triage Tag Bar ---
            var triageBar = CreateUIPanel(canvasGo.transform, "TriageBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(560, 48), new Color(0.06f, 0.08f, 0.12f, 0.9f));
            var btnTagRed = CreateButton(triageBar.transform, "BtnTagRed", "RED (Immediate)", new Vector2(-200, 0), new Vector2(130, 36), new Color(0.9f, 0.15f, 0.15f));
            var btnTagYellow = CreateButton(triageBar.transform, "BtnTagYellow", "YELLOW (Delayed)", new Vector2(-65, 0), new Vector2(130, 36), new Color(0.95f, 0.75f, 0.1f));
            var btnTagGreen = CreateButton(triageBar.transform, "BtnTagGreen", "GREEN (Minimal)", new Vector2(70, 0), new Vector2(130, 36), new Color(0.2f, 0.8f, 0.3f));
            var btnTagBlack = CreateButton(triageBar.transform, "BtnTagBlack", "BLACK (Expectant)", new Vector2(205, 0), new Vector2(130, 36), new Color(0.25f, 0.25f, 0.25f));

            // --- Scenario Selector & Scale Controls (Top Right) ---
            var scenarioPanel = CreateUIPanel(canvasGo.transform, "ScenarioControls", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -140), new Vector2(220, 245), new Color(0.08f, 0.1f, 0.14f, 0.9f));
            var scenTitle = CreateTextMesh(scenarioPanel.transform, "ScenTitle", "<b>SCENARIOS & AR</b>", 14, TextAlignmentOptions.Center, Color.white);
            SetRect(scenTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(200, 24));

            var btnEarthquake = CreateButton(scenarioPanel.transform, "BtnEarthquake", "1. Earthquake Collapse", new Vector2(0, 68), new Vector2(190, 30), new Color(0.25f, 0.3f, 0.4f));
            var btnGasFire = CreateButton(scenarioPanel.transform, "BtnGasFire", "2. Fire & Gas Leak", new Vector2(0, 34), new Vector2(190, 30), new Color(0.25f, 0.3f, 0.4f));
            var btnHospital = CreateButton(scenarioPanel.transform, "BtnHospital", "3. Hospital MCI", new Vector2(0, 0), new Vector2(190, 30), new Color(0.25f, 0.3f, 0.4f));
            var btnWebcam = CreateButton(scenarioPanel.transform, "BtnWebcam", "[CAM] WebCam AR: ON/OFF", new Vector2(0, -34), new Vector2(190, 30), new Color(0.18f, 0.55f, 0.85f));
            var btnRestart = CreateButton(scenarioPanel.transform, "BtnRestart", "Restart Mission", new Vector2(0, -68), new Vector2(190, 30), new Color(0.6f, 0.2f, 0.2f));

            // --- Debriefing Modal ---
            var modalRoot = CreateUIPanel(canvasGo.transform, "DebriefingModal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 420), new Color(0.06f, 0.08f, 0.12f, 0.98f));
            var debriefHeader = CreateTextMesh(modalRoot.transform, "DebriefHeader", "INCIDENT AFTER-ACTION REVIEW", 22, TextAlignmentOptions.Center, Color.white);
            SetRect(debriefHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(500, 36));

            var gradeTxt = CreateTextMesh(modalRoot.transform, "GradeText", "A", 64, TextAlignmentOptions.Center, new Color(0.2f, 0.9f, 0.3f));
            SetRect(gradeTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(200, 75));
            gradeTxt.fontStyle = FontStyles.Bold;

            var scoreTxt = CreateTextMesh(modalRoot.transform, "ScoreText", "Tactical Efficiency: <b>88%</b>", 18, TextAlignmentOptions.Center, Color.white);
            SetRect(scoreTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(450, 30));

            var summaryTxt = CreateTextMesh(modalRoot.transform, "SummaryText", "Incident successfully neutralized. All casualties extracted.", 15, TextAlignmentOptions.Center, new Color(0.8f, 0.85f, 0.9f));
            SetRect(summaryTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -195), new Vector2(500, 45));

            var timeTxt = CreateTextMesh(modalRoot.transform, "TimeText", "Time: <b>02:14</b>", 15, TextAlignmentOptions.Center, Color.white);
            SetRect(timeTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(400, 26));

            var casualtyDebriefTxt = CreateTextMesh(modalRoot.transform, "CasualtyDebriefText", "Rescued: 4/4 | Fatalities: 0", 15, TextAlignmentOptions.Center, Color.white);
            SetRect(casualtyDebriefTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -268), new Vector2(400, 26));

            var triageAccTxt = CreateTextMesh(modalRoot.transform, "TriageAccText", "Triage Accuracy: 100%", 15, TextAlignmentOptions.Center, Color.white);
            SetRect(triageAccTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -296), new Vector2(400, 26));

            var btnDebriefRestart = CreateButton(modalRoot.transform, "BtnDebriefRestart", "Next / Restart Incident", new Vector2(0, -170), new Vector2(260, 44), new Color(0.2f, 0.6f, 0.9f));
            modalRoot.SetActive(false);

            // Bind references via reflection
            SetPrivateField(hud, "timerText", timerText);
            SetPrivateField(hud, "casualtyStatusText", casualtyText);
            SetPrivateField(hud, "containmentText", containmentText);
            SetPrivateField(hud, "objectiveTickerText", tickerText);
            SetPrivateField(hud, "scannerButton", btnScanner);
            SetPrivateField(hud, "extinguisherButton", btnExtinguisher);
            SetPrivateField(hud, "spreaderButton", btnSpreader);
            SetPrivateField(hud, "medicalKitButton", btnMedKit);
            SetPrivateField(hud, "triagePanel", triageBar);
            SetPrivateField(hud, "tagRedButton", btnTagRed);
            SetPrivateField(hud, "tagYellowButton", btnTagYellow);
            SetPrivateField(hud, "tagGreenButton", btnTagGreen);
            SetPrivateField(hud, "tagBlackButton", btnTagBlack);

            SetPrivateField(scenarioUI, "earthquakeScenarioButton", btnEarthquake);
            SetPrivateField(scenarioUI, "fireGasScenarioButton", btnGasFire);
            SetPrivateField(scenarioUI, "hospitalScenarioButton", btnHospital);
            SetPrivateField(scenarioUI, "restartButton", btnRestart);
            SetPrivateField(scenarioUI, "webcamToggleButton", btnWebcam);

            SetPrivateField(debriefUI, "modalRoot", modalRoot);
            SetPrivateField(debriefUI, "gradeText", gradeTxt);
            SetPrivateField(debriefUI, "scoreText", scoreTxt);
            SetPrivateField(debriefUI, "summaryText", summaryTxt);
            SetPrivateField(debriefUI, "timeText", timeTxt);
            SetPrivateField(debriefUI, "casualtyText", casualtyDebriefTxt);
            SetPrivateField(debriefUI, "triageAccuracyText", triageAccTxt);
            SetPrivateField(debriefUI, "restartMissionButton", btnDebriefRestart);
        }

        #region UI Helpers

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateTextMesh(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.25f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.sizeDelta = Vector2.zero;
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            return btn;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }

        #endregion
    }
}
