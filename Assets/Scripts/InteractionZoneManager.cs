using MyGame.Events;
using R3;
using UnityEngine;

public class InteractionZoneManager : MonoBehaviour
{
    public static InteractionZoneManager Instance { get; private set; }

    [SerializeField]
    private InteractionZone[] interactionZones;
    private DisposableBag subscriptions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        GetAllInteractionZonesInScene();

        GameMessages.Subscribe<OpenedGateEvent>(ref subscriptions, OnOpenedGateEvent);
        GameMessages.Subscribe<ClosedGateEvent>(ref subscriptions, OnClosedGateEvent);
    }

    void GetAllInteractionZonesInScene()
    {
        interactionZones = FindObjectsByType<InteractionZone>();
    }

    void OnOpenedGateEvent(OpenedGateEvent openedGateEvent)
    {
        foreach (var zone in interactionZones)
        {
            if (zone.InteractionType != InteractionType.OpenGate) continue;

            if (zone.InteractionId == openedGateEvent.GateId)
            {
                zone.gameObject.SetActive(false);
            }
        }
    }

    void OnClosedGateEvent(ClosedGateEvent closedGateEvent)
    {
        foreach (var zone in interactionZones)
        {
            if (zone.InteractionType != InteractionType.OpenGate) continue;

            if (zone.InteractionId == closedGateEvent.GateId)
            {
                zone.gameObject.SetActive(true);
            }
        }
    }
    void OnDestroy()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
    }

}