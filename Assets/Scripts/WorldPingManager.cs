using MyGame.Events;
using R3;
using UnityEngine;

public class WorldPingManager : MonoBehaviour
{
    public GameObject pingPrefab;
    private DisposableBag subscriptions;

    private void Start()
    {
        GameMessages.Subscribe<WorldPingEvent>(ref subscriptions, OnWorldPingEvent);
    }

    private void OnDestroy()
    {
        subscriptions.Dispose();
        subscriptions = new DisposableBag();
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