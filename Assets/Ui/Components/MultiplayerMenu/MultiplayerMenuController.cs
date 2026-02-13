using Mirror;
using MyGame.Events.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DefaultExecutionOrder(100)]
public class MultiplayerMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement rootVisualElement;

    public string MainMenuSceneName = "MainMenu";

    private Button buttonHostGame;
    private Button buttonJoinGame;
    private Button buttonServerBrowser;
    private Button buttonServerOnly;
    private Button buttonBackToMainMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        rootVisualElement = uiDocument.rootVisualElement;
        buttonHostGame = rootVisualElement.Q<Button>("buttonHostGame");
        buttonJoinGame = rootVisualElement.Q<Button>("buttonJoinGame");
        buttonServerBrowser = rootVisualElement.Q<Button>("buttonServerBrowser");
        buttonServerOnly = rootVisualElement.Q<Button>("buttonServerOnly");
        buttonBackToMainMenu = rootVisualElement.Q<Button>("buttonBackToMainMenu");

        buttonHostGame.clicked += HostGame;
        buttonJoinGame.clicked += JoinGame;
        buttonServerBrowser.clicked += OpenServerBrowser;
        buttonServerOnly.clicked += StartServerOnly;
        buttonBackToMainMenu.clicked += BackToMainMenu;

        buttonHostGame.RegisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonJoinGame.RegisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonServerBrowser.RegisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonServerOnly.RegisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonBackToMainMenu.RegisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());

        EventManager.Instance.Subscribe<OpenMultiplayerMenu>(HandleOpenMultiplayerMenu);
    }

    void OnDestroy()
    {
        buttonHostGame.clicked -= HostGame;
        buttonJoinGame.clicked -= JoinGame;
        buttonServerBrowser.clicked -= OpenServerBrowser;
        buttonServerOnly.clicked -= StartServerOnly;
        buttonBackToMainMenu.clicked -= BackToMainMenu;

        buttonHostGame.UnregisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonJoinGame.UnregisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonServerBrowser.UnregisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonServerOnly.UnregisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());
        buttonBackToMainMenu.UnregisterCallback<MouseEnterEvent>(evt => PlayButtonHoverSoundEffect());

        EventManager.Instance.Unsubscribe<OpenMultiplayerMenu>(HandleOpenMultiplayerMenu);
    }

    private void HandleOpenMultiplayerMenu(OpenMultiplayerMenu game)
    {
        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void HostGame()
    {
        PlayButtonClickSoundEffect();
        NetworkManager.singleton.StartHost();
    }

    public void JoinGame()
    {
        PlayButtonClickSoundEffect();
        rootVisualElement.style.display = DisplayStyle.None;
        EventManager.Instance.Publish(new OpenFormJoinGame());
    }

    public void OpenServerBrowser()
    {
        PlayButtonClickSoundEffect();
        Debug.Log("Server Browser button clicked");
        // Implement server browser logic here
    }

    public void StartServerOnly()
    {
        PlayButtonClickSoundEffect();
        NetworkManager.singleton.StartServer();
    }

    public void BackToMainMenu()
    {
        PlayButtonClickSoundEffect();
        // Cleanly stop any active networking and destroy the NetworkManager GameObject
        if (NetworkManager.singleton != null)
        {
            // Stop in the safest order depending on current state
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                // Running as Host
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                // Server only
                NetworkManager.singleton.StopServer();
            }
            else if (NetworkClient.isConnected)
            {
                // Client only
                NetworkManager.singleton.StopClient();
            }

            // Destroy the persistent NetworkManager instance so a fresh one can be created in the menu or next gameplay scene
            Destroy(NetworkManager.singleton.gameObject);
        }

        // Load main menu scene afterward
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void PlayButtonClickSoundEffect()
    {
        SoundManager.Instance.PlaySound("UiButtonClick");
    }

    public void PlayButtonHoverSoundEffect()
    {
        SoundManager.Instance.PlaySound("UiButtonHover");
    }
}
