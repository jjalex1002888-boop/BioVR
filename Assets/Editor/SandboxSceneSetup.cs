#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BioVR.Dynamics;
using BioVR.Macro;
using BioVR.Cellular;
using BioVR.Molecular;
using BioVR.UI;
using BioVR.Validation;

namespace BioVR.Editor
{
    public class SandboxSceneSetup : EditorWindow
    {
        [MenuItem("BioVR/Automate Sandbox Scene Setup")]
        public static void SetupCompleteSandbox()
        {
            Debug.Log("[BioVR Setup] Commencing automatic high-fidelity scene construction...");

            // 1. Create Core Manager Parent
            GameObject managerObj = GameObject.Find("BioVR_Sandbox_Core");
            if (managerObj == null)
            {
                managerObj = new GameObject("BioVR_Sandbox_Core");
            }
            Undo.RegisterCreatedObjectUndo(managerObj, "Create BioVR Core");

            // 2. Attach Dynamics Solvers & Managers
            WilsonCowanSolver solver = managerObj.GetComponent<WilsonCowanSolver>();
            if (solver == null) solver = managerObj.AddComponent<WilsonCowanSolver>();

            TimeWarpManager timeManager = managerObj.GetComponent<TimeWarpManager>();
            if (timeManager == null) timeManager = managerObj.AddComponent<TimeWarpManager>();

            GcpCloudBridge cloudBridge = managerObj.GetComponent<GcpCloudBridge>();
            if (cloudBridge == null) cloudBridge = managerObj.AddComponent<GcpCloudBridge>();

            AtomicRenderer atomicRenderer = managerObj.GetComponent<AtomicRenderer>();
            if (atomicRenderer == null) atomicRenderer = managerObj.AddComponent<AtomicRenderer>();

            SandboxVerificationRunner verifier = managerObj.GetComponent<SandboxVerificationRunner>();
            if (verifier == null) verifier = managerObj.AddComponent<SandboxVerificationRunner>();

            // 3. Setup Macro Cerebrum shell (with legibility scale and rotator controls)
            GameObject cerebrumObj = GameObject.Find("Procedural_Cerebrum");
            if (cerebrumObj == null)
            {
                cerebrumObj = new GameObject("Procedural_Cerebrum");
                cerebrumObj.transform.SetParent(managerObj.transform);
            }
            cerebrumObj.transform.localPosition = new Vector3(0.1f, 1.3f, 1.5f); // Centered at eye level in VR
            cerebrumObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // Biological scaled-down comfort size
            
            BrainController brainController = cerebrumObj.GetComponent<BrainController>();
            if (brainController == null) brainController = cerebrumObj.AddComponent<BrainController>();

            ModelRotator modelRotator = cerebrumObj.GetComponent<ModelRotator>();
            if (modelRotator == null) modelRotator = cerebrumObj.AddComponent<ModelRotator>();

            // 4. Setup Isolatable Structures (Deep brain targets)
            GameObject amygdalaObj = GameObject.Find("DeepStructure_Amygdala");
            if (amygdalaObj == null)
            {
                amygdalaObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                amygdalaObj.name = "DeepStructure_Amygdala";
                amygdalaObj.transform.SetParent(cerebrumObj.transform);
                amygdalaObj.transform.localPosition = new Vector3(-0.3f, -0.2f, 0.1f);
                amygdalaObj.transform.localScale = new Vector3(0.2f, 0.15f, 0.25f);
                
                // Color Amygdala glowing red
                MeshRenderer mr = amygdalaObj.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.9f, 0.1f, 0.2f, 0.8f);
                mr.sharedMaterial = mat;
            }
            IsolatableStructure amygdalaIso = amygdalaObj.GetComponent<IsolatableStructure>();
            if (amygdalaIso == null) amygdalaIso = amygdalaObj.AddComponent<IsolatableStructure>();
            amygdalaIso.structureName = "Amygdala";
            amygdalaIso.structureId = "amygdala";
            amygdalaIso.pullAxis = new Vector3(-1f, -0.5f, 0f);
            amygdalaIso.hoverGlowColor = Color.red;

            // 5. Setup White Matter Tract Particle Flow
            GameObject tractsObj = GameObject.Find("WhiteMatter_Tract_System");
            if (tractsObj == null)
            {
                tractsObj = new GameObject("WhiteMatter_Tract_System");
                tractsObj.transform.SetParent(managerObj.transform);
                tractsObj.transform.localPosition = Vector3.zero;

                ParticleSystem ps = tractsObj.AddComponent<ParticleSystem>();
                
                // Configure particle system shape to flow through cerebrum
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;

                var main = ps.main;
                main.startSize = 0.05f;
                main.duration = 5f;
                main.loop = true;
                main.playOnAwake = true;

                var emission = ps.emission;
                emission.rateOverTime = 50f;
            }
            WhiteMatterTractParticles tractParticles = tractsObj.GetComponent<WhiteMatterTractParticles>();
            if (tractParticles == null) tractParticles = tractsObj.AddComponent<WhiteMatterTractParticles>();
            tractParticles.neuralSolver = solver; // Link solver!

            // 6. Setup Synaptic Cleft Assembly
            GameObject synapseObj = GameObject.Find("Cellular_Synapse_Cleft");
            if (synapseObj == null)
            {
                synapseObj = new GameObject("Cellular_Synapse_Cleft");
                synapseObj.transform.SetParent(managerObj.transform);
                synapseObj.transform.localPosition = new Vector3(-2.0f, 1.0f, 1.0f); // Offset to the side of the cerebrum
            }
            SynapseController synapseController = synapseObj.GetComponent<SynapseController>();
            if (synapseController == null) synapseController = synapseObj.AddComponent<SynapseController>();

            GameObject preTermObj = GameObject.Find("PreSynapticTerminal");
            if (preTermObj == null)
            {
                preTermObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                preTermObj.name = "PreSynapticTerminal";
                preTermObj.transform.SetParent(synapseObj.transform);
                preTermObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                preTermObj.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);

                MeshRenderer mr = preTermObj.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.1f, 0.6f, 0.8f, 0.9f);
                mr.sharedMaterial = mat;
            }

            GameObject postTermObj = GameObject.Find("PostSynapticTerminal");
            if (postTermObj == null)
            {
                postTermObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                postTermObj.name = "PostSynapticTerminal";
                postTermObj.transform.SetParent(synapseObj.transform);
                postTermObj.transform.localPosition = new Vector3(0f, -0.8f, 0f);
                postTermObj.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f);

                MeshRenderer mr = postTermObj.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.1f, 0.4f, 0.7f, 0.9f);
                mr.sharedMaterial = mat;
            }
            synapseController.preSynapticTerminal = preTermObj.transform;
            synapseController.postSynapticTerminal = postTermObj.transform;
            synapseController.maxSeparationDistance = 1.5f;

            // 7. Setup Vesicles & Release System
            GameObject vesicleSysObj = GameObject.Find("Vesicle_Release_System");
            if (vesicleSysObj == null)
            {
                vesicleSysObj = new GameObject("Vesicle_Release_System");
                vesicleSysObj.transform.SetParent(synapseObj.transform);
                vesicleSysObj.transform.localPosition = Vector3.zero;

                ParticleSystem ps = vesicleSysObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startSize = 0.04f;
                main.startSpeed = 0.8f;
                main.duration = 2f;
                main.loop = false;
                main.playOnAwake = false;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                shape.radius = 0.1f;
                
                // point cone downward (from pre to post)
                vesicleSysObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            NeurotransmitterSystem neuroSystem = synapseObj.GetComponent<NeurotransmitterSystem>();
            if (neuroSystem == null) neuroSystem = synapseObj.AddComponent<NeurotransmitterSystem>();
            neuroSystem.synapseController = synapseController;
            neuroSystem.neuralSolver = solver;
            neuroSystem.transmitterParticleSystem = vesicleSysObj.GetComponent<ParticleSystem>();
            neuroSystem.vesiclePoolSize = 8;
            neuroSystem.vesicleSpeed = 0.4f;

            // 8. Create a VR HUD Anchor GameObject
            GameObject hudAnchor = GameObject.Find("VR_Glassmorphic_HUD");
            if (hudAnchor == null)
            {
                hudAnchor = new GameObject("VR_Glassmorphic_HUD");
                hudAnchor.transform.SetParent(managerObj.transform);
            }
            hudAnchor.transform.localPosition = new Vector3(-0.9f, 1.3f, 1.4f); // Floating on the left
            hudAnchor.transform.localRotation = Quaternion.Euler(0f, 25f, 0f); // Angled towards the center eye
            hudAnchor.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            VrHudController hudController = hudAnchor.GetComponent<VrHudController>();
            if (hudController == null) hudController = hudAnchor.AddComponent<VrHudController>();
            hudController.neuralSolver = solver;
            hudController.synapseController = synapseController;

            // 9. Setup Hybrid Camera Rig
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj == null)
            {
                camObj = GameObject.Find("BioVR_Camera_Rig");
            }
            if (camObj == null)
            {
                camObj = new GameObject("BioVR_Camera_Rig");
                camObj.AddComponent<Camera>();
            }
            camObj.tag = "MainCamera";

            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }

            HybridCameraRig cameraRig = camObj.GetComponent<HybridCameraRig>();
            if (cameraRig == null)
            {
                cameraRig = camObj.AddComponent<HybridCameraRig>();
            }
            cameraRig.focalPoint = cerebrumObj.transform.position;

            // Log completion details
            Debug.Log("==========================================================================");
            Debug.Log("[SUCCESS] BioVR Sandbox Scene configured and fully linked in the hierarchy!");
            Debug.Log("GameObjects instantiated: BioVR_Sandbox_Core, Procedural_Cerebrum, DeepStructure_Amygdala, Cellular_Synapse_Cleft, VR_Glassmorphic_HUD, BioVR_Camera_Rig.");
            Debug.Log("Internal solver, camera, and particle linkages fully established!");
            Debug.Log("To view, open the active scene, select 'BioVR_Sandbox_Core' in your hierarchy, and click Play!");
            Debug.Log("==========================================================================");

            EditorUtility.DisplayDialog("BioVR Scene Automation", 
                "High-fidelity scientific sandbox hierarchy built successfully!\n\nAll physics solver linkages, dynamic particle flows, procedural cerebrum matrices, synaptic cleft structures, and HUD controllers have been dynamically instantiated and wired with zero manual friction.", 
                "Excellent");
        }
    }
}
#endif
