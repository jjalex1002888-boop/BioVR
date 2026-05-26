using UnityEngine;
using BioVR.Dynamics;

namespace BioVR.Macro
{
    [RequireComponent(typeof(ParticleSystem))]
    public class WhiteMatterTractParticles : MonoBehaviour
    {
        [Header("Dependency References")]
        public WilsonCowanSolver neuralSolver;

        [Header("Base Particle Modulators")]
        public float baseEmissionRate = 50f;
        public float baseSpeed = 2f;
        public float baseSize = 0.05f;

        [Header("Hormone Visual Profiles")]
        public Color dopamineColor = new Color(1.0f, 0.8f, 0.0f, 0.8f); // Golden glow
        public Color serotoninColor = new Color(0.5f, 0.3f, 1.0f, 0.8f); // Electric violet
        public Color cortisolColor = new Color(1.0f, 0.1f, 0.2f, 0.8f); // Crimson red

        private ParticleSystem tractParticles;
        private ParticleSystem.EmissionModule emissionModule;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.NoiseModule noiseModule;

        private void Start()
        {
            tractParticles = GetComponent<ParticleSystem>();
            emissionModule = tractParticles.emission;
            mainModule = tractParticles.main;
            noiseModule = tractParticles.noise;

            // Ensure noise is configured so we can modulate turbulence/jitter dynamically
            noiseModule.enabled = true;
            noiseModule.strength = 0.0f;
            noiseModule.frequency = 1.0f;

            // Configure URP additive particle material dynamically to eliminate pink textures
            ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (particleShader == null) particleShader = Shader.Find("Unlit/Color");

                if (particleShader != null)
                {
                    Material particleMat = new Material(particleShader);
                    particleMat.name = "WhiteMatter_Tract_Material";
                    particleMat.color = Color.white;

                    // Configure translucent additive blending
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

        private void Update()
        {
            if (neuralSolver == null) return;

            // 1. Read values from solver
            float excitation = neuralSolver.excitatoryState; // [0, 1]
            float dopamine = neuralSolver.dopamine;
            float serotonin = neuralSolver.serotonin;
            float cortisol = neuralSolver.cortisol;

            // 2. Modulate Emission Rate based on neural excitation
            // Higher excitation = spiking = rapid particle burst flow
            var rate = emissionModule.rateOverTime;
            rate.constant = baseEmissionRate * (0.2f + excitation * 2.8f);
            emissionModule.rateOverTime = rate;

            // 3. Modulate Speed based on excitation and dopamine
            // Dopamine speeds up neural signal transmission velocity
            mainModule.startSpeed = baseSpeed * (0.5f + (excitation * 0.5f) + (dopamine * 1.0f));

            // 4. Determine Dynamic Color blending based on active hormone levels
            // Synthesize visual identity through HSL-curated color weights
            Color targetColor = Color.white;
            float totalHormone = dopamine + serotonin + cortisol;

            if (totalHormone > 0.05f)
            {
                Color blended = (dopamineColor * dopamine + 
                               serotoninColor * serotonin + 
                               cortisolColor * cortisol) / totalHormone;
                
                // Blend with clear white base for glow opacity
                targetColor = Color.Lerp(Color.white, blended, 0.8f);
            }
            else
            {
                targetColor = new Color(0.7f, 0.85f, 1.0f, 0.5f); // Passive glowing light blue
            }

            mainModule.startColor = targetColor;

            // 5. Modulate Particle Turbulence (Jitter/Noise) based on Cortisol vs Serotonin
            // Cortisol induces hyper-frantic, erratic chaotic noise.
            // Serotonin suppresses noise, stabilizing pathways to calm flowing states.
            float noiseStrength = (cortisol * 0.8f) - (serotonin * 0.4f);
            noiseModule.strength = Mathf.Max(0.0f, noiseStrength);
            noiseModule.frequency = 1.0f + (cortisol * 2.0f); // Higher cortisol increases jitter frequency

            // 6. Modulate Particle Size slightly based on activity
            mainModule.startSize = baseSize * (0.8f + excitation * 0.4f);
        }
    }
}
