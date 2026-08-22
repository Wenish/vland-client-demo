using System;
using MessagePipe;
using Mirror;
using MyGame.Events.Ui;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.FormJoinGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormJoinGameController : MonoBehaviour
    {
        private const string PrefKeyServerAddress = "FormJoinGame_ServerAddress";
        private const string PrefKeyPort = "FormJoinGame_Port";

        private UIDocument uiDocument;
        private VisualElement rootVisualElement;
        private Button buttonCancel;
        private Button buttonJoinGame;
        private TextField inputServerAddress;
        private TextField inputPort;
        private ISubscriber<OpenFormJoinGameEvent> openForm;
        private IPublisher<OpenMultiplayerMenuEvent> openMenu;
        private R3.DisposableBag subscriptions;

        [Inject]
        internal void Construct(
            ISubscriber<OpenFormJoinGameEvent> injectedOpenForm,
            IPublisher<OpenMultiplayerMenuEvent> injectedOpenMenu)
        {
            openForm = injectedOpenForm;
            openMenu = injectedOpenMenu;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("FormJoinGameController: UIDocument root is missing.");
                return;
            }

            rootVisualElement = uiDocument.rootVisualElement;
            rootVisualElement.style.display = DisplayStyle.None;
            UiGameplayInputGuard.Apply(rootVisualElement, blockMovementKeys: false);

            buttonCancel = rootVisualElement.Q<Button>("buttonCancel");
            buttonJoinGame = rootVisualElement.Q<Button>("buttonJoinGame");
            inputServerAddress = rootVisualElement.Q<TextField>("inputServerAddress");
            inputPort = rootVisualElement.Q<TextField>("inputPort");

            if (buttonCancel != null)
                buttonCancel.clicked += CloseForm;
            if (buttonJoinGame != null)
                buttonJoinGame.clicked += JoinGame;
        }

        private void Start()
        {
            if (openForm == null || openMenu == null)
            {
                UnityEngine.Debug.LogError(
                    "FormJoinGameController: MessagePipe was not injected. Add GameLifetimeScope and FormJoinGameLifetimeScope.");
                return;
            }

            subscriptions.Add(openForm.Subscribe(_ => Show()));
        }

        private void OnDestroy()
        {
            subscriptions.Dispose();

            if (buttonCancel != null)
                buttonCancel.clicked -= CloseForm;
            if (buttonJoinGame != null)
                buttonJoinGame.clicked -= JoinGame;
        }

        private void Show()
        {
            LoadCachedInputs();
            if (rootVisualElement != null)
                rootVisualElement.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            if (rootVisualElement != null)
                rootVisualElement.style.display = DisplayStyle.None;

            openMenu?.Publish(new OpenMultiplayerMenuEvent());
        }

        private void JoinGame()
        {
            var serverAddress = (inputServerAddress?.value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(serverAddress))
            {
                serverAddress = PlayerPrefs.GetString(
                    PrefKeyServerAddress,
                    NetworkManager.singleton != null ? NetworkManager.singleton.networkAddress : "localhost");
            }

            if (!int.TryParse(inputPort?.value, out var port) || port <= 0)
                port = PlayerPrefs.GetInt(PrefKeyPort, 7777);

            SaveCachedInputs(serverAddress, port);

            var uri = new Uri($"tcp4://{serverAddress}:{port}");
            NetworkManager.singleton.StartClient(uri);
        }

        private void LoadCachedInputs()
        {
            var defaultAddress = NetworkManager.singleton != null
                ? NetworkManager.singleton.networkAddress
                : "localhost";
            var cachedAddress = PlayerPrefs.GetString(PrefKeyServerAddress, defaultAddress);
            var cachedPort = PlayerPrefs.GetInt(PrefKeyPort, 7777);

            if (inputServerAddress != null)
                inputServerAddress.value = cachedAddress;

            if (inputPort != null)
                inputPort.value = cachedPort.ToString();
        }

        private void SaveCachedInputs(string address, int port)
        {
            PlayerPrefs.SetString(PrefKeyServerAddress, address);
            PlayerPrefs.SetInt(PrefKeyPort, port);
            PlayerPrefs.Save();
        }
    }
}
