using UnityEngine;
using UnityEngine.UIElements;

public class FpsDisplayController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement rootVisualElement;
    private Label fpsLabel;

    [SerializeField] private float updateInterval = 0.25f; // seconds between UI updates

    private float timeAccum;
    private int frames;

    void Awake()
    {
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
    }

    void Update()
    {
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
}
