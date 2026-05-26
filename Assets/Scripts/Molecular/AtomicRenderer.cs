using UnityEngine;
using System.Collections.Generic;
using BioVR.UI;

namespace BioVR.Molecular
{
    public class AtomicRenderer : MonoBehaviour
    {
        [Header("Visual Preferences")]
        public float atomScale = 0.12f;
        public float bondRadius = 0.03f;

        [Header("Atom Colors (CPK Coloring Standard)")]
        public Color carbonColor = new Color(0.3f, 0.3f, 0.3f);     // Dark Grey
        public Color hydrogenColor = Color.white;                  // White
        public Color oxygenColor = new Color(0.9f, 0.1f, 0.1f);      // Vibrant Red
        public Color nitrogenColor = new Color(0.1f, 0.3f, 0.9f);    // Electric Blue
        public Color receptorHelixColor = new Color(0.9f, 0.1f, 0.8f); // Glowing Magenta

        [Header("Advanced Lobe/Neuron Holograms")]
        public Color neuronSomaColor = new Color(0.8f, 0.4f, 1.0f, 0.9f);      // Glowing Violet
        public Color neuronDendriteColor = new Color(0.2f, 0.8f, 1.0f, 0.85f);  // Glowing Cyan
        public Color myelinSheathColor = new Color(0.0f, 0.6f, 1.0f, 0.95f);    // Glowing Blue
        public Color synapticBulbColor = new Color(0.1f, 1.0f, 0.4f, 0.95f);    // Glowing Green

        [Header("Comparative Workspace Controls")]
        public bool sideBySideComparisonMode = false;
        public bool uniformScaleAlignment = true;

        private List<GameObject> activeStructureRoots = new List<GameObject>();

        private void Start()
        {
            // Register callback with GcpCloudBridge
            if (GcpCloudBridge.Instance != null)
            {
                GcpCloudBridge.Instance.OnModelReady += RenderMolecule;
            }
        }

        private void OnDestroy()
        {
            if (GcpCloudBridge.Instance != null)
            {
                GcpCloudBridge.Instance.OnModelReady -= RenderMolecule;
            }
        }

        public void RenderMolecule(string name)
        {
            // 1. Manage Workspace Comparison Clearings
            if (!sideBySideComparisonMode)
            {
                ClearWorkspace();
            }

            Debug.Log($"[AtomicRenderer] Assembling model structure in comparative workspace: {name}");

            // 2. Setup structural offset
            Vector3 offset = Vector3.zero;
            if (sideBySideComparisonMode)
            {
                // Spawns side-by-side along the X-axis (1.5 meters spacing)
                offset = new Vector3(activeStructureRoots.Count * 1.5f, 0f, 0f);
            }

            // 3. Spawn Root GameObject for this specific entity
            GameObject structRootObj = new GameObject(name + "_Structure_Root");
            structRootObj.transform.SetParent(transform, false);
            structRootObj.transform.localPosition = offset;
            activeStructureRoots.Add(structRootObj);

            // 4. Generate visual elements into root
            string queryName = name.ToLower();
            if (queryName.Contains("dopamine"))
            {
                GenerateDopamineStructure(structRootObj);
            }
            else if (queryName.Contains("serotonin"))
            {
                GenerateSerotoninStructure(structRootObj);
            }
            else if (queryName.Contains("neuron"))
            {
                GenerateProceduralNeuron(structRootObj);
            }
            else
            {
                // Fallback / Complex receptor (Nicotinic receptor alpha-helices)
                GenerateProteinReceptorHelices(structRootObj);
            }

            // 5. Apply Uniform Scaling if checked
            if (uniformScaleAlignment)
            {
                // Normalize molecule scales to comfort proportions
                structRootObj.transform.localScale = Vector3.one;
            }
            else
            {
                // Custom scale based on type
                if (queryName.Contains("neuron")) structRootObj.transform.localScale = Vector3.one * 0.75f;
                else structRootObj.transform.localScale = Vector3.one * 1.2f;
            }

            // 6. Programmatically fit a Box Collider enclosing all spawned elements
            Bounds bounds = new Bounds(structRootObj.transform.position, Vector3.zero);
            Renderer[] renderers = structRootObj.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            BoxCollider bc = structRootObj.AddComponent<BoxCollider>();
            bc.center = structRootObj.transform.InverseTransformPoint(bounds.center);
            // Pad slightly for ease of clicking
            bc.size = bounds.size + Vector3.one * 0.05f;

            // 7. Attach dynamic DraggableObject and ModelRotator scripts for smooth 3D positioning and free rotations!
            structRootObj.AddComponent<DraggableObject>();
            structRootObj.AddComponent<ModelRotator>();
            Debug.Log($"[AtomicRenderer] Spawned interactive draggable structure at local offset {offset}. total active: {activeStructureRoots.Count}");
        }

        public void ClearWorkspace()
        {
            foreach (GameObject root in activeStructureRoots)
            {
                if (root != null) Destroy(root);
            }
            activeStructureRoots.Clear();
            Debug.Log("[AtomicRenderer] Comparative workspace purged.");
        }

        private void GenerateDopamineStructure(GameObject root)
        {
            Vector3[] carbons = new Vector3[8];
            float ringRadius = 0.4f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                carbons[i] = new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0f);
                CreateAtom(root, carbons[i], carbonColor, $"C_{i}");
            }

            carbons[6] = carbons[0] + new Vector3(0.35f, 0.2f, 0.1f);
            carbons[7] = carbons[6] + new Vector3(0.35f, -0.1f, -0.1f);
            CreateAtom(root, carbons[6], carbonColor, "C_Ethyl_1");
            CreateAtom(root, carbons[7], carbonColor, "C_Ethyl_2");

            Vector3 nitrogen = carbons[7] + new Vector3(0.3f, 0.1f, 0.05f);
            CreateAtom(root, nitrogen, nitrogenColor, "N_Amine");

            Vector3 oxygen1 = carbons[3] + new Vector3(-0.3f, 0.1f, 0f);
            Vector3 oxygen2 = carbons[4] + new Vector3(-0.3f, -0.1f, 0f);
            CreateAtom(root, oxygen1, oxygenColor, "O_Hydroxyl_1");
            CreateAtom(root, oxygen2, oxygenColor, "O_Hydroxyl_2");

            for (int i = 0; i < 6; i++) CreateBond(root, carbons[i], carbons[(i + 1) % 6]);
            CreateBond(root, carbons[0], carbons[6]);
            CreateBond(root, carbons[6], carbons[7]);
            CreateBond(root, carbons[7], nitrogen);
            CreateBond(root, carbons[3], oxygen1);
            CreateBond(root, carbons[4], oxygen2);
        }

        private void GenerateSerotoninStructure(GameObject root)
        {
            Vector3[] carbons = new Vector3[10];
            float ringRadius = 0.4f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                carbons[i] = new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0f);
                CreateAtom(root, carbons[i], carbonColor, $"C_Benz_{i}");
            }

            carbons[6] = carbons[0] + new Vector3(0.3f, 0.35f, 0f);
            Vector3 pyrroleNitrogen = carbons[1] + new Vector3(0.3f, -0.35f, 0f);
            carbons[7] = new Vector3(0.5f, 0.0f, 0f);

            CreateAtom(root, carbons[6], carbonColor, "C_Pyrr_1");
            CreateAtom(root, pyrroleNitrogen, nitrogenColor, "N_Pyrr");
            CreateAtom(root, carbons[7], carbonColor, "C_Pyrr_2");

            carbons[8] = carbons[7] + new Vector3(0.35f, 0.2f, 0.1f);
            carbons[9] = carbons[8] + new Vector3(0.35f, -0.1f, -0.1f);
            CreateAtom(root, carbons[8], carbonColor, "C_Ethyl_1");
            CreateAtom(root, carbons[9], carbonColor, "C_Ethyl_2");

            Vector3 amineNitrogen = carbons[9] + new Vector3(0.3f, 0.1f, 0.05f);
            CreateAtom(root, amineNitrogen, nitrogenColor, "N_Amine");

            Vector3 hydroxylOxygen = carbons[4] + new Vector3(-0.35f, -0.2f, 0f);
            CreateAtom(root, hydroxylOxygen, oxygenColor, "O_Hydroxyl");

            for (int i = 0; i < 6; i++) CreateBond(root, carbons[i], carbons[(i + 1) % 6]);
            CreateBond(root, carbons[0], carbons[6]);
            CreateBond(root, carbons[6], carbons[7]);
            CreateBond(root, carbons[7], pyrroleNitrogen);
            CreateBond(root, pyrroleNitrogen, carbons[1]);
            CreateBond(root, carbons[7], carbons[8]);
            CreateBond(root, carbons[8], carbons[9]);
            CreateBond(root, carbons[9], amineNitrogen);
            CreateBond(root, carbons[4], hydroxylOxygen);
        }

        private void GenerateProteinReceptorHelices(GameObject root)
        {
            int helicesCount = 5;
            float poreRadius = 0.5f;

            for (int h = 0; h < helicesCount; h++)
            {
                float angleOffset = h * Mathf.PI * 2f / helicesCount;
                Vector3 center = new Vector3(Mathf.Cos(angleOffset) * poreRadius, 0f, Mathf.Sin(angleOffset) * poreRadius);

                int segments = 80;
                float height = 2.0f;
                float coilRadius = 0.12f;
                float coilFrequency = 6.0f;

                Vector3 lastPt = Vector3.zero;

                for (int s = 0; s < segments; s++)
                {
                    float t = s / (float)(segments - 1);
                    float theta = t * coilFrequency * Mathf.PI * 2f;
                    float y = (t - 0.5f) * height;

                    Vector3 localSpiral = new Vector3(
                        Mathf.Cos(theta) * coilRadius,
                        y,
                        Mathf.Sin(theta) * coilRadius
                    );

                    Vector3 worldPt = center + root.transform.TransformVector(localSpiral);

                    if (s % 4 == 0)
                    {
                        Color nodeColor = Color.Lerp(receptorHelixColor, Color.cyan, t);
                        CreateAtom(root, worldPt, nodeColor, $"CA_{h}_{s}", atomScale * 0.7f);
                    }

                    if (s > 0)
                    {
                        CreateBond(root, lastPt, worldPt, bondRadius * 0.4f, Color.Lerp(receptorHelixColor, Color.cyan, t));
                    }
                    lastPt = worldPt;
                }
            }
        }

        private void GenerateProceduralNeuron(GameObject root)
        {
            Debug.Log("[BioVR Neuron] Assembling procedurally detailed Multipolar Neuron...");

            // 1. Build organic segmented Soma (Cell Body) at root center
            Vector3 somaPos = Vector3.zero;
            // Central core nucleus
            CreateAtom(root, somaPos, neuronSomaColor, "Neuron_Soma_Core", 0.35f);
            
            // Generate overlapping organic lobes to create irregular wrinkly cell body shape
            CreateAtom(root, somaPos + new Vector3(0.12f, -0.06f, 0.08f), neuronSomaColor, "Neuron_Soma_Lobe1", 0.24f);
            CreateAtom(root, somaPos + new Vector3(-0.1f, 0.08f, -0.05f), neuronSomaColor, "Neuron_Soma_Lobe2", 0.26f);
            CreateAtom(root, somaPos + new Vector3(0.05f, 0.12f, 0.08f), neuronSomaColor, "Neuron_Soma_Lobe3", 0.22f);
            CreateAtom(root, somaPos + new Vector3(-0.06f, -0.1f, 0.1f), neuronSomaColor, "Neuron_Soma_Lobe4", 0.24f);

            // 2. Build Branching Dendrite spiral fibers (6 directions)
            Vector3[] dendriteDirs = {
                new Vector3(1.0f, 0.6f, 0.2f).normalized,
                new Vector3(-0.9f, 0.7f, -0.3f).normalized,
                new Vector3(0.2f, 0.9f, 0.6f).normalized,
                new Vector3(-0.7f, -0.6f, 0.7f).normalized,
                new Vector3(0.8f, -0.5f, -0.5f).normalized,
                new Vector3(-0.8f, 0.3f, -0.7f).normalized
            };

            for (int d = 0; d < dendriteDirs.Length; d++)
            {
                Vector3 dir = dendriteDirs[d];
                int segments = 8;
                float segLen = 0.16f;
                Vector3 curPos = somaPos + dir * 0.2f;

                for (int s = 0; s < segments; s++)
                {
                    float pct = s / (float)segments;
                    // Add random twist vectors
                    Vector3 randomJitter = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f));
                    Vector3 nextPos = curPos + (dir * 0.7f + randomJitter).normalized * segLen;

                    float thick = 0.024f * (1.0f - pct * 0.7f); // Thins out dynamically
                    CreateBond(root, curPos, nextPos, thick, neuronDendriteColor);

                    // Periodically spawn branching dendrite split ends!
                    if (s == 4)
                    {
                        Vector3 branchDir = (dir + Vector3.Cross(dir, Vector3.up) * 0.8f).normalized;
                        Vector3 branchPos = curPos;
                        for (int bs = 0; bs < 4; bs++)
                        {
                            Vector3 nextBranchPos = branchPos + (branchDir + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f))).normalized * segLen;
                            CreateBond(root, branchPos, nextBranchPos, thick * 0.8f, neuronDendriteColor);
                            branchPos = nextBranchPos;
                        }
                    }

                    curPos = nextPos;
                }
            }

            // 3. Build Long Axon Cable extending straight down along -Y
            Vector3 axonStart = somaPos + new Vector3(0f, -0.2f, 0f);
            int axonSegments = 12;
            float axonSegLen = 0.15f;
            Vector3 curAxonPos = axonStart;

            for (int a = 0; a < axonSegments; a++)
            {
                Vector3 nextAxonPos = curAxonPos + new Vector3(Mathf.Sin(a * 1.5f) * 0.02f, -1.0f, Mathf.Cos(a * 1.5f) * 0.02f).normalized * axonSegLen;
                CreateBond(root, curAxonPos, nextAxonPos, 0.02f, new Color(0.6f, 0.65f, 0.7f, 0.85f));

                // 4. Build rounded glowing Myelin Sheath segmented beads String along the axon
                if (a > 1 && a < 10 && a % 2 == 0)
                {
                    Vector3 sheathCenter = Vector3.Lerp(curAxonPos, nextAxonPos, 0.5f);
                    GameObject myelin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    myelin.name = $"Myelin_Sheath_Bead_{a}";
                    myelin.transform.SetParent(root.transform, false);
                    myelin.transform.position = sheathCenter;
                    myelin.transform.localScale = new Vector3(0.08f, axonSegLen * 0.42f, 0.08f);
                    myelin.transform.up = (nextAxonPos - curAxonPos).normalized;

                    MeshRenderer mr = myelin.GetComponent<MeshRenderer>();
                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlit == null) unlit = Shader.Find("Unlit/Color");
                    Material mMat = new Material(unlit);
                    mMat.color = myelinSheathColor;
                    mMat.EnableKeyword("_EMISSION");
                    mMat.SetColor("_EmissionColor", myelinSheathColor * 0.25f);
                    mr.sharedMaterial = mMat;
                }

                curAxonPos = nextAxonPos;
            }

            // 5. Build Spreading Axon Terminal Branches & Synaptic Buttons at the end
            Vector3 terminalBase = curAxonPos;
            Vector3[] terminalDirs = {
                new Vector3(0.5f, -0.6f, 0.2f).normalized,
                new Vector3(-0.6f, -0.5f, -0.3f).normalized,
                new Vector3(0.1f, -0.7f, 0.5f).normalized,
                new Vector3(-0.2f, -0.6f, -0.6f).normalized
            };

            for (int t = 0; t < terminalDirs.Length; t++)
            {
                Vector3 tDir = terminalDirs[t];
                Vector3 endPos = terminalBase + tDir * 0.25f;
                CreateBond(root, terminalBase, endPos, 0.008f, new Color(0.5f, 0.55f, 0.6f, 0.8f));

                // Glowing green synaptic bulb buttons!
                CreateAtom(root, endPos, synapticBulbColor, $"Synaptic_Bulb_{t}", 0.038f);
            }
        }

        private void CreateAtom(GameObject root, Vector3 position, Color color, string atomName, float customScale = 0f)
        {
            GameObject atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atom.name = atomName;
            atom.transform.SetParent(root.transform, false);
            atom.transform.position = position;
            
            float scale = customScale > 0f ? customScale : atomScale;
            atom.transform.localScale = Vector3.one * scale;

            MeshRenderer mr = atom.GetComponent<MeshRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            Material mat = new Material(unlit);
            mat.color = color;
            mr.sharedMaterial = mat;
        }

        private void CreateBond(GameObject root, Vector3 start, Vector3 end, float customRadius = 0f, Color? customColor = null)
        {
            GameObject bond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bond.name = "CovalentBond";
            bond.transform.SetParent(root.transform, false);

            Vector3 direction = end - start;
            float length = direction.magnitude;
            
            float radius = customRadius > 0f ? customRadius : bondRadius;
            bond.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            bond.transform.position = start + direction * 0.5f;
            bond.transform.up = direction.normalized;

            MeshRenderer mr = bond.GetComponent<MeshRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");

            Material mat = new Material(unlit);
            mat.color = customColor.HasValue ? customColor.Value : new Color(0.7f, 0.7f, 0.7f, 0.8f);
            mr.sharedMaterial = mat;
        }
    }
}
