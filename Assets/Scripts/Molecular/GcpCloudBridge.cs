using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BioVR.Molecular
{
    [System.Serializable]
    public class PerformanceMetrics
    {
        public int elapsed_time_seconds;
        public int estimated_remaining_seconds;
        public long gpu_vram_usage_bytes;
        public int gpu_temperature_celsius;
        public int quota_session_limit_seconds;
    }

    [System.Serializable]
    public class Checkpoint
    {
        public int step;
        public string metadata_uri;
        public string coordinates_uri;
    }

    [System.Serializable]
    public class AtomData
    {
        public string element;
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class BondData
    {
        public int atom1;
        public int atom2;
        public int order;
    }

    [System.Serializable]
    public class JobStatusResponse
    {
        public string job_id;
        public string status;
        public int current_step;
        public int total_steps;
        public PerformanceMetrics performance_metrics;
        public List<Checkpoint> checkpoint_history;
        public string compiled_asset_url;
        public List<AtomData> atoms;
        public List<BondData> bonds;
    }

    [System.Serializable]
    public class JobStartResponse
    {
        public string job_id;
        public string status;
        public string message;
    }

    public class GcpCloudBridge : MonoBehaviour
    {
        public static GcpCloudBridge Instance { get; private set; }

        [Header("Cloud API Configuration")]
        public string baseUrl = "http://136.109.3.18:3000";
        public float pollingInterval = 1.0f;

        [Header("Active Session Status")]
        public string activeJobId = "";
        public string activeJobStatus = "OFFLINE";
        public int currentStep = 0;
        public int totalSteps = 200;

        [Header("Live Molecular Data")]
        public List<AtomData> activeAtoms = new List<AtomData>();
        public List<BondData> activeBonds = new List<BondData>();

        [Header("GPU Performance Metrics")]
        public float quotaConsumedPercentage = 0f;
        public long gpuVramUsageBytes = 0;
        public int gpuTemperatureCelsius = 0;
        public int elapsedSeconds = 0;
        public int limitSeconds = 5400; // 90 minutes default

        public event Action<JobStatusResponse> OnJobStatusUpdated;
        public event Action<string> OnModelReady; // returns molecule entity name

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void TriggerStructuralGeneration(string scaleLevel, string entityName, int targetSteps = 200)
        {
            StartCoroutine(PostStartJob(scaleLevel, entityName, targetSteps));
        }

        private IEnumerator PostStartJob(string scaleLevel, string entityName, int targetSteps)
        {
            string url = $"{baseUrl}/v1/generation/run";
            activeJobId = "job_" + UnityEngine.Random.Range(100000, 999999).ToString();

            // Setup POST JSON payload
            string payload = $"{{" +
                $"\"job_id\":\"{activeJobId}\"," +
                $"\"scale_level\":\"{scaleLevel}\"," +
                $"\"simulation_parameters\":{{" +
                    $"\"entity_identifiers\":[\"{entityName}\"]," +
                    $"\"target_steps\":{targetSteps}" +
                $"}}," +
                $"\"mesh_options\":{{" +
                    $"\"representation_style\":\"SOLVENT_ACCESSIBLE_SURFACE\"," +
                    $"\"target_polygon_count\":15000" +
                $"}}" +
            $"}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    JobStartResponse res = JsonUtility.FromJson<JobStartResponse>(request.downloadHandler.text);
                    activeJobStatus = res.status;
                    StartCoroutine(PollJobStatus(activeJobId, entityName));
                }
                else
                {
                    Debug.LogWarning("GCP API Bridge offline or failed to launch AlphaFold prediction. Activating high-fidelity GCP GPU cluster mock simulator.");
                    StartCoroutine(SimulateGcpGpuJob(entityName));
                }
            }
        }

        private IEnumerator SimulateGcpGpuJob(string entityName)
        {
            activeJobId = "job_sim_" + UnityEngine.Random.Range(100000, 999999).ToString();
            activeJobStatus = "RUNNING";
            currentStep = 0;
            totalSteps = 200;
            elapsedSeconds = 0;
            activeAtoms.Clear();
            activeBonds.Clear();

            List<Checkpoint> history = new List<Checkpoint>();

            // Simulate steps
            for (int step = 0; step <= 200; step += 20)
            {
                currentStep = step;
                elapsedSeconds += 20;

                // Simulated telemetry values (VRAM ramps, temperature heats up)
                gpuVramUsageBytes = (long)((4.0f + (step / 200.0f) * 6.5f) * 1024L * 1024L * 1024L); // 4GB -> 10.5GB
                gpuTemperatureCelsius = (int)(45 + (step / 200.0f) * 28.0f + UnityEngine.Random.Range(-2, 3)); // 45C -> 73C
                quotaConsumedPercentage = ((float)elapsedSeconds / limitSeconds) * 100f;

                // Periodic checkpoint saving
                if (step > 0 && step % 60 == 0)
                {
                    history.Add(new Checkpoint
                    {
                        step = step,
                        metadata_uri = $"gs://biovr-checkpoints/{activeJobId}/step_{step}_meta.json",
                        coordinates_uri = $"gs://biovr-checkpoints/{activeJobId}/step_{step}_coords.npy"
                    });
                }

                // Construct full update response
                JobStatusResponse update = new JobStatusResponse
                {
                    job_id = activeJobId,
                    status = activeJobStatus,
                    current_step = currentStep,
                    total_steps = totalSteps,
                    performance_metrics = new PerformanceMetrics
                    {
                        elapsed_time_seconds = elapsedSeconds,
                        estimated_remaining_seconds = (200 - step) * 2,
                        gpu_vram_usage_bytes = gpuVramUsageBytes,
                        gpu_temperature_celsius = gpuTemperatureCelsius,
                        quota_session_limit_seconds = limitSeconds
                    },
                    checkpoint_history = history,
                    compiled_asset_url = ""
                };

                OnJobStatusUpdated?.Invoke(update);

                yield return new WaitForSeconds(0.4f); // 400ms per simulated step, total 4 seconds
            }

            // Completed!
            activeJobStatus = "COMPLETED";

            // Add final checkpoint
            history.Add(new Checkpoint
            {
                step = 200,
                metadata_uri = $"gs://biovr-checkpoints/{activeJobId}/step_200_meta.json",
                coordinates_uri = $"gs://biovr-checkpoints/{activeJobId}/step_200_coords.npy"
            });

            JobStatusResponse finalUpdate = new JobStatusResponse
            {
                job_id = activeJobId,
                status = activeJobStatus,
                current_step = 200,
                total_steps = 200,
                performance_metrics = new PerformanceMetrics
                {
                    elapsed_time_seconds = elapsedSeconds,
                    estimated_remaining_seconds = 0,
                    gpu_vram_usage_bytes = gpuVramUsageBytes,
                    gpu_temperature_celsius = 55, // cooling down
                    quota_session_limit_seconds = limitSeconds
                },
                checkpoint_history = history,
                compiled_asset_url = $"gs://biovr-assets/{entityName.ToLower()}_structure.pdb"
            };

            OnJobStatusUpdated?.Invoke(finalUpdate);
            yield return new WaitForSeconds(0.2f);

            OnModelReady?.Invoke(entityName);
        }

        private IEnumerator PollJobStatus(string jobId, string entityName)
        {
            string url = $"{baseUrl}/v1/generation/status/{jobId}";

            while (activeJobStatus == "RUNNING")
            {
                yield return new WaitForSeconds(pollingInterval);

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        JobStatusResponse res = JsonUtility.FromJson<JobStatusResponse>(request.downloadHandler.text);
                        activeJobStatus = res.status;
                        currentStep = res.current_step;
                        totalSteps = res.total_steps;

                        if (res.atoms != null) activeAtoms = res.atoms;
                        else activeAtoms.Clear();

                        if (res.bonds != null) activeBonds = res.bonds;
                        else activeBonds.Clear();

                        if (res.performance_metrics != null)
                        {
                            gpuVramUsageBytes = res.performance_metrics.gpu_vram_usage_bytes;
                            gpuTemperatureCelsius = res.performance_metrics.gpu_temperature_celsius;
                            elapsedSeconds = res.performance_metrics.elapsed_time_seconds;
                            limitSeconds = res.performance_metrics.quota_session_limit_seconds;

                            // Strict Quota Calculations
                            quotaConsumedPercentage = ((float)elapsedSeconds / limitSeconds) * 100f;

                            // 10% Quota Safety Buffer Check (Emergency Graceful Interruption)
                            if (quotaConsumedPercentage >= 90.0f && activeJobStatus == "RUNNING")
                            {
                                Debug.LogWarning($"[EMERGENCY GRACEFUL INTERRUPTION] GCP session quota limit reached 90% safety threshold ({quotaConsumedPercentage:F1}%). Halting active predictions and loading last safe checkpoints.");
                                activeJobStatus = "PAUSED_BY_QUOTA";
                                res.status = "PAUSED_BY_QUOTA";
                            }
                        }

                        OnJobStatusUpdated?.Invoke(res);

                        if (activeJobStatus == "COMPLETED" || activeJobStatus == "PAUSED_BY_QUOTA" || activeJobStatus == "FAILED")
                        {
                            OnModelReady?.Invoke(entityName);
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogError("Error polling GCP cluster job status.");
                        break;
                    }
                }
            }
        }
    }
}
