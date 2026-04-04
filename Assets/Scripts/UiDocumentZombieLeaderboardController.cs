using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UiDocumentZombieLeaderboardController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _leaderboardPanel;
    private VisualElement _leaderboardRows;
    private Label _leaderboardEmptyLabel;
    private ZombieGameManager _zombieGameManager;
    private bool _isLeaderboardVisible;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            return;
        }

        var root = _uiDocument.rootVisualElement;
        _leaderboardPanel = root.Q<VisualElement>(name: "leaderboardPanel");
        _leaderboardRows = root.Q<VisualElement>(name: "leaderboardRows");
        _leaderboardEmptyLabel = root.Q<Label>(name: "leaderboardEmptyLabel");

        SetLeaderboardVisible(false);
    }

    private void Update()
    {
        TryBindZombieLeaderboard();
        HandleLeaderboardInput();
    }

    private void OnDestroy()
    {
        if (_zombieGameManager != null)
        {
            _zombieGameManager.OnLeaderboardChanged -= HandleLeaderboardChanged;
            _zombieGameManager.OnGameOverStateChanged -= HandleGameOverStateChanged;
            _zombieGameManager = null;
        }
    }

    private void TryBindZombieLeaderboard()
    {
        if (_zombieGameManager != null)
        {
            return;
        }

        _zombieGameManager = ZombieGameManager.Singleton;
        if (_zombieGameManager == null)
        {
            return;
        }

        _zombieGameManager.OnLeaderboardChanged += HandleLeaderboardChanged;
        _zombieGameManager.OnGameOverStateChanged += HandleGameOverStateChanged;

        if (_zombieGameManager.IsGameOver)
        {
            _isLeaderboardVisible = true;
            SetLeaderboardVisible(true);
        }

        RenderLeaderboard();
    }

    private void HandleLeaderboardInput()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame)
        {
            return;
        }

        _isLeaderboardVisible = !_isLeaderboardVisible;
        SetLeaderboardVisible(_isLeaderboardVisible);
        if (_isLeaderboardVisible)
        {
            RenderLeaderboard();
        }
    }

    private void SetLeaderboardVisible(bool visible)
    {
        if (_leaderboardPanel == null)
        {
            return;
        }

        _leaderboardPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleLeaderboardChanged()
    {
        if (!_isLeaderboardVisible)
        {
            return;
        }

        RenderLeaderboard();
    }

    private void HandleGameOverStateChanged(bool isGameOver)
    {
        if (!isGameOver)
        {
            _isLeaderboardVisible = false;
            SetLeaderboardVisible(false);
            return;
        }

        _isLeaderboardVisible = true;
        SetLeaderboardVisible(true);
        RenderLeaderboard();
    }

    private void RenderLeaderboard()
    {
        if (_leaderboardRows == null || _leaderboardEmptyLabel == null)
        {
            return;
        }

        _leaderboardRows.Clear();
        if (_zombieGameManager == null || _zombieGameManager.LeaderboardEntries == null || _zombieGameManager.LeaderboardEntries.Count == 0)
        {
            _leaderboardEmptyLabel.style.display = DisplayStyle.Flex;
            return;
        }

        _leaderboardEmptyLabel.style.display = DisplayStyle.None;
        for (int i = 0; i < _zombieGameManager.LeaderboardEntries.Count; i++)
        {
            var entry = _zombieGameManager.LeaderboardEntries[i];
            _leaderboardRows.Add(CreateLeaderboardRow(entry, i + 1));
        }
    }

    private VisualElement CreateLeaderboardRow(ZombieGameManager.ZombieLeaderboardEntry entry, int rank)
    {
        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                width = Length.Percent(100),
                paddingLeft = 6,
                paddingRight = 6,
                paddingTop = 4,
                paddingBottom = 4,
                marginBottom = 2,
                backgroundColor = rank % 2 == 0 ? new Color(0.11f, 0.11f, 0.11f, 0.6f) : new Color(0.06f, 0.06f, 0.06f, 0.6f)
            }
        };

        string playerName = entry.PlayerName;
        if (!entry.IsConnected)
        {
            playerName = $"{playerName} (left)";
        }

        row.Add(CreateFixedLeaderboardCell(rank.ToString(), 34));
        row.Add(CreateFlexibleLeaderboardCell(playerName));
        row.Add(CreateFixedLeaderboardCell(entry.Points.ToString(), 60));
        row.Add(CreateFixedLeaderboardCell(entry.Kills.ToString(), 46));
        row.Add(CreateFixedLeaderboardCell(entry.Deaths.ToString(), 56));
        row.Add(CreateFixedLeaderboardCell(entry.GoldGathered.ToString(), 60));

        return row;
    }

    private static Label CreateFixedLeaderboardCell(string text, float width)
    {
        var label = new Label(text)
        {
            style =
            {
                width = width,
                minWidth = width,
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleLeft,
                fontSize = 12
            }
        };

        return label;
    }

    private static Label CreateFlexibleLeaderboardCell(string text)
    {
        var label = new Label(text)
        {
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                minWidth = 90,
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleLeft,
                fontSize = 12,
                whiteSpace = WhiteSpace.NoWrap,
                overflow = Overflow.Hidden,
                textOverflow = TextOverflow.Ellipsis
            }
        };

        return label;
    }
}
