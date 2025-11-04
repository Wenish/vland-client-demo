using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicAreaZone", menuName = "Game/Skills/Effects/Mechanic/AreaZone")]
public class SkillEffectMechanicAreaZone : SkillEffectMechanic
{
    public AreaZoneData areaZoneData;
    public bool spawnAtAimPoint = false;

    [Tooltip("Effect to apply on each tick of the area zone.")]
    public SkillEffectChainData onTickEffect;

    [Tooltip("Targeting logic for the area zone.")]
    public SkillEffectTarget skillEffectTarget;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            var position = spawnAtAimPoint && castContext.aimPoint.HasValue ? castContext.aimPoint.Value : target.transform.position;
            var rotation = spawnAtAimPoint && castContext.aimRotation.HasValue ? castContext.aimRotation.Value : target.transform.rotation;
            SpawnAreaZone(castContext, position, rotation);
        }
        return targets;
    }

    private void SpawnAreaZone(CastContext castContext, Vector3 position, Quaternion rotation)
    {
        if (areaZoneData == null)
        {
            Debug.LogWarning("AreaZoneData is null. Cannot spawn area zone.");
            return;
        }

        var areaZoneInstance = AreaZoneSpawner.Instance.SpawnAreaZone(areaZoneData, position, rotation);

        AreaZoneController areaZoneController = areaZoneInstance.GetComponent<AreaZoneController>();
        areaZoneController.caster = castContext.caster;

        // Subscribe to events BEFORE initializing the zone so the first tick at t=0 is observed
        areaZoneController.OnTick += (zone) =>
        {
            if (onTickEffect != null && zone.caster is MonoBehaviour mb)
            {
                var unitsInZone = GetTargetsInAreaZone(castContext, zone);
                mb.StartCoroutine(onTickEffect.ExecuteCoroutine(castContext, unitsInZone));
            }
        };

        areaZoneController.OnAreaZoneDestoryed += (zone) =>
        {
            zone.OnTick -= null;
            zone.OnAreaZoneDestoryed -= null;
            castContext.skillInstance.OnCleanup -= null;
        };

        if (castContext.skillInstance != null && NetworkServer.active)
        {
            castContext.skillInstance.OnCleanup += (skillInstance) =>
            {
                if (areaZoneController != null && areaZoneController.isServer)
                {
                    areaZoneController.DestroySelf();
                }
                castContext.skillInstance.OnCleanup -= null;
            };
        }
        // Now initialize and start the zone timeline (will trigger start tick if configured)
        areaZoneController.SetAreaZoneName(areaZoneData.areaZoneName);
    }



    private List<UnitController> GetTargetsInAreaZone(CastContext castContext, AreaZoneController zone)
    {
        GameObject mockUnit = new GameObject("MockUnit");
        mockUnit.transform.position = zone.transform.position;
        mockUnit.transform.rotation = zone.transform.rotation;
        mockUnit.SetActive(false);
        mockUnit.AddComponent<NetworkIdentity>();
        UnitController mockUnitController = mockUnit.AddComponent<UnitController>();
        mockUnitController.enabled = false;


        // never include the mock unit itself in the results
        var potentialTargets = skillEffectTarget.GetTargets(castContext, new List<UnitController> { mockUnitController });
        potentialTargets.Remove(mockUnitController);
        Destroy(mockUnit);
        return potentialTargets;
    }
    
}