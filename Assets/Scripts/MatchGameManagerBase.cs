using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class MatchGameManagerBase : NetworkBehaviour
{
    public static MatchGameManagerBase ActiveInstance { get; private set; }

    public enum MatchLifecycleState : byte
    {
        PreMatch = 0,
        MatchStarting = 1,
        InMatch = 2,
        PostMatch = 3,
        MatchEnded = 4
    }

    [SyncVar(hook = nameof(HookOnLifecycleStateChanged))]
    public MatchLifecycleState LifecycleState = MatchLifecycleState.PreMatch;

    [SyncVar(hook = nameof(HookOnTeamSelectionLockedChanged))]
    public bool TeamSelectionLocked = false;

    [SyncVar(hook = nameof(HookOnLifecycleWinnerTeamChanged))]
    public int LifecycleWinnerTeamId = -1;

    [SyncVar(hook = nameof(HookOnReturnToLobbyCountdownChanged))]
    public float ReturnToLobbyCountdownRemaining = 0f;

    [Header("Match End")]
    [SerializeField] private bool autoReturnToLobbyOnMatchEnd = true;
    [SerializeField, Min(0f)] private float returnToLobbyDelaySeconds = 15f;

    [Header("Team Switching")]
    [SerializeField] private bool requireBalancedManualTeamSwitching = false;

    public event Action<MatchLifecycleState> OnLifecycleStateChanged = delegate { };
    public event Action<bool> OnTeamSelectionLockChanged = delegate { };
    public event Action<(int connectionId, int teamId)> OnPlayerTeamAssigned = delegate { };
    public event Action<int> OnLifecycleMatchEnded = delegate { };
    public event Action<float> OnReturnToLobbyCountdownChanged = delegate { };

    protected readonly Dictionary<int, int> ConnectionTeamAssignments = new Dictionary<int, int>();
    private Coroutine _returnToLobbyCoroutine;

    public abstract int TeamCount { get; }

    protected virtual void Awake()
    {
        if (ActiveInstance != null && ActiveInstance != this)
        {
            Debug.LogWarning($"[{nameof(MatchGameManagerBase)}] Multiple active managers detected. Replacing previous instance.", this);
        }

        ActiveInstance = this;
    }

    protected virtual void OnDestroy()
    {
        if (ActiveInstance == this)
        {
            ActiveInstance = null;
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_returnToLobbyCoroutine != null)
        {
            StopCoroutine(_returnToLobbyCoroutine);
            _returnToLobbyCoroutine = null;
        }

        ServerSetReturnToLobbyCountdownRemaining(0f);
    }

    [Server]
    protected void ServerSetLifecycleState(MatchLifecycleState state)
    {
        if (LifecycleState == state)
        {
            return;
        }

        LifecycleState = state;

        if (isServerOnly)
        {
            OnLifecycleStateChanged(state);
        }
    }

    [Server]
    protected void ServerSetTeamSelectionLocked(bool isLocked)
    {
        if (TeamSelectionLocked == isLocked)
        {
            return;
        }

        TeamSelectionLocked = isLocked;

        if (isServerOnly)
        {
            OnTeamSelectionLockChanged(isLocked);
        }
    }

    [Server]
    public void ServerLockTeamSwitching()
    {
        ServerSetTeamSelectionLocked(true);
    }

    [Server]
    public void ServerUnlockTeamSwitching()
    {
        ServerSetTeamSelectionLocked(false);
    }

    [Server]
    public void ServerSetTeamSwitchingLocked(bool isLocked)
    {
        ServerSetTeamSelectionLocked(isLocked);
    }

    [ContextMenu("Server Lock Team Switching")]
    private void ContextServerLockTeamSwitching()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"[{nameof(MatchGameManagerBase)}] Cannot lock team switching because server is not active.", this);
            return;
        }

        ServerLockTeamSwitching();
    }

    [ContextMenu("Server Unlock Team Switching")]
    private void ContextServerUnlockTeamSwitching()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"[{nameof(MatchGameManagerBase)}] Cannot unlock team switching because server is not active.", this);
            return;
        }

        ServerUnlockTeamSwitching();
    }

    [Server]
    protected void ServerSetReturnToLobbyCountdownRemaining(float value)
    {
        if (Mathf.Approximately(ReturnToLobbyCountdownRemaining, value))
        {
            return;
        }

        ReturnToLobbyCountdownRemaining = value;

        if (isServerOnly)
        {
            OnReturnToLobbyCountdownChanged(value);
        }
    }

    [Server]
    protected void ServerEnterPreMatch()
    {
        LifecycleWinnerTeamId = -1;
        ServerSetReturnToLobbyCountdownRemaining(0f);
        ServerSetTeamSelectionLocked(false);
        ServerSetLifecycleState(MatchLifecycleState.PreMatch);
    }

    [Server]
    protected void ServerStartMatchLifecycle()
    {
        ServerSetTeamSelectionLocked(true);
        ServerSetLifecycleState(MatchLifecycleState.MatchStarting);
        ServerSetLifecycleState(MatchLifecycleState.InMatch);
    }

    [Server]
    protected void ServerEnterPostMatch()
    {
        ServerSetLifecycleState(MatchLifecycleState.PostMatch);
    }

    [Server]
    protected void ServerEndMatchLifecycle(int winnerTeamId)
    {
        LifecycleWinnerTeamId = winnerTeamId;
        ServerSetTeamSelectionLocked(true);
        ServerSetLifecycleState(MatchLifecycleState.MatchEnded);

        if (autoReturnToLobbyOnMatchEnd && _returnToLobbyCoroutine == null)
        {
            _returnToLobbyCoroutine = StartCoroutine(ServerReturnToLobbyAfterDelay());
        }

        if (winnerTeamId >= 0)
        {
            OnLifecycleMatchEnded(winnerTeamId);
        }
    }

    [Server]
    private IEnumerator ServerReturnToLobbyAfterDelay()
    {
        float delay = Mathf.Max(0f, returnToLobbyDelaySeconds);

        float endTime = Time.time + delay;
        while (isServer && Time.time < endTime)
        {
            ServerSetReturnToLobbyCountdownRemaining(Mathf.Max(0f, endTime - Time.time));
            yield return null;
        }

        ServerSetReturnToLobbyCountdownRemaining(0f);

        if (NetworkManager.singleton is NetworkRoomManager roomManager)
        {
            roomManager.ServerChangeScene(roomManager.RoomScene);
        }
        else
        {
            Debug.LogWarning($"[{nameof(MatchGameManagerBase)}] NetworkManager is not a NetworkRoomManager. Unable to return to room scene.", this);
        }

        _returnToLobbyCoroutine = null;
    }

    [Server]
    protected void ClearTeamAssignments()
    {
        ConnectionTeamAssignments.Clear();
    }

    [Server]
    public virtual bool ServerTryChooseTeam(int connectionId, int requestedTeamId, out string reason)
    {
        reason = null;

        if (!CanAcceptTeamSelection())
        {
            reason = "Team selection is locked.";
            return false;
        }

        if (TeamCount <= 0)
        {
            reason = "No teams are configured.";
            return false;
        }

        if (requestedTeamId < 0 || requestedTeamId >= TeamCount)
        {
            reason = "Selected team is out of range.";
            return false;
        }

        int currentTeam = -1;
        bool hadExistingTeam = ConnectionTeamAssignments.TryGetValue(connectionId, out currentTeam);
        if (hadExistingTeam && currentTeam == requestedTeamId)
        {
            reason = "Already assigned to that team.";
            return true;
        }

        if (requireBalancedManualTeamSwitching && !CanAssignTeamWithBalanceRules(connectionId, requestedTeamId, hadExistingTeam ? currentTeam : -1))
        {
            reason = "That team is currently full compared to others.";
            return false;
        }

        ConnectionTeamAssignments[connectionId] = requestedTeamId;
        OnServerPlayerTeamAssigned(connectionId, requestedTeamId);

        if (isServerOnly)
        {
            OnPlayerTeamAssigned((connectionId, requestedTeamId));
        }

        return true;
    }

    [Server]
    protected virtual bool CanAcceptTeamSelection()
    {
        return !TeamSelectionLocked;
    }

    [Server]
    protected virtual bool CanAssignTeamWithBalanceRules(int connectionId, int requestedTeamId, int previousTeamId)
    {
        var projectedPopulation = new int[Mathf.Max(1, TeamCount)];

        foreach (var assignedTeam in ConnectionTeamAssignments.Values)
        {
            if (assignedTeam < 0 || assignedTeam >= projectedPopulation.Length)
            {
                continue;
            }

            projectedPopulation[assignedTeam]++;
        }

        if (previousTeamId >= 0 && previousTeamId < projectedPopulation.Length)
        {
            projectedPopulation[previousTeamId] = Mathf.Max(0, projectedPopulation[previousTeamId] - 1);
        }

        projectedPopulation[requestedTeamId]++;

        int minCount = int.MaxValue;
        int maxCount = int.MinValue;

        for (int i = 0; i < projectedPopulation.Length; i++)
        {
            minCount = Mathf.Min(minCount, projectedPopulation[i]);
            maxCount = Mathf.Max(maxCount, projectedPopulation[i]);
        }

        return (maxCount - minCount) <= 1;
    }

    protected bool TryGetAssignedTeam(int connectionId, out int teamId)
    {
        return ConnectionTeamAssignments.TryGetValue(connectionId, out teamId);
    }

    [Server]
    protected virtual void OnServerPlayerTeamAssigned(int connectionId, int teamId)
    {
        if (PlayerUnitsManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
        {
            var playerUnit = PlayerUnitsManager.Instance.playerUnits[i];
            if (playerUnit.ConnectionId != connectionId || playerUnit.Unit == null)
            {
                continue;
            }

            var unitController = playerUnit.Unit.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.SetTeam(teamId);
            }

            return;
        }
    }

    private void HookOnLifecycleStateChanged(MatchLifecycleState oldValue, MatchLifecycleState newValue)
    {
        OnLifecycleStateChanged(newValue);
    }

    private void HookOnTeamSelectionLockedChanged(bool oldValue, bool newValue)
    {
        OnTeamSelectionLockChanged(newValue);
    }

    private void HookOnLifecycleWinnerTeamChanged(int oldValue, int newValue)
    {
        if (newValue < 0)
        {
            return;
        }

        if (LifecycleState == MatchLifecycleState.MatchEnded)
        {
            OnLifecycleMatchEnded(newValue);
        }
    }

    private void HookOnReturnToLobbyCountdownChanged(float oldValue, float newValue)
    {
        OnReturnToLobbyCountdownChanged(newValue);
    }
}
