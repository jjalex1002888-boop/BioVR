using UnityEngine;
using UnityEngine.EventSystems;

namespace BioVR.UI
{
    /// <summary>
    /// Handles smooth, tactile drag-to-move interactions for floating World-Space UI panels in VR & Editor.
    /// Projects coordinates into 3D world space and slides the panel horizontally and vertically
    /// along its own flat local plane, maintaining distance (depth) and preventing rotational drift.
    /// </summary>
    public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Target Panel to Move")]
        public RectTransform targetPanel;

        [Header("Glide Interpolation")]
        public float smoothSpeed = 15.0f;

        private Vector3 startPanelWorldPos;
        private Vector3 targetWorldPos;
        private Vector3 dragStartWorldPos;
        private Camera eventCamera;
        private float dragDepth;
        private bool isDragging = false;

        private void Start()
        {
            if (targetPanel == null)
            {
                // Fallback to parent RectTransform if none was explicitly linked
                targetPanel = transform.parent as RectTransform;
            }
            if (targetPanel != null)
            {
                targetWorldPos = targetPanel.position;
            }
        }

        private void OnEnable()
        {
            if (targetPanel != null)
            {
                targetWorldPos = targetPanel.position;
            }
            isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetPanel == null) return;

            eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            if (eventCamera == null) return;

            isDragging = true;
            startPanelWorldPos = targetPanel.position;

            // Calculate depth of the panel from the camera
            dragDepth = eventCamera.WorldToScreenPoint(targetPanel.position).z;

            // Calculate starting grab offset in 3D world space
            dragStartWorldPos = eventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetPanel == null || !isDragging || eventCamera == null) return;

            Vector3 currentDragWorldPos = eventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth));
            Vector3 delta = currentDragWorldPos - dragStartWorldPos;

            // Project drag delta onto the panel's local Up and Right axes
            // This maintains depth relative to camera and avoids weird depth-scaling movements
            Vector3 localDeltaY = targetPanel.up * Vector3.Dot(delta, targetPanel.up);
            Vector3 localDeltaX = targetPanel.right * Vector3.Dot(delta, targetPanel.right);

            targetWorldPos = startPanelWorldPos + localDeltaY + localDeltaX;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void Update()
        {
            if (targetPanel == null) return;

            // Smooth frame-rate independent glide physics
            if (Vector3.Distance(targetPanel.position, targetWorldPos) > 0.001f)
            {
                targetPanel.position = Vector3.Lerp(targetPanel.position, targetWorldPos, Time.deltaTime * smoothSpeed);
            }
            else
            {
                targetPanel.position = targetWorldPos;
            }
        }
    }
}
