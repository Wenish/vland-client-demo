using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField]
    private UIDocument uiDocument;

    [SerializeField]
    private VisualElement inGameMenuRoot;

    [Scene]
    public string LobbySceneName = "LobbyScene";

    private Button buttonExitGame;
    private Button buttonReturnToGame;
    private Button buttonLeaveServer;
    private Button buttonStopServer;
    private Button buttonEndMatch;
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var rootElement = uiDocument.rootVisualElement;
        inGameMenuRoot = rootElement.Q<VisualElement>("game-menu");

        buttonExitGame = inGameMenuRoot.Q<Button>("buttonExitGame");
        buttonReturnToGame = inGameMenuRoot.Q<Button>("buttonReturnToGame");

        buttonEndMatch = inGameMenuRoot.Q<Button>("buttonEndMatch");
        buttonStopServer = inGameMenuRoot.Q<Button>("buttonStopServer");

        buttonLeaveServer = inGameMenuRoot.Q<Button>("buttonLeaveServer");

        var isServer = NetworkServer.active;

        ShowButton(buttonStopServer, isServer);

        var isInLobby = SceneManager.GetActiveScene().name == LobbySceneName;

        ShowButton(buttonEndMatch, isServer && !isInLobby);

        var isOnlyClient = NetworkClient.isConnected && !NetworkServer.active;
        ShowButton(buttonLeaveServer, isOnlyClient);

        buttonExitGame.clicked += ExitGame;
        buttonReturnToGame.clicked += CloseMenu;
        buttonLeaveServer.clicked += LeaveServer;
        buttonStopServer.clicked += StopServer;
        buttonEndMatch.clicked += EndMatch;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsMenuOpen())
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    public void OpenMenu()
    {
        inGameMenuRoot.style.display = DisplayStyle.Flex;
    }

    public void CloseMenu()
    {
        inGameMenuRoot.style.display = DisplayStyle.None;
    }

    public void ShowButton(Button button, bool show)
    {
        if (button != null)
        {
            button.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public bool IsMenuOpen()
    {
        return inGameMenuRoot.style.display == DisplayStyle.Flex;
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LeaveServer()
    {
        if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        CloseMenu();
    }

    public void StopServer()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        CloseMenu();
    }

    public void EndMatch()
    {
        // Ensure menu is closed and input unblocked before switching scenes
        CloseMenu();
        NetworkManager.singleton.ServerChangeScene(LobbySceneName);
    }
}
