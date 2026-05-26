using UnityEngine;
using UnityEngine.UI;
using BioVR.Molecular;

namespace BioVR.UI
{
    public class HolographicGpuStatsPanel : MonoBehaviour
    {
        [Header("UI Component Outlets")]
        public Text txtGpuName;
        public Text txtJobStatus;
        public Text txtVramReadout;
        public Text txtSessionTime;
        public Text txtQuotaPercentage;
        public Text txtCheckpointLogs;

        [Header("Progress Meter Images")]
        public Image imgVramProgress;
        public Image imgQuotaProgress;

        [Header("Emergency Interruption Banner")]
        public GameObject emergencyWarningBanner;

        private void Start()
        {
            // Subscribe to cloud metrics updates
            if (GcpCloudBridge.Instance != null)
            {
                GcpCloudBridge.Instance.OnJobStatusUpdated += UpdateStatsUI;
            }

            if (emergencyWarningBanner != null)
            {
                emergencyWarningBanner.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (GcpCloudBridge.Instance != null)
            {
                GcpCloudBridge.Instance.OnJobStatusUpdated -= UpdateStatsUI;
            }
        }

        public void UpdateStatsUI(JobStatusResponse data)
        {
            // 1. Update Core GPU / Node details
            if (txtGpuName != null) txtGpuName.text = "GCP NODE: NVIDIA Tesla T4 Cluster";
            
            if (txtJobStatus != null)
            {
                txtJobStatus.text = $"STATUS: {data.status} (Step {data.current_step}/{data.total_steps})";
                
                // Colorize text based on active states
                if (data.status == "COMPLETED") txtJobStatus.color = Color.green;
                else if (data.status == "PAUSED_BY_QUOTA") txtJobStatus.color = new Color(1.0f, 0.5f, 0.0f); // orange
                else if (data.status == "FAILED") txtJobStatus.color = Color.red;
                else txtJobStatus.color = Color.cyan;
            }

            // 2. Telemetry and VRAM Calculations (Safe Guarded against null metrics)
            if (data.performance_metrics != null)
            {
                long maxVramBytes = 16L * 1024L * 1024L * 1024L; // 16 GB Max
                long curVramBytes = data.performance_metrics.gpu_vram_usage_bytes;
                float vramPercentage = (float)curVramBytes / maxVramBytes;

                if (txtVramReadout != null)
                {
                    float vramGb = curVramBytes / (1024f * 1024f * 1024f);
                    txtVramReadout.text = $"{vramGb:F1} GB / 16.0 GB";
                }

                if (imgVramProgress != null)
                {
                    imgVramProgress.fillAmount = Mathf.Clamp01(vramPercentage);
                }

                // 3. Quota Calculations (Strict 90% Limit Warnings)
                int elapsed = data.performance_metrics.elapsed_time_seconds;
                int limit = data.performance_metrics.quota_session_limit_seconds;
                float quotaPct = Mathf.Min(100f, ((float)elapsed / limit) * 100f);

                if (txtQuotaPercentage != null)
                {
                    txtQuotaPercentage.text = $"{quotaPct:F1}% Consumed";
                }

                if (imgQuotaProgress != null)
                {
                    imgQuotaProgress.fillAmount = Mathf.Clamp01(quotaPct / 100f);
                    // Turn red if nearing the 90% threshold
                    imgQuotaProgress.color = quotaPct >= 90.0f ? Color.red : Color.cyan;
                }

                if (txtSessionTime != null)
                {
                    int mins = elapsed / 60;
                    int secs = elapsed % 60;
                    int limitMins = limit / 60;
                    txtSessionTime.text = $"{mins:D2}:{secs:D2} / {limitMins:D2}:00";
                }

                // Enable warning banner if 90% quota safety buffer is triggered
                if (emergencyWarningBanner != null)
                {
                    emergencyWarningBanner.SetActive(quotaPct >= 90.0f || data.status == "PAUSED_BY_QUOTA");
                }
            }
            else
            {
                if (txtVramReadout != null) txtVramReadout.text = "0.0 GB / 16.0 GB";
                if (imgVramProgress != null) imgVramProgress.fillAmount = 0f;
                if (txtQuotaPercentage != null) txtQuotaPercentage.text = "0.0% Consumed";
                if (imgQuotaProgress != null)
                {
                    imgQuotaProgress.fillAmount = 0f;
                    imgQuotaProgress.color = Color.cyan;
                }
                if (txtSessionTime != null) txtSessionTime.text = "00:00 / 90:00";
                if (emergencyWarningBanner != null) emergencyWarningBanner.SetActive(false);
            }

            // 4. Update Checkpoint NumPy logs
            if (txtCheckpointLogs != null && data.checkpoint_history != null)
            {
                string logText = "";
                int showCount = Mathf.Min(data.checkpoint_history.Count, 3);
                for (int i = data.checkpoint_history.Count - showCount; i < data.checkpoint_history.Count; i++)
                {
                    if (i >= 0)
                    {
                        var cp = data.checkpoint_history[i];
                        logText += $"[Step {cp.step}] Saved metadata JSON & coordinates NPY\n";
                    }
                }
                txtCheckpointLogs.text = logText;
            }
        }
    }
}
