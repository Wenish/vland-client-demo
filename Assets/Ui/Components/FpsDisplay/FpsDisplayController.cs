using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class FpsDisplayController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement rootVisualElement;
    private Label fpsLabel;

    [SerializeField] private float updateInterval = 0.25f; // seconds between UI updates
    [SerializeField] private bool enableKeyboardToggle = true;
    [SerializeField] private bool showWindow = false;

    private float timeAccum;
    private int frames;
    private const string PREFS_KEY_VISIBLE = "FpsDisplay_Visible";

    void Awake()
    {
        // Load saved visibility state
        showWindow = PlayerPrefs.GetInt(PREFS_KEY_VISIBLE, showWindow ? 1 : 0) == 1;

        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("FpsDisplayController: UIDocument not found on GameObject.");
            return;
        }

        rootVisualElement = uiDocument.rootVisualElement;
        if (rootVisualElement == null)
        {
            Debug.LogWarning("FpsDisplayController: rootVisualElement is null.");
            return;
        }

        fpsLabel = rootVisualElement.Q<Label>("fps-label");
        if (fpsLabel == null)
        {
            Debug.LogWarning("FpsDisplayController: Could not find Label named 'fps-label' in UXML.");
        }

        // Apply initial visibility
        if (rootVisualElement != null)
        {
            rootVisualElement.style.display = showWindow ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    void Update()
    {
        // Toggle visibility with keyboard (using new Input System)
        if (enableKeyboardToggle && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
        {
            ToggleWindow();
        }

        // Early out if UI isn't wired
        if (fpsLabel == null)
            return;

        timeAccum += Time.unscaledDeltaTime; // ignore timescale
        frames++;

        if (timeAccum >= updateInterval)
        {
            float fps = frames / timeAccum;
            float ms = fps > 0f ? (1000f / fps) : 0f;

            fpsLabel.text = $"FPS: {fps:0} ({ms:0.0} ms)";

            // reset counters
            timeAccum = 0f;
            frames = 0;
        }
    }

    private void ToggleWindow()
    {
        showWindow = !showWindow;
        if (rootVisualElement != null)
        {
            rootVisualElement.style.display = showWindow ? DisplayStyle.Flex : DisplayStyle.None;
        }
        // Save state to PlayerPrefs
        PlayerPrefs.SetInt(PREFS_KEY_VISIBLE, showWindow ? 1 : 0);
        PlayerPrefs.Save();
    }
}
