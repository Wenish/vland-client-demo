using System.Collections.Generic;
using System.Linq;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.World;
using UnityEngine;

[RequireComponent(typeof(SkirmishGameManager))]
public class SkirmishGatesManager : MonoBehaviour
{
    private DisposableBag subscriptions;

    private void OnEnable()
    {
        if (!NetworkServer.active)
            return;

        GameMessages.Subscribe<SkirmishRoundStateChangedEvent>(ref subscriptions, OnRoundStateChanged);
    }

    private void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
    }

    private void OnRoundStateChanged(SkirmishRoundStateChangedEvent evt)
    {
        if (!NetworkServer.active || evt == null)
            return;

        switch (evt.State)
        {
            case SkirmishGameManager.RoundState.WaitingToStart:
                CloseAllGates();
                break;
            case SkirmishGameManager.RoundState.PreRoundCountdown:
                CloseAllGates();
                break;
            case SkirmishGameManager.RoundState.InRound:
                OpenAllGates();
                break;
        }
    }

    private static IEnumerable<GateController> GetGates()
    {
        var registry = GameServices.Get<IGateRegistry>();
        return registry != null ? registry.Gates : Enumerable.Empty<GateController>();
    }

    private void CloseAllGates()
    {
        foreach (var gate in GetGates())
        {
            if (gate != null)
                gate.CloseGate();
        }
    }

    private void OpenAllGates()
    {
        foreach (var gate in GetGates())
        {
            if (gate != null)
                gate.OpenGate();
        }
    }
}
