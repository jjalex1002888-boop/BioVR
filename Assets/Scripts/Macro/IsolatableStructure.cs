using UnityEngine;
using UnityEngine.EventSystems;

namespace BioVR.Macro
{
    public class IsolatableStructure : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Structure Metadata")]
        public string structureName = "Deep Brain Structure";
        public string structureId = "structure";

        [Header("Explode / Pull Movement")]
        public Vector3 pullAxis = Vector3.left;
        public float pullDistance = 1.2f;
        public float lerpSpeed = 5.0f;

        [Header("Hover Highlight Scaling")]
        public float hoverScaleMultiplier = 1.15f;
        public Color hoverGlowColor = Color.yellow;
        
        [HideInInspector] public Vector3 restingLocalPosition;
        private Vector3 targetLocalPosition;
        private Vector3 normalScale;
        
        private Material meshMaterial;
        private Color originalColor;
        private bool isHovered = false;
        private bool isPulled = false;

        private HolographicTether tether;

        private void Start()
        {
            restingLocalPosition = transform.localPosition;
            targetLocalPosition = restingLocalPosition;
            normalScale = transform.localScale;

            // Cache material to modify colors/glows upon interaction
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                meshMaterial = renderer.material;
                originalColor = meshMaterial.color;
            }

            // Find or setup connecting tether
            tether = GetComponent<HolographicTether>();
            if (tether == null)
            {
                tether = gameObject.AddComponent<HolographicTether>();
            }
            tether.SetAnchorPoints(transform, transform.parent != null ? transform.parent : null, restingLocalPosition);
        }

        private void Update()
        {
            // Smoothly Lerp between rest and pulled position states
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * lerpSpeed);

            // Pulse scale slowly when hovered for interactive feedback
            if (isHovered && !isPulled)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.03f;
                transform.localScale = normalScale * hoverScaleMultiplier * pulse;
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, normalScale, Time.deltaTime * 10f);
            }
        }

        public void OnHoverStart()
        {
            isHovered = true;
            if (meshMaterial != null)
            {
                meshMaterial.color = hoverGlowColor;
                meshMaterial.EnableKeyword("_EMISSION");
                meshMaterial.SetColor("_EmissionColor", hoverGlowColor * 0.5f);
            }
        }

        public void OnHoverEnd()
        {
            isHovered = false;
            if (meshMaterial != null && !isPulled)
            {
                meshMaterial.color = originalColor;
                meshMaterial.SetColor("_EmissionColor", Color.black);
            }
        }

        public void TogglePull()
        {
            // Propagate interaction to halt idle floating/bouncing in the parent structure
            var idleAnim = GetComponentInParent<BioVR.UI.ModelIdleAnimation>();
            if (idleAnim != null)
            {
                idleAnim.OnUserInteracted();
            }

            if (isPulled)
            {
                Retract();
            }
            else
            {
                Pull();
            }
        }

        public void Pull()
        {
            isPulled = true;
            targetLocalPosition = restingLocalPosition + (pullAxis.normalized * pullDistance);
            
            // Turn on the connecting holographic tether
            if (tether != null)
            {
                tether.SetTetherActive(true);
            }
        }

        public void Retract()
        {
            isPulled = false;
            targetLocalPosition = restingLocalPosition;

            // Turn off tether
            if (tether != null)
            {
                tether.SetTetherActive(false);
            }

            if (meshMaterial != null)
            {
                meshMaterial.color = originalColor;
                meshMaterial.SetColor("_EmissionColor", Color.black);
            }
        }

        // --- EventSystem Pointer Interface Implementations ---
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverStart();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverEnd();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TogglePull();
        }
    }
}
