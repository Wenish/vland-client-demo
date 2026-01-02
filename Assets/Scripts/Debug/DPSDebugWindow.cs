using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace ShadowInfection.Debug
{
    /// <summary>
    /// Modern debug UI window for displaying real-time DPS statistics.
    /// Uses UI Toolkit for clean, performant rendering.
    /// Non-invasive: reads data from DPSTracker without affecting gameplay systems.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DPSDebugWindow : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Enable/disable the DPS window")]
        [SerializeField]
        private bool showWindow = true;

        [Tooltip("Update frequency (times per second)")]
        [SerializeField]
        private float updateFrequency = 2f;

        [Tooltip("Enable keyboard toggle (F3 key)")]
        [SerializeField]
        private bool enableKeyboardToggle = true;

        [Header("Display Settings")]
        [Tooltip("Maximum number of units to display")]
        [SerializeField]
        private int maxUnitsToDisplay = 10;

        [Tooltip("Minimum DPS to display (filter out low values)")]
        [SerializeField]
        private float minimumDPSToShow = 0.1f;

        // UI Elements
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement windowContainer;
        private VisualElement headerContainer;
        private Label titleLabel;
        private Label statusLabel;
        private VisualElement contentContainer;
        private ScrollView dpsListScrollView;

        // Runtime state
        private float lastUpdateTime;
        private Dictionary<UnitController, VisualElement> unitRowCache = new Dictionary<UnitController, VisualElement>();
        private const string PREFS_KEY_VISIBLE = "DPSDebugWindow_Visible";

        #region Unity Lifecycle

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("[DPSDebugWindow] UIDocument component not found!");
                enabled = false;
                return;
            }

            // Load saved visibility state
            showWindow = PlayerPrefs.GetInt(PREFS_KEY_VISIBLE, showWindow ? 1 : 0) == 1;
        }

        private void OnEnable()
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                InitializeUI();
            }
        }

        private void Update()
        {
            // Toggle visibility with keyboard (using new Input System)
            if (enableKeyboardToggle && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ToggleWindow();
            }

            // Update display at specified frequency
            if (showWindow && Time.time - lastUpdateTime >= 1f / updateFrequency)
            {
                UpdateDisplay();
                lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region UI Initialization

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;
            
            // Build UI structure programmatically
            BuildUIStructure();
            
            // Apply initial visibility
            if (windowContainer != null)
            {
                windowContainer.style.display = showWindow ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void BuildUIStructure()
        {
            // Main window container
            windowContainer = new VisualElement();
            windowContainer.name = "dps-window";
            windowContainer.AddToClassList("dps-window");
            root.Add(windowContainer);

            // Header
            headerContainer = new VisualElement();
            headerContainer.name = "dps-header";
            headerContainer.AddToClassList("dps-header");
            windowContainer.Add(headerContainer);

            titleLabel = new Label("DPS Monitor");
            titleLabel.AddToClassList("dps-title");
            headerContainer.Add(titleLabel);

            statusLabel = new Label("Initializing...");
            statusLabel.AddToClassList("dps-status");
            headerContainer.Add(statusLabel);

            // Content area
            contentContainer = new VisualElement();
            contentContainer.name = "dps-content";
            contentContainer.AddToClassList("dps-content");
            windowContainer.Add(contentContainer);

            // Scroll view for DPS list
            dpsListScrollView = new ScrollView(ScrollViewMode.Vertical);
            dpsListScrollView.name = "dps-list";
            dpsListScrollView.AddToClassList("dps-list");
            contentContainer.Add(dpsListScrollView);

            // Apply styles
            ApplyStyles();
        }

        private void ApplyStyles()
        {
            // Window container styles
            windowContainer.style.position = Position.Absolute;
            windowContainer.style.top = 20;
            windowContainer.style.right = 20;
            windowContainer.style.width = 320;
            windowContainer.style.maxHeight = 600;
            windowContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            windowContainer.style.borderLeftWidth = 2;
            windowContainer.style.borderRightWidth = 2;
            windowContainer.style.borderTopWidth = 2;
            windowContainer.style.borderBottomWidth = 2;
            windowContainer.style.borderLeftColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            windowContainer.style.borderRightColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            windowContainer.style.borderTopColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            windowContainer.style.borderBottomColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            windowContainer.style.borderTopLeftRadius = 6;
            windowContainer.style.borderTopRightRadius = 6;
            windowContainer.style.borderBottomLeftRadius = 6;
            windowContainer.style.borderBottomRightRadius = 6;
            windowContainer.style.paddingLeft = 12;
            windowContainer.style.paddingRight = 12;
            windowContainer.style.paddingTop = 10;
            windowContainer.style.paddingBottom = 10;

            // Header styles
            headerContainer.style.marginBottom = 10;
            headerContainer.style.paddingBottom = 8;
            headerContainer.style.borderBottomWidth = 1;
            headerContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Title styles
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            titleLabel.style.marginBottom = 4;

            // Status styles
            statusLabel.style.fontSize = 11;
            statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Content styles
            contentContainer.style.flexGrow = 1;

            // Scroll view styles
            dpsListScrollView.style.maxHeight = 500;
        }

        #endregion

        #region Display Update

        private void UpdateDisplay()
        {
            if (DPSTracker.Instance == null)
            {
                statusLabel.text = "DPS Tracker not found";
                ClearDPSList();
                return;
            }

            if (!DPSTracker.Instance.IsInitialized())
            {
                statusLabel.text = "Waiting for player spawn...";
                ClearDPSList();
                return;
            }

            // Get active DPS units
            var activeUnits = DPSTracker.Instance.GetActiveDPSUnits();

            // Filter and limit
            activeUnits.RemoveAll(u => u.dps < minimumDPSToShow);
            if (activeUnits.Count > maxUnitsToDisplay)
            {
                activeUnits.RemoveRange(maxUnitsToDisplay, activeUnits.Count - maxUnitsToDisplay);
            }

            // Update status
            float timeWindow = DPSTracker.Instance.GetTimeWindow();
            statusLabel.text = $"{activeUnits.Count} active units ({timeWindow:F0}s window) | F3 to toggle";

            // Update DPS list
            UpdateDPSList(activeUnits);
        }

        private void UpdateDPSList(List<(UnitController unit, float dps)> activeUnits)
        {
            // Clear existing rows
            dpsListScrollView.Clear();
            unitRowCache.Clear();

            if (activeUnits.Count == 0)
            {
                // Show "no data" message
                var noDataLabel = new Label("No active units");
                noDataLabel.style.fontSize = 12;
                noDataLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noDataLabel.style.paddingTop = 20;
                noDataLabel.style.paddingBottom = 20;
                dpsListScrollView.Add(noDataLabel);
                return;
            }

            // Create rows for each unit
            for (int i = 0; i < activeUnits.Count; i++)
            {
                var (unit, dps) = activeUnits[i];
                var row = CreateUnitRow(unit, dps, i + 1);
                dpsListScrollView.Add(row);
                unitRowCache[unit] = row;
            }
        }

        private VisualElement CreateUnitRow(UnitController unit, float dps, int rank)
        {
            var row = new VisualElement();
            row.name = $"dps-row-{unit.GetInstanceID()}";
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom = 2;
            row.style.backgroundColor = rank % 2 == 0 
                ? new Color(0.15f, 0.15f, 0.15f, 0.5f) 
                : new Color(0.12f, 0.12f, 0.12f, 0.3f);
            row.style.borderTopLeftRadius = 3;
            row.style.borderTopRightRadius = 3;
            row.style.borderBottomLeftRadius = 3;
            row.style.borderBottomRightRadius = 3;

            // Left side: rank and name
            var leftContainer = new VisualElement();
            leftContainer.style.flexDirection = FlexDirection.Row;
            leftContainer.style.alignItems = Align.Center;
            leftContainer.style.flexGrow = 1;
            row.Add(leftContainer);

            // Rank label
            var rankLabel = new Label($"#{rank}");
            rankLabel.style.fontSize = 11;
            rankLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            rankLabel.style.marginRight = 8;
            rankLabel.style.minWidth = 24;
            leftContainer.Add(rankLabel);

            // Unit name
            var nameLabel = new Label(unit.unitName);
            nameLabel.style.fontSize = 13;
            nameLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            nameLabel.style.flexGrow = 1;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            leftContainer.Add(nameLabel);

            // Right side: DPS value
            var dpsLabel = new Label(FormatDPS(dps));
            dpsLabel.style.fontSize = 14;
            dpsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            dpsLabel.style.color = GetDPSColor(dps);
            dpsLabel.style.marginLeft = 12;
            row.Add(dpsLabel);

            return row;
        }

        private void ClearDPSList()
        {
            dpsListScrollView.Clear();
            unitRowCache.Clear();
        }

        #endregion

        #region Utility

        private string FormatDPS(float dps)
        {
            if (dps >= 1000f)
            {
                return $"{dps / 1000f:F1}K DPS";
            }
            return $"{dps:F1} DPS";
        }

        private Color GetDPSColor(float dps)
        {
            // Color gradient based on DPS value
            if (dps >= 100f)
                return new Color(1f, 0.4f, 0.4f, 1f); // Red - Very high
            else if (dps >= 50f)
                return new Color(1f, 0.7f, 0.3f, 1f); // Orange - High
            else if (dps >= 20f)
                return new Color(1f, 0.9f, 0.3f, 1f); // Yellow - Medium
            else
                return new Color(0.6f, 0.9f, 0.6f, 1f); // Green - Low
        }

        public void ToggleWindow()
        {
            showWindow = !showWindow;
            if (windowContainer != null)
            {
                windowContainer.style.display = showWindow ? DisplayStyle.Flex : DisplayStyle.None;
            }
            // Save state to PlayerPrefs
            PlayerPrefs.SetInt(PREFS_KEY_VISIBLE, showWindow ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetWindowVisible(bool visible)
        {
            showWindow = visible;
            if (windowContainer != null)
            {
                windowContainer.style.display = showWindow ? DisplayStyle.Flex : DisplayStyle.None;
            }
            // Save state to PlayerPrefs
            PlayerPrefs.SetInt(PREFS_KEY_VISIBLE, showWindow ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
