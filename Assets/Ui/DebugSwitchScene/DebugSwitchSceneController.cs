using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(UIDocument))]
public class DebugSwitchSceneController : MonoBehaviour
{
    private UIDocument uiDocument;
    public Key toggleKey = Key.F4;

    private Button ButtonZombieMap;
    private Button ButtonSkirmishAMap;
    private Button ButtonSkirmishBMap;
    private Button ButtonSkirmishCMap;
    private Button ButtonSkirmishDMap;
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
        ButtonZombieMap = rootVisualElement.Q<Button>("ButtonZombieMap");
        if (ButtonZombieMap == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ButtonZombieMap' in UXML.");
        }
        ButtonSkirmishAMap = rootVisualElement.Q<Button>("ButtonSkirmishAMap");
        if (ButtonSkirmishAMap == null)
        {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ButtonSkirmishAMap' in UXML.");
        }
        ButtonSkirmishBMap = rootVisualElement.Q<Button>("ButtonSkirmishBMap");
        if (ButtonSkirmishBMap == null) {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ButtonSkirmishBMap' in UXML.");
        }
        ButtonSkirmishCMap = rootVisualElement.Q<Button>("ButtonSkirmishCMap");
        if (ButtonSkirmishCMap == null) {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ButtonSkirmishCMap' in UXML.");
        }
        ButtonSkirmishDMap = rootVisualElement.Q<Button>("ButtonSkirmishDMap");
        if (ButtonSkirmishDMap == null) {
            Debug.LogWarning("DebugSwitchSceneController: Could not find Button named 'ButtonSkirmishDMap' in UXML.");
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
        if (ButtonZombieMap != null)        {
            ButtonZombieMap.clicked += ZombieMapButtonClicked;
            Debug.Log("Subscribed to Zombie Map Button Click");
        }
        if (ButtonSkirmishAMap != null)        {
            ButtonSkirmishAMap.clicked += SkirmishAMapButtonClicked;
        }
        if (ButtonSkirmishBMap != null)        {
            ButtonSkirmishBMap.clicked += SkirmishBMapButtonClicked;
        }
        if (ButtonSkirmishCMap != null)        {
            ButtonSkirmishCMap.clicked += SkirmishCMapButtonClicked;
        }
        if (ButtonSkirmishDMap != null)        {
            ButtonSkirmishDMap.clicked += SkirmishDMapButtonClicked;
        }
    }

    void OnDisable()
    {
        if (ButtonZombieMap != null)        {
            ButtonZombieMap.clicked -= ZombieMapButtonClicked;
        }
        if (ButtonSkirmishAMap != null)        {
            ButtonSkirmishAMap.clicked -= SkirmishAMapButtonClicked;
        }
        if (ButtonSkirmishBMap != null)        {
            ButtonSkirmishBMap.clicked -= SkirmishBMapButtonClicked;
        }
        if (ButtonSkirmishCMap != null)        {
            ButtonSkirmishCMap.clicked -= SkirmishCMapButtonClicked;
        }
        if (ButtonSkirmishDMap != null)        {
            ButtonSkirmishDMap.clicked -= SkirmishDMapButtonClicked;
        }
    }

    private void ZombieMapButtonClicked()
    {
        Debug.Log("Zombie Map Button Clicked - Load Zombie Map Scene");
        NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as NetworkRoomManager;
        networkRoomManager.GameplayScene = "ZombyGameScene";
    }

    private void SkirmishAMapButtonClicked()
    {
        Debug.Log("Skirmish A Map Button Clicked - Load Skirmish A Map Scene");
        NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as NetworkRoomManager;
        networkRoomManager.GameplayScene = "GameSkirmishAScene";
    }
    private void SkirmishBMapButtonClicked()
    {
        Debug.Log("Skirmish B Map Button Clicked - Load Skirmish B Map Scene");
        NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as NetworkRoomManager;
        networkRoomManager.GameplayScene = "GameSkirmishBScene";
    }
    private void SkirmishCMapButtonClicked()
    {
        Debug.Log("Skirmish C Map Button Clicked - Load Skirmish C Map Scene");
        NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as NetworkRoomManager;
        networkRoomManager.GameplayScene = "GameSkirmishCScene";
    }
    private void SkirmishDMapButtonClicked()
    {
        Debug.Log("Skirmish D Map Button Clicked - Load Skirmish D Map Scene");
        NetworkRoomManager networkRoomManager = NetworkRoomManager.singleton as NetworkRoomManager;
        networkRoomManager.GameplayScene = "GameSkirmishDScene";
    }
}
