using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.World;
using UnityEngine;

public class ZombieSpawnManager : NetworkBehaviour
{
    public GateMapping[] gateMappings;
    private DisposableBag serverSubscriptions;

    void Start()
    {
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

    [System.Serializable]
    public struct GateMapping
    {
        public int gateId;
        public int[] spawnGroupId;
    }

    void OnGateOpenEvent(OpenGateEvent openGateEvent)
    {
        var spawns = GameServices.Get<IZombieSpawnRegistry>()?.Spawns;
        if (spawns == null)
            return;

        foreach (var gateMapping in gateMappings)
        {
            if (gateMapping.gateId != openGateEvent.GateId)
                continue;

            foreach (var spawnGroupId in gateMapping.spawnGroupId)
            {
                foreach (var zombieSpawn in spawns)
                {
                    if (zombieSpawn != null && zombieSpawn.spawnGroupId == spawnGroupId)
                        zombieSpawn.isActive = true;
                }
            }
        }
    }
}
