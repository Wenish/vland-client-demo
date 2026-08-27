using ShadowInfection.DI;
using ShadowInfection.UI.SettingsPanel;
using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentSettings : MonoBehaviour
{
    private UIDocument uiDocument;
    private SettingsPanelView panelView;
    private SettingsPanelBinder binder;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on this GameObject.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        panelView = new SettingsPanelView(root);
        binder = new SettingsPanelBinder(panelView);
    }

    void OnEnable()
    {
        TryBind();
    }

    void OnDisable()
    {
        binder?.Unbind();
    }

    void Start()
    {
        TryBind();
        binder?.Refresh();
    }

    private void TryBind()
    {
        if (binder == null)
            return;

        var settings = GameServices.Settings;
        if (settings == null)
            return;

        binder.Bind(settings);
    }
}
