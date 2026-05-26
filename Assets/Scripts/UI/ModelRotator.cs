using UnityEngine;
using UnityEngine.EventSystems;

namespace BioVR.UI
{
    /// <summary>
    /// Adds manual drag-based 3D rotations with premium inertial deceleration to holographic models.
    /// Works with both Meta Quest controller pointers and editor mouse drags using standard EventSystem.
    /// </summary>
    public class ModelRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Rotation Settings")]
        public float rotationSensitivity = 0.4f;
        public float friction = 5.0f; // Deceleration rate

        [Header("Axis Constraints")]
        public bool clampXRotation = false;
        public float minX = -60f;
        public float maxX = 60f;

        public bool IsDragging { get; private set; }
        public bool IsDecelerating { get; private set; }

        private Vector2 angularVelocity;
        private Vector3 currentAngles;
        private Camera eventCamera;

        private void Start()
        {
            currentAngles = transform.localEulerAngles;
            
            // Safe caching of active camera
            eventCamera = Camera.main;
        }

        private void Update()
        {
            // If dragging, we update rotation immediately. If not dragging, apply inertia friction decay.
            if (!IsDragging && angularVelocity.sqrMagnitude > 0.001f)
            {
                IsDecelerating = true;
                
                // Exponential decay (friction)
                angularVelocity = Vector2.Lerp(angularVelocity, Vector2.zero, Time.deltaTime * friction);

                // Rotate based on velocity
                ApplyRotationDelta(angularVelocity * Time.deltaTime * 60f);
            }
            else if (!IsDragging)
            {
                IsDecelerating = false;
                angularVelocity = Vector2.zero;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
            IsDecelerating = false;
            angularVelocity = Vector2.zero;

            if (eventData.pressEventCamera != null)
            {
                eventCamera = eventData.pressEventCamera;
            }
            else if (Camera.main != null)
            {
                eventCamera = Camera.main;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            // Calculate drag delta relative to viewport/screen size
            Vector2 delta = eventData.delta * rotationSensitivity;
            
            // Save as current velocity for inertia when released
            angularVelocity = delta;

            ApplyRotationDelta(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
        }

        private void ApplyRotationDelta(Vector2 delta)
        {
            // Horizontal drag (delta.x) rotates around Y axis (up)
            // Vertical drag (delta.y) rotates around X axis (right)
            float rotY = -delta.x;
            float rotX = delta.y;

            // We perform rotation relative to the camera's reference frame for intuitive control!
            Transform camTransform = eventCamera != null ? eventCamera.transform : null;
            if (camTransform != null)
            {
                // Project drag directions into world space relative to camera view
                Vector3 worldRight = camTransform.right;
                Vector3 worldUp = camTransform.up;

                // Rotate around the camera's local horizontal and vertical vectors
                transform.Rotate(worldUp, rotY, Space.World);
                transform.Rotate(worldRight, rotX, Space.World);
            }
            else
            {
                // Fallback to local coordinate rotation
                transform.Rotate(Vector3.up, rotY, Space.Self);
                transform.Rotate(Vector3.right, rotX, Space.Self);
            }

            // Optional constraint clamping on local X axis
            if (clampXRotation)
            {
                Vector3 angles = transform.localEulerAngles;
                float clampedX = angles.x;
                if (clampedX > 180f) clampedX -= 360f;
                clampedX = Mathf.Clamp(clampedX, minX, maxX);
                angles.x = clampedX;
                transform.localEulerAngles = angles;
            }
        }
    }
}
