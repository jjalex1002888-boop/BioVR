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


        [System.Serializable]
        public struct HighFidelityStructureDef
        {
            public string id;          // GameObject name in GLTF
            public string displayName; // User-facing name
            public Vector3 pullAxis;   // Direction of explode/pull (Unity coordinates: x, z, y)
            public float pullDistance; // Distance of pull
            public Color hoverColor;   // Interactive hover glow color
        }

        [Header("High-Fidelity Anatomical Mapping")]
        public System.Collections.Generic.List<HighFidelityStructureDef> highFidelityStructures = new System.Collections.Generic.List<HighFidelityStructureDef>();

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

            // Populate high-fidelity list definitions dynamically
            InitializeHighFidelityMappings();

            // Try to hook up high-fidelity imported meshes from the GLB
            bool hasHighFidelity = TryHookUpHighFidelityMeshes();

            // Only build programmatic ellipsoidal fallback lobes if no high-fidelity meshes were found
            if (!hasHighFidelity)
            {
                GenerateRealisticAnatomicalBrain();
            }
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
            // Try to find an existing child by name matching this anatomical structure (e.g., from imported high-fidelity Blender blend/fbx model!)
            Transform existingChild = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.ToLower().Contains(structureId.Replace("_l", "").Replace("_r", "").ToLower()) || 
                    child.name.ToLower().Replace(" ", "_").Contains(partName.ToLower()))
                {
                    existingChild = child;
                    break;
                }
            }

            GameObject lobeObj;
            if (existingChild != null)
            {
                lobeObj = existingChild.gameObject;
                Debug.Log($"[BioVR Brain] Hooked high-fidelity Blender geometry for: '{displayName}' (GameObject name: '{lobeObj.name}')");
                
                // Add Sphere/Box Collider to existing child if missing
                Collider col = lobeObj.GetComponent<Collider>();
                if (col == null)
                {
                    Renderer rend = lobeObj.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        BoxCollider bc = lobeObj.AddComponent<BoxCollider>();
                        bc.center = lobeObj.transform.InverseTransformPoint(rend.bounds.center);
                        bc.size = rend.bounds.size;
                    }
                    else
                    {
                        SphereCollider sc = lobeObj.AddComponent<SphereCollider>();
                        sc.center = Vector3.zero;
                        sc.radius = Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;
                    }
                }
            }
            else
            {
                // FALLBACK: Generate the mathematical pink lobe meshes procedurally
                lobeObj = new GameObject(partName);
                lobeObj.transform.SetParent(transform, false);
                lobeObj.transform.localPosition = offset;
                
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

                SphereCollider sc = lobeObj.AddComponent<SphereCollider>();
                sc.center = Vector3.zero;
                sc.radius = Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;
            }

            // Hook up IsolatableStructure script for interactive exploded curves
            IsolatableStructure structure = lobeObj.GetComponent<IsolatableStructure>();
            if (structure == null)
            {
                structure = lobeObj.AddComponent<IsolatableStructure>();
            }
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

        private void InitializeHighFidelityMappings()
        {
            highFidelityStructures.Clear();

            // Neocortex lobes
            AddDef("Cerebrum_Left", "Left Cerebral Hemisphere", new Vector3(-0.65f, 0.0f, 0.20f), 1.2f, new Color(0.95f, 0.40f, 0.65f));
            AddDef("Cerebrum_Right", "Right Cerebral Hemisphere", new Vector3(0.65f, 0.0f, 0.20f), 1.2f, new Color(0.95f, 0.40f, 0.65f));
            
            // Cerebellum
            AddDef("Cerebellum", "Cerebellum", new Vector3(0.0f, -0.60f, -0.25f), 1.3f, new Color(0.70f, 0.18f, 0.28f));
            
            // Commissural/Bridging White Matter
            AddDef("Corpus_Callosum", "Corpus Callosum", new Vector3(0.0f, 0.0f, 0.40f), 1.2f, new Color(0.95f, 0.92f, 0.88f));
            AddDef("Fornix", "Fornix (Limbic Bridge)", new Vector3(0.0f, -0.04f, 0.28f), 1.3f, new Color(0.95f, 0.92f, 0.88f));
            AddDef("Pineal_Stalks", "Pineal Stalks", new Vector3(0.0f, -0.24f, 0.06f), 1.2f, new Color(0.95f, 0.92f, 0.88f));
            AddDef("Spinal_Nerve_Rootlets", "Spinal Nerve Rootlets", new Vector3(0.0f, -0.16f, -0.70f), 1.3f, new Color(0.95f, 0.92f, 0.88f));

            // Thalamus and adhesion
            AddDef("Thalamus_Left", "Left Thalamus", new Vector3(-0.20f, 0.08f, 0.15f), 1.3f, new Color(0.42f, 0.32f, 0.85f));
            AddDef("Thalamus_Right", "Right Thalamus", new Vector3(0.20f, 0.08f, 0.15f), 1.3f, new Color(0.42f, 0.32f, 0.85f));
            AddDef("Interthalamic_Adhesion", "Interthalamic Adhesion", new Vector3(0.0f, 0.04f, 0.20f), 1.2f, new Color(0.42f, 0.32f, 0.85f));

            // Basal Ganglia
            AddDef("Caudate_Left", "Left Caudate Nucleus", new Vector3(-0.30f, 0.04f, 0.25f), 1.3f, new Color(0.98f, 0.44f, 0.52f));
            AddDef("Caudate_Right", "Right Caudate Nucleus", new Vector3(0.30f, 0.04f, 0.25f), 1.3f, new Color(0.98f, 0.44f, 0.52f));
            AddDef("Putamen_Left", "Left Putamen", new Vector3(-0.40f, -0.04f, 0.12f), 1.3f, new Color(0.88f, 0.11f, 0.28f));
            AddDef("Putamen_Right", "Right Putamen", new Vector3(0.40f, -0.04f, 0.12f), 1.3f, new Color(0.88f, 0.11f, 0.28f));
            AddDef("Globus_Pallidus_Left", "Left Globus Pallidus", new Vector3(-0.26f, -0.04f, 0.08f), 1.3f, new Color(0.99f, 0.64f, 0.69f));
            AddDef("Globus_Pallidus_Right", "Right Globus Pallidus", new Vector3(0.26f, -0.04f, 0.08f), 1.3f, new Color(0.99f, 0.64f, 0.69f));
            
            // Limbic & Reward Systems
            AddDef("Nucleus_Accumbens_Left", "Left Nucleus Accumbens", new Vector3(-0.30f, 0.16f, -0.04f), 1.3f, new Color(0.02f, 0.84f, 0.63f));
            AddDef("Nucleus_Accumbens_Right", "Right Nucleus Accumbens", new Vector3(0.30f, 0.16f, -0.04f), 1.3f, new Color(0.02f, 0.84f, 0.63f));
            AddDef("Hippocampus_Left", "Left Hippocampus", new Vector3(-0.30f, 0.12f, -0.08f), 1.3f, new Color(0.10f, 0.68f, 0.48f));
            AddDef("Hippocampus_Right", "Right Hippocampus", new Vector3(0.30f, 0.12f, -0.08f), 1.3f, new Color(0.10f, 0.68f, 0.48f));
            AddDef("Amygdala_Left", "Left Amygdala", new Vector3(-0.34f, 0.20f, -0.12f), 1.3f, new Color(0.10f, 0.68f, 0.48f));
            AddDef("Amygdala_Right", "Right Amygdala", new Vector3(0.34f, 0.20f, -0.12f), 1.3f, new Color(0.10f, 0.68f, 0.48f));

            // Homeostatic / Endocrine Control Systems
            AddDef("Hypothalamus", "Hypothalamus", new Vector3(0.0f, 0.16f, 0.08f), 1.3f, new Color(0.95f, 0.42f, 0.08f));
            AddDef("Infundibulum_Stalk", "Infundibular Stalk", new Vector3(0.0f, 0.20f, -0.04f), 1.3f, new Color(0.95f, 0.42f, 0.08f));
            AddDef("Pituitary_Gland", "Pituitary Gland", new Vector3(0.0f, 0.28f, -0.12f), 1.3f, new Color(0.08f, 0.68f, 0.85f));
            AddDef("Pineal_Gland", "Pineal Gland", new Vector3(0.0f, -0.28f, 0.08f), 1.3f, new Color(0.92f, 0.78f, 0.12f));

            // Brainstem Structures
            AddDef("Midbrain", "Midbrain (Mesencephalon)", new Vector3(0.0f, 0.12f, -0.08f), 1.3f, new Color(0.55f, 0.62f, 0.70f));
            AddDef("Pons", "Pons", new Vector3(0.0f, 0.20f, -0.16f), 1.3f, new Color(0.55f, 0.62f, 0.70f));
            AddDef("Medulla_Oblongata", "Medulla Oblongata", new Vector3(0.0f, 0.12f, -0.32f), 1.3f, new Color(0.55f, 0.62f, 0.70f));
            AddDef("Spinal_Cord", "Spinal Cord (CNS Axis)", new Vector3(0.0f, -0.08f, -0.55f), 1.3f, new Color(0.55f, 0.62f, 0.70f));
            
            // Midbrain specialized dopaminergic/motor cores
            AddDef("VTA", "Ventral Tegmental Area (VTA)", new Vector3(0.0f, 0.08f, -0.08f), 1.3f, new Color(0.96f, 0.62f, 0.04f));
            AddDef("Substantia_Nigra_Left", "Left Substantia Nigra", new Vector3(-0.12f, 0.04f, -0.12f), 1.3f, new Color(0.28f, 0.33f, 0.41f));
            AddDef("Substantia_Nigra_Right", "Right Substantia Nigra", new Vector3(0.12f, 0.04f, -0.12f), 1.3f, new Color(0.28f, 0.33f, 0.41f));
        }

        private void AddDef(string id, string name, Vector3 pullAxis, float distance, Color color)
        {
            HighFidelityStructureDef def = new HighFidelityStructureDef();
            def.id = id;
            def.displayName = name;
            def.pullAxis = pullAxis;
            def.pullDistance = distance;
            def.hoverColor = color;
            highFidelityStructures.Add(def);
        }

        private bool TryHookUpHighFidelityMeshes()
        {
            bool foundAny = false;

            // Dictionary for O(1) matching speed
            var defs = new System.Collections.Generic.Dictionary<string, HighFidelityStructureDef>();
            foreach (var def in highFidelityStructures)
            {
                defs[def.id.ToLower()] = def;
            }

            // Traverse recursively to handle imported GLTF prefab structures nested deep in parent
            System.Collections.Generic.List<Transform> allChildren = new System.Collections.Generic.List<Transform>();
            GetChildrenRecursive(transform, allChildren);

            foreach (Transform child in allChildren)
            {
                string childName = child.name.ToLower();
                HighFidelityStructureDef matchedDef;
                bool isMatch = false;

                if (defs.TryGetValue(childName, out matchedDef))
                {
                    isMatch = true;
                }
                else
                {
                    // Loose prefix/contains checking to handle import suffixes gracefully
                    foreach (var pair in defs)
                    {
                        if (childName.Contains(pair.Key.ToLower()))
                        {
                            matchedDef = pair.Value;
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    foundAny = true;
                    HookUpHighFidelityPart(child.gameObject, matchedDef);
                }
            }

            if (foundAny)
            {
                Debug.Log($"[BioVR Brain] Successfully initialized high-fidelity interactive hooks for custom GLB meshes.");
            }
            return foundAny;
        }

        private void GetChildrenRecursive(Transform parent, System.Collections.Generic.List<Transform> list)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                list.Add(child);
                GetChildrenRecursive(child, list);
            }
        }

        private void HookUpHighFidelityPart(GameObject partObj, HighFidelityStructureDef def)
        {
            Debug.Log($"[BioVR Brain] Hooking interactive scripts to high-fidelity node: '{def.displayName}' ({partObj.name})");

            // 1. Ensure Raycasting physics collider is present
            Collider col = partObj.GetComponent<Collider>();
            if (col == null)
            {
                Renderer rend = partObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    BoxCollider bc = partObj.AddComponent<BoxCollider>();
                    bc.center = partObj.transform.InverseTransformPoint(rend.bounds.center);
                    bc.size = rend.bounds.size;
                }
                else
                {
                    SphereCollider sc = partObj.AddComponent<SphereCollider>();
                    sc.center = Vector3.zero;
                    sc.radius = 0.5f;
                }
            }

            // 2. Attach IsolatableStructure components for exploded views
            IsolatableStructure structure = partObj.GetComponent<IsolatableStructure>();
            if (structure == null)
            {
                structure = partObj.AddComponent<IsolatableStructure>();
            }
            structure.structureName = def.displayName;
            structure.structureId = def.id;
            structure.pullAxis = def.pullAxis;
            structure.pullDistance = def.pullDistance;
            structure.hoverGlowColor = def.hoverColor;
        }
    }
}
