using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BioVR.UI
{
    /// <summary>
    /// Filters out mouse inputs (PointerID -1) on UI Sliders to ensure that in Meta Quest VR
    /// builds only tracked controllers/hand rays can interact with the sliders.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VrOnlySliderHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private Slider targetSlider;

        private void Awake()
        {
            targetSlider = GetComponent<Slider>();
        }

        private bool ShouldBlockInput(PointerEventData eventData)
        {
            if (eventData.pointerId < 0)
            {
                // Do not block mouse inputs in the Unity Editor or if VR/XR device is offline.
                #if UNITY_EDITOR
                return false;
                #else
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    return false;
                }
                return true;
                #endif
            }
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Pointer ID -1 corresponds to standard Mouse Left Click.
            // Pointer ID -2 is Middle Mouse, -3 is Right Mouse.
            if (ShouldBlockInput(eventData))
            {
                // Consume the event so the Slider selectable does not receive it.
                eventData.Use();
                Debug.Log($"[BioVR VR-Interaction] Blocked mouse pointer {eventData.pointerId} click on slider {gameObject.name}. purely VR input required!");
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (ShouldBlockInput(eventData))
            {
                eventData.Use();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ShouldBlockInput(eventData))
            {
                eventData.Use();
            }
        }
    }
}
