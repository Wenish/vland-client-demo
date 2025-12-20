using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicSpawnUnit", menuName = "Game/Skills/Effects/Mechanic/SpawnUnit")]
public class SkillEffectMechanicSpawnUnit : SkillEffectMechanic
{
    public UnitData unitData;
    public bool spawnAtAimPoint = false;

    public int maxSpawnCount = 1;

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

        // Additional setup for the spawned unit can be done here
    }
}