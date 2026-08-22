using MessagePipe;
using Mirror;
using MyGame.Events.Ui;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.MultiplayerMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(100)]
    public sealed class MultiplayerMenuController : MonoBehaviour
    {
        public string MainMenuSceneName = SceneNames.MainMenu;

        private UIDocument uiDocument;
        private VisualElement rootVisualElement;
        private Button buttonHostGame;
        private Button buttonJoinGame;
        private Button buttonServerBrowser;
        private Button buttonServerOnly;
        private Button buttonBackToMainMenu;
        private ISubscriber<OpenMultiplayerMenuEvent> openMenu;
        private IPublisher<OpenFormJoinGameEvent> openJoinForm;
        private R3.DisposableBag subscriptions;

        [Inject]
        internal void Construct(
            ISubscriber<OpenMultiplayerMenuEvent> injectedOpenMenu,
            IPublisher<OpenFormJoinGameEvent> injectedOpenJoinForm)
        {
            openMenu = injectedOpenMenu;
            openJoinForm = injectedOpenJoinForm;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("MultiplayerMenuController: UIDocument root is missing.");
                return;
            }

            rootVisualElement = uiDocument.rootVisualElement;
            UiGameplayInputGuard.Apply(rootVisualElement);
            buttonHostGame = rootVisualElement.Q<Button>("buttonHostGame");
            buttonJoinGame = rootVisualElement.Q<Button>("buttonJoinGame");
            buttonServerBrowser = rootVisualElement.Q<Button>("buttonServerBrowser");
            buttonServerOnly = rootVisualElement.Q<Button>("buttonServerOnly");
            buttonBackToMainMenu = rootVisualElement.Q<Button>("buttonBackToMainMenu");

            if (buttonHostGame != null)
                buttonHostGame.clicked += HostGame;
            if (buttonJoinGame != null)
                buttonJoinGame.clicked += JoinGame;
            if (buttonServerBrowser != null)
                buttonServerBrowser.clicked += OpenServerBrowser;
            if (buttonServerOnly != null)
                buttonServerOnly.clicked += StartServerOnly;
            if (buttonBackToMainMenu != null)
                buttonBackToMainMenu.clicked += BackToMainMenu;
        }

        private void Start()
        {
            if (openMenu == null || openJoinForm == null)
            {
                UnityEngine.Debug.LogError(
                    "MultiplayerMenuController: MessagePipe was not injected. Add GameLifetimeScope and MultiplayerMenuLifetimeScope.");
                return;
            }

            subscriptions.Add(openMenu.Subscribe(_ => Show()));
        }

        private void OnDestroy()
        {
            subscriptions.Dispose();

            if (buttonHostGame != null)
                buttonHostGame.clicked -= HostGame;
            if (buttonJoinGame != null)
                buttonJoinGame.clicked -= JoinGame;
            if (buttonServerBrowser != null)
                buttonServerBrowser.clicked -= OpenServerBrowser;
            if (buttonServerOnly != null)
                buttonServerOnly.clicked -= StartServerOnly;
            if (buttonBackToMainMenu != null)
                buttonBackToMainMenu.clicked -= BackToMainMenu;
        }

        private void Show()
        {
            if (rootVisualElement != null)
                rootVisualElement.style.display = DisplayStyle.Flex;
        }

        private void HostGame()
        {
            NetworkManager.singleton.StartHost();
        }

        private void JoinGame()
        {
            if (rootVisualElement != null)
                rootVisualElement.style.display = DisplayStyle.None;

            openJoinForm?.Publish(new OpenFormJoinGameEvent());
        }

        private void OpenServerBrowser()
        {
            UnityEngine.Debug.Log("Server Browser button clicked");
        }

        private void StartServerOnly()
        {
            NetworkManager.singleton.StartServer();
        }

        private void BackToMainMenu()
        {
            if (NetworkManager.singleton != null)
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                    NetworkManager.singleton.StopHost();
                else if (NetworkServer.active)
                    NetworkManager.singleton.StopServer();
                else if (NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();

                Destroy(NetworkManager.singleton.gameObject);
            }

            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}
