using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicSpawnUnit", menuName = "Game/Skills/Effects/Mechanic/SpawnUnit")]
public class SkillEffectMechanicSpawnUnit : SkillEffectMechanic
{
    public UnitData unitData;
    public bool spawnAtAimPoint = false;
    public int maxSpawnCount = 1;

    [Header("Lifetime")]
    [Tooltip("How long the spawned unit lives in seconds. 0 = infinite.")]
    public float lifetime = 0f;

    [Header("Despawn")]
    [Tooltip("Delay in seconds before the unit is actually destroyed after despawn is triggered (death, lifetime, etc.).")]
    public float despawnDelay = 1f;

    [Tooltip("If true, all spawned units are despawned when the caster dies.")]
    public bool despawnOnCasterDeath = true;

    [Tooltip("If true, all spawned units are despawned when the caster changes loadout.")]
    public bool despawnOnLoadoutChange = true;

    // Track spawned units per caster so we can cull the oldest when exceeding the cap.
    private readonly Dictionary<uint, Queue<GameObject>> _spawnedUnitsByCaster = new();

    // Track which casters we've already subscribed to, to avoid duplicate subscriptions.
    private readonly HashSet<uint> _subscribedCasters = new();

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            var position = spawnAtAimPoint && castContext.aimPoint.HasValue ? castContext.aimPoint.Value : target.transform.position;
            var rotation = spawnAtAimPoint && castContext.aimRotation.HasValue ? castContext.aimRotation.Value : target.transform.rotation;
            SpawnUnit(castContext, position, rotation);
        }
        return targets;
    }

    [Server]
    private void SpawnUnit(CastContext castContext, Vector3 position, Quaternion rotation)
    {
        if (unitData == null)
        {
            Debug.LogWarning("UnitData is null. Cannot spawn unit.");
            return;
        }

        var unitInstance = UnitSpawner.Instance.Spawn(unitData, position, rotation);

        var unitController = unitInstance.GetComponent<UnitController>();
        unitController.SetTeam(castContext.caster.team);

        // Add lifetime component for timed despawn and despawn event
        var lifetimeComponent = unitInstance.AddComponent<SpawnedUnitLifetime>();
        lifetimeComponent.Initialize(lifetime, despawnDelay);

        // When spawned unit dies, trigger despawn through the lifetime component
        unitController.OnDied += lifetimeComponent.Despawn;

        SubscribeToCasterEvents(castContext.caster);
        TrackSpawnAndCullOldest(castContext.caster, unitInstance);
    }

    private void SubscribeToCasterEvents(UnitController caster)
    {
        var netId = caster.netId;
        if (!_subscribedCasters.Add(netId)) return;

        caster.OnDied += () =>
        {
            if (despawnOnCasterDeath) DespawnAllForCaster(netId);
        };

        var skillSystem = caster.GetComponent<SkillSystem>();
        if (skillSystem != null)
        {
            skillSystem.OnLoadoutReplaced += () =>
            {
                if (despawnOnLoadoutChange) DespawnAllForCaster(netId);
            };
        }
    }

    [Server]
    private void DespawnAllForCaster(uint casterNetId)
    {
        if (!_spawnedUnitsByCaster.TryGetValue(casterNetId, out var queue)) return;

        while (queue.Count > 0)
        {
            var unit = queue.Dequeue();
            if (unit == null) continue;

            var lifetimeComponent = unit.GetComponent<SpawnedUnitLifetime>();
            if (lifetimeComponent != null)
            {
                lifetimeComponent.Despawn();
            }
            else
            {
                NetworkServer.Destroy(unit);
            }
        }
    }

    private void TrackSpawnAndCullOldest(UnitController caster, GameObject unitInstance)
    {
        if (caster == null || unitInstance == null) return;

        var queue = GetSpawnQueue(caster);
        if (queue == null) return;

        queue.Enqueue(unitInstance);

        if (maxSpawnCount > 0)
        {
            while (queue.Count > maxSpawnCount)
            {
                var oldest = queue.Dequeue();
                if (oldest == null) continue;

                var lifetimeComponent = oldest.GetComponent<SpawnedUnitLifetime>();
                if (lifetimeComponent != null)
                {
                    lifetimeComponent.Despawn();
                }
                else
                {
                    NetworkServer.Destroy(oldest);
                }
            }
        }
    }

    private Queue<GameObject> GetSpawnQueue(UnitController caster)
    {
        var netId = caster.netId;

        if (!_spawnedUnitsByCaster.TryGetValue(netId, out var queue))
        {
            queue = new Queue<GameObject>();
            _spawnedUnitsByCaster[netId] = queue;
        }

        PruneDestroyed(queue);
        return queue;
    }

    private static void PruneDestroyed(Queue<GameObject> queue)
    {
        if (queue == null || queue.Count == 0) return;

        var count = queue.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = queue.Dequeue();
            if (entry != null)
            {
                queue.Enqueue(entry);
            }
        }
    }
}