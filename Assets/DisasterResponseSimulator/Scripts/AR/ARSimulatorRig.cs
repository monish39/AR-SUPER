using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DisasterResponse.AR
{
    public class ARSimulatorRig : MonoBehaviour
    {
        public static ARSimulatorRig Instance { get; private set; }

        [Header("Camera & Rig")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARPlacementReticle reticle;

        [Header("Simulated Ground Plane (In-Editor)")]
        [SerializeField] private GameObject simulatedGroundPlane;
        [SerializeField] private LayerMask planeLayerMask;

        [Header("Editor Navigation Speeds")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float lookSensitivity = 2.0f;
        [SerializeField] private float zoomSpeed = 5.0f;

        private float yaw = 0f;
        private float pitch = 25f;
        private bool isRightMouseDown = false;

        public Camera ARCamera => arCamera;
        public ARPlacementReticle Reticle => reticle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (arCamera == null) arCamera = GetComponentInChildren<Camera>();
            if (arCamera == null) arCamera = Camera.main;

            SetupSimulatedGround();
        }

        private void SetupSimulatedGround()
        {
            // Only create simulated ground if in Editor or non-AR device
            if (simulatedGroundPlane == null)
            {
                simulatedGroundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                simulatedGroundPlane.name = "Simulated_AR_Ground";
                simulatedGroundPlane.transform.SetParent(transform, false);
                simulatedGroundPlane.transform.position = new Vector3(0, -0.01f, 0);
                simulatedGroundPlane.transform.localScale = new Vector3(5f, 1f, 5f);

                // Keep collider for surface raycasts, but disable the opaque visual mesh renderer
                // so the user's real room / floor / desk is 100% visible through the WebCam!
                var mr = simulatedGroundPlane.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = false;
                }
            }
        }

        private void Update()
        {
            HandleEditorCameraMovement();
            UpdateReticlePosition();
        }

        private void HandleEditorCameraMovement()
        {
            // WASD + Q/E Keyboard navigation
            Vector3 inputDir = Vector3.zero;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null)
            {
                if (kb.wKey.isPressed) inputDir += arCamera.transform.forward;
                if (kb.sKey.isPressed) inputDir -= arCamera.transform.forward;
                if (kb.aKey.isPressed) inputDir -= arCamera.transform.right;
                if (kb.dKey.isPressed) inputDir += arCamera.transform.right;
                if (kb.eKey.isPressed) inputDir += Vector3.up;
                if (kb.qKey.isPressed) inputDir -= Vector3.up;
            }

            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame) isRightMouseDown = true;
                if (mouse.rightButton.wasReleasedThisFrame) isRightMouseDown = false;

                if (isRightMouseDown)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * lookSensitivity * 0.1f;
                    pitch -= delta.y * lookSensitivity * 0.1f;
                    pitch = Mathf.Clamp(pitch, -80f, 85f);
                    arCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    arCamera.transform.position += arCamera.transform.forward * (scroll * 0.005f * zoomSpeed);
                }
            }

            if (inputDir.sqrMagnitude > 0.001f)
            {
                arCamera.transform.position += inputDir.normalized * (moveSpeed * Time.deltaTime);
            }
        }

        private void UpdateReticlePosition()
        {
            if (reticle == null || arCamera == null) return;

            Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var mouse = Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
            }

            Ray ray = arCamera.ScreenPointToRay(screenPos);
            int layerMask = ~LayerMask.GetMask("Ignore Raycast");
            if (Physics.Raycast(ray, out RaycastHit hit, 50f, layerMask))
            {
                reticle.SetPose(hit.point + hit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, hit.normal));
                reticle.SetVisible(true);
            }
            else
            {
                reticle.SetVisible(false);
            }
        }
    }
}
