using System.Collections.Generic;
using System.Linq;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.World;
using UnityEngine;

[RequireComponent(typeof(CastleSiegeManager))]
public class CastleSiegeGateManager : MonoBehaviour
{
    private DisposableBag subscriptions;

    private void OnEnable()
    {
        if (!NetworkServer.active)
            return;

        GameMessages.Subscribe<CastleSiegePhaseChangedEvent>(ref subscriptions, OnMatchPhaseChanged);
    }

    private void OnDisable()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
    }

    private void OnMatchPhaseChanged(CastleSiegePhaseChangedEvent evt)
    {
        if (!NetworkServer.active || evt == null)
            return;

        switch (evt.Phase)
        {
            case CastleSiegeManager.MatchPhase.Warmup:
                CloseAllGates();
                break;
            case CastleSiegeManager.MatchPhase.InGame:
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
