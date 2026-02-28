using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleResetOnCastlePhase : NetworkBehaviour
{
    [SerializeField]
    private CastleSiegeManager.MatchPhase rebuildOnPhase = CastleSiegeManager.MatchPhase.Countdown;

    private DestructibleObjective _objective;
    private CastleSiegeManager _manager;

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

        _manager = CastleSiegeManager.Instance;
        if (_manager != null)
        {
            _manager.OnMatchPhaseChanged += OnMatchPhaseChanged;
        }
    }

    public override void OnStopServer()
    {
        if (_manager != null)
        {
            _manager.OnMatchPhaseChanged -= OnMatchPhaseChanged;
            _manager = null;
        }

        base.OnStopServer();
    }

    [Server]
    private void OnMatchPhaseChanged(CastleSiegeManager.MatchPhase phase)
    {
        if (phase != rebuildOnPhase)
        {
            return;
        }

        _objective?.ServerRebuildNow();
    }
}