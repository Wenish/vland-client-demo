using Mirror;
using MyGame.Events;
using UnityEngine;

public class ZombieSpawnManager : NetworkBehaviour
{
    public ZombieSpawnController[] zombieSpawns;
    public GateMapping[] gateMappings;

    void Start()
    {
        GetAllZombieSpawnInScene();

        if (isServer)
        {
            EventManager.Instance.Subscribe<OpenGateEvent>(OnGateOpenEvent);
        }
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<OpenGateEvent>(OnGateOpenEvent);
    }

    void GetAllZombieSpawnInScene()
    {
        zombieSpawns = FindObjectsByType<ZombieSpawnController>(FindObjectsSortMode.None);
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