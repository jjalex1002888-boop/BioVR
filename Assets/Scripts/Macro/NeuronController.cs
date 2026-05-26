using UnityEngine;
using BioVR.UI;

namespace BioVR.Macro
{
    /// <summary>
    /// Generates a scientifically accurate, highly-detailed 3D Pyramidal Neuron model procedurally.
    /// Creates interactive sub-structures (Soma, Nucleus, Dendrites, Axon, Myelin Sheath, Axon Terminals)
    /// equipped with standard IsolatableStructure hooks so they support VR/Editor hover, clicks, and pulls.
    /// </summary>
    public class NeuronController : MonoBehaviour
    {
        private void Start()
        {
            GenerateProceduralNeuron();
        }

        private void GenerateProceduralNeuron()
        {
            Debug.Log("[BioVR Neuron] Commencing high-fidelity procedural generation of interactive Pyramidal Neuron...");

            // Medically realistic pastel colors matching scientific diagram aesthetics
            Color somaColor = new Color(0.96f, 0.70f, 0.72f, 0.85f);        // Soft Rose Pink
            Color nucleusColor = new Color(1.0f, 0.90f, 0.60f, 0.95f);      // Glowing Golden Yellow
            Color fiberColor = new Color(0.92f, 0.60f, 0.65f, 0.80f);        // Slightly darker rose pink for fibers
            Color myelinColor = new Color(0.96f, 0.94f, 0.90f, 0.90f);       // White-matter Cream/Vanilla
            Color terminalColor = new Color(0.85f, 0.52f, 0.58f, 0.85f);     // Pinkish-purple terminals

            // 1. Central Soma (Cell Body - bumpy pyramidal structure)
            GameObject somaObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            somaObj.name = "Neuron_Soma";
            somaObj.transform.SetParent(transform, false);
            somaObj.transform.localPosition = Vector3.zero;
            somaObj.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f); // Pyramidal teardrop shape
            
            ConfigureAnatomicalMaterial(somaObj, somaColor, true);
            var somaIso = somaObj.AddComponent<IsolatableStructure>();
            somaIso.structureName = "Soma (Cell Body)";
            somaIso.structureId = "neuron_soma";
            somaIso.pullAxis = Vector3.up;
            somaIso.pullDistance = 1.0f;
            somaIso.hoverGlowColor = Color.white;

            // 2. Central Nucleus (Glowing sphere inside Soma)
            GameObject nucleusObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nucleusObj.name = "Neuron_Nucleus";
            nucleusObj.transform.SetParent(somaObj.transform, false);
            nucleusObj.transform.localPosition = Vector3.zero;
            // Fits elegantly inside
            nucleusObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            
            ConfigureAnatomicalMaterial(nucleusObj, nucleusColor, true);
            // Nucleus glows highly to stand out inside the translucent Soma
            var nucRenderer = nucleusObj.GetComponent<MeshRenderer>();
            if (nucRenderer != null && nucRenderer.material != null)
            {
                nucRenderer.material.EnableKeyword("_EMISSION");
                nucRenderer.material.SetColor("_EmissionColor", nucleusColor * 0.8f);
            }
            var nucleusIso = nucleusObj.AddComponent<IsolatableStructure>();
            nucleusIso.structureName = "Nucleus (DNA Core)";
            nucleusIso.structureId = "neuron_nucleus";
            nucleusIso.pullAxis = Vector3.forward;
            nucleusIso.pullDistance = 0.8f;
            nucleusIso.hoverGlowColor = Color.yellow;

            // 3. Dendrites (Apical and Basal branching fibers)
            // Apical Dendrite (Top shaft)
            CreateFiberBranch("Apical_Dendrite", new Vector3(0.15f, 1.0f, 0.15f), new Vector3(0f, 1.1f, 0f), 
                Quaternion.identity, fiberColor, Vector3.up * 0.5f, "Apical Dendrite", "dendrite_apical");
            
            // Apical branching twigs
            CreateFiberBranch("Apical_Branch_Left", new Vector3(0.08f, 0.6f, 0.08f), new Vector3(-0.25f, 1.8f, 0f), 
                Quaternion.Euler(0f, 0f, 45f), fiberColor, new Vector3(-0.5f, 0.5f, 0f), "Apical Branch (Left)", "dendrite_ap_l");
            CreateFiberBranch("Apical_Branch_Right", new Vector3(0.08f, 0.6f, 0.08f), new Vector3(0.25f, 1.8f, 0f), 
                Quaternion.Euler(0f, 0f, -45f), fiberColor, new Vector3(0.5f, 0.5f, 0f), "Apical Branch (Right)", "dendrite_ap_r");

            // Basal Dendrites (Bottom lateral twigs)
            CreateFiberBranch("Basal_Dendrite_Left", new Vector3(0.1f, 0.7f, 0.1f), new Vector3(-0.5f, -0.7f, 0.2f), 
                Quaternion.Euler(0f, 30f, 120f), fiberColor, new Vector3(-0.8f, -0.4f, 0.3f), "Basal Dendrite (Left)", "dendrite_ba_l");
            CreateFiberBranch("Basal_Dendrite_Right", new Vector3(0.1f, 0.7f, 0.1f), new Vector3(0.5f, -0.7f, -0.2f), 
                Quaternion.Euler(0f, -30f, -120f), fiberColor, new Vector3(0.8f, -0.4f, -0.3f), "Basal Dendrite (Right)", "dendrite_ba_r");

            // 4. Axon (The long transmission trunk)
            GameObject axonObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            axonObj.name = "Neuron_Axon";
            axonObj.transform.SetParent(transform, false);
            axonObj.transform.localPosition = new Vector3(0f, -1.8f, 0f);
            axonObj.transform.localScale = new Vector3(0.08f, 1.3f, 0.08f);
            
            ConfigureAnatomicalMaterial(axonObj, fiberColor, false);
            var axonIso = axonObj.AddComponent<IsolatableStructure>();
            axonIso.structureName = "Axon (Signal Pathway)";
            axonIso.structureId = "neuron_axon";
            axonIso.pullAxis = Vector3.back;
            axonIso.pullDistance = 0.5f;

            // 5. Myelin Sheaths (Insulating rounded beads wrapped around Axon)
            float[] myelinYOffsets = { -1.1f, -1.8f, -2.5f };
            for (int i = 0; i < myelinYOffsets.Length; i++)
            {
                GameObject sheath = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sheath.name = $"Myelin_Sheath_{i + 1}";
                sheath.transform.SetParent(transform, false);
                sheath.transform.localPosition = new Vector3(0f, myelinYOffsets[i], 0f);
                // Plump cylinders wrapping around the axon
                sheath.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
                
                ConfigureAnatomicalMaterial(sheath, myelinColor, false);
                var sheathIso = sheath.AddComponent<IsolatableStructure>();
                sheathIso.structureName = $"Myelin Sheath Node {i + 1}";
                sheathIso.structureId = $"myelin_sheath_{i}";
                // Pulls lateral left/right alternating for exploded visualization
                sheathIso.pullAxis = (i % 2 == 0) ? Vector3.left : Vector3.right;
                sheathIso.pullDistance = 0.9f;
                sheathIso.hoverGlowColor = Color.cyan;
            }

            // 6. Axon Terminals (Bottom branching output structures)
            CreateFiberBranch("Axon_Terminal_Left", new Vector3(0.06f, 0.5f, 0.06f), new Vector3(-0.25f, -3.3f, 0.1f), 
                Quaternion.Euler(0f, 20f, 140f), terminalColor, new Vector3(-0.6f, -0.6f, 0.2f), "Axon Terminal (Left)", "axon_term_l");
            CreateFiberBranch("Axon_Terminal_Right", new Vector3(0.06f, 0.5f, 0.06f), new Vector3(0.25f, -3.3f, -0.1f), 
                Quaternion.Euler(0f, -20f, -140f), terminalColor, new Vector3(0.6f, -0.6f, -0.2f), "Axon Terminal (Right)", "axon_term_r");
        }

        private void CreateFiberBranch(string branchName, Vector3 scale, Vector3 offset, Quaternion rotation, Color color, Vector3 pullAxis, string displayName, string structureId)
        {
            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.name = branchName;
            branch.transform.SetParent(transform, false);
            branch.transform.localPosition = offset;
            branch.transform.localRotation = rotation;
            branch.transform.localScale = scale;

            ConfigureAnatomicalMaterial(branch, color, false);
            var branchIso = branch.AddComponent<IsolatableStructure>();
            branchIso.structureName = displayName;
            branchIso.structureId = structureId;
            branchIso.pullAxis = pullAxis;
            branchIso.pullDistance = 0.8f;
            branchIso.hoverGlowColor = Color.white;
        }

        private void ConfigureAnatomicalMaterial(GameObject obj, Color color, bool isTranslucent)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            // Generate high-end clinical unlit holographic material matching URP defaults
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            Material mat = new Material(unlitShader);
            mat.color = color;

            if (isTranslucent)
            {
                mat.SetFloat("_Surface", 1.0f); // Transparent
                mat.SetFloat("_Blend", 0.0f);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Glowing edge boost
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.15f);
            }
            else
            {
                // Solid pristine clinical matte coating
                mat.SetFloat("_Surface", 0.0f); // Opaque
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }

            renderer.sharedMaterial = mat;
        }
    }
}
