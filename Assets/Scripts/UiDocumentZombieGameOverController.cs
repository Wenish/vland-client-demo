using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public class UiDocumentZombieGameOverController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement gameOverRoot;
    private Label titleLabel;
    private Label countdownLabel;
    private Button returnToLobbyButton;

    private ZombieGameManager zombieGameManager;
    private bool isSubscribed;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UiDocumentZombieGameOverController requires a UIDocument component on the same GameObject.", this);
            enabled = false;
            return;
        }

        CacheUiElements();
        ApplyInitialVisibility();
    }

    private void OnDestroy()
    {
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.clicked -= HandleReturnToLobbyClicked;
        }

        UnsubscribeFromManager();
    }

    private void Update()
    {
        TryBindZombieGameManager();
        UpdateReturnButtonVisibility();
    }

    private void CacheUiElements()
    {
        var root = uiDocument.rootVisualElement;
        gameOverRoot = root.Q<VisualElement>("zombieGameOverRoot");
        titleLabel = root.Q<Label>("zombieGameOverTitle");
        countdownLabel = root.Q<Label>("zombieReturnCountdownLabel");
        returnToLobbyButton = root.Q<Button>("zombieReturnToLobbyButton");

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.clicked += HandleReturnToLobbyClicked;
        }

        if (titleLabel != null)
        {
            titleLabel.text = "Game Over";
        }
    }

    private void ApplyInitialVisibility()
    {
        SetPanelVisible(false);
        SetCountdownVisible(false, 0f);

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.style.display = DisplayStyle.None;
        }
    }

    private void TryBindZombieGameManager()
    {
        if (zombieGameManager != null)
        {
            return;
        }

        zombieGameManager = ZombieGameManager.Singleton;
        if (zombieGameManager == null)
        {
            return;
        }

        if (!isSubscribed)
        {
            zombieGameManager.OnGameOverStateChanged += HandleGameOverStateChanged;
            zombieGameManager.OnAutoReturnToLobbyEnabledChanged += HandleAutoReturnEnabledChanged;
            zombieGameManager.OnReturnToLobbyCountdownChanged += HandleReturnCountdownChanged;
            isSubscribed = true;
        }

        HandleGameOverStateChanged(zombieGameManager.IsGameOver);
        HandleAutoReturnEnabledChanged(zombieGameManager.IsAutoReturnToLobbyEnabled);
        HandleReturnCountdownChanged(zombieGameManager.ReturnToLobbyCountdownSeconds);
    }

    private void UnsubscribeFromManager()
    {
        if (zombieGameManager == null || !isSubscribed)
        {
            return;
        }

        zombieGameManager.OnGameOverStateChanged -= HandleGameOverStateChanged;
        zombieGameManager.OnAutoReturnToLobbyEnabledChanged -= HandleAutoReturnEnabledChanged;
        zombieGameManager.OnReturnToLobbyCountdownChanged -= HandleReturnCountdownChanged;
        isSubscribed = false;
        zombieGameManager = null;
    }

    private void HandleGameOverStateChanged(bool isGameOver)
    {
        SetPanelVisible(isGameOver);

        if (!isGameOver)
        {
            SetCountdownVisible(false, 0f);
        }
    }

    private void HandleAutoReturnEnabledChanged(bool isEnabled)
    {
        float seconds = zombieGameManager != null ? zombieGameManager.ReturnToLobbyCountdownSeconds : 0f;
        SetCountdownVisible(isEnabled, seconds);
    }

    private void HandleReturnCountdownChanged(float seconds)
    {
        bool showCountdown = zombieGameManager != null
            && zombieGameManager.IsGameOver
            && zombieGameManager.IsAutoReturnToLobbyEnabled;

        SetCountdownVisible(showCountdown, seconds);
    }

    private void HandleReturnToLobbyClicked()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (zombieGameManager == null)
        {
            return;
        }

        zombieGameManager.ServerReturnToLobby();
    }

    private void UpdateReturnButtonVisibility()
    {
        if (returnToLobbyButton == null)
        {
            return;
        }

        bool canUseButton = NetworkServer.active
            && zombieGameManager != null
            && zombieGameManager.IsGameOver;

        returnToLobbyButton.style.display = canUseButton ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetPanelVisible(bool visible)
    {
        if (gameOverRoot == null)
        {
            return;
        }

        gameOverRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetCountdownVisible(bool visible, float seconds)
    {
        if (countdownLabel == null)
        {
            return;
        }

        countdownLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        countdownLabel.text = $"Returning to lobby in {Mathf.CeilToInt(Mathf.Max(0f, seconds))}s";
    }
}
