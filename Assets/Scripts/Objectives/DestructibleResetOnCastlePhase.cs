using Mirror;
using MyGame.Events;
using R3;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleResetOnCastlePhase : NetworkBehaviour
{
    [SerializeField]
    private CastleSiegeManager.MatchPhase rebuildOnPhase = CastleSiegeManager.MatchPhase.Countdown;

    private DestructibleObjective _objective;
    private DisposableBag subscriptions;

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_objective == null)
            _objective = GetComponent<DestructibleObjective>();

        GameMessages.Subscribe<CastleSiegePhaseChangedEvent>(ref subscriptions, OnMatchPhaseChanged);
    }

    public override void OnStopServer()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
        base.OnStopServer();
    }

    [Server]
    private void OnMatchPhaseChanged(CastleSiegePhaseChangedEvent evt)
    {
        if (evt == null || evt.Phase != rebuildOnPhase)
            return;

        _objective?.ServerRebuildNow();
    }
}
