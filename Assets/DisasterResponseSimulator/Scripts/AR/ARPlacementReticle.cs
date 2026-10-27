using UnityEngine;

namespace DisasterResponse.AR
{
    public class ARPlacementReticle : MonoBehaviour
    {
        [Header("Reticle Visuals")]
        [SerializeField] private GameObject visualRing;
        [SerializeField] private MeshRenderer ringRenderer;

        [Header("Colors")]
        [SerializeField] private Color placementColor = new Color(0.2f, 0.85f, 1.0f, 0.8f);
        [SerializeField] private Color hoverHazardColor = new Color(1.0f, 0.25f, 0.25f, 0.85f);
        [SerializeField] private Color hoverVictimColor = new Color(0.3f, 1.0f, 0.4f, 0.85f);

        private void Awake()
        {
            // Remove any collider on this object so it never interferes with raycasts
            var col = GetComponent<Collider>();
            if (col != null) Destroy(col);
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            EnsureVisualRing();
        }

        private void Start()
        {
            EnsureVisualRing();
        }

        private void EnsureVisualRing()
        {
            if (visualRing == null)
            {
                if (transform.childCount > 0)
                {
                    visualRing = transform.GetChild(0).gameObject;
                }
                else
                {
                    // Create a flat circular reticle mesh
                    visualRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visualRing.name = "ReticleVisualMesh";
                    visualRing.transform.SetParent(transform, false);
                    visualRing.transform.localPosition = Vector3.zero;
                    visualRing.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);

                    var childCol = visualRing.GetComponent<Collider>();
                    if (childCol != null) Destroy(childCol);
                    visualRing.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
            }

            if (ringRenderer == null && visualRing != null)
            {
                ringRenderer = visualRing.GetComponent<MeshRenderer>();
                if (ringRenderer != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
                    mat.color = placementColor;
                    ringRenderer.material = mat;
                }
            }
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            if (visualRing != null) visualRing.SetActive(visible);
        }

        public void SetMode(int mode) // 0: placement, 1: hazard, 2: victim
        {
            if (ringRenderer == null || ringRenderer.material == null) return;

            Color target = placementColor;
            if (mode == 1) target = hoverHazardColor;
            else if (mode == 2) target = hoverVictimColor;

            ringRenderer.material.color = target;
        }
    }
}
