using UnityEngine;
using UnityEngine.EventSystems;
using DisasterResponse.Core;
using DisasterResponse.Tools;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DisasterResponse.AR
{
    public class ARInteractionRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private bool isHoldingInteraction = false;

        private void Start()
        {
            if (interactionCamera == null) interactionCamera = Camera.main;
        }

        private void Update()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
                if (interactionCamera == null) return;
            }

            bool pointerDown = false;
            bool pointerUp = false;
            bool pointerHeld = false;
            Vector2 screenPos = Vector2.zero;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                pointerDown = touch.primaryTouch.press.wasPressedThisFrame;
                pointerUp = touch.primaryTouch.press.wasReleasedThisFrame;
                pointerHeld = touch.primaryTouch.press.isPressed;
            }
            else if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                pointerDown = mouse.leftButton.wasPressedThisFrame;
                pointerUp = mouse.leftButton.wasReleasedThisFrame;
                pointerHeld = mouse.leftButton.isPressed;
            }

            // Check if pointer is over UI element
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (pointerUp) StopCurrentToolHold();
                return;
            }

            if (pointerDown)
            {
                HandlePointerDown(screenPos);
            }
            else if (pointerHeld && isHoldingInteraction)
            {
                HandlePointerHold(screenPos);
            }
            else if (pointerUp)
            {
                StopCurrentToolHold();
            }
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            Ray ray = interactionCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactionLayers))
            {
                // If disaster zone is not yet anchored, place it on the surface!
                if (DisasterSimulationManager.Instance != null && !DisasterSimulationManager.Instance.IsZonePlaced)
                {
                    DisasterSimulationManager.Instance.PlaceDisasterZone(hit.point, Quaternion.identity);
                    return;
                }

                isHoldingInteraction = true;
                ToolController.Instance?.PerformToolAction(hit, false, Time.deltaTime);
            }
        }

        private void HandlePointerHold(Vector2 screenPos)
        {
            Ray ray = interactionCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactionLayers))
            {
                ToolController.Instance?.PerformToolAction(hit, true, Time.deltaTime);
            }
            else
            {
                StopCurrentToolHold();
            }
        }

        private void StopCurrentToolHold()
        {
            isHoldingInteraction = false;
            ToolController.Instance?.StopContinuousToolAction();
        }
    }
}
