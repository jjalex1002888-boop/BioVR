#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using UnityEngine;
using System.Collections;
using BioVR.Macro;

namespace BioVR.UI
{
    public class HybridCameraRig : MonoBehaviour
    {
        [Header("Fly Camera Settings")]
        public float lookSpeed = 2.0f;
        public float moveSpeed = 5.0f;
        public float fastMoveMultiplier = 3.0f;
        public float zoomSpeed = 8.0f;

        [Header("Orbit Focus Point")]
        public Vector3 focalPoint = new Vector3(0f, 1.5f, 2.0f); // Centers on the Procedural Cerebrum

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        
        private IsolatableStructure currentlyHoveredStructure = null;

        private void Start()
        {
            // Position the camera rig beautifully relative to the brain and UI
            transform.position = new Vector3(0f, 1.3f, -0.8f);
            transform.rotation = Quaternion.Euler(5f, 0f, 0f);

            targetPosition = transform.position;
            targetRotation = transform.rotation;

            rotationX = transform.localEulerAngles.y;
            rotationY = transform.localEulerAngles.x;

            // Automatically check and assign Main Camera tag
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                gameObject.tag = "MainCamera";

                // Configure clinical high-light, low-attention endless white void
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.95f, 0.96f, 0.98f, 1f); // Pristine clinical soft-white void
            }

            // Apply soft matching flat ambient lighting to make holographic shaders pop
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.95f, 0.96f, 0.98f);
            RenderSettings.skybox = null; // Suppress legacy dark skybox to isolate in clinical white void

            StartCoroutine(CheckAndInitializeXR());
        }

        private IEnumerator CheckAndInitializeXR()
        {
            // Give the XR system a fraction of a second to boot up
            yield return new WaitForSeconds(0.1f);

            bool vrActive = false;
            
            // Check if VR/XR device is active at runtime
            #if !UNITY_EDITOR
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                vrActive = true;
            }
            #endif

            if (vrActive)
            {
                Debug.Log("[BioVR Camera] XR Device active. Transitioning to VR Headset tracking.");
                // XR headset is tracking the camera automatically, so disable editor fly scripts
                enabled = false;
            }
            else
            {
                Debug.Log("[BioVR Camera] XR device offline. Activating smooth Fly & Orbit Editor controls (Use WASD + Hold Right Click to look).");
            }
        }

        private void Update()
        {
            float speed = moveSpeed;
            Vector3 moveDirection = Vector3.zero;
            float scroll = 0f;
            bool isRotating = false;
            float lookX = 0f;
            float lookY = 0f;

            // 1. Fetch input values safely based on active Input System
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.leftShiftKey.isPressed) speed *= fastMoveMultiplier;
                if (keyboard.wKey.isPressed) moveDirection += transform.forward;
                if (keyboard.sKey.isPressed) moveDirection -= transform.forward;
                if (keyboard.aKey.isPressed) moveDirection -= transform.right;
                if (keyboard.dKey.isPressed) moveDirection += transform.right;
                if (keyboard.eKey.isPressed) moveDirection += transform.up;
                if (keyboard.qKey.isPressed) moveDirection -= transform.up;
            }

            if (mouse != null)
            {
                isRotating = mouse.rightButton.isPressed;
                Vector2 delta = mouse.delta.ReadValue() * 0.1f; // mouse delta scale factor
                lookX = delta.x;
                lookY = delta.y;
                scroll = mouse.scroll.ReadValue().y * 0.005f; // scroll scale factor
            }
#else
            if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMoveMultiplier;
            if (Input.GetKey(KeyCode.W)) moveDirection += transform.forward;
            if (Input.GetKey(KeyCode.S)) moveDirection -= transform.forward;
            if (Input.GetKey(KeyCode.A)) moveDirection -= transform.right;
            if (Input.GetKey(KeyCode.D)) moveDirection += transform.right;
            if (Input.GetKey(KeyCode.E)) moveDirection += transform.up;
            if (Input.GetKey(KeyCode.Q)) moveDirection -= transform.up;

            isRotating = Input.GetMouseButton(1);
            lookX = Input.GetAxis("Mouse X");
            lookY = Input.GetAxis("Mouse Y");
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif

            // 2. Mouse Drag View Rotation (Look around)
            if (isRotating)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                rotationX += lookX * lookSpeed;
                rotationY -= lookY * lookSpeed;
                rotationY = Mathf.Clamp(rotationY, -80f, 80f); // Avoid flipping over

                targetRotation = Quaternion.Euler(rotationY, rotationX, 0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 3. Apply Flight Movement
            targetPosition += moveDirection * speed * Time.deltaTime;

            // 4. Scroll Wheel Zooming (Tethers focal point)
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 toFocus = focalPoint - transform.position;
                targetPosition += toFocus.normalized * scroll * zoomSpeed;
            }

            // 5. Smooth Damp positioning for high-end cinematic feel
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 6.0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8.0f);

            // 6. Dynamic 3D Object interactions (Hover highlights and click pulls)
            Handle3DInteractions();
        }

        private void Handle3DInteractions()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) return;

            // Cast a ray from mouse cursor into 3D scene space
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                IsolatableStructure structure = hit.collider.GetComponent<IsolatableStructure>();
                if (structure != null)
                {
                    if (currentlyHoveredStructure != structure)
                    {
                        if (currentlyHoveredStructure != null)
                        {
                            currentlyHoveredStructure.OnHoverEnd();
                        }
                        currentlyHoveredStructure = structure;
                        currentlyHoveredStructure.OnHoverStart();
                    }

                    // Toggles 3D pull tether on mouse left click
                    if (Input.GetMouseButtonDown(0))
                    {
                        currentlyHoveredStructure.TogglePull();
                    }
                }
                else
                {
                    ClearHover();
                }
            }
            else
            {
                ClearHover();
            }
        }

        private void ClearHover()
        {
            if (currentlyHoveredStructure != null)
            {
                currentlyHoveredStructure.OnHoverEnd();
                currentlyHoveredStructure = null;
            }
        }
    }
}
