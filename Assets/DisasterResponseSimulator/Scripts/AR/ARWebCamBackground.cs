using System;
using UnityEngine;
using UnityEngine.UI;

namespace DisasterResponse.AR
{
    public class ARWebCamBackground : MonoBehaviour
    {
        public static ARWebCamBackground Instance { get; private set; }

        [Header("WebCam Settings")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private int requestedWidth = 1920;
        [SerializeField] private int requestedHeight = 1080;
        [SerializeField] private int requestedFPS = 30;

        [Header("UI Canvas Reference")]
        [SerializeField] private Canvas backgroundCanvas;
        [SerializeField] private RawImage backgroundRawImage;
        [SerializeField] private AspectRatioFitter aspectFitter;

        private WebCamTexture webCamTexture;
        private WebCamDevice[] devices;
        private int selectedDeviceIndex = 0;
        private bool isCameraRunning = false;

        public bool IsCameraRunning => isCameraRunning;
        public string CurrentDeviceName => (devices != null && devices.Length > selectedDeviceIndex) ? devices[selectedDeviceIndex].name : "None";

        public event Action<bool> OnWebCamStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupBackgroundCanvas();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartWebCam();
            }
        }

        private void SetupBackgroundCanvas()
        {
            if (backgroundCanvas != null && backgroundRawImage != null) return;

            Camera targetCam = Camera.main;
            if (targetCam == null) targetCam = FindFirstObjectByType<Camera>();

            // Create dedicated background canvas
            var canvasGo = new GameObject("AR_WebCam_Background_Canvas");
            canvasGo.transform.SetParent(transform, false);

            backgroundCanvas = canvasGo.AddComponent<Canvas>();
            backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backgroundCanvas.worldCamera = targetCam;
            backgroundCanvas.planeDistance = targetCam != null ? targetCam.farClipPlane - 5f : 990f;
            backgroundCanvas.sortingOrder = -100; // Render behind everything else

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // RawImage for rendering the camera feed
            var rawImageGo = new GameObject("WebCamRawImage");
            rawImageGo.transform.SetParent(canvasGo.transform, false);

            backgroundRawImage = rawImageGo.AddComponent<RawImage>();
            backgroundRawImage.color = Color.white;

            var rect = backgroundRawImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;

            aspectFitter = rawImageGo.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectFitter.aspectRatio = 16f / 9f;

            // Configure Camera ClearFlags so background feed is visible
            if (targetCam != null)
            {
                targetCam.clearFlags = CameraClearFlags.Depth;
            }
        }

        public void StartWebCam()
        {
            devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[ARWebCamBackground] No physical WebCam devices detected on this computer. Using simulated AR passthrough.");
                SetFallbackBackground();
                return;
            }

            Debug.Log($"[ARWebCamBackground] Detected {devices.Length} camera device(s). Using: {devices[selectedDeviceIndex].name}");

            try
            {
                if (webCamTexture != null && webCamTexture.isPlaying)
                {
                    webCamTexture.Stop();
                }

                webCamTexture = new WebCamTexture(devices[selectedDeviceIndex].name, requestedWidth, requestedHeight, requestedFPS);
                if (backgroundRawImage != null)
                {
                    backgroundRawImage.texture = webCamTexture;
                    backgroundRawImage.enabled = true;
                }

                webCamTexture.Play();
                isCameraRunning = true;
                OnWebCamStateChanged?.Invoke(true);

                Debug.Log("[ARWebCamBackground] WebCam successfully started and streaming into scene background!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARWebCamBackground] Failed to start WebCam: {ex.Message}");
                SetFallbackBackground();
            }
        }

        private void Update()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                // Correct orientation and aspect ratio
                if (aspectFitter != null && webCamTexture.width > 16 && webCamTexture.height > 16)
                {
                    aspectFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
                }

                // Handle video rotation or vertical flip if necessary
                if (backgroundRawImage != null)
                {
                    float angle = -webCamTexture.videoRotationAngle;
                    Vector3 scale = Vector3.one;
                    if (webCamTexture.videoVerticallyMirrored) scale.y = -1f;
                    backgroundRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
                    backgroundRawImage.rectTransform.localScale = scale;
                }
            }
        }

        public void StopWebCam()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            if (backgroundRawImage != null)
            {
                backgroundRawImage.enabled = false;
            }

            isCameraRunning = false;
            OnWebCamStateChanged?.Invoke(false);
        }

        public void ToggleWebCam()
        {
            if (isCameraRunning)
            {
                StopWebCam();
            }
            else
            {
                StartWebCam();
            }
        }

        public void SwitchCamera()
        {
            if (devices == null || devices.Length <= 1) return;

            selectedDeviceIndex = (selectedDeviceIndex + 1) % devices.Length;
            StartWebCam();
        }

        private void SetFallbackBackground()
        {
            // If no camera exists, provide a sleek dark gradient AR backdrop
            if (backgroundRawImage != null)
            {
                backgroundRawImage.texture = null;
                backgroundRawImage.color = new Color(0.08f, 0.10f, 0.13f, 1.0f);
                backgroundRawImage.enabled = true;
            }
        }

        private void OnDestroy()
        {
            StopWebCam();
        }
    }
}
