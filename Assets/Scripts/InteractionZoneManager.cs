using System.Collections.Generic;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Interactions;
using UnityEngine;

public class InteractionZoneManager : MonoBehaviour
{
    private DisposableBag subscriptions;

    void Start()
    {
        GameMessages.Subscribe<OpenedGateEvent>(ref subscriptions, OnOpenedGateEvent);
        GameMessages.Subscribe<ClosedGateEvent>(ref subscriptions, OnClosedGateEvent);
    }

    void OnOpenedGateEvent(OpenedGateEvent openedGateEvent)
    {
        foreach (var zone in SnapshotZones())
        {
            if (zone == null || zone.InteractionType != InteractionType.OpenGate) continue;

            if (zone.InteractionId == openedGateEvent.GateId)
            {
                zone.gameObject.SetActive(false);
            }
        }
    }

    void OnClosedGateEvent(ClosedGateEvent closedGateEvent)
    {
        foreach (var zone in SnapshotZones())
        {
            if (zone == null || zone.InteractionType != InteractionType.OpenGate) continue;

            if (zone.InteractionId == closedGateEvent.GateId)
            {
                zone.gameObject.SetActive(true);
            }
        }
    }

    private static List<InteractionZone> SnapshotZones()
    {
        var registry = GameServices.Get<IInteractionZoneRegistry>();
        if (registry == null)
            return new List<InteractionZone>();

        return new List<InteractionZone>(registry.Zones);
    }

    void OnDestroy()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
    }
}
