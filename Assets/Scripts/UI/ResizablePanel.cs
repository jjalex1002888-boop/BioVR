using UnityEngine;
using UnityEngine.EventSystems;

namespace BioVR.UI
{
    /// <summary>
    /// Handles fluid drag-to-resize behavior for floating World-Space UI panels in VR & Editor.
    /// Resizes the target RectTransform's width and height (sizeDelta) independently in local coordinates,
    /// mimicking modern desktop OS window sizing (like Google Chrome under Windows 11).
    /// </summary>
    public class ResizablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Target Panel to Resize")]
        public RectTransform targetPanel;

        [Header("Size Boundaries")]
        public float minWidth = 200f;
        public float maxWidth = 900f;
        public float minHeight = 250f;
        public float maxHeight = 1300f;

        [Header("Glide Interpolation")]
        public float smoothSpeed = 15.0f;

        private Vector2 localStartPoint;
        private Vector2 startSize;
        private Vector2 targetSize;
        private bool isDragging = false;

        // Legacy scale fields kept for backwards compatibility in external components
        [HideInInspector] public float minScale = 0.5f;
        [HideInInspector] public float maxScale = 2.5f;

        private void Start()
        {
            if (targetPanel == null)
            {
                // Fallback to parent RectTransform if none was explicitly linked
                targetPanel = transform.parent as RectTransform;
            }

            if (targetPanel != null)
            {
                targetSize = targetPanel.sizeDelta;
            }
        }

        private void OnEnable()
        {
            if (targetPanel != null)
            {
                targetSize = targetPanel.sizeDelta;
            }
            isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;

            // Map current mouse screen position into targetPanel local coordinate space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetPanel, eventData.position, eventData.pressEventCamera, out localStartPoint);
            startSize = targetPanel.sizeDelta;
            targetSize = startSize;
            isDragging = true;

            Debug.Log($"[BioVR Resizer] Drag-resize started. Initial Size: {startSize}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null || !isDragging) return;

            // Fetch current drag coordinate in local panel coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetPanel, eventData.position, eventData.pressEventCamera, out Vector2 localCurrentPoint);
            Vector2 localDelta = localCurrentPoint - localStartPoint;

            // In local RectTransform system:
            // - Dragging RIGHT (localDelta.x > 0) increases width.
            // - Dragging DOWN (localDelta.y < 0) increases height.
            float newWidth = startSize.x + localDelta.x;
            float newHeight = startSize.y - localDelta.y;

            // Apply size constraints
            newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);
            newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

            targetSize = new Vector2(newWidth, newHeight);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            Debug.Log($"[BioVR Resizer] Drag-resize completed. Target Size: {targetSize}");
        }

        private void Update()
        {
            if (targetPanel == null) return;

            // Interpolate smoothly for organic, responsive layout transitions
            if (Vector2.Distance(targetPanel.sizeDelta, targetSize) > 0.05f)
            {
                targetPanel.sizeDelta = Vector2.Lerp(targetPanel.sizeDelta, targetSize, Time.deltaTime * smoothSpeed);
            }
            else
            {
                targetPanel.sizeDelta = targetSize;
            }
        }
    }
}
