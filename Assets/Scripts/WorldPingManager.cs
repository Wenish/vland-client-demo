using MyGame.Events;
using UnityEngine;

public class WorldPingManager : MonoBehaviour
{
    public GameObject pingPrefab;

    private void Start()
    {
        EventManager.Instance.Subscribe<WorldPingEvent>(OnWorldPingEvent);
    }

    private void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<WorldPingEvent>(OnWorldPingEvent);
    }

    private void OnWorldPingEvent(WorldPingEvent pingEvent)
    {
        SpawnPing(pingEvent.Position);
    }

    private void SpawnPing(Vector3 position)
    {
        Instantiate(pingPrefab, position, Quaternion.identity);
    }
}