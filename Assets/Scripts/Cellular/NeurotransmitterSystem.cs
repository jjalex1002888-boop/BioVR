using UnityEngine;
using System.Collections.Generic;
using BioVR.Dynamics;

namespace BioVR.Cellular
{
    public class NeurotransmitterSystem : MonoBehaviour
    {
        [Header("Dependency References")]
        public SynapseController synapseController;
        public WilsonCowanSolver neuralSolver;
        public ParticleSystem transmitterParticleSystem;

        [Header("Vesicle Configuration")]
        public int vesiclePoolSize = 6;
        public float vesicleSpeed = 0.5f;
        public float fusionThresholdY = 0.05f; // Y limit inside pre-synaptic terminal
        
        [Header("Receptor Channels")]
        public Transform postSynapticMembrane;
        public float bindingDistanceThreshold = 0.15f;

        private List<Transform> activeVesicles = new List<Transform>();
        private List<Vector3> vesicleStartLocalPositions = new List<Vector3>();

        private void Start()
        {
            InitializeVesiclePool();

            // Dynamic URP Particle material configuration to resolve pink vesicles
            if (transmitterParticleSystem != null)
            {
                ParticleSystemRenderer psRenderer = transmitterParticleSystem.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (particleShader == null) particleShader = Shader.Find("Unlit/Color");

                    if (particleShader != null)
                    {
                        Material particleMat = new Material(particleShader);
                        particleMat.name = "Transmitter_Particle_Material";
                        // Gorgeous glowing light-green neurotransmitter profile color
                        particleMat.color = new Color(0.1f, 1.0f, 0.4f, 0.9f);

                        particleMat.SetFloat("_Surface", 1.0f); // Transparent
                        particleMat.SetFloat("_Blend", 1.0f);   // Additive
                        particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        particleMat.SetInt("_ZWrite", 0);

                        particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        particleMat.EnableKeyword("_ALPHABLEND_ON");
                        particleMat.DisableKeyword("_ALPHATEST_ON");

                        psRenderer.sharedMaterial = particleMat;
                    }
                }
            }
        }

        private void InitializeVesiclePool()
        {
            if (synapseController == null || synapseController.preSynapticTerminal == null) return;

            Transform preTerminal = synapseController.preSynapticTerminal;

            // Generate glowing procedural vesicles (spheres) inside the pre-synaptic terminal
            for (int i = 0; i < vesiclePoolSize; i++)
            {
                GameObject vesicle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vesicle.name = $"Vesicle_{i}";
                vesicle.transform.SetParent(preTerminal, false);
                
                // Random position in the upper pre-synaptic terminal zone
                Vector3 localPos = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    Random.Range(0.3f, 0.7f),
                    Random.Range(-0.4f, 0.4f)
                );
                vesicle.transform.localPosition = localPos;
                vesicle.transform.localScale = Vector3.one * 0.12f;

                // Glowing green neurotransmitter material profile
                MeshRenderer mr = vesicle.GetComponent<MeshRenderer>();
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
                Material vMat = new Material(unlitShader);
                vMat.color = new Color(0.1f, 1.0f, 0.4f, 0.9f);
                mr.sharedMaterial = vMat;

                activeVesicles.Add(vesicle.transform);
                vesicleStartLocalPositions.Add(localPos);
            }
        }

        private void Update()
        {
            if (synapseController == null || activeVesicles.Count == 0) return;

            float dt = Time.deltaTime;
            
            // If TimeWarpManager exists, apply its active scaling factor
            if (TimeWarpManager.Instance != null)
            {
                dt = Time.deltaTime * TimeWarpManager.Instance.timeScaleFactor;
            }

            // Move each vesicle down towards the pre-synaptic active zone (bottom edge)
            for (int i = 0; i < activeVesicles.Count; i++)
            {
                Transform vesicle = activeVesicles[i];
                if (vesicle == null) continue;

                // Slowly migrate down along local -Y axis
                vesicle.localPosition += Vector3.down * vesicleSpeed * dt;

                // Check for fusion at the active membrane boundary (approx local Y = 0.05)
                if (vesicle.localPosition.y <= fusionThresholdY)
                {
                    FuseVesicle(vesicle, i);
                }
            }
        }

        private void FuseVesicle(Transform vesicle, int poolIndex)
        {
            // 1. Trigger vesicle fusion visual effect (burst particles into cleft)
            if (transmitterParticleSystem != null)
            {
                // Align particle emission to fusion site
                transmitterParticleSystem.transform.position = vesicle.position;
                
                // Blast neurotransmitters down into the cleft
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = vesicle.position;
                transmitterParticleSystem.Emit(emitParams, 30);
            }

            // 2. Simulate post-synaptic channel binding and influx
            // This triggers depolarizing feedback back to the macro population solvers!
            if (neuralSolver != null)
            {
                // Simulate EPSP influx, exciting the Wilson-Cowan population spike
                neuralSolver.excitatoryState = Mathf.Clamp01(neuralSolver.excitatoryState + 0.08f);
            }

            // 3. Reset/recycle vesicle back to the reserve pool (vesicle recycling / endocytosis)
            vesicle.localPosition = vesicleStartLocalPositions[poolIndex];
        }
    }
}
