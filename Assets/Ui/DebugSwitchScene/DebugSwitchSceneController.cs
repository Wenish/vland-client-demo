using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(UIDocument))]
public class DebugSwitchSceneController : MonoBehaviour
{
    private UIDocument uiDocument;
    public Key toggleKey = Key.F4;

    private Button ZombieMapButton;
    private Button SkirmishMapButton;
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: UIDocument not found on GameObject.");
            return;
        }
        var rootVisualElement = uiDocument.rootVisualElement;
        if (rootVisualElement == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: rootVisualElement is null.");
            return;
        }
        ZombieMapButton = rootVisualElement.Q<Button>("ZombieMap");
        if (ZombieMapButton == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ZombieMap' in UXML.");
        }
        SkirmishMapButton = rootVisualElement.Q<Button>("SkirmishMap");
        if (SkirmishMapButton == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'SkirmishMap' in UXML.");
        }

        HideWindow();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleWindow();
        }
    }

    private void ToggleWindow()
    {
        if (uiDocument == null) return;

        var rootVisualElement = uiDocument.rootVisualElement;
        if (rootVisualElement == null) return;

        if (rootVisualElement.style.display == DisplayStyle.None)
        {
            ShowWindow();
        }
        else
        {
            HideWindow();
        }
    }

    private void HideWindow()
    {
        if (uiDocument == null) return;

        var rootVisualElement = uiDocument.rootVisualElement;
        if (rootVisualElement == null) return;

        rootVisualElement.style.display = DisplayStyle.None;
    }

    private void ShowWindow()
    {
        var isServer = NetworkManager.singleton.isNetworkActive && NetworkServer.active;
        if (!isServer)
        {
            Debug.LogWarning("DebugSwitchSceneController: Only the server can switch scenes. This client is not the server.");
            return;
        }
        
        if (uiDocument == null) return;

        var rootVisualElement = uiDocument.rootVisualElement;
        if (rootVisualElement == null) return;

        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    void OnEnable()
    {
        if (ZombieMapButton != null)        {
            ZombieMapButton.clicked += ZombieMapButtonClicked;
            Debug.Log("Subscribed to Zombie Map Button Click");
        }
        if (SkirmishMapButton != null)        {
            SkirmishMapButton.clicked += SkirmishMapButtonClicked;
        }
    }

    void OnDisable()
    {
        if (ZombieMapButton != null)        {
            ZombieMapButton.clicked -= ZombieMapButtonClicked;
        }
        if (SkirmishMapButton != null)        {
            SkirmishMapButton.clicked -= SkirmishMapButtonClicked;
        }
    }

    private void ZombieMapButtonClicked()
    {
        Debug.Log("Zombie Map Button Clicked - Load Zombie Map Scene");
        NetworkManager.singleton.ServerChangeScene("ZombyGameScene");
    }

    private void SkirmishMapButtonClicked()
    {
        Debug.Log("Skirmish Map Button Clicked - Load Skirmish Map Scene");
        NetworkManager.singleton.ServerChangeScene("GameSkirmish1Scene");
    }
}
