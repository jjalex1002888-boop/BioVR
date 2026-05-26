using UnityEngine;
using UnityEngine.EventSystems;

namespace BioVR.UI
{
    [RequireComponent(typeof(Collider))]
    public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Vector3 offset;
        private float dragDepth;
        private bool isDragging = false;
        private Vector3 targetPos;
        private Camera eventCamera;

        private void Start()
        {
            targetPos = transform.localPosition;
            eventCamera = Camera.main;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            if (eventCamera == null) return;

            isDragging = true;
            
            // Calculate depth of the object from camera
            dragDepth = eventCamera.WorldToScreenPoint(transform.position).z;
            
            // Calculate starting grab offset in 3D world space
            Vector3 grabWorldPos = eventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth));
            offset = transform.position - grabWorldPos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || eventCamera == null) return;

            Vector3 curWorldPos = eventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth)) + offset;
            
            if (transform.parent != null)
            {
                targetPos = transform.parent.InverseTransformPoint(curWorldPos);
            }
            else
            {
                targetPos = curWorldPos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void Update()
        {
            // Smooth glide interpolation for premium physics feel
            if (isDragging)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 12.0f);
            }
        }
    }
}
