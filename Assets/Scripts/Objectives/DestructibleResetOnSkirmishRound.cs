using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleResetOnSkirmishRound : NetworkBehaviour
{
    [SerializeField]
    private SkirmishGameManager.RoundState rebuildOnState = SkirmishGameManager.RoundState.PreRoundCountdown;

    private DestructibleObjective _objective;
    private SkirmishGameManager _manager;

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_objective == null)
        {
            _objective = GetComponent<DestructibleObjective>();
        }

        _manager = SkirmishGameManager.Instance;
        if (_manager != null)
        {
            _manager.OnRoundStateChanged += OnRoundStateChanged;
        }
    }

    public override void OnStopServer()
    {
        if (_manager != null)
        {
            _manager.OnRoundStateChanged -= OnRoundStateChanged;
            _manager = null;
        }

        base.OnStopServer();
    }

    [Server]
    private void OnRoundStateChanged(SkirmishGameManager.RoundState state)
    {
        if (state != rebuildOnState)
        {
            return;
        }

        _objective?.ServerRebuildNow();
    }
}