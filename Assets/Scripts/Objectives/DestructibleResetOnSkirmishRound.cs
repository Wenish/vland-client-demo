using Mirror;
using MyGame.Events;
using R3;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleResetOnSkirmishRound : NetworkBehaviour
{
    [SerializeField]
    private SkirmishGameManager.RoundState rebuildOnState = SkirmishGameManager.RoundState.PreRoundCountdown;

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

        GameMessages.Subscribe<SkirmishRoundStateChangedEvent>(ref subscriptions, OnRoundStateChanged);
    }

    public override void OnStopServer()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
        base.OnStopServer();
    }

    [Server]
    private void OnRoundStateChanged(SkirmishRoundStateChangedEvent evt)
    {
        if (evt == null || evt.State != rebuildOnState)
            return;

        _objective?.ServerRebuildNow();
    }
}
