using UnityEngine;
using BioVR.UI;

namespace BioVR.Macro
{
    public class BrainController : MonoBehaviour
    {
        [Header("Procedural Folding Configuration")]
        public int longitudeSegments = 24;
        public int latitudeSegments = 16;
        public float brainFoldingFrequency = 15.0f; // Sulci/gyri folding rate
        public float brainFoldingAmplitude = 0.05f; // Fold depth



        private void Start()
        {
            // Attach unified ModelIdleAnimation to drive floating and rotation
            var idle = GetComponent<BioVR.UI.ModelIdleAnimation>();
            if (idle == null)
            {
                idle = gameObject.AddComponent<BioVR.UI.ModelIdleAnimation>();
                idle.modelType = "Brain";
                idle.bounceAmplitude = 0.08f;
                idle.rotationSpeed = 15.0f;
            }

            // Build the multi-lobed ultra-realistic brain hierarchy
            GenerateRealisticAnatomicalBrain();
        }

        private void Update()
        {
            // Idle movement and rotation are now handled completely by ModelIdleAnimation to prevent competing transformations!
        }

        private void GenerateRealisticAnatomicalBrain()
        {
            Debug.Log("[BioVR Brain] Commencing programmatic generation of highly realistic lobed brain with realistic pink diagrammatic colors...");

            // Realistic anatomical diagram soft pink palette (subtle shading variants for clear boundaries)
            Color frontalColor = new Color(0.96f, 0.70f, 0.72f, 0.85f);     // Soft Rose Pink
            Color parietalColor = new Color(0.94f, 0.65f, 0.68f, 0.85f);    // Medium Soft Pink
            Color occipitalColor = new Color(0.92f, 0.60f, 0.64f, 0.85f);   // Deep Rose Pink
            Color temporalColor = new Color(0.95f, 0.68f, 0.70f, 0.85f);    // Soft Coral Pink
            Color cerebellumColor = new Color(0.86f, 0.52f, 0.58f, 0.88f);  // Darker Purplish Pink
            Color brainstemColor = new Color(0.96f, 0.94f, 0.90f, 0.90f);   // White-matter Cream/Vanilla

            // Build Lobe 1: Frontal Lobe (Anterior/Front)
            CreateLobePart("Frontal_Lobe", new Vector3(1.3f, 1.1f, 1.1f), new Vector3(0f, 0.15f, 0.3f), 
                brainFoldingFrequency, brainFoldingAmplitude, frontalColor, new Vector3(0f, 0.4f, 0.8f), "Frontal Lobe", "frontal");

            // Build Lobe 2: Parietal Lobe (Superior/Top-Back)
            CreateLobePart("Parietal_Lobe", new Vector3(1.2f, 1.0f, 1.0f), new Vector3(0f, 0.35f, -0.15f), 
                brainFoldingFrequency, brainFoldingAmplitude, parietalColor, new Vector3(0f, 0.8f, -0.2f), "Parietal Lobe", "parietal");

            // Build Lobe 3: Occipital Lobe (Posterior/Back)
            CreateLobePart("Occipital_Lobe", new Vector3(1.1f, 0.9f, 0.9f), new Vector3(0f, 0.1f, -0.5f), 
                brainFoldingFrequency, brainFoldingAmplitude, occipitalColor, new Vector3(0f, 0f, -1.0f), "Occipital Lobe", "occipital");

            // Build Lobe 4: Left Temporal Lobe (Lateral Left)
            CreateLobePart("Left_Temporal_Lobe", new Vector3(0.7f, 0.7f, 1.0f), new Vector3(-0.45f, -0.05f, 0.1f), 
                brainFoldingFrequency - 3.0f, brainFoldingAmplitude, temporalColor, new Vector3(-1.0f, 0f, 0f), "Left Temporal Lobe", "temporal_l");

            // Build Lobe 5: Right Temporal Lobe (Lateral Right)
            CreateLobePart("Right_Temporal_Lobe", new Vector3(0.7f, 0.7f, 1.0f), new Vector3(0.45f, -0.05f, 0.1f), 
                brainFoldingFrequency - 3.0f, brainFoldingAmplitude, temporalColor, new Vector3(1.0f, 0f, 0f), "Right Temporal Lobe", "temporal_r");

            // Build Lobe 6: Cerebellum (Inferior-Posterior / Bottom-Back - micro wrinkly folds!)
            CreateLobePart("Cerebellum", new Vector3(1.0f, 0.65f, 0.8f), new Vector3(0f, -0.3f, -0.35f), 
                brainFoldingFrequency * 1.8f, brainFoldingAmplitude * 0.7f, cerebellumColor, new Vector3(0f, -0.6f, -0.6f), "Cerebellum", "cerebellum");

            // Build Lobe 7: Brainstem (Inferior-Anterior / Bottom-Center - smooth cylindrical)
            CreateLobePart("Brainstem", new Vector3(0.35f, 0.8f, 0.35f), new Vector3(0f, -0.45f, 0.05f), 
                0.0f, 0.0f, brainstemColor, new Vector3(0f, -1.0f, 0f), "Brainstem", "brainstem");
        }

        private void CreateLobePart(string partName, Vector3 scale, Vector3 offset, float foldFreq, float foldAmp, Color color, Vector3 pullAxis, string displayName, string structureId)
        {
            GameObject lobeObj = new GameObject(partName);
            lobeObj.transform.SetParent(transform, false);
            lobeObj.transform.localPosition = offset;
            
            // Build procedural sphere mesh with sinusoidal folding noise
            MeshFilter filter = lobeObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = lobeObj.AddComponent<MeshRenderer>();
            
            Mesh mesh = GenerateLobeMesh(scale, foldFreq, foldAmp);
            mesh.name = partName + "_Mesh";
            filter.sharedMesh = mesh;

            // Generate URP-compatible glowing translucent material
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            Material mat = new Material(unlitShader);
            mat.color = color;
            mat.SetFloat("_Surface", 1.0f); // Transparent
            mat.SetFloat("_Blend", 0.0f);   // Alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Apply slight emission to look holographic
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.15f);
            
            renderer.sharedMaterial = mat;

            // Add standard Sphere Collider for Raycasting hover and click pulls
            SphereCollider sc = lobeObj.AddComponent<SphereCollider>();
            sc.center = Vector3.zero;
            // Fits collider roughly to actual bounds
            sc.radius = Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;

            // Hook up IsolatableStructure script for interactive exploded curves
            IsolatableStructure structure = lobeObj.AddComponent<IsolatableStructure>();
            structure.structureName = displayName;
            structure.structureId = structureId;
            structure.pullAxis = pullAxis;
            structure.pullDistance = 1.3f;
            structure.hoverGlowColor = Color.white;
        }

        private Mesh GenerateLobeMesh(Vector3 ellipsoidalScale, float foldingFrequency, float foldingAmplitude)
        {
            Mesh mesh = new Mesh();
            int lonSegments = longitudeSegments;
            int latSegments = latitudeSegments;

            int vertexCount = (lonSegments + 1) * (latSegments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];

            int v = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = lat * Mathf.PI / latSegments;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = lon * Mathf.PI * 2f / lonSegments;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    // Base sphere normal
                    Vector3 n = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
                    
                    // Ellipsoid scaling
                    Vector3 p = Vector3.Scale(n, ellipsoidalScale * 0.5f);

                    // Sine folding mathematical noise for gyri & sulci wrinkles
                    if (foldingFrequency > 0.05f)
                    {
                        float foldX = p.x * foldingFrequency;
                        float foldY = p.y * foldingFrequency;
                        float foldZ = p.z * foldingFrequency;
                        
                        // Layered folding harmonics
                        float folds = Mathf.Sin(foldX) * Mathf.Cos(foldY) * Mathf.Sin(foldZ);
                        folds += 0.3f * Mathf.Sin(foldX * 2.0f) * Mathf.Cos(foldY * 2.0f);
                        
                        p += n * folds * foldingAmplitude;
                    }

                    vertices[v] = p;
                    normals[v] = n;
                    uvs[v] = new Vector2((float)lon / lonSegments, (float)lat / latSegments);
                    v++;
                }
            }

            int triangleCount = lonSegments * latSegments * 6;
            int[] triangles = new int[triangleCount];
            int t = 0;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * (lonSegments + 1) + lon;
                    int next = current + 1;
                    int bottom = current + lonSegments + 1;
                    int bottomNext = bottom + 1;

                    // Triangle 1
                    triangles[t++] = current;
                    triangles[t++] = bottom;
                    triangles[t++] = next;

                    // Triangle 2
                    triangles[t++] = next;
                    triangles[t++] = bottom;
                    triangles[t++] = bottomNext;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            
            // Build sub-mesh for wireframe overlays if needed, or recalculate
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
