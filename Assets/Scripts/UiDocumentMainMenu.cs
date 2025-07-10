using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentMainMenu : MonoBehaviour
{
    private UIDocument uiDocument;

    private Button buttonStartGame;
    private Button buttonSettings;
    private Button buttonCredits;
    private Button buttonQuit;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on this GameObject.");
            return;
        }
        // Get the root VisualElement
        VisualElement root = uiDocument.rootVisualElement;
        // Find buttons by name
        buttonStartGame = root.Q<Button>("ButtonStartGame");
        buttonSettings = root.Q<Button>("ButtonSettings");
        buttonCredits = root.Q<Button>("ButtonCredits");
        buttonQuit = root.Q<Button>("ButtonQuit");
    }

    void OnEnable()
    {
        if (buttonStartGame != null)
            buttonStartGame.clicked += OnButtonStartGame;
        if (buttonSettings != null)
            buttonSettings.clicked += OnButtonSettings;
        if (buttonCredits != null)
            buttonCredits.clicked += OnButtonCredits;
        if (buttonQuit != null)
            buttonQuit.clicked += OnButtonQuit;
    }

    void OnDisable()
    {
        if (buttonStartGame != null)
            buttonStartGame.clicked -= OnButtonStartGame;
        if (buttonSettings != null)
            buttonSettings.clicked -= OnButtonSettings;
        if (buttonCredits != null)
            buttonCredits.clicked -= OnButtonCredits;
        if (buttonQuit != null)
            buttonQuit.clicked -= OnButtonQuit;
    }

    void OnButtonStartGame()
    {
        Debug.Log("Start Game button clicked");
        // Add logic to start the game
        // For example, load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("ZombyOfflineScene");
    }

    void OnButtonSettings()
    {
        Debug.Log("Settings button clicked");
        // Add logic to open settings
    }

    void OnButtonCredits()
    {
        Debug.Log("Credits button clicked");
        // Add logic to show credits
    }

    void OnButtonQuit()
    {
        Debug.Log("Quit button clicked");
        // Add logic to quit the application
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
