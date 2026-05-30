using UnityEngine;

namespace BioVR.Dynamics
{
    public class WilsonCowanSolver : MonoBehaviour
    {
        [Header("Active Hormone Levels")]
        [Range(0f, 1f)] public float dopamine = 0.5f;
        [Range(0f, 1f)] public float serotonin = 0.5f;
        [Range(0f, 1f)] public float cortisol = 0.1f;

        [Header("State Variables")]
        public float excitatoryState = 0.1f;
        public float inhibitoryState = 0.05f;
        public float membranePotential = -70.0f; // Vm in mV

        [Header("Time Constants")]
        public float tauE = 0.010f; // 10 ms
        public float tauI = 0.010f; // 10 ms

        [Header("Refractory & Threshold Parameters")]
        public float rE = 1.0f;
        public float rI = 1.0f;
        public float thetaERest = 1.0f;
        public float thetaIRest = 1.5f;
        public float aE = 1.2f;
        public float aI = 1.0f;

        [Header("Base Coupling Weights")]
        public float wEE_Base = 1.6f;
        public float wEI_Base = 1.5f;
        public float wIE_Base = 1.5f;
        public float wII_Base = 1.1f;

        [Header("External Stimulus")]
        public float externalE = 0.0f;
        public float externalI = 0.0f;

        // Current adjusted weights
        private float wEE, wEI, wIE, wII;
        private float thetaE, thetaI;

        private void Update()
        {
            // Modulate coupling constants and thresholds using current hormone levels
            ModulateParameters();

            // Perform numerical ODE integration using Euler step (linked to Unity dt and TimeWarp scaling)
            float dt = Time.deltaTime;
            if (TimeWarpManager.Instance != null)
            {
                if (TimeWarpManager.Instance.isPaused)
                {
                    dt = 0f;
                }
                else
                {
                    dt *= TimeWarpManager.Instance.timeScaleFactor;
                }
            }

            if (dt > 0f)
            {
                Integrate(dt);
            }

            // Sync state back to membrane potential readout
            // Normal resting potential is -70mV, peak spike activation at +40mV
            membranePotential = -70.0f + (excitatoryState * 110.0f);
        }

        private void ModulateParameters()
        {
            // Dopamine modulates the self-excitation gain and lowers the excitation threshold
            wEE = wEE_Base * (1.0f + dopamine * 0.8f);
            thetaE = thetaERest * (1.0f - dopamine * 0.4f);

            // Serotonin enhances inhibitory coupling and increases stability thresholds
            wII = wII_Base * (1.0f + serotonin * 0.5f);
            thetaI = thetaIRest * (1.0f + serotonin * 0.3f);

            // Cortisol adds noise and direct baseline bias/stress stimulus
            float cortisolNoise = (Random.value - 0.5f) * cortisol * 0.2f;
            wEI = wEI_Base * (1.0f - cortisol * 0.2f); // decreases feedback inhibition slightly
            wIE = wIE_Base;

            externalE = (cortisol * 0.5f) + cortisolNoise;
            externalI = cortisol * 0.1f;
        }

        private void Integrate(float dt)
        {
            // Calculate sigmoidal response functions
            float inputE = wEE * excitatoryState - wEI * inhibitoryState + externalE;
            float inputI = wIE * excitatoryState - wII * inhibitoryState + externalI;

            float sE = 1.0f / (1.0f + Mathf.Exp(-aE * (inputE - thetaE)));
            float sI = 1.0f / (1.0f + Mathf.Exp(-aI * (inputI - thetaI)));

            // Compute rates of change
            float dE_dt = (-excitatoryState + (1.0f - rE * excitatoryState) * sE) / tauE;
            float dI_dt = (-inhibitoryState + (1.0f - rI * inhibitoryState) * sI) / tauI;

            // Apply Euler step, clamping states in [0, 1] bounds
            excitatoryState = Mathf.Clamp01(excitatoryState + dE_dt * dt);
            inhibitoryState = Mathf.Clamp01(inhibitoryState + dI_dt * dt);
        }
    }
}
