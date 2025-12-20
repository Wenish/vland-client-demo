using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicSpawnUnit", menuName = "Game/Skills/Effects/Mechanic/SpawnUnit")]
public class SkillEffectMechanicSpawnUnit : SkillEffectMechanic
{
    public UnitData unitData;
    public bool spawnAtAimPoint = false;

    public int maxSpawnCount = 1;

    // Track spawned units per caster so we can cull the oldest when exceeding the cap.
    private readonly Dictionary<uint, Queue<GameObject>> _spawnedUnitsByCaster = new Dictionary<uint, Queue<GameObject>>();

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

        TrackSpawnAndCullOldest(castContext.caster, unitInstance);

        // Additional setup for the spawned unit can be done here
    }

    private void TrackSpawnAndCullOldest(UnitController caster, GameObject unitInstance)
    {
        if (caster == null || unitInstance == null)
        {
            return;
        }

        var queue = GetSpawnQueue(caster);
        if (queue == null)
        {
            return;
        }

        queue.Enqueue(unitInstance);

        if (maxSpawnCount > 0)
        {
            while (queue.Count > maxSpawnCount)
            {
                var oldest = queue.Dequeue();
                if (oldest != null)
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
        if (queue == null || queue.Count == 0)
        {
            return;
        }

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