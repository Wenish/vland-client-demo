using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HostAdminOverlayPresenter : MonoBehaviour
{
    [SerializeField] private bool hideWhenNotHost = true;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private Label _managerLabel;
    private Label _teamSwitchingLabel;
    private Button _lockButton;
    private Button _unlockButton;
    private MatchGameManagerBase _manager;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _managerLabel = _root.Q<Label>("LabelManager");
        _teamSwitchingLabel = _root.Q<Label>("LabelTeamSwitching");
        _lockButton = _root.Q<Button>("ButtonLockTeamSwitching");
        _unlockButton = _root.Q<Button>("ButtonUnlockTeamSwitching");
    }

    private void OnEnable()
    {
        if (_lockButton != null)
        {
            _lockButton.clicked += HandleLockClicked;
        }

        if (_unlockButton != null)
        {
            _unlockButton.clicked += HandleUnlockClicked;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (_lockButton != null)
        {
            _lockButton.clicked -= HandleLockClicked;
        }

        if (_unlockButton != null)
        {
            _unlockButton.clicked -= HandleUnlockClicked;
        }
    }

    private void Update()
    {
        Refresh();
    }

    private void HandleLockClicked()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        ResolveManager();
        if (_manager == null)
        {
            return;
        }

        _manager.ServerLockTeamSwitching();
        Refresh();
    }

    private void HandleUnlockClicked()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        ResolveManager();
        if (_manager == null)
        {
            return;
        }

        _manager.ServerUnlockTeamSwitching();
        Refresh();
    }

    private void ResolveManager()
    {
        if (_manager != null)
        {
            return;
        }

        _manager = MatchGameManagerBase.ActiveInstance;
        if (_manager == null)
        {
            _manager = FindAnyObjectByType<MatchGameManagerBase>();
        }
    }

    private void Refresh()
    {
        if (_root == null)
        {
            return;
        }

        bool isHost = NetworkServer.active && NetworkClient.active;
        bool shouldShow = !hideWhenNotHost || isHost;
        _root.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;

        if (!shouldShow)
        {
            return;
        }

        ResolveManager();

        if (_manager == null)
        {
            _managerLabel.text = "Manager: Not Found";
            _teamSwitchingLabel.text = "Team Switching: -";
            _lockButton?.SetEnabled(false);
            _unlockButton?.SetEnabled(false);
            return;
        }

        _managerLabel.text = $"Manager: {_manager.GetType().Name}";

        bool isLocked = _manager.TeamSelectionLocked;
        _teamSwitchingLabel.text = isLocked ? "Team Switching: Locked" : "Team Switching: Unlocked";

        _lockButton?.SetEnabled(!isLocked);
        _unlockButton?.SetEnabled(isLocked);
    }
}
