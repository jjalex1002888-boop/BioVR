using UnityEngine;

namespace BioVR.Macro
{
    [RequireComponent(typeof(LineRenderer))]
    public class HolographicTether : MonoBehaviour
    {
        [Header("Tether Visual Style")]
        public Color tetherColor = new Color(0.0f, 0.7f, 1.0f, 0.8f);
        public float startWidth = 0.015f;
        public float endWidth = 0.005f;
        public int curveSegments = 20;

        [Header("Curvature Configuration")]
        public float bezierControlOffset = 0.5f;

        private LineRenderer lineRenderer;
        private Transform activeTransform;
        private Transform parentTransform;
        private Vector3 restLocalPos;
        private bool isTetherActive = false;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = curveSegments;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.useWorldSpace = true;
            
            // Standard dynamic glow material setup
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            Material lineMat = new Material(unlitShader);
            lineMat.color = tetherColor;
            lineMat.SetFloat("_Surface", 1.0f); // Transparent
            lineMat.SetFloat("_Blend", 0.0f);   // Alpha blend
            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_ZWrite", 0);
            lineMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            lineMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            
            lineRenderer.material = lineMat;

            // Define holographic transparency fade gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(tetherColor, 0.0f), new GradientColorKey(tetherColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.1f, 1.0f) } // Fades out toward rest anchor
            );
            lineRenderer.colorGradient = gradient;
            lineRenderer.enabled = false;
        }

        public void SetAnchorPoints(Transform activeNode, Transform parentNode, Vector3 restingLocalPos)
        {
            activeTransform = activeNode;
            parentTransform = parentNode;
            restLocalPos = restingLocalPos;
        }

        public void SetTetherActive(bool active)
        {
            isTetherActive = active;
            lineRenderer.enabled = active;
        }

        private void Update()
        {
            if (!isTetherActive || activeTransform == null) return;

            // Get start point (current moving physical structure world pos)
            Vector3 startPoint = activeTransform.position;

            // Get end point (original rest anchor position in world space)
            Vector3 endPoint = parentTransform != null 
                ? parentTransform.TransformPoint(restLocalPos) 
                : restLocalPos;

            DrawBezierTether(startPoint, endPoint);
        }

        private void DrawBezierTether(Vector3 start, Vector3 end)
        {
            // Compute a holographic high-tech Bezier arch control point
            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            Vector3 controlPoint = midPoint + Vector3.up * bezierControlOffset;

            // Calculate points along Bezier curve
            for (int i = 0; i < curveSegments; i++)
            {
                float t = i / (float)(curveSegments - 1);
                
                // Quadratic Bezier interpolation: B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                Vector3 point = Mathf.Pow(1f - t, 2f) * start + 
                                2f * (1f - t) * t * controlPoint + 
                                Mathf.Pow(t, 2f) * end;
                                
                lineRenderer.SetPosition(i, point);
            }
        }
    }
}
