using UnityEngine;
using System;

namespace BioVR.Dynamics
{
    public class TimeWarpManager : MonoBehaviour
    {
        public static TimeWarpManager Instance { get; private set; }

        [Header("Playback Configuration")]
        [Range(0f, 10f)] public float timeScaleFactor = 1.0f; // 0.0x is paused, 1.0x is real-time, 10.0x fast-forward
        public bool isPaused = false;

        [Header("State Metrics")]
        public float elapsedMilliseconds = 0.0f;
        public float virtualTimelineProgress = 0.0f; // Normalized timeline progress [0, 100]

        // Event for active scripts to subscribe to
        public event Action<float, float> OnTimeStep; // parameter 1: dt (scaled), parameter 2: elapsedMs

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isPaused) return;

            // Calculate scaled simulation delta time
            float scaledDt = Time.deltaTime * timeScaleFactor;

            // Advance timelines
            elapsedMilliseconds += scaledDt * 1000f; // dt is in seconds, convert to ms
            virtualTimelineProgress = (elapsedMilliseconds % 100000f) / 1000f; // wrap at 100 seconds

            // Dispatch time update event
            OnTimeStep?.Invoke(scaledDt, elapsedMilliseconds);
        }

        public void Play()
        {
            isPaused = false;
            if (timeScaleFactor == 0.0f) timeScaleFactor = 1.0f;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void SetTimeScale(float scale)
        {
            timeScaleFactor = Mathf.Clamp(scale, 0f, 10f);
            if (timeScaleFactor > 0f) isPaused = false;
        }

        public void ScrubTo(float progressPercentage)
        {
            virtualTimelineProgress = Mathf.Clamp(progressPercentage, 0f, 100f);
            elapsedMilliseconds = virtualTimelineProgress * 1000f;
        }
    }
}
