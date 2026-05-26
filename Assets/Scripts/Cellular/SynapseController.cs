using UnityEngine;

namespace BioVR.Cellular
{
    public class SynapseController : MonoBehaviour
    {
        [Header("Synaptic Terminal Components")]
        public Transform preSynapticTerminal;
        public Transform postSynapticTerminal;

        [Header("Explosion Mechanics")]
        [Range(0f, 1f)] public float explosionFactor = 0.0f;
        public float maxSeparationDistance = 2.0f;
        public float lerpSpeed = 5.0f;

        [Header("Procedural Cleft Visuals")]
        public Color cleftGlowColor = new Color(0.0f, 1.0f, 0.5f, 0.2f);
        
        private Vector3 preRestLocalPos;
        private Vector3 postRestLocalPos;

        private void Start()
        {
            // Cache rest positions
            if (preSynapticTerminal != null)
            {
                preRestLocalPos = preSynapticTerminal.localPosition;
            }
            if (postSynapticTerminal != null)
            {
                postRestLocalPos = postSynapticTerminal.localPosition;
            }
        }

        private void Update()
        {
            // Smoothly Lerp positions based on current explosionFactor slider value
            if (preSynapticTerminal != null)
            {
                Vector3 targetPre = preRestLocalPos + Vector3.up * (explosionFactor * maxSeparationDistance);
                preSynapticTerminal.localPosition = Vector3.Lerp(preSynapticTerminal.localPosition, targetPre, Time.deltaTime * lerpSpeed);
            }

            if (postSynapticTerminal != null)
            {
                Vector3 targetPost = postRestLocalPos + Vector3.down * (explosionFactor * maxSeparationDistance);
                postSynapticTerminal.localPosition = Vector3.Lerp(postSynapticTerminal.localPosition, targetPost, Time.deltaTime * lerpSpeed);
            }
        }

        // Exposed public method to set the explosion factor directly from URP UI Sliders
        public void SetExplosionFactor(float value)
        {
            explosionFactor = Mathf.Clamp01(value);
        }
    }
}
