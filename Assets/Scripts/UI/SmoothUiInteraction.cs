using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BioVR.UI
{
    /// <summary>
    /// Implements high-fidelity, fluidly interruptible micro-interactions (hover scale, glow, and click squish)
    /// using time-independent Lerp algorithms for a premium, tactile user experience.
    /// </summary>
    public class SmoothUiInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Micro-Animations")]
        public float hoverScale = 1.05f;
        public float clickScale = 0.95f;
        public float animSpeed = 15.0f;

        [Header("Visual Highlighting")]
        public Color hoverColorMultiplier = new Color(1.05f, 1.05f, 1.08f, 1.0f);
        public bool enableGlow = true;
        public Color glowColor = new Color(0.08f, 0.08f, 0.08f, 0.25f); // sleek translucent slate-charcoal

        private Vector3 originalScale;
        private Vector3 targetScale;
        
        private Image targetImage;
        private Color originalColor;
        private Color targetColor;

        private Outline targetOutline;
        private Color originalOutlineColor;
        private Color targetOutlineColor;

        private bool isHovered = false;
        private bool isPressed = false;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;

            targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                originalColor = targetImage.color;
                targetColor = originalColor;
            }

            targetOutline = GetComponent<Outline>();
            if (targetOutline != null)
            {
                originalOutlineColor = targetOutline.effectColor;
                targetOutlineColor = originalOutlineColor;
            }
        }

        private void OnEnable()
        {
            // Reset to default states on enable to avoid stuck animation frames
            transform.localScale = originalScale;
            targetScale = originalScale;
            isHovered = false;
            isPressed = false;

            if (targetImage != null)
            {
                targetImage.color = originalColor;
                targetColor = originalColor;
            }

            if (targetOutline != null)
            {
                targetOutline.effectColor = originalOutlineColor;
                targetOutlineColor = originalOutlineColor;
            }
        }

        private void Update()
        {
            // Smooth, fluidly interruptible interpolation
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

            if (targetImage != null)
            {
                targetImage.color = Color.Lerp(targetImage.color, targetColor, Time.deltaTime * animSpeed);
            }

            if (targetOutline != null)
            {
                targetOutline.effectColor = Color.Lerp(targetOutline.effectColor, targetOutlineColor, Time.deltaTime * animSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            UpdateVisualStates();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            UpdateVisualStates();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            UpdateVisualStates();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            UpdateVisualStates();
        }

        private void UpdateVisualStates()
        {
            // Determine Scale
            if (isPressed)
            {
                targetScale = originalScale * clickScale;
            }
            else if (isHovered)
            {
                targetScale = originalScale * hoverScale;
            }
            else
            {
                targetScale = originalScale;
            }

            // Determine Color & Glow
            if (targetImage != null)
            {
                if (isHovered)
                {
                    targetColor = originalColor * hoverColorMultiplier;
                }
                else
                {
                    targetColor = originalColor;
                }
            }

            if (targetOutline != null)
            {
                if (isHovered && enableGlow)
                {
                    targetOutlineColor = glowColor;
                }
                else
                {
                    targetOutlineColor = originalOutlineColor;
                }
            }
        }
    }
}
