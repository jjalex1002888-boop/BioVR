using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BioVR.Dynamics;
using BioVR.Cellular;
using BioVR.Molecular;
using BioVR.Macro;

namespace BioVR.UI
{
    public class VrHudController : MonoBehaviour
    {
        [Header("Engine Dependency References")]
        public WilsonCowanSolver neuralSolver;
        public SynapseController synapseControllerRef; 
        public AtomicRenderer atomicRenderer;

        [Header("Hormone UI Sliders")]
        public Slider sliderDopamine;
        public Slider sliderSerotonin;
        public Slider sliderCortisol;

        [Header("Hormone Value Readouts")]
        public Text txtDopamineVal;
        public Text txtSerotoninVal;
        public Text txtCortisolVal;

        [Header("Simulation Playback Sliders")]
        public Slider sliderTimeWarp;
        public Slider sliderTimeline;
        public Text txtTimeWarpVal;
        public Text txtTimelineVal;

        [Header("Micro Cleft Sliders")]
        public Slider sliderSynapseExplode;
        public Text txtSynapseExplodeVal;

        [Header("Neural Activity Indicators")]
        public Text txtMembranePotential;

        [Header("Molecular Job Launch Input")]
        public InputField inputMolecularTarget;
        public Button btnLaunchAlphaFoldJob;

        [Header("Holographic Toggle Buttons")]
        public Button btnToggleComparison;
        public Text txtToggleComparison;
        public Button btnToggleScale;
        public Text txtToggleScale;
        public Button btnClearWorkspace;

        private bool isUpdatingTimeline = false;
        private GameObject consoleObjRef;

        [Header("Model Selection Toggles")]
        public Button btnToggleTrueSize;
        public Text txtTrueSizeStatus;
        public Text txtMeasurementsReadout;

        private bool trueSizeEnabled = false;
        private string activeModelType = "CEREBRUM"; // CEREBRUM, NEURON, SYNAPSE
        private GameObject activeNeuronObj = null;
        private Sprite roundedCornerSprite = null;

        // Modern dynamic selector sliding highlight references
        private Button btnCerebrum;
        private Button btnNeuron;
        private Button btnSynapse;
        private Text txtCerebrum;
        private Text txtNeuron;
        private Text txtSynapse;
        private RectTransform highlightRect;

        // Dynamic Text Size Adjustments
        private System.Collections.Generic.Dictionary<Text, int> originalFontSizes = new System.Collections.Generic.Dictionary<Text, int>();
        private Slider sliderTextSize;
        private Text txtTextSizeVal;

        // Panel Category Filtration Dropdown
        private Dropdown sectionFilterDropdown;
        private System.Collections.Generic.List<GameObject> uiSections = new System.Collections.Generic.List<GameObject>();

        // Minimization State & Transforms
        private bool isMinimized = false;
        private RectTransform mainRect;
        private GameObject minimizedIconObj;
        private RectTransform minimizedIconRect;

        public void ToggleMinimize()
        {
            isMinimized = !isMinimized;
            Debug.Log($"[BioVR UI] Toggled minimization to: {isMinimized}");
            
            // Toggle active components or raycasting so click events don't pass through minimized panel
            if (mainRect != null)
            {
                CanvasGroup cg = mainRect.GetComponent<CanvasGroup>();
                if (cg == null) cg = mainRect.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = !isMinimized;
                cg.interactable = !isMinimized;
            }

            if (minimizedIconRect != null)
            {
                CanvasGroup iconCg = minimizedIconRect.GetComponent<CanvasGroup>();
                if (iconCg == null) iconCg = minimizedIconRect.gameObject.AddComponent<CanvasGroup>();
                iconCg.blocksRaycasts = isMinimized;
                iconCg.interactable = isMinimized;
            }
        }

        public void SetTextSizeMultiplier(float multiplier)
        {
            foreach (var kvp in originalFontSizes)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.fontSize = Mathf.RoundToInt(kvp.Value * multiplier);
                }
            }
        }

        private void RegisterAllHUDTexts()
        {
            Text[] allTexts = GetComponentsInChildren<Text>(true);
            foreach (var txt in allTexts)
            {
                if (txt != null && !originalFontSizes.ContainsKey(txt))
                {
                    originalFontSizes[txt] = txt.fontSize;
                }
            }
        }

        public void AlignUiToPlayer()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Transform camTransform = mainCam.transform;

                // Position the UI beautifully in front of the camera, slightly offset and angled
                Vector3 targetPos = camTransform.position + camTransform.forward * 1.4f - camTransform.right * 0.4f;
                // Place at a comfortable height relative to camera
                targetPos.y = camTransform.position.y - 0.05f;

                transform.position = targetPos;

                // Rotate to face the camera, maintaining flat horizontal rotation (y-axis only)
                Vector3 lookPos = camTransform.position - transform.position;
                lookPos.y = 0;
                if (lookPos != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(-lookPos);
                    // Tilt slightly toward the player (yaw rotation)
                    transform.Rotate(0f, 25f, 0f);
                }
            }
            else
            {
                // Fallback default position if Main Camera is not immediately available
                transform.localPosition = new Vector3(-0.9f, 1.3f, 1.4f);
                transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            }
        }

        private void OnTextSizeSliderChanged(float val)
        {
            SetTextSizeMultiplier(val);
            if (txtTextSizeVal != null)
            {
                txtTextSizeVal.text = $"{val:F2}x";
            }
        }

        private void OnSectionFilterChanged(int index)
        {
            // Indexes correspond to:
            // 0: ALL SECTIONS
            // 1: MACRO NEURAL SOLVER
            // 2: SIMULATION MODEL SELECTION
            // 3: CELLULAR SYNAPTIC CLEFT
            // 4: ALPHAFOLD ATOMIC MOLECULES
            // 5: RECORDING TIME SCALES
            
            Debug.Log($"[BioVR UI] Section filter changed to index: {index}");
            
            for (int i = 0; i < uiSections.Count; i++)
            {
                if (uiSections[i] == null) continue;
                
                if (index == 0)
                {
                    // Show all sections
                    uiSections[i].SetActive(true);
                }
                else
                {
                    // Index 1 corresponds to section at index 0 in list, index 2 to index 1, etc.
                    uiSections[i].SetActive(i == (index - 1));
                }
            }
        }

        private void OnSettingsClicked()
        {
            if (consoleObjRef != null)
            {
                bool nextState = !consoleObjRef.activeSelf;
                consoleObjRef.SetActive(nextState);
                Debug.Log($"[BioVR UI] Toggled developer settings overlay to: {nextState}");
            }
        }

        // Custom property access to handle name bindings correctly
        public SynapseController synapseController
        {
            get { return synapseControllerRef; }
            set { synapseControllerRef = value; }
        }

        private void Awake()
        {
            // Auto-detect components in parent/siblings if not manually linked
            if (neuralSolver == null) neuralSolver = FindAnyObjectByType<WilsonCowanSolver>();
            if (synapseControllerRef == null) synapseControllerRef = FindAnyObjectByType<SynapseController>();
            if (atomicRenderer == null) atomicRenderer = FindAnyObjectByType<AtomicRenderer>();

            // Ensure EventSystem exists in the scene so that UI click events register in Play Mode and on Quest!
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                Debug.Log("[BioVR HUD] Spawned dynamic EventSystem.");
            }

            // Setup input module dynamically based on Unity's active Input System
#if ENABLE_INPUT_SYSTEM
            var legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacyModule != null) legacyModule.enabled = false;

            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[BioVR HUD] Added InputSystemUIInputModule for New Input System support.");
            }
#else
            var legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacyModule == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
#endif

            // Add OVRInputModule for Meta Quest controller laser rays support
            var ovrModule = eventSystem.GetComponent<OVRInputModule>();
            if (ovrModule == null)
            {
                eventSystem.gameObject.AddComponent<OVRInputModule>();
                Debug.Log("[BioVR HUD] Attached OVRInputModule to EventSystem for Meta Quest controller lasers.");
            }

            // Ensure PhysicsRaycaster exists on Main Camera so standard 3D EventSystem events (Hover, Click, Drag) work out-of-the-box!
            if (Camera.main != null && Camera.main.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                Camera.main.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
                Debug.Log("[BioVR HUD] Attached PhysicsRaycaster to Main Camera for dynamic 3D object EventSystem interactions.");
            }

            // Generate rounded corner sprite procedurally for modern aesthetics
            roundedCornerSprite = CreateRoundedCornerSprite(128, 128, 32);

            // If slider is null, trigger programmatic generation of a stunning glassmorphic UI!
            if (sliderDopamine == null)
            {
                BuildBeautifulVRHUDCanvas();
            }
        }

        private void Start()
        {
            // Bind listeners for dynamic hormone adjustments
            if (sliderDopamine != null)
            {
                sliderDopamine.onValueChanged.AddListener(OnDopamineChanged);
                sliderDopamine.value = neuralSolver != null ? neuralSolver.dopamine : 0.5f;
                OnDopamineChanged(sliderDopamine.value);
            }
            if (sliderSerotonin != null)
            {
                sliderSerotonin.onValueChanged.AddListener(OnSerotoninChanged);
                sliderSerotonin.value = neuralSolver != null ? neuralSolver.serotonin : 0.5f;
                OnSerotoninChanged(sliderSerotonin.value);
            }
            if (sliderCortisol != null)
            {
                sliderCortisol.onValueChanged.AddListener(OnCortisolChanged);
                sliderCortisol.value = neuralSolver != null ? neuralSolver.cortisol : 0.1f;
                OnCortisolChanged(sliderCortisol.value);
            }

            // Bind simulation timeline warp sliders
            if (sliderTimeWarp != null)
            {
                sliderTimeWarp.onValueChanged.AddListener(OnTimeWarpChanged);
                sliderTimeWarp.value = TimeWarpManager.Instance != null ? TimeWarpManager.Instance.timeScaleFactor : 1.0f;
                OnTimeWarpChanged(sliderTimeWarp.value);
            }
            if (sliderTimeline != null)
            {
                sliderTimeline.onValueChanged.AddListener(OnTimelineScrubbed);
            }

            // Bind cleft exploded view slider
            if (sliderSynapseExplode != null)
            {
                sliderSynapseExplode.onValueChanged.AddListener(OnSynapseExplodeChanged);
                sliderSynapseExplode.value = synapseControllerRef != null ? synapseControllerRef.explosionFactor : 0.0f;
                OnSynapseExplodeChanged(sliderSynapseExplode.value);
            }

            // Bind molecular API trigger button
            if (btnLaunchAlphaFoldJob != null)
            {
                btnLaunchAlphaFoldJob.onClick.AddListener(OnLaunchJobClicked);
            }

            // Initialize default active model (Cerebrum)
            SpawnModel("CEREBRUM");

            // Register all texts dynamically for real-time text size scaling
            RegisterAllHUDTexts();

            // Align the UI to face the player's starting spawn orientation
            AlignUiToPlayer();
        }

        private void Update()
        {
            // Smoothly animate minimization and maximization with premium fluid lerp
            if (mainRect != null)
            {
                Vector3 targetPanelScaleVec = isMinimized ? Vector3.zero : Vector3.one;
                mainRect.localScale = Vector3.Lerp(mainRect.localScale, targetPanelScaleVec, Time.deltaTime * 12.0f);
            }

            if (minimizedIconRect != null)
            {
                Vector3 targetIconScaleVec = isMinimized ? Vector3.one : Vector3.zero;
                minimizedIconRect.localScale = Vector3.Lerp(minimizedIconRect.localScale, targetIconScaleVec, Time.deltaTime * 12.0f);
            }

            // Smoothly glide active selector highlight across segment row tabs
            if (highlightRect != null)
            {
                RectTransform targetBtnRect = null;
                if (activeModelType == "CEREBRUM" && btnCerebrum != null) targetBtnRect = btnCerebrum.GetComponent<RectTransform>();
                else if (activeModelType == "NEURON" && btnNeuron != null) targetBtnRect = btnNeuron.GetComponent<RectTransform>();
                else if (activeModelType == "SYNAPSE" && btnSynapse != null) targetBtnRect = btnSynapse.GetComponent<RectTransform>();

                if (targetBtnRect != null)
                {
                    highlightRect.anchoredPosition = Vector2.Lerp(highlightRect.anchoredPosition, targetBtnRect.anchoredPosition, Time.deltaTime * 15.0f);
                    highlightRect.sizeDelta = Vector2.Lerp(highlightRect.sizeDelta, targetBtnRect.sizeDelta, Time.deltaTime * 15.0f);
                }
            }

            // Real-time synchronization of neural spiking states to the floating VR HUD
            if (neuralSolver != null && txtMembranePotential != null)
            {
                txtMembranePotential.text = $"{neuralSolver.membranePotential:F1} mV";
                
                // Colorize membrane readout during depolarizations
                if (neuralSolver.membranePotential > -40f) 
                    txtMembranePotential.color = new Color(0.12f, 0.58f, 0.95f, 0.95f); // glowing electric blue
                else 
                    txtMembranePotential.color = new Color(0.4f, 0.4f, 0.5f, 0.85f); // elegant charcoal slate
            }

            // Update running timelines
            if (TimeWarpManager.Instance != null)
            {
                if (txtTimelineVal != null)
                {
                    txtTimelineVal.text = $"{TimeWarpManager.Instance.elapsedMilliseconds / 1000f:F2}s";
                }
                if (sliderTimeline != null)
                {
                    isUpdatingTimeline = true;
                    sliderTimeline.value = TimeWarpManager.Instance.virtualTimelineProgress;
                    isUpdatingTimeline = false;
                }
            }
        }

        // --- Hormone Modulators ---
        private void OnDopamineChanged(float val)
        {
            if (neuralSolver != null) neuralSolver.dopamine = val;
            if (txtDopamineVal != null) txtDopamineVal.text = $"{val * 100f:F0}%";
        }

        private void OnSerotoninChanged(float val)
        {
            if (neuralSolver != null) neuralSolver.serotonin = val;
            if (txtSerotoninVal != null) txtSerotoninVal.text = $"{val * 100f:F0}%";
        }

        private void OnCortisolChanged(float val)
        {
            if (neuralSolver != null) neuralSolver.cortisol = val;
            if (txtCortisolVal != null) txtCortisolVal.text = $"{val * 100f:F0}%";
        }

        // --- Time Warp Manager Modulators ---
        private void OnTimeWarpChanged(float val)
        {
            if (TimeWarpManager.Instance != null) TimeWarpManager.Instance.SetTimeScale(val);
            if (txtTimeWarpVal != null) txtTimeWarpVal.text = $"{val:F1}x";
        }

        private void OnTimelineScrubbed(float val)
        {
            if (isUpdatingTimeline) return;
            if (TimeWarpManager.Instance != null) TimeWarpManager.Instance.ScrubTo(val);
        }

        // --- Micro Synaptic exploded view ---
        private void OnSynapseExplodeChanged(float val)
        {
            if (synapseControllerRef != null) synapseControllerRef.SetExplosionFactor(val);
            if (txtSynapseExplodeVal != null) txtSynapseExplodeVal.text = $"{val * 100f:F0}%";
        }

        // --- Remote GCP AlphaFold Job Launch ---
        private void OnLaunchJobClicked()
        {
            if (inputMolecularTarget == null || string.IsNullOrEmpty(inputMolecularTarget.text)) return;

            string target = inputMolecularTarget.text;
            Debug.Log($"[VR HUD] Triggering remote GCP AlphaFold job request for: {target}");

            if (GcpCloudBridge.Instance != null)
            {
                GcpCloudBridge.Instance.TriggerStructuralGeneration("MOLECULAR", target);
            }
        }

        // --- Holographic Cybernetic Workspace Toggles ---
        private void OnToggleComparisonClicked()
        {
            if (atomicRenderer == null) return;
            atomicRenderer.sideBySideComparisonMode = !atomicRenderer.sideBySideComparisonMode;
            UpdateWorkspaceToggleButtons();
        }

        private void OnToggleScaleClicked()
        {
            if (atomicRenderer == null) return;
            atomicRenderer.uniformScaleAlignment = !atomicRenderer.uniformScaleAlignment;
            UpdateWorkspaceToggleButtons();
        }

        private void OnClearWorkspaceClicked()
        {
            if (atomicRenderer == null) return;
            atomicRenderer.ClearWorkspace();
        }

        private void UpdateWorkspaceToggleButtons()
        {
            if (atomicRenderer == null) return;

            // Update Comparison Mode indicator
            if (txtToggleComparison != null && btnToggleComparison != null)
            {
                bool active = atomicRenderer.sideBySideComparisonMode;
                txtToggleComparison.text = active ? "SIDE-BY-SIDE: ON" : "SIDE-BY-SIDE: OFF";
                btnToggleComparison.GetComponent<Image>().color = active ? new Color(0.08f, 0.08f, 0.08f, 0.95f) : new Color(0.9f, 0.9f, 0.92f, 0.8f);
                txtToggleComparison.color = active ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.9f);
            }

            // Update Scale Alignment indicator
            if (txtToggleScale != null && btnToggleScale != null)
            {
                bool active = atomicRenderer.uniformScaleAlignment;
                txtToggleScale.text = active ? "SCALE UNIFORM: ON" : "SCALE UNIFORM: OFF";
                btnToggleScale.GetComponent<Image>().color = active ? new Color(0.08f, 0.08f, 0.08f, 0.95f) : new Color(0.9f, 0.9f, 0.92f, 0.8f);
                txtToggleScale.color = active ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.9f);
            }
        }

        // =========================================================================
        // HIGH-FIDELITY PROGRAMMATIC DYNAMIC CANVAS BUILDER FOR ZERO-FRICTION SETUP
        // =========================================================================
        private void BuildBeautifulVRHUDCanvas()
        {
            Debug.Log("[BioVR HUD] Instantiating vertical responsive glassmorphic canvas with rounded corners...");
            uiSections.Clear();

            // 1. Fetch default fonts safely for cross-platform support (Quest 3 & PC)
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Roboto", 14);
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("sans-serif", 14);
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 14);

            // 2. Set Canvas Root (Vertical rectangle 450x780)
            gameObject.name = "VR_Glassmorphic_HUD";
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(450f, 780f);
            // Placed elegantly on the left side, angled toward the viewer
            canvasRect.localPosition = new Vector3(-0.9f, 1.3f, 1.4f);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            canvasRect.localRotation = Quaternion.Euler(0f, 25f, 0f);

            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Add OVRRaycaster so Meta Quest controllers can raycast the UI
            gameObject.AddComponent<OVRRaycaster>();
            canvas.worldCamera = Camera.main;

            // 3. Create Main Translucent Background Panel (Sleek Glassmorphic Light Mode)
            GameObject mainPanelObj = new GameObject("TranslucentGlass_Panel");
            mainPanelObj.transform.SetParent(transform, false);
            mainRect = mainPanelObj.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.pivot = new Vector2(0.5f, 0.5f);
            mainRect.offsetMin = new Vector2(10f, 10f);
            mainRect.offsetMax = new Vector2(-10f, -10f);
            
            Image mainImg = mainPanelObj.AddComponent<Image>();
            // Translucent pristine clinical white glass
            mainImg.color = new Color(0.96f, 0.96f, 0.98f, 0.94f);
            
            // Apply rounded corners with 9-slicing
            if (roundedCornerSprite != null)
            {
                mainImg.sprite = roundedCornerSprite;
                mainImg.type = Image.Type.Sliced;
            }

            // Minimalist charcoal outline for premium depth
            Outline outline = mainPanelObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.3f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // ==========================================
            // RESPONSIVE TITLE BAR AT TOP
            // ==========================================
            GameObject titleBarObj = new GameObject("TitleBar");
            titleBarObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform titleBarRect = titleBarObj.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0f, 1f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            titleBarRect.pivot = new Vector2(0.5f, 1f);
            titleBarRect.anchoredPosition = new Vector2(0f, -15f);
            titleBarRect.sizeDelta = new Vector2(-30f, 45f);

            // Wire up the custom sliding panel draggable script
            DraggablePanel titleDrag = titleBarObj.AddComponent<DraggablePanel>();
            titleDrag.targetPanel = mainRect;

            // 4. Main HUD Title
            GameObject titleObj = new GameObject("HUD_Title");
            titleObj.transform.SetParent(titleBarObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0.7f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = Vector2.zero;
            
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "BIOSANDBOX VR";
            if (defaultFont != null) titleText.font = defaultFont;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            titleText.alignment = TextAnchor.MiddleLeft;

            // 4c. Minimize Panel Button (Top-Right, next to Settings)
            GameObject minimizeBtnObj = new GameObject("Minimize_Button");
            minimizeBtnObj.transform.SetParent(titleBarObj.transform, false);
            RectTransform minimizeBtnRect = minimizeBtnObj.AddComponent<RectTransform>();
            minimizeBtnRect.anchorMin = new Vector2(1f, 0.5f);
            minimizeBtnRect.anchorMax = new Vector2(1f, 0.5f);
            minimizeBtnRect.pivot = new Vector2(1f, 0.5f);
            minimizeBtnRect.anchoredPosition = new Vector2(-45f, 0f); // offset to the left of the settings gear button
            minimizeBtnRect.sizeDelta = new Vector2(35f, 35f);

            Image minimizeImg = minimizeBtnObj.AddComponent<Image>();
            minimizeImg.color = new Color(0.88f, 0.88f, 0.9f, 0.6f);
            if (roundedCornerSprite != null)
            {
                minimizeImg.sprite = roundedCornerSprite;
                minimizeImg.type = Image.Type.Sliced;
            }
            Outline minimizeOutline = minimizeBtnObj.AddComponent<Outline>();
            minimizeOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.15f);

            Button minimizeButton = minimizeBtnObj.AddComponent<Button>();
            minimizeButton.onClick.AddListener(ToggleMinimize);
            minimizeBtnObj.AddComponent<SmoothUiInteraction>();

            GameObject minimizeTxtObj = new GameObject("Text");
            minimizeTxtObj.transform.SetParent(minimizeBtnObj.transform, false);
            RectTransform minimizeTxtRect = minimizeTxtObj.AddComponent<RectTransform>();
            minimizeTxtRect.anchorMin = Vector2.zero;
            minimizeTxtRect.anchorMax = Vector2.one;
            minimizeTxtRect.offsetMin = Vector2.zero;
            minimizeTxtRect.offsetMax = Vector2.zero;
            Text minimizeTextComp = minimizeTxtObj.AddComponent<Text>();
            minimizeTextComp.text = "➖";
            if (defaultFont != null) minimizeTextComp.font = defaultFont;
            minimizeTextComp.fontSize = 12;
            minimizeTextComp.fontStyle = FontStyle.Bold;
            minimizeTextComp.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            minimizeTextComp.alignment = TextAnchor.MiddleCenter;

            // 4b. Settings Icon Gear Button (Top-Right)
            GameObject settingsBtnObj = new GameObject("Settings_Button");
            settingsBtnObj.transform.SetParent(titleBarObj.transform, false);
            RectTransform settingsRect = settingsBtnObj.AddComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 0.5f);
            settingsRect.anchorMax = new Vector2(1f, 0.5f);
            settingsRect.pivot = new Vector2(1f, 0.5f);
            settingsRect.anchoredPosition = Vector2.zero;
            settingsRect.sizeDelta = new Vector2(35f, 35f);
            
            Image settingsImg = settingsBtnObj.AddComponent<Image>();
            settingsImg.color = new Color(0.88f, 0.88f, 0.9f, 0.6f);
            if (roundedCornerSprite != null)
            {
                settingsImg.sprite = roundedCornerSprite;
                settingsImg.type = Image.Type.Sliced;
            }
            Outline settingsOutline = settingsBtnObj.AddComponent<Outline>();
            settingsOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.15f);
            
            Button settingsButton = settingsBtnObj.AddComponent<Button>();
            settingsButton.onClick.AddListener(OnSettingsClicked);
            settingsBtnObj.AddComponent<SmoothUiInteraction>();
            
            GameObject settingsTxtObj = new GameObject("Text");
            settingsTxtObj.transform.SetParent(settingsBtnObj.transform, false);
            RectTransform settingsTxtRect = settingsTxtObj.AddComponent<RectTransform>();
            settingsTxtRect.anchorMin = Vector2.zero;
            settingsTxtRect.anchorMax = Vector2.one;
            settingsTxtRect.offsetMin = Vector2.zero;
            settingsTxtRect.offsetMax = Vector2.zero;
            Text settingsTextComp = settingsTxtObj.AddComponent<Text>();
            settingsTextComp.text = "⚙";
            if (defaultFont != null) settingsTextComp.font = defaultFont;
            settingsTextComp.fontSize = 20;
            settingsTextComp.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            settingsTextComp.alignment = TextAnchor.MiddleCenter;

            // ==========================================
            // RESPONSIVE CATEGORY FILTRATION DROPDOWN
            // ==========================================
            GameObject dropdownObj = new GameObject("Section_Dropdown");
            dropdownObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform ddRect = dropdownObj.AddComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0f, 1f);
            ddRect.anchorMax = new Vector2(1f, 1f);
            ddRect.pivot = new Vector2(0.5f, 1f);
            ddRect.anchoredPosition = new Vector2(0f, -70f); // Positioned between Title and Scroll View
            ddRect.sizeDelta = new Vector2(-30f, 35f);

            Image ddImg = dropdownObj.AddComponent<Image>();
            ddImg.color = new Color(0.9f, 0.9f, 0.92f, 0.8f);
            if (roundedCornerSprite != null)
            {
                ddImg.sprite = roundedCornerSprite;
                ddImg.type = Image.Type.Sliced;
            }
            dropdownObj.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.15f);

            sectionFilterDropdown = dropdownObj.AddComponent<Dropdown>();
            
            System.Collections.Generic.List<Dropdown.OptionData> options = new System.Collections.Generic.List<Dropdown.OptionData>();
            options.Add(new Dropdown.OptionData("📂 Category: Show All Panels"));
            options.Add(new Dropdown.OptionData("🧠 Section: Macro Neural Solver"));
            options.Add(new Dropdown.OptionData("🔬 Section: Simulation Model Selection"));
            options.Add(new Dropdown.OptionData("⚡ Section: Cellular Synaptic Cleft"));
            options.Add(new Dropdown.OptionData("🧬 Section: AlphaFold Atomic Molecules"));
            options.Add(new Dropdown.OptionData("⏳ Section: Recording Time Scales"));
            sectionFilterDropdown.AddOptions(options);

            sectionFilterDropdown.onValueChanged.AddListener(OnSectionFilterChanged);

            GameObject ddLabelObj = new GameObject("Label");
            ddLabelObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform ddLabelRect = ddLabelObj.AddComponent<RectTransform>();
            ddLabelRect.anchorMin = Vector2.zero;
            ddLabelRect.anchorMax = Vector2.one;
            ddLabelRect.offsetMin = new Vector2(12f, 0f);
            ddLabelRect.offsetMax = new Vector2(-35f, 0f);
            Text ddText = ddLabelObj.AddComponent<Text>();
            ddText.text = options[0].text;
            if (defaultFont != null) ddText.font = defaultFont;
            ddText.fontSize = 11;
            ddText.fontStyle = FontStyle.Bold;
            ddText.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            ddText.alignment = TextAnchor.MiddleLeft;
            sectionFilterDropdown.captionText = ddText;

            GameObject ddTemplateObj = new GameObject("Template");
            ddTemplateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform ddTemplateRect = ddTemplateObj.AddComponent<RectTransform>();
            ddTemplateRect.anchorMin = new Vector2(0f, 0f);
            ddTemplateRect.anchorMax = new Vector2(1f, 0f);
            ddTemplateRect.pivot = new Vector2(0.5f, 1f);
            ddTemplateRect.anchoredPosition = new Vector2(0f, -2f);
            ddTemplateRect.sizeDelta = new Vector2(0f, 180f);
            Image templateImg = ddTemplateObj.AddComponent<Image>();
            templateImg.color = new Color(0.96f, 0.96f, 0.98f, 0.98f);
            if (roundedCornerSprite != null)
            {
                templateImg.sprite = roundedCornerSprite;
                templateImg.type = Image.Type.Sliced;
            }
            ddTemplateObj.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.25f);
            ddTemplateObj.SetActive(false);
            sectionFilterDropdown.template = ddTemplateRect;

            GameObject ddViewport = new GameObject("Viewport");
            ddViewport.transform.SetParent(ddTemplateObj.transform, false);
            RectTransform ddViewportRect = ddViewport.AddComponent<RectTransform>();
            ddViewportRect.anchorMin = Vector2.zero;
            ddViewportRect.anchorMax = Vector2.one;
            ddViewportRect.sizeDelta = Vector2.zero;
            ddViewport.AddComponent<RectMask2D>();

            GameObject ddContent = new GameObject("Content");
            ddContent.transform.SetParent(ddViewport.transform, false);
            RectTransform ddContentRect = ddContent.AddComponent<RectTransform>();
            ddContentRect.anchorMin = new Vector2(0f, 1f);
            ddContentRect.anchorMax = new Vector2(1f, 1f);
            ddContentRect.pivot = new Vector2(0.5f, 1f);
            ddContentRect.sizeDelta = new Vector2(0f, 180f);

            ScrollRect ddScroll = ddTemplateObj.AddComponent<ScrollRect>();
            ddScroll.horizontal = false;
            ddScroll.vertical = true;
            ddScroll.viewport = ddViewportRect;
            ddScroll.content = ddContentRect;

            GameObject ddItem = new GameObject("Item");
            ddItem.transform.SetParent(ddContent.transform, false);
            RectTransform ddItemRect = ddItem.AddComponent<RectTransform>();
            ddItemRect.anchorMin = new Vector2(0f, 0.5f);
            ddItemRect.anchorMax = new Vector2(1f, 0.5f);
            ddItemRect.sizeDelta = new Vector2(0f, 30f);

            Toggle ddToggle = ddItem.AddComponent<Toggle>();

            GameObject ddItemLabel = new GameObject("Item Label");
            ddItemLabel.transform.SetParent(ddItem.transform, false);
            RectTransform ddItemLabelRect = ddItemLabel.AddComponent<RectTransform>();
            ddItemLabelRect.anchorMin = Vector2.zero;
            ddItemLabelRect.anchorMax = Vector2.one;
            ddItemLabelRect.offsetMin = new Vector2(15f, 0f);
            ddItemLabelRect.offsetMax = new Vector2(-15f, 0f);
            Text ddItemText = ddItemLabel.AddComponent<Text>();
            if (defaultFont != null) ddItemText.font = defaultFont;
            ddItemText.fontSize = 11;
            ddItemText.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            ddItemText.alignment = TextAnchor.MiddleLeft;
            
            sectionFilterDropdown.itemText = ddItemText;
            ddItem.AddComponent<SmoothUiInteraction>();

            // ==========================================
            // RESPONSIVE MIDDLE SCROLL VIEW
            // ==========================================
            GameObject scrollViewObj = new GameObject("Scroll_View");
            scrollViewObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform scrollRectTransform = scrollViewObj.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(15f, 65f); // 65px room at bottom
            scrollRectTransform.offsetMax = new Vector2(-15f, -115f); // 115px room at top (45px shifted for dropdown)

            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 15f;
            
            // Create Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.sizeDelta = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            // Create Scroll Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 850f);
            
            VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 15f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false; // elements set their own height
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(5, 5, 10, 10);

            ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            // ==========================================
            // SECTION 1: MACRO NEURAL SOLVER
            // ==========================================
            GameObject dynamicsSection = new GameObject("Section_Dynamics");
            dynamicsSection.transform.SetParent(contentObj.transform, false);
            uiSections.Add(dynamicsSection);
            RectTransform dynamicsRect = dynamicsSection.AddComponent<RectTransform>();
            dynamicsRect.sizeDelta = new Vector2(0f, 220f);
            dynamicsSection.AddComponent<LayoutElement>().preferredHeight = 220f;
            
            Image dynBg = dynamicsSection.AddComponent<Image>();
            dynBg.color = new Color(1f, 1f, 1f, 0.45f);
            if (roundedCornerSprite != null)
            {
                dynBg.sprite = roundedCornerSprite;
                dynBg.type = Image.Type.Sliced;
            }
            dynamicsSection.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);

            VerticalLayoutGroup dynLayout = dynamicsSection.AddComponent<VerticalLayoutGroup>();
            dynLayout.spacing = 10f;
            dynLayout.padding = new RectOffset(12, 12, 10, 10);
            dynLayout.childControlWidth = true;
            dynLayout.childControlHeight = false;
            dynLayout.childForceExpandWidth = true;

            CreateHeader(dynamicsSection.transform, "MACRO NEURAL SOLVER", defaultFont, 25f);

            // Vm Meter (Membrane Potential readout)
            GameObject vmMeterObj = new GameObject("Vm_Meter");
            vmMeterObj.transform.SetParent(dynamicsSection.transform, false);
            RectTransform vmMeterRect = vmMeterObj.AddComponent<RectTransform>();
            vmMeterRect.sizeDelta = new Vector2(0f, 35f);
            vmMeterObj.AddComponent<LayoutElement>().preferredHeight = 35f;
            
            Image vmMeterImg = vmMeterObj.AddComponent<Image>();
            vmMeterImg.color = new Color(0.9f, 0.9f, 0.92f, 0.6f);
            if (roundedCornerSprite != null)
            {
                vmMeterImg.sprite = roundedCornerSprite;
                vmMeterImg.type = Image.Type.Sliced;
            }
            Outline vmMeterOutline = vmMeterObj.AddComponent<Outline>();
            vmMeterOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.1f);

            GameObject vmLabelObj = new GameObject("Vm_Label");
            vmLabelObj.transform.SetParent(vmMeterObj.transform, false);
            RectTransform vmLabelRect = vmLabelObj.AddComponent<RectTransform>();
            vmLabelRect.anchorMin = new Vector2(0f, 0.5f);
            vmLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            vmLabelRect.pivot = new Vector2(0f, 0.5f);
            vmLabelRect.anchoredPosition = new Vector2(10f, 0f);
            vmLabelRect.sizeDelta = new Vector2(150f, 30f);
            Text vmLabelText = vmLabelObj.AddComponent<Text>();
            vmLabelText.text = "Membrane Potential:";
            if (defaultFont != null) vmLabelText.font = defaultFont;
            vmLabelText.fontSize = 11;
            vmLabelText.fontStyle = FontStyle.Bold;
            vmLabelText.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            vmLabelText.alignment = TextAnchor.MiddleLeft;

            GameObject vmValObj = new GameObject("Vm_Value");
            vmValObj.transform.SetParent(vmMeterObj.transform, false);
            RectTransform vmValRect = vmValObj.AddComponent<RectTransform>();
            vmValRect.anchorMin = new Vector2(0.5f, 0.5f);
            vmValRect.anchorMax = new Vector2(1f, 0.5f);
            vmValRect.pivot = new Vector2(1f, 0.5f);
            vmValRect.anchoredPosition = new Vector2(-10f, 0f);
            vmValRect.sizeDelta = new Vector2(140f, 30f);
            txtMembranePotential = vmValObj.AddComponent<Text>();
            txtMembranePotential.text = "-70.0 mV";
            if (defaultFont != null) txtMembranePotential.font = defaultFont;
            txtMembranePotential.fontSize = 15;
            txtMembranePotential.fontStyle = FontStyle.Bold;
            txtMembranePotential.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            txtMembranePotential.alignment = TextAnchor.MiddleRight;

            // Sliders for Dopamine, Serotonin, Cortisol
            sliderDopamine = CreateProgrammaticSlider(dynamicsSection.transform, "Dopamine (Gain)", 0f, 1f, 0.5f, 35f, defaultFont, out txtDopamineVal);
            sliderSerotonin = CreateProgrammaticSlider(dynamicsSection.transform, "Serotonin (Stability)", 0f, 1f, 0.5f, 35f, defaultFont, out txtSerotoninVal);
            sliderCortisol = CreateProgrammaticSlider(dynamicsSection.transform, "Cortisol (Stress)", 0f, 1f, 0.1f, 35f, defaultFont, out txtCortisolVal);

            // ==========================================
            // SECTION 2: SIMULATION MODEL SELECTION
            // ==========================================
            GameObject selectorSection = new GameObject("Section_Selector");
            selectorSection.transform.SetParent(contentObj.transform, false);
            uiSections.Add(selectorSection);
            RectTransform selectorRect = selectorSection.AddComponent<RectTransform>();
            selectorRect.sizeDelta = new Vector2(0f, 135f);
            selectorSection.AddComponent<LayoutElement>().preferredHeight = 135f;

            Image selBg = selectorSection.AddComponent<Image>();
            selBg.color = new Color(1f, 1f, 1f, 0.45f);
            if (roundedCornerSprite != null)
            {
                selBg.sprite = roundedCornerSprite;
                selBg.type = Image.Type.Sliced;
            }
            selectorSection.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);

            VerticalLayoutGroup selLayout = selectorSection.AddComponent<VerticalLayoutGroup>();
            selLayout.spacing = 8f;
            selLayout.padding = new RectOffset(12, 12, 10, 10);
            selLayout.childControlWidth = true;
            selLayout.childControlHeight = false;
            selLayout.childForceExpandWidth = true;

            CreateHeader(selectorSection.transform, "SIMULATION MODEL SELECTION", defaultFont, 20f);

            // Select Row Container
            GameObject selRowObj = new GameObject("SelectRow");
            selRowObj.transform.SetParent(selectorSection.transform, false);
            RectTransform selRowRect = selRowObj.AddComponent<RectTransform>();
            selRowRect.sizeDelta = new Vector2(0f, 32f);
            selRowObj.AddComponent<LayoutElement>().preferredHeight = 32f;

            HorizontalLayoutGroup selRowLayout = selRowObj.AddComponent<HorizontalLayoutGroup>();
            selRowLayout.spacing = 8f;
            selRowLayout.childAlignment = TextAnchor.MiddleCenter;
            selRowLayout.childControlWidth = true;
            selRowLayout.childControlHeight = true;
            selRowLayout.childForceExpandWidth = true;
            selRowLayout.childForceExpandHeight = true;

            // Spawning Select Buttons
            btnCerebrum = CreateSpawnSelectButton(selRowObj.transform, "CEREBRUM", "BRAIN", defaultFont, out txtCerebrum);
            btnNeuron = CreateSpawnSelectButton(selRowObj.transform, "NEURON", "NEURON", defaultFont, out txtNeuron);
            btnSynapse = CreateSpawnSelectButton(selRowObj.transform, "SYNAPSE", "SYNAPSE", defaultFont, out txtSynapse);

            // Dynamic Segment highlight background (spawned behind buttons)
            GameObject highlightObj = new GameObject("Selector_Highlight");
            highlightObj.transform.SetParent(selRowObj.transform, false);
            highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.pivot = new Vector2(0.5f, 0.5f);
            
            Image highlightImg = highlightObj.AddComponent<Image>();
            highlightImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f); // solid charcoal black
            if (roundedCornerSprite != null)
            {
                highlightImg.sprite = roundedCornerSprite;
                highlightImg.type = Image.Type.Sliced;
            }
            highlightObj.transform.SetAsFirstSibling(); // Draw behind buttons!

            // Row 2: True Size & Measurements (In Horizontal layout)
            GameObject rowTwoObj = new GameObject("Selector_Row2");
            rowTwoObj.transform.SetParent(selectorSection.transform, false);
            RectTransform rowTwoRect = rowTwoObj.AddComponent<RectTransform>();
            rowTwoRect.sizeDelta = new Vector2(0f, 50f);
            rowTwoObj.AddComponent<LayoutElement>().preferredHeight = 50f;

            HorizontalLayoutGroup rowTwoLayout = rowTwoObj.AddComponent<HorizontalLayoutGroup>();
            rowTwoLayout.spacing = 10f;
            rowTwoLayout.childControlWidth = true;
            rowTwoLayout.childControlHeight = true;
            rowTwoLayout.childForceExpandWidth = true;
            rowTwoLayout.childForceExpandHeight = true;

            // True Size Toggle Button
            GameObject sizeToggleObj = new GameObject("TrueSize_Button");
            sizeToggleObj.transform.SetParent(rowTwoObj.transform, false);
            Image sizeToggleImg = sizeToggleObj.AddComponent<Image>();
            sizeToggleImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            if (roundedCornerSprite != null)
            {
                sizeToggleImg.sprite = roundedCornerSprite;
                sizeToggleImg.type = Image.Type.Sliced;
            }
            Outline sizeOutline = sizeToggleObj.AddComponent<Outline>();
            sizeOutline.effectColor = new Color(0.9f, 0.9f, 0.9f, 0.2f);
            
            btnToggleTrueSize = sizeToggleObj.AddComponent<Button>();
            btnToggleTrueSize.onClick.AddListener(() => {
                trueSizeEnabled = !trueSizeEnabled;
                SpawnModel(activeModelType);
            });
            sizeToggleObj.AddComponent<SmoothUiInteraction>();

            GameObject sizeTxtObj = new GameObject("Text");
            sizeTxtObj.transform.SetParent(sizeToggleObj.transform, false);
            RectTransform sizeTxtRect = sizeTxtObj.AddComponent<RectTransform>();
            sizeTxtRect.anchorMin = Vector2.zero;
            sizeTxtRect.anchorMax = Vector2.one;
            sizeTxtRect.offsetMin = Vector2.zero;
            sizeTxtRect.offsetMax = Vector2.zero;
            txtTrueSizeStatus = sizeTxtObj.AddComponent<Text>();
            txtTrueSizeStatus.text = "TRUE SIZE: OFF";
            if (defaultFont != null) txtTrueSizeStatus.font = defaultFont;
            txtTrueSizeStatus.fontSize = 11;
            txtTrueSizeStatus.fontStyle = FontStyle.Bold;
            txtTrueSizeStatus.color = Color.white;
            txtTrueSizeStatus.alignment = TextAnchor.MiddleCenter;

            // Physical Measurements Panel (Displays active sizes/scales)
            GameObject measPanelObj = new GameObject("MeasurementsPanel");
            measPanelObj.transform.SetParent(rowTwoObj.transform, false);
            Image measPanelImg = measPanelObj.AddComponent<Image>();
            measPanelImg.color = new Color(0.9f, 0.9f, 0.92f, 0.8f);
            if (roundedCornerSprite != null)
            {
                measPanelImg.sprite = roundedCornerSprite;
                measPanelImg.type = Image.Type.Sliced;
            }
            measPanelObj.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.1f);

            GameObject measTxtObj = new GameObject("MeasurementsText");
            measTxtObj.transform.SetParent(measPanelObj.transform, false);
            RectTransform measTxtRect = measTxtObj.AddComponent<RectTransform>();
            measTxtRect.anchorMin = Vector2.zero;
            measTxtRect.anchorMax = Vector2.one;
            measTxtRect.offsetMin = new Vector2(8f, 4f);
            measTxtRect.offsetMax = new Vector2(-8f, -4f);
            
            txtMeasurementsReadout = measTxtObj.AddComponent<Text>();
            txtMeasurementsReadout.text = "SCALE: MAGNIFIED\nModel: Cerebrum\nSize: 25 cm (Comfort)";
            if (defaultFont != null) txtMeasurementsReadout.font = defaultFont;
            txtMeasurementsReadout.fontSize = 9;
            txtMeasurementsReadout.lineSpacing = 1.1f;
            txtMeasurementsReadout.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            txtMeasurementsReadout.alignment = TextAnchor.MiddleLeft;

            // ==========================================
            // SECTION 3: CELLULAR SYNAPTIC CLEFT
            // ==========================================
            GameObject cellularSection = new GameObject("Section_Cellular");
            cellularSection.transform.SetParent(contentObj.transform, false);
            uiSections.Add(cellularSection);
            RectTransform cellularRect = cellularSection.AddComponent<RectTransform>();
            cellularRect.sizeDelta = new Vector2(0f, 85f);
            cellularSection.AddComponent<LayoutElement>().preferredHeight = 85f;

            Image cellBg = cellularSection.AddComponent<Image>();
            cellBg.color = new Color(1f, 1f, 1f, 0.45f);
            if (roundedCornerSprite != null)
            {
                cellBg.sprite = roundedCornerSprite;
                cellBg.type = Image.Type.Sliced;
            }
            cellularSection.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);

            VerticalLayoutGroup cellLayout = cellularSection.AddComponent<VerticalLayoutGroup>();
            cellLayout.spacing = 8f;
            cellLayout.padding = new RectOffset(12, 12, 10, 10);
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = false;
            cellLayout.childForceExpandWidth = true;

            CreateHeader(cellularSection.transform, "CELLULAR SYNAPTIC CLEFT", defaultFont, 20f);
            sliderSynapseExplode = CreateProgrammaticSlider(cellularSection.transform, "Cleft Separation (Exploded View)", 0f, 1f, 0.0f, 35f, defaultFont, out txtSynapseExplodeVal);

            // ==========================================
            // SECTION 4: ALPHAFOLD ATOMIC MOLECULES
            // ==========================================
            GameObject molecularSection = new GameObject("Section_Molecular");
            molecularSection.transform.SetParent(contentObj.transform, false);
            uiSections.Add(molecularSection);
            RectTransform molecularRect = molecularSection.AddComponent<RectTransform>();
            molecularRect.sizeDelta = new Vector2(0f, 175f);
            molecularSection.AddComponent<LayoutElement>().preferredHeight = 175f;

            Image molBg = molecularSection.AddComponent<Image>();
            molBg.color = new Color(1f, 1f, 1f, 0.45f);
            if (roundedCornerSprite != null)
            {
                molBg.sprite = roundedCornerSprite;
                molBg.type = Image.Type.Sliced;
            }
            molecularSection.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);

            VerticalLayoutGroup molLayout = molecularSection.AddComponent<VerticalLayoutGroup>();
            molLayout.spacing = 8f;
            molLayout.padding = new RectOffset(12, 12, 10, 10);
            molLayout.childControlWidth = true;
            molLayout.childControlHeight = false;
            molLayout.childForceExpandWidth = true;

            CreateHeader(molecularSection.transform, "ALPHAFOLD ATOMIC MOLECULES", defaultFont, 20f);

            // Row 1: Molecular target input field + Generate Button
            GameObject inputRowObj = new GameObject("Molecular_InputRow");
            inputRowObj.transform.SetParent(molecularSection.transform, false);
            RectTransform inputRowRect = inputRowObj.AddComponent<RectTransform>();
            inputRowRect.sizeDelta = new Vector2(0f, 32f);
            inputRowObj.AddComponent<LayoutElement>().preferredHeight = 32f;

            HorizontalLayoutGroup inputRowLayout = inputRowObj.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.spacing = 8f;
            inputRowLayout.childControlWidth = true;
            inputRowLayout.childControlHeight = true;
            inputRowLayout.childForceExpandWidth = true;
            inputRowLayout.childForceExpandHeight = true;

            GameObject inputObj = new GameObject("MoleculeInput");
            inputObj.transform.SetParent(inputRowObj.transform, false);
            Image inputImg = inputObj.AddComponent<Image>();
            inputImg.color = new Color(0.9f, 0.9f, 0.92f, 0.8f);
            if (roundedCornerSprite != null)
            {
                inputImg.sprite = roundedCornerSprite;
                inputImg.type = Image.Type.Sliced;
            }
            inputObj.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.15f);
            
            inputMolecularTarget = inputObj.AddComponent<InputField>();
            
            GameObject inputPlaceholderObj = new GameObject("Placeholder");
            inputPlaceholderObj.transform.SetParent(inputObj.transform, false);
            RectTransform placeholderRect = inputPlaceholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 4f);
            placeholderRect.offsetMax = new Vector2(-10f, -4f);
            Text pText = inputPlaceholderObj.AddComponent<Text>();
            pText.text = "Enter UniProt ID / Target Name...";
            if (defaultFont != null) pText.font = defaultFont;
            pText.fontSize = 11;
            pText.fontStyle = FontStyle.Italic;
            pText.color = new Color(0.4f, 0.4f, 0.45f, 0.7f);
            pText.alignment = TextAnchor.MiddleLeft;
            
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(inputObj.transform, false);
            RectTransform textRect = inputTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);
            Text iText = inputTextObj.AddComponent<Text>();
            if (defaultFont != null) iText.font = defaultFont;
            iText.fontSize = 11;
            iText.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            iText.alignment = TextAnchor.MiddleLeft;

            inputMolecularTarget.placeholder = pText;
            inputMolecularTarget.textComponent = iText;

            // Launch button (Sleek black)
            GameObject btnObj = new GameObject("LaunchButton");
            btnObj.transform.SetParent(inputRowObj.transform, false);
            btnObj.AddComponent<LayoutElement>().preferredWidth = 90f;
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            if (roundedCornerSprite != null)
            {
                btnImg.sprite = roundedCornerSprite;
                btnImg.type = Image.Type.Sliced;
            }
            btnLaunchAlphaFoldJob = btnObj.AddComponent<Button>();
            btnObj.AddComponent<SmoothUiInteraction>();
            
            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTxtRect = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.offsetMin = Vector2.zero;
            btnTxtRect.offsetMax = Vector2.zero;
            Text bText = btnTxtObj.AddComponent<Text>();
            bText.text = "GENERATE";
            if (defaultFont != null) bText.font = defaultFont;
            bText.fontSize = 11;
            bText.fontStyle = FontStyle.Bold;
            bText.color = Color.white;
            bText.alignment = TextAnchor.MiddleCenter;

            // Row 2: Quick Molecular Selection grid (X aligned)
            GameObject selectGrid = new GameObject("SelectGrid");
            selectGrid.transform.SetParent(molecularSection.transform, false);
            RectTransform selectGridRect = selectGrid.AddComponent<RectTransform>();
            selectGridRect.sizeDelta = new Vector2(0f, 30f);
            selectGrid.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup selectGridLayout = selectGrid.AddComponent<HorizontalLayoutGroup>();
            selectGridLayout.spacing = 6f;
            selectGridLayout.childControlWidth = true;
            selectGridLayout.childControlHeight = true;
            selectGridLayout.childForceExpandWidth = true;
            selectGridLayout.childForceExpandHeight = true;

            CreateQuickSelectButton(selectGrid.transform, "DOPAMINE", defaultFont);
            CreateQuickSelectButton(selectGrid.transform, "SEROTONIN", defaultFont);
            CreateQuickSelectButton(selectGrid.transform, "RECEPTOR", defaultFont);
            CreateQuickSelectButton(selectGrid.transform, "GPCR", defaultFont);

            // Row 3: Workspace control toggles
            GameObject togglePanel = new GameObject("TogglePanel");
            togglePanel.transform.SetParent(molecularSection.transform, false);
            RectTransform togglePanelRect = togglePanel.AddComponent<RectTransform>();
            togglePanelRect.sizeDelta = new Vector2(0f, 30f);
            togglePanel.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup togglePanelLayout = togglePanel.AddComponent<HorizontalLayoutGroup>();
            togglePanelLayout.spacing = 8f;
            togglePanelLayout.childControlWidth = true;
            togglePanelLayout.childControlHeight = true;
            togglePanelLayout.childForceExpandWidth = true;
            togglePanelLayout.childForceExpandHeight = true;

            // Cyber comparison toggle
            GameObject compObj = new GameObject("CompMode_Button");
            compObj.transform.SetParent(togglePanel.transform, false);
            Image compImg = compObj.AddComponent<Image>();
            compImg.color = new Color(0.9f, 0.9f, 0.92f, 0.8f);
            if (roundedCornerSprite != null)
            {
                compImg.sprite = roundedCornerSprite;
                compImg.type = Image.Type.Sliced;
            }
            compObj.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.15f);
            btnToggleComparison = compObj.AddComponent<Button>();
            btnToggleComparison.onClick.AddListener(OnToggleComparisonClicked);
            compObj.AddComponent<SmoothUiInteraction>();
            
            GameObject compTxtObj = new GameObject("Text");
            compTxtObj.transform.SetParent(compObj.transform, false);
            RectTransform compTxtRect = compTxtObj.AddComponent<RectTransform>();
            compTxtRect.anchorMin = Vector2.zero;
            compTxtRect.anchorMax = Vector2.one;
            compTxtRect.offsetMin = Vector2.zero;
            compTxtRect.offsetMax = Vector2.zero;
            txtToggleComparison = compTxtObj.AddComponent<Text>();
            txtToggleComparison.text = "SIDE-BY-SIDE: OFF";
            if (defaultFont != null) txtToggleComparison.font = defaultFont;
            txtToggleComparison.fontSize = 11;
            txtToggleComparison.fontStyle = FontStyle.Bold;
            txtToggleComparison.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            txtToggleComparison.alignment = TextAnchor.MiddleCenter;

            // Cyber scale toggle
            GameObject scaleObj = new GameObject("ScaleMode_Button");
            scaleObj.transform.SetParent(togglePanel.transform, false);
            Image scaleImg = scaleObj.AddComponent<Image>();
            scaleImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            if (roundedCornerSprite != null)
            {
                scaleImg.sprite = roundedCornerSprite;
                scaleImg.type = Image.Type.Sliced;
            }
            btnToggleScale = scaleObj.AddComponent<Button>();
            btnToggleScale.onClick.AddListener(OnToggleScaleClicked);
            scaleObj.AddComponent<SmoothUiInteraction>();
            
            GameObject scaleTxtObj = new GameObject("Text");
            scaleTxtObj.transform.SetParent(scaleObj.transform, false);
            RectTransform scaleTxtRect = scaleTxtObj.AddComponent<RectTransform>();
            scaleTxtRect.anchorMin = Vector2.zero;
            scaleTxtRect.anchorMax = Vector2.one;
            scaleTxtRect.offsetMin = Vector2.zero;
            scaleTxtRect.offsetMax = Vector2.zero;
            txtToggleScale = scaleTxtObj.AddComponent<Text>();
            txtToggleScale.text = "SCALE UNIFORM: ON";
            if (defaultFont != null) txtToggleScale.font = defaultFont;
            txtToggleScale.fontSize = 11;
            txtToggleScale.fontStyle = FontStyle.Bold;
            txtToggleScale.color = Color.white;
            txtToggleScale.alignment = TextAnchor.MiddleCenter;

            // ==========================================
            // SECTION 5: PLAYBACK CONTROLS
            // ==========================================
            GameObject midCol = new GameObject("Section_Playback");
            midCol.transform.SetParent(contentObj.transform, false);
            uiSections.Add(midCol);
            RectTransform midRect = midCol.AddComponent<RectTransform>();
            midRect.sizeDelta = new Vector2(0f, 85f);
            midCol.AddComponent<LayoutElement>().preferredHeight = 85f;

            Image playBg = midCol.AddComponent<Image>();
            playBg.color = new Color(1f, 1f, 1f, 0.45f);
            if (roundedCornerSprite != null)
            {
                playBg.sprite = roundedCornerSprite;
                playBg.type = Image.Type.Sliced;
            }
            midCol.AddComponent<Outline>().effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);

            VerticalLayoutGroup playLayout = midCol.AddComponent<VerticalLayoutGroup>();
            playLayout.spacing = 8f;
            playLayout.padding = new RectOffset(12, 12, 10, 10);
            playLayout.childControlWidth = true;
            playLayout.childControlHeight = false;
            playLayout.childForceExpandWidth = true;

            CreateHeader(midCol.transform, "RECORDING TIME SCALES", defaultFont, 20f);
            sliderTimeWarp = CreateProgrammaticSlider(midCol.transform, "Simulation Time Scale Warp", 0f, 10f, 1.0f, 35f, defaultFont, out txtTimeWarpVal);

            // ==========================================
            // RESPONSIVE STATUS PANEL & PURGE AT BOTTOM
            // ==========================================
            GameObject statusPanel = new GameObject("StatusPanel");
            statusPanel.transform.SetParent(mainPanelObj.transform, false);
            RectTransform statusRect = statusPanel.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 15f);
            statusRect.sizeDelta = new Vector2(-30f, 40f);

            HorizontalLayoutGroup statusLayout = statusPanel.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 15f;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = true;
            statusLayout.childForceExpandWidth = true;
            statusLayout.childForceExpandHeight = true;

            HolographicGpuStatsPanel gpuPanel = mainPanelObj.GetComponent<HolographicGpuStatsPanel>();
            if (gpuPanel == null) gpuPanel = mainPanelObj.AddComponent<HolographicGpuStatsPanel>();

            // Job Status text
            GameObject statusTextObj = new GameObject("StatusTextObj");
            statusTextObj.transform.SetParent(statusPanel.transform, false);
            gpuPanel.txtJobStatus = statusTextObj.AddComponent<Text>();
            gpuPanel.txtJobStatus.text = "STATUS: OFFLINE";
            if (defaultFont != null) gpuPanel.txtJobStatus.font = defaultFont;
            gpuPanel.txtJobStatus.fontSize = 12;
            gpuPanel.txtJobStatus.fontStyle = FontStyle.Bold;
            gpuPanel.txtJobStatus.color = new Color(0.4f, 0.4f, 0.5f);
            gpuPanel.txtJobStatus.alignment = TextAnchor.MiddleLeft;

            // Clear Workspace Button
            GameObject clearObj = new GameObject("ClearWorkspace_Button");
            clearObj.transform.SetParent(statusPanel.transform, false);
            clearObj.AddComponent<LayoutElement>().preferredWidth = 100f;
            Image clearImg = clearObj.AddComponent<Image>();
            clearImg.color = new Color(0.95f, 0.85f, 0.85f, 0.8f);
            if (roundedCornerSprite != null)
            {
                clearImg.sprite = roundedCornerSprite;
                clearImg.type = Image.Type.Sliced;
            }
            Outline clearOutline = clearObj.AddComponent<Outline>();
            clearOutline.effectColor = new Color(0.8f, 0.1f, 0.1f, 0.4f);
            
            btnClearWorkspace = clearObj.AddComponent<Button>();
            btnClearWorkspace.onClick.AddListener(OnClearWorkspaceClicked);
            clearObj.AddComponent<SmoothUiInteraction>();

            GameObject clearTxtObj = new GameObject("Text");
            clearTxtObj.transform.SetParent(clearObj.transform, false);
            RectTransform clearTxtRect = clearTxtObj.AddComponent<RectTransform>();
            clearTxtRect.anchorMin = Vector2.zero;
            clearTxtRect.anchorMax = Vector2.one;
            clearTxtRect.offsetMin = Vector2.zero;
            clearTxtRect.offsetMax = Vector2.zero;
            Text clearText = clearTxtObj.AddComponent<Text>();
            clearText.text = "CLEAR ALL";
            if (defaultFont != null) clearText.font = defaultFont;
            clearText.fontSize = 11;
            clearText.fontStyle = FontStyle.Bold;
            clearText.color = new Color(0.8f, 0.1f, 0.1f);
            clearText.alignment = TextAnchor.MiddleCenter;

            // Dynamic session and GPU stats tracker setup (hidden but tracked inside component)
            GameObject hiddenStats = new GameObject("HiddenStats");
            hiddenStats.transform.SetParent(mainPanelObj.transform, false);
            Text gpuNameTxt;
            gpuPanel.imgVramProgress = CreateProgressBar(hiddenStats.transform, "GPU VRAM Buffer", new Color(0.08f, 0.08f, 0.08f, 0.8f), new Vector2(100f, 10f), Vector2.zero, defaultFont, out gpuPanel.txtVramReadout, out gpuNameTxt);
            gpuPanel.txtGpuName = gpuNameTxt;
            Text sessionTimeTxt;
            gpuPanel.imgQuotaProgress = CreateProgressBar(hiddenStats.transform, "Session Quota", new Color(0.08f, 0.08f, 0.08f, 0.8f), new Vector2(100f, 10f), Vector2.zero, defaultFont, out gpuPanel.txtQuotaPercentage, out sessionTimeTxt);
            gpuPanel.txtSessionTime = sessionTimeTxt;
            hiddenStats.SetActive(false);

            // 12. Console Panel overlay setup
            GameObject consoleObj = new GameObject("ConsolePanel");
            consoleObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform consoleRect = consoleObj.AddComponent<RectTransform>();
            consoleRect.anchorMin = Vector2.zero;
            consoleRect.anchorMax = Vector2.one;
            consoleRect.pivot = new Vector2(0.5f, 0.5f);
            consoleRect.offsetMin = new Vector2(10f, 10f);
            consoleRect.offsetMax = new Vector2(-10f, -10f);
            
            Image consoleImg = consoleObj.AddComponent<Image>();
            consoleImg.color = new Color(0.02f, 0.02f, 0.04f, 0.96f);
            if (roundedCornerSprite != null)
            {
                consoleImg.sprite = roundedCornerSprite;
                consoleImg.type = Image.Type.Sliced;
            }
            Outline consoleOutline = consoleObj.AddComponent<Outline>();
            consoleOutline.effectColor = new Color(0.9f, 0.9f, 0.9f, 0.2f);

            GameObject closeOverlayBtn = new GameObject("CloseOverlay_Button");
            closeOverlayBtn.transform.SetParent(consoleObj.transform, false);
            RectTransform closeRect = closeOverlayBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-15f, -15f);
            closeRect.sizeDelta = new Vector2(30f, 30f);
            Image closeImg = closeOverlayBtn.AddComponent<Image>();
            closeImg.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            if (roundedCornerSprite != null)
            {
                closeImg.sprite = roundedCornerSprite;
                closeImg.type = Image.Type.Sliced;
            }
            Button closeBtn = closeOverlayBtn.AddComponent<Button>();
            closeBtn.onClick.AddListener(OnSettingsClicked);
            closeOverlayBtn.AddComponent<SmoothUiInteraction>();
            
            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeOverlayBtn.transform, false);
            RectTransform closeTxtRect = closeTxtObj.AddComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;
            Text closeText = closeTxtObj.AddComponent<Text>();
            closeText.text = "X";
            if (defaultFont != null) closeText.font = defaultFont;
            closeText.fontSize = 14;
            closeText.fontStyle = FontStyle.Bold;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;

            GameObject consoleHeader = new GameObject("Console_Header");
            consoleHeader.transform.SetParent(consoleObj.transform, false);
            RectTransform consoleHeaderRect = consoleHeader.AddComponent<RectTransform>();
            consoleHeaderRect.anchorMin = new Vector2(0f, 1f);
            consoleHeaderRect.anchorMax = new Vector2(0.7f, 1f);
            consoleHeaderRect.pivot = new Vector2(0f, 1f);
            consoleHeaderRect.anchoredPosition = new Vector2(15f, -15f);
            consoleHeaderRect.sizeDelta = new Vector2(0f, 30f);
            Text chText = consoleHeader.AddComponent<Text>();
            chText.text = "DIAGNOSTIC SYSTEM CONSOLE";
            if (defaultFont != null) chText.font = defaultFont;
            chText.fontSize = 13;
            chText.fontStyle = FontStyle.Bold;
            chText.color = Color.white;
            chText.alignment = TextAnchor.MiddleLeft;

            // 12b. Add Text Size Adjustment Slider to the Console overlay
            GameObject textSizeSliderObj = new GameObject("TextSize_Slider_Row");
            textSizeSliderObj.transform.SetParent(consoleObj.transform, false);
            RectTransform tsRect = textSizeSliderObj.AddComponent<RectTransform>();
            tsRect.anchorMin = new Vector2(0f, 1f);
            tsRect.anchorMax = new Vector2(1f, 1f);
            tsRect.pivot = new Vector2(0.5f, 1f);
            tsRect.anchoredPosition = new Vector2(0f, -55f);
            tsRect.sizeDelta = new Vector2(-30f, 35f);

            sliderTextSize = CreateProgrammaticSlider(textSizeSliderObj.transform, "UI Text Scale Factor", 0.6f, 1.5f, 1.0f, 35f, defaultFont, out txtTextSizeVal);
            sliderTextSize.onValueChanged.AddListener(OnTextSizeSliderChanged);
            
            // Adjust slider colors to look crisp on the dark console overlay
            Text tsLabel = textSizeSliderObj.GetComponentInChildren<Text>();
            if (tsLabel != null) tsLabel.color = new Color(0.9f, 0.9f, 0.92f, 0.9f);
            if (txtTextSizeVal != null) txtTextSizeVal.color = Color.white;
            
            Image tsFill = sliderTextSize.fillRect.GetComponent<Image>();
            if (tsFill != null) tsFill.color = new Color(0.9f, 0.9f, 0.92f, 0.95f);

            GameObject logScrollObj = new GameObject("Log_Scroll");
            logScrollObj.transform.SetParent(consoleObj.transform, false);
            RectTransform logScrollRect = logScrollObj.AddComponent<RectTransform>();
            logScrollRect.anchorMin = Vector2.zero;
            logScrollRect.anchorMax = Vector2.one;
            logScrollRect.offsetMin = new Vector2(15f, 15f);
            logScrollRect.offsetMax = new Vector2(-15f, -100f); // shifted down by 45px to fit slider

            ScrollRect logScroll = logScrollObj.AddComponent<ScrollRect>();
            logScroll.horizontal = false;
            logScroll.vertical = true;

            GameObject logViewport = new GameObject("Viewport");
            logViewport.transform.SetParent(logScrollObj.transform, false);
            RectTransform logViewportRect = logViewport.AddComponent<RectTransform>();
            logViewportRect.anchorMin = Vector2.zero;
            logViewportRect.anchorMax = Vector2.one;
            logViewportRect.sizeDelta = Vector2.zero;
            logViewport.AddComponent<RectMask2D>();
            logScroll.viewport = logViewportRect;

            GameObject logContent = new GameObject("Content");
            logContent.transform.SetParent(logViewport.transform, false);
            RectTransform logContentRect = logContent.AddComponent<RectTransform>();
            logContentRect.anchorMin = new Vector2(0f, 1f);
            logContentRect.anchorMax = new Vector2(1f, 1f);
            logContentRect.pivot = new Vector2(0.5f, 1f);
            logContentRect.sizeDelta = new Vector2(0f, 300f);
            logScroll.content = logContentRect;

            gpuPanel.txtCheckpointLogs = CreateLabel(logContent.transform, "[Console Idle] Ready to trigger molecular simulation.", defaultFont, 300f, new Color(0.7f, 0.8f, 0.9f, 0.9f));
            gpuPanel.txtCheckpointLogs.fontSize = 11;
            gpuPanel.txtCheckpointLogs.alignment = TextAnchor.UpperLeft;
            
            // Anchor log label to stretch horizontally
            RectTransform labelRt = gpuPanel.txtCheckpointLogs.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            consoleObjRef = consoleObj;
            consoleObj.SetActive(false);

            // 13. Emergency Banner
            GameObject bannerObj = new GameObject("EmergencyWarning_Banner");
            bannerObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 1f);
            bannerRect.anchorMax = new Vector2(1f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -70f); // just below title
            bannerRect.sizeDelta = new Vector2(-30f, 30f);
            Image bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.9f, 0.1f, 0.1f, 0.95f);
            if (roundedCornerSprite != null)
            {
                bannerImg.sprite = roundedCornerSprite;
                bannerImg.type = Image.Type.Sliced;
            }
            
            Text bannerText = CreateLabel(bannerObj.transform, "⚠️ QUOTA EXHAUSTED 90%! SHUTTING DOWN VM...", defaultFont, 25f, Color.white);
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.fontSize = 11;
            bannerText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform bTxtRt = bannerText.GetComponent<RectTransform>();
            bTxtRt.anchorMin = Vector2.zero;
            bTxtRt.anchorMax = Vector2.one;
            bTxtRt.offsetMin = Vector2.zero;
            bTxtRt.offsetMax = Vector2.zero;

            gpuPanel.emergencyWarningBanner = bannerObj;
            bannerObj.SetActive(false);

            // 14. CLICK-AND-DRAG WINDOW RESIZER HANDLE (Bottom-Right Corner)
            GameObject resizeHandleObj = new GameObject("ResizeHandle");
            resizeHandleObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform resizeRect = resizeHandleObj.AddComponent<RectTransform>();
            resizeRect.anchorMin = new Vector2(1f, 0f);
            resizeRect.anchorMax = new Vector2(1f, 0f);
            resizeRect.pivot = new Vector2(1f, 0f);
            resizeRect.anchoredPosition = new Vector2(5f, -5f);
            resizeRect.sizeDelta = new Vector2(30f, 30f);

            Image resizeImg = resizeHandleObj.AddComponent<Image>();
            resizeImg.color = new Color(0.3f, 0.3f, 0.35f, 0.4f);
            if (roundedCornerSprite != null)
            {
                resizeImg.sprite = roundedCornerSprite;
                resizeImg.type = Image.Type.Sliced;
            }

            GameObject resizeTxtObj = new GameObject("Text");
            resizeTxtObj.transform.SetParent(resizeHandleObj.transform, false);
            RectTransform resizeTxtRect = resizeTxtObj.AddComponent<RectTransform>();
            resizeTxtRect.anchorMin = Vector2.zero;
            resizeTxtRect.anchorMax = Vector2.one;
            resizeTxtRect.offsetMin = Vector2.zero;
            resizeTxtRect.offsetMax = Vector2.zero;
            Text resizeText = resizeTxtObj.AddComponent<Text>();
            resizeText.text = "◢";
            if (defaultFont != null) resizeText.font = defaultFont;
            resizeText.fontSize = 18;
            resizeText.color = new Color(0.08f, 0.08f, 0.08f, 0.6f);
            resizeText.alignment = TextAnchor.LowerRight;

            // Wire up the custom scaling resizer script
            ResizablePanel resizer = resizeHandleObj.AddComponent<ResizablePanel>();
            resizer.targetPanel = mainRect;

            // 15. CIRCULAR DISSOLVING FLOATING MINIMIZED UI ICON
            minimizedIconObj = new GameObject("Minimized_Icon");
            minimizedIconObj.transform.SetParent(transform, false);
            minimizedIconRect = minimizedIconObj.AddComponent<RectTransform>();
            minimizedIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            minimizedIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            minimizedIconRect.pivot = new Vector2(0.5f, 0.5f);
            minimizedIconRect.anchoredPosition = Vector2.zero;
            minimizedIconRect.sizeDelta = new Vector2(75f, 75f);
            minimizedIconRect.localScale = Vector3.zero; // Start minimized at scale zero!

            Image iconImg = minimizedIconObj.AddComponent<Image>();
            // Sleek translucent circular frosted glass
            iconImg.color = new Color(0.96f, 0.96f, 0.98f, 0.96f);
            if (roundedCornerSprite != null)
            {
                iconImg.sprite = roundedCornerSprite;
                iconImg.type = Image.Type.Sliced;
            }

            // Outline
            Outline iconOutline = minimizedIconObj.AddComponent<Outline>();
            iconOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.3f);
            iconOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Make it clickable to maximize back!
            Button iconButton = minimizedIconObj.AddComponent<Button>();
            iconButton.onClick.AddListener(ToggleMinimize);
            minimizedIconObj.AddComponent<SmoothUiInteraction>();

            // Pulsing brain text emoji inside the circle
            GameObject brainEmojiObj = new GameObject("EmojiText");
            brainEmojiObj.transform.SetParent(minimizedIconObj.transform, false);
            RectTransform brainEmojiRect = brainEmojiObj.AddComponent<RectTransform>();
            brainEmojiRect.anchorMin = Vector2.zero;
            brainEmojiRect.anchorMax = Vector2.one;
            brainEmojiRect.offsetMin = Vector2.zero;
            brainEmojiRect.offsetMax = Vector2.zero;
            Text emojiText = brainEmojiObj.AddComponent<Text>();
            emojiText.text = "🧠";
            if (defaultFont != null) emojiText.font = defaultFont;
            emojiText.fontSize = 34;
            emojiText.alignment = TextAnchor.MiddleCenter;
        }

        private Button CreateSpawnSelectButton(Transform parent, string modelTag, string btnLabel, Font font, out Text txtComponent)
        {
            GameObject btn = new GameObject($"Select_{modelTag}_Button");
            btn.transform.SetParent(parent, false);
            
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.06f); // light backdrop
            if (roundedCornerSprite != null)
            {
                img.sprite = roundedCornerSprite;
                img.type = Image.Type.Sliced;
            }
            Outline outline = btn.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.08f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => {
                SpawnModel(modelTag);
            });
            btn.AddComponent<SmoothUiInteraction>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            
            txtComponent = txtObj.AddComponent<Text>();
            txtComponent.text = btnLabel;
            if (font != null) txtComponent.font = font;
            txtComponent.fontSize = 11;
            txtComponent.fontStyle = FontStyle.Bold;
            txtComponent.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            txtComponent.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private void CreateQuickSelectButton(Transform parent, string moleculeName, Font font)
        {
            GameObject btn = new GameObject($"Quick_{moleculeName}_Button");
            btn.transform.SetParent(parent, false);
            
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            if (roundedCornerSprite != null)
            {
                img.sprite = roundedCornerSprite;
                img.type = Image.Type.Sliced;
            }
            Outline outline = btn.AddComponent<Outline>();
            outline.effectColor = new Color(0.9f, 0.9f, 0.9f, 0.2f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => {
                if (inputMolecularTarget != null)
                {
                    inputMolecularTarget.text = moleculeName;
                    OnLaunchJobClicked();
                }
            });
            btn.AddComponent<SmoothUiInteraction>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            Text text = txtObj.AddComponent<Text>();
            text.text = moleculeName;
            if (font != null) text.font = font;
            text.fontSize = 10;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateHeader(Transform parent, string titleText, Font font, float height)
        {
            GameObject headerObj = new GameObject("Header_" + titleText);
            headerObj.transform.SetParent(parent, false);
            RectTransform rect = headerObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            headerObj.AddComponent<LayoutElement>().preferredHeight = height;
            
            Text text = headerObj.AddComponent<Text>();
            text.text = titleText.ToUpper();
            if (font != null) text.font = font;
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            text.alignment = TextAnchor.MiddleLeft;

            // Draw line below header (crisp grey line)
            GameObject underline = new GameObject("Underline");
            underline.transform.SetParent(headerObj.transform, false);
            RectTransform underRect = underline.AddComponent<RectTransform>();
            underRect.anchorMin = new Vector2(0f, 0f);
            underRect.anchorMax = new Vector2(1f, 0f);
            underRect.pivot = new Vector2(0.5f, 0f);
            underRect.anchoredPosition = new Vector2(0f, -2f);
            underRect.sizeDelta = new Vector2(0f, 1.5f);
            Image lineImg = underline.AddComponent<Image>();
            lineImg.color = new Color(0.08f, 0.08f, 0.08f, 0.15f);
        }

        private Text CreateLabel(Transform parent, string content, Font font, float height, Color color)
        {
            GameObject obj = new GameObject("Label_" + content.GetHashCode());
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            obj.AddComponent<LayoutElement>().preferredHeight = height;

            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            if (font != null) txt.font = font;
            txt.fontSize = 12;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private Slider CreateProgrammaticSlider(Transform parent, string labelText, float min, float max, float startVal, float height, Font font, out Text valueReadout)
        {
            GameObject sliderObj = new GameObject(labelText + "_Slider");
            sliderObj.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0f, height);
            sliderObj.AddComponent<LayoutElement>().preferredHeight = height;
            Slider slider = sliderObj.AddComponent<Slider>();

            // Label (Anchored left-center)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0.6f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(4f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 18f);
            Text labelTextComp = labelObj.AddComponent<Text>();
            labelTextComp.text = labelText;
            if (font != null) labelTextComp.font = font;
            labelTextComp.fontSize = 11;
            labelTextComp.fontStyle = FontStyle.Bold;
            labelTextComp.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            labelTextComp.alignment = TextAnchor.MiddleLeft;

            // Value readout text (Anchored right-center)
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(sliderObj.transform, false);
            RectTransform valRect = valObj.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.6f, 1f);
            valRect.anchorMax = new Vector2(1f, 1f);
            valRect.pivot = new Vector2(1f, 1f);
            valRect.anchoredPosition = new Vector2(-4f, 0f);
            valRect.sizeDelta = new Vector2(0f, 18f);
            Text valTextComp = valObj.AddComponent<Text>();
            valTextComp.text = startVal.ToString("F1");
            if (font != null) valTextComp.font = font;
            valTextComp.fontSize = 11;
            valTextComp.fontStyle = FontStyle.Bold;
            valTextComp.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            valTextComp.alignment = TextAnchor.MiddleRight;
            valueReadout = valTextComp;

            // Background track (stretches horizontally)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, 4f);
            bgRect.sizeDelta = new Vector2(-8f, 6f);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.88f, 0.88f, 0.9f, 0.8f);

            // Fill Area
            GameObject fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 0f);
            fillAreaRect.pivot = new Vector2(0.5f, 0f);
            fillAreaRect.anchoredPosition = new Vector2(0f, 4f);
            fillAreaRect.sizeDelta = new Vector2(-8f, 6f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(0f, 6f);
            fillRect.anchoredPosition = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            // Handle Area
            GameObject handleAreaObj = new GameObject("HandleArea");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 0f);
            handleAreaRect.pivot = new Vector2(0.5f, 0f);
            handleAreaRect.anchoredPosition = new Vector2(0f, 4f);
            handleAreaRect.sizeDelta = new Vector2(-8f, 6f);

            // Handle (Crisp round handle)
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(12f, 12f);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white;
            Outline handleOutline = handleObj.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            handleOutline.effectDistance = new Vector2(1f, -1f);

            // Connect Slider references
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = startVal;
            slider.direction = Slider.Direction.LeftToRight;

            // Setup anchors for correct slider resizing behavior
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;

            // Force slider helper to intercept mouse pointer clicks
            sliderObj.AddComponent<VrOnlySliderHelper>();

            return slider;
        }

        private Image CreateProgressBar(Transform parent, string labelText, Color progressColor, Vector2 size, Vector2 anchoredPos, Font font, out Text valReadout, out Text nameReadout)
        {
            GameObject container = new GameObject("ProgressBar_" + labelText);
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchoredPosition = anchoredPos;
            containerRect.sizeDelta = size;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(-size.x / 2f + 110f, 14f);
            labelRect.sizeDelta = new Vector2(220f, 20f);
            nameReadout = labelObj.AddComponent<Text>();
            nameReadout.text = labelText;
            if (font != null) nameReadout.font = font;
            nameReadout.fontSize = 11;
            nameReadout.fontStyle = FontStyle.Bold;
            nameReadout.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);

            // Value text
            GameObject valObj = new GameObject("Value");
            valObj.transform.SetParent(container.transform, false);
            RectTransform valRect = valObj.AddComponent<RectTransform>();
            valRect.anchoredPosition = new Vector2(size.x / 2f - 40f, 14f);
            valRect.sizeDelta = new Vector2(80f, 20f);
            valReadout = valObj.AddComponent<Text>();
            valReadout.text = "0%";
            if (font != null) valReadout.font = font;
            valReadout.fontSize = 11;
            valReadout.fontStyle = FontStyle.Bold;
            valReadout.color = progressColor;
            valReadout.alignment = TextAnchor.MiddleRight;

            // Bar background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(container.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(size.x, 8f);
            bgRect.anchoredPosition = new Vector2(0f, -8f);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.88f, 0.88f, 0.9f, 0.8f);

            // Progress Fill Image
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(container.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0.5f);
            fillRect.anchorMax = new Vector2(0, 0.5f);
            fillRect.sizeDelta = new Vector2(0f, 8f);
            fillRect.anchoredPosition = new Vector2(0f, -8f);
            
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = progressColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Align fill Rect to act like standard progress meter
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            fillRect.sizeDelta = new Vector2(size.x, 8f);
            fillRect.anchoredPosition = new Vector2(-size.x/2f, -8f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            return fillImg;
        }

        // =========================================================================
        // HIGH-FIDELITY HELPER SYSTEMS FOR SCIENTIFIC SIMULATION CONTROLS
        // =========================================================================

        /// <summary>
        /// Generates a pixel-perfect 9-sliced rounded corner sprite at runtime.
        /// Avoids blocky visual artifacts and guarantees premium glassmorphic UI aesthetics.
        /// </summary>
        public static Sprite CreateRoundedCornerSprite(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] cols = new Color32[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0, dy = 0;
                    bool inCorner = false;
                    
                    if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inCorner = true; }
                    else if (x > width - 1 - radius && y < radius) { dx = x - (width - 1 - radius); dy = radius - y; inCorner = true; }
                    else if (x < radius && y > height - 1 - radius) { dx = radius - x; dy = y - (height - 1 - radius); inCorner = true; }
                    else if (x > width - 1 - radius && y > height - 1 - radius) { dx = x - (width - 1 - radius); dy = y - (height - 1 - radius); inCorner = true; }
                    
                    float alpha = 255;
                    if (inCorner)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > radius)
                        {
                            alpha = 0;
                        }
                        else if (dist > radius - 1f)
                        {
                            // Blend edge smoothly for perfect anti-aliasing
                            alpha = (1f - (dist - (radius - 1f))) * 255f;
                        }
                    }
                    
                    cols[y * width + x] = new Color32(255, 255, 255, (byte)alpha);
                }
            }
            
            tex.SetPixels32(cols);
            tex.Apply();
            
            Vector4 border = new Vector4(radius, radius, radius, radius);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return sprite;
        }

        /// <summary>
        /// Handles switching and spawning of 3D models with appropriate comfort vs. True Size configurations.
        /// </summary>
        public void SpawnModel(string modelName)
        {
            activeModelType = modelName.ToUpper();
            Debug.Log($"[BioVR Spawn] Transitioning model view to: {activeModelType}");

            // 1. Locate existing scene references
            GameObject brain = GameObject.Find("Procedural_Cerebrum");
            GameObject synapse = GameObject.Find("Cellular_Synapse_Cleft");

            // Deactivate all structures first to cleanly swap the viewport
            if (brain != null) brain.SetActive(false);
            if (synapse != null) synapse.SetActive(false);
            if (activeNeuronObj != null) activeNeuronObj.SetActive(false);

            // 2. Spawn and scale active target
            if (activeModelType == "CEREBRUM")
            {
                if (brain != null)
                {
                    brain.SetActive(true);
                    brain.transform.localPosition = new Vector3(0.1f, 1.3f, 1.5f);
                    brain.transform.localScale = trueSizeEnabled ? new Vector3(0.15f, 0.15f, 0.15f) : new Vector3(0.25f, 0.25f, 0.25f);
                    
                    // Attach bouncing idle animation
                    var anim = brain.GetComponent<ModelIdleAnimation>();
                    if (anim == null) anim = brain.AddComponent<ModelIdleAnimation>();
                    anim.modelType = "Brain";
                }
            }
            else if (activeModelType == "NEURON")
            {
                if (activeNeuronObj == null)
                {
                    activeNeuronObj = new GameObject("Procedural_Neuron");
                    activeNeuronObj.transform.SetParent(transform.parent != null ? transform.parent : null);
                    activeNeuronObj.transform.localPosition = new Vector3(0.1f, 1.3f, 1.5f);

                    // Add our high-fidelity procedural 3D Neuron builder
                    activeNeuronObj.AddComponent<NeuronController>();
                    activeNeuronObj.AddComponent<ModelRotator>();

                    var anim = activeNeuronObj.AddComponent<ModelIdleAnimation>();
                    anim.modelType = "Neuron";
                    anim.bounceAmplitude = 0.05f;
                    anim.rotationSpeed = 10f;
                }
                
                activeNeuronObj.SetActive(true);
                activeNeuronObj.transform.localPosition = new Vector3(0.1f, 1.3f, 1.5f);
                
                // Pyramidal neuron is physically microscopic (~20 microns), so True Size scales it to 0.00002
                // Comfort View scales it to a comparable cerebrum size (0.25) so it matches the brain perfectly!
                activeNeuronObj.transform.localScale = trueSizeEnabled ? new Vector3(0.00002f, 0.00002f, 0.00002f) : new Vector3(0.25f, 0.25f, 0.25f);
            }
            else if (activeModelType == "SYNAPSE")
            {
                if (synapse != null)
                {
                    synapse.SetActive(true);
                    synapse.transform.localPosition = new Vector3(0.1f, 1.3f, 1.5f);
                    
                    // Synaptic cleft gap is physically nanoscopic (~20 nanometers), so True Size scales it to 0.00000002
                    synapse.transform.localScale = trueSizeEnabled ? new Vector3(0.00000002f, 0.00000002f, 0.00000002f) : new Vector3(0.25f, 0.25f, 0.25f);
                    
                    var anim = synapse.GetComponent<ModelIdleAnimation>();
                    if (anim == null) anim = synapse.AddComponent<ModelIdleAnimation>();
                    anim.modelType = "Synapse";
                }
            }

            // Sync True Size toggle status text on UI
            if (txtTrueSizeStatus != null)
            {
                txtTrueSizeStatus.text = trueSizeEnabled ? "TRUE SIZE: ON" : "TRUE SIZE: OFF";
                btnToggleTrueSize.GetComponent<Image>().color = trueSizeEnabled ? new Color(0.85f, 0.2f, 0.35f, 0.9f) : new Color(0.08f, 0.08f, 0.08f, 0.95f);
            }

            // Sync active text colors
            if (txtCerebrum != null) txtCerebrum.color = activeModelType == "CEREBRUM" ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.8f);
            if (txtNeuron != null) txtNeuron.color = activeModelType == "NEURON" ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.8f);
            if (txtSynapse != null) txtSynapse.color = activeModelType == "SYNAPSE" ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.8f);

            UpdateMeasurementsReadout();
        }

        /// <summary>
        /// Updates the real-time scientific measurement readouts and active unit dimensions on the HUD.
        /// </summary>
        private void UpdateMeasurementsReadout()
        {
            if (txtMeasurementsReadout == null) return;

            string text = "";
            if (trueSizeEnabled)
            {
                text += "<color=#D92B54>SCALE: ACTUAL PHYSICAL SIZE</color>\n";
                if (activeModelType == "CEREBRUM")
                {
                    text += "Model: Human Cerebrum\n";
                    text += "Actual Dimensions: ~15 cm x 12 cm\n";
                    text += "Magnification Level: 1.0x (Direct Scale)\n";
                    text += "Active Coordinates: cm (Centimeters)";
                }
                else if (activeModelType == "NEURON")
                {
                    text += "Model: Pyramidal Neuron Soma\n";
                    text += "Actual Dimensions: ~20 μm x 20 μm\n";
                    text += "Magnification Level: 1.0x (True Micro Scale)\n";
                    text += "Active Coordinates: μm (Micrometers)";
                }
                else if (activeModelType == "SYNAPSE")
                {
                    text += "Model: Synaptic Cleft Assembly\n";
                    text += "Actual Dimensions: ~20 nm gap\n";
                    text += "Magnification Level: 1.0x (True Nano Scale)\n";
                    text += "Active Coordinates: nm (Nanometers)";
                }
            }
            else
            {
                text += "<color=#008050>SCALE: MAGNIFIED COMFORT VIEW</color>\n";
                if (activeModelType == "CEREBRUM")
                {
                    text += "Model: Human Cerebrum\n";
                    text += "Visual Sandbox Size: 25 cm (0.25m)\n";
                    text += "Magnification Level: 1.66x\n";
                    text += "Active Coordinates: Meters (Sandbox Space)";
                }
                else if (activeModelType == "NEURON")
                {
                    text += "Model: Pyramidal Neuron (Scaled)\n";
                    text += "Visual Sandbox Size: 25 cm (0.25m)\n";
                    text += "Magnification Level: 12,500x\n";
                    text += "Active Coordinates: Meters (Sandbox Space)";
                }
                else if (activeModelType == "SYNAPSE")
                {
                    text += "Model: Synaptic Cleft (Scaled)\n";
                    text += "Visual Sandbox Size: 25 cm (0.25m)\n";
                    text += "Magnification Level: 12,500,000x\n";
                    text += "Active Coordinates: Meters (Sandbox Space)";
                }
            }

            txtMeasurementsReadout.text = text;
        }
    }
}
