using UnityEngine;
using System.Collections.Generic;

namespace BioVR.UI
{
    /// <summary>
    /// Applies a gentle, floating, game-style idle animation (rotation + sine wave bounce) to spawned structures.
    /// Tracks spawned types globally to ensure the idle animation only plays for the first spawn of a type since launching,
    /// and halts the bouncing effect permanently once the player clicks or interacts with any part of the model.
    /// </summary>
    public class ModelIdleAnimation : MonoBehaviour
    {
        [Header("Model Category Tracking")]
        [Tooltip("Unique ID representing this model type (e.g., 'Brain', 'Neuron', 'Synapse')")]
        public string modelType = "Brain";

        [Header("Idle Animation Parameters")]
        public float rotationSpeed = 15.0f; // degrees per second
        public float bounceAmplitude = 0.08f; // Peak height of floating bounce
        public float bounceFrequency = 0.5f; // Oscillations per second

        // Global tracker for spawned models to satisfy the user's specific first-spawn rule
        private static readonly HashSet<string> spawnedTypes = new HashSet<string>();

        private Vector3 startLocalPosition;
        private bool isIdleEnabled = false;
        private bool isBouncing = false;
        private float hoverTime = 0.0f;

        private ModelRotator modelRotator;

        private void Start()
        {
            startLocalPosition = transform.localPosition;
            modelRotator = GetComponent<ModelRotator>();

            // Perform single static registration check
            if (!spawnedTypes.Contains(modelType))
            {
                spawnedTypes.Add(modelType);
                isIdleEnabled = true;
                isBouncing = true;
                hoverTime = Random.Range(0f, 100f); // Randomize phase shift
                Debug.Log($"[BioVR Idle] First spawn of model type '{modelType}' detected! Activating bouncing idle animation.");
            }
            else
            {
                isIdleEnabled = false;
                isBouncing = false;
                Debug.Log($"[BioVR Idle] Model type '{modelType}' has been spawned previously. Keeping static.");
            }
        }

        private void Update()
        {
            if (!isIdleEnabled) return;

            // 1. Slow, elegant, cinematic rotation
            bool isManualRotating = modelRotator != null && (modelRotator.IsDragging || modelRotator.IsDecelerating);
            if (!isManualRotating)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            // 2. Gentle vertical game-item bounce oscillation
            if (isBouncing)
            {
                hoverTime += Time.deltaTime;
                float verticalOffset = Mathf.Sin(hoverTime * bounceFrequency * Mathf.PI * 2.0f) * bounceAmplitude;
                transform.localPosition = startLocalPosition + new Vector3(0f, verticalOffset, 0f);
            }
        }

        /// <summary>
        /// Triggered immediately when the player interacts with any sub-part of this model (Hover/Click/Pull).
        /// Permanently stops the floating bounce effect.
        /// </summary>
        public void OnUserInteracted()
        {
            if (!isBouncing) return;

            isBouncing = false;
            Debug.Log($"[BioVR Idle] User interaction detected on '{modelType}'. Disabling bounce effect and returning to rest position.");
            
            // Smoothly snap back to base position
            StartCoroutine(SmoothReturnToStart());
        }

        private System.Collections.IEnumerator SmoothReturnToStart()
        {
            float elapsed = 0.0f;
            float duration = 0.5f;
            Vector3 currentPos = transform.localPosition;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(currentPos, startLocalPosition, elapsed / duration);
                yield return null;
            }
            transform.localPosition = startLocalPosition;
        }

        /// <summary>
        /// Resets the first-spawn tracking registry. Useful if refreshing or restarting the game workspace.
        /// </summary>
        public static void ResetRegistry()
        {
            spawnedTypes.Clear();
            Debug.Log("[BioVR Idle] Spawn tracking registry cleared.");
        }
    }
}
