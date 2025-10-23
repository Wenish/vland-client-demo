using System;
using Mirror;
using MyGame.Events.Ui;
using UnityEngine;
using UnityEngine.UIElements;

public class FormJoinGameController : MonoBehaviour
{
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
        rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void CloseOpenFormJoinGame()
    {
        rootVisualElement.style.display = DisplayStyle.None;
        EventManager.Instance.Publish(new OpenMultiplayerMenu());
    }
    
    private void JoinGame()
    {
        string serverAddress = inputServerAddress.value;
        int port = int.Parse(inputPort.value);
        Uri uri = new Uri($"tcp4://{serverAddress}:{port}");
        NetworkManager.singleton.StartClient(uri);
    }
}
