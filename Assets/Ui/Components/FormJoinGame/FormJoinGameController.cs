using System;
using Mirror;
using MyGame.Events.Ui;
using UnityEngine;
using UnityEngine.UIElements;

public class FormJoinGameController : MonoBehaviour
{
    private const string PrefKeyServerAddress = "FormJoinGame_ServerAddress";
    private const string PrefKeyPort = "FormJoinGame_Port";

    private UIDocument uiDocument;
    private VisualElement rootVisualElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Button buttonCancel;
    Button buttonJoinGame;

    TextField inputServerAddress;
    TextField inputPort;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        rootVisualElement = uiDocument.rootVisualElement;
        rootVisualElement.style.display = DisplayStyle.None;

        buttonCancel = rootVisualElement.Q<Button>("buttonCancel");
        buttonJoinGame = rootVisualElement.Q<Button>("buttonJoinGame");
        inputServerAddress = rootVisualElement.Q<TextField>("inputServerAddress");
        inputPort = rootVisualElement.Q<TextField>("inputPort");

        EventManager.Instance.Subscribe<OpenFormJoinGame>(HandleOpenFormJoinGame);
        buttonCancel.clicked += CloseOpenFormJoinGame;
        buttonJoinGame.clicked += JoinGame;
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<OpenFormJoinGame>(HandleOpenFormJoinGame);
    }

    private void HandleOpenFormJoinGame(OpenFormJoinGame game)
    {
        // Prefill inputs with last-used values (or sensible defaults) each time the form opens
        LoadCachedInputs();
        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void CloseOpenFormJoinGame()
    {
        rootVisualElement.style.display = DisplayStyle.None;
        EventManager.Instance.Publish(new OpenMultiplayerMenu());
    }
    
    private void JoinGame()
    {
        string serverAddress = (inputServerAddress?.value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(serverAddress))
        {
            // Fallback to previous cached value or default
            serverAddress = PlayerPrefs.GetString(PrefKeyServerAddress,
                NetworkManager.singleton != null ? NetworkManager.singleton.networkAddress : "localhost");
        }

        int port;
        if (!int.TryParse(inputPort?.value, out port) || port <= 0)
        {
            // Fallback to previous cached value or Mirror default port
            port = PlayerPrefs.GetInt(PrefKeyPort, 7777);
        }

        // Cache the values for next time
        SaveCachedInputs(serverAddress, port);

        Uri uri = new Uri($"tcp4://{serverAddress}:{port}");
        NetworkManager.singleton.StartClient(uri);
    }

    private void LoadCachedInputs()
    {
        // Defaults: Mirror's NetworkManager networkAddress if available, else localhost; port defaults to 7777
        string defaultAddress = NetworkManager.singleton != null ? NetworkManager.singleton.networkAddress : "localhost";
        int defaultPort = 7777;

        string cachedAddress = PlayerPrefs.GetString(PrefKeyServerAddress, defaultAddress);
        int cachedPort = PlayerPrefs.GetInt(PrefKeyPort, defaultPort);

        if (inputServerAddress != null)
        {
            inputServerAddress.value = cachedAddress;
        }

        if (inputPort != null)
        {
            inputPort.value = cachedPort.ToString();
        }
    }

    private void SaveCachedInputs(string address, int port)
    {
        PlayerPrefs.SetString(PrefKeyServerAddress, address);
        PlayerPrefs.SetInt(PrefKeyPort, port);
        PlayerPrefs.Save();
    }
}
