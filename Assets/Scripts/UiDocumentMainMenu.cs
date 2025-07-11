using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UiDocumentMainMenu : MonoBehaviour
{
    private UIDocument uiDocument;

    /* Section Page Main Menu */
    private VisualElement pageMainMenu;
    private Button buttonStartGame;
    private Button buttonSettings;
    private Button buttonCredits;
    private Button buttonQuit;

    /* Section Page Settings */
    private VisualElement pageSettings;

    private List<VisualElement> pages = new List<VisualElement>();

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

        // Find pagees
        pageMainMenu = root.Q<VisualElement>("PageMainMenu");
        pageSettings = root.Q<VisualElement>("PageSettings");
        
        // Add pages to the list
        pages.Add(pageMainMenu);
        pages.Add(pageSettings);

        // Find page main menu buttons by name
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

    void HideAllPages()
    {
        foreach (var page in pages)
        {
            page.style.display = DisplayStyle.None;
        }
    }

    public enum MenuPage
    {
        MainMenu,
        Settings
    }

    void ShowPage(MenuPage pageType)
    {
        HideAllPages();
        VisualElement pageToShow = null;
        switch (pageType)
        {
            case MenuPage.MainMenu:
                pageToShow = pageMainMenu;
                break;
            case MenuPage.Settings:
                pageToShow = pageSettings;
                break;
        }
        Debug.Log($"Showing page: {pageType}");
        if (pageToShow != null)
        {
            pageToShow.style.display = DisplayStyle.Flex;
        }
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
        ShowPage(MenuPage.Settings);
        // You can also play a sound effect here if needed
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
