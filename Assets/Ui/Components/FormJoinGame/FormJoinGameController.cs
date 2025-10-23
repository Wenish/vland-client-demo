using MyGame.Events.Ui;
using UnityEngine;
using UnityEngine.UIElements;

public class FormJoinGameController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement rootVisualElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        rootVisualElement = uiDocument.rootVisualElement;
        rootVisualElement.style.display = DisplayStyle.None;

        Button buttonCancel = rootVisualElement.Q<Button>("buttonCancel");
        // Subscribe to the OpenFormJoinGame event
        EventManager.Instance.Subscribe<OpenFormJoinGame>(HandleOpenFormJoinGame);
        buttonCancel.clicked += CloseOpenFormJoinGame;
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
}
