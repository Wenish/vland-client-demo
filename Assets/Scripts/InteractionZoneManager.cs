using MyGame.Events;
using UnityEngine;

public class InteractionZoneManager : MonoBehaviour
{
    public static InteractionZoneManager Instance { get; private set; }

    [SerializeField]
    private InteractionZone[] interactionZones;

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

        EventManager.Instance.Subscribe<OpenedGateEvent>(OnOpenedGateEvent);
        EventManager.Instance.Subscribe<ClosedGateEvent>(OnClosedGateEvent);
    }

    void GetAllInteractionZonesInScene()
    {
        interactionZones = FindObjectsByType<InteractionZone>(FindObjectsSortMode.None);
    }

    void OnOpenedGateEvent(OpenedGateEvent openedGateEvent)
    {
        foreach (var zone in interactionZones)
        {
            if (zone.interactionType != InteractionType.OpenGate) continue;

            if (zone.interactionId == openedGateEvent.GateId)
            {
                zone.gameObject.SetActive(false);
            }
        }
    }

    void OnClosedGateEvent(ClosedGateEvent closedGateEvent)
    {
        foreach (var zone in interactionZones)
        {
            if (zone.interactionType != InteractionType.OpenGate) continue;

            if (zone.interactionId == closedGateEvent.GateId)
            {
                zone.gameObject.SetActive(true);
            }
        }
    }
    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<OpenedGateEvent>(OnOpenedGateEvent);
        EventManager.Instance.Unsubscribe<ClosedGateEvent>(OnClosedGateEvent);
    }

}