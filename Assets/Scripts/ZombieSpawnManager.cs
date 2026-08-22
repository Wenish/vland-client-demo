using Mirror;
using MyGame.Events;
using R3;
using UnityEngine;

public class ZombieSpawnManager : NetworkBehaviour
{
    public ZombieSpawnController[] zombieSpawns;
    public GateMapping[] gateMappings;
    private DisposableBag serverSubscriptions;

    void Start()
    {
        GetAllZombieSpawnInScene();

        if (isServer)
        {
            GameMessages.Subscribe<OpenGateEvent>(ref serverSubscriptions, OnGateOpenEvent);
        }
    }

    void OnDestroy()
    {
        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
    }

    void GetAllZombieSpawnInScene()
    {
        zombieSpawns = FindObjectsByType<ZombieSpawnController>();
    }

    [System.Serializable]
    public struct GateMapping
    {
        public int gateId;
        public int[] spawnGroupId;
    }

    void OnGateOpenEvent(OpenGateEvent openGateEvent)
    {
        foreach (var gateMapping in gateMappings)
        {
            if (gateMapping.gateId == openGateEvent.GateId)
            {
                foreach (var spawnGroupId in gateMapping.spawnGroupId)
                {
                    foreach (var zombieSpawn in zombieSpawns)
                    {
                        if (zombieSpawn.spawnGroupId == spawnGroupId)
                        {
                            zombieSpawn.isActive = true;
                        }
                    }
                }
            }
        }
    }
}