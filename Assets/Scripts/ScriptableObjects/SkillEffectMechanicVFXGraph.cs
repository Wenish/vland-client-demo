using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicVFXGraph", menuName = "Game/Skills/Effects/VFX/Graph")]
public class SkillEffectMechanicVFXGraph : SkillEffectMechanic
{
    [Tooltip("Prefab with a VisualEffect component (assign in inspector)")]
    public GameObject vfxPrefab;

    [Tooltip("How long the VFX GameObject should live")]
    public float duration = 1f;

    [Tooltip("How long the particle should live")]
    public float lifetime = 1f;

    [Tooltip("If true, the VFX will be parented to the target unit's transform")]
    public bool attachToTarget = true;

    [Tooltip("Spawn VFX at the aim point when available (uses CastContext.aimPoint/aimRotation)")]
    public bool spawnAtAimPoint = false;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            if (Mirror.NetworkServer.active && vfxPrefab != null)
            {
                Vector3 position = (spawnAtAimPoint && castContext.aimPoint.HasValue)
                    ? castContext.aimPoint.Value
                    : target.transform.position;
                Quaternion rotation = (spawnAtAimPoint && castContext.aimRotation.HasValue)
                    ? castContext.aimRotation.Value
                    : target.transform.rotation;
                uint targetNetId = target.netId;

                // If spawning at aim point, avoid parenting so the VFX stays at world position
                bool shouldAttach = attachToTarget && !(spawnAtAimPoint && castContext.aimPoint.HasValue);

                // Call a networked RPC to spawn the VFX on all clients
                castContext.skillInstance.Rpc_SpawnVFXGraphPrefab(
                    position,
                    rotation,
                    duration,
                    lifetime,
                    shouldAttach,
                    targetNetId,
                    vfxPrefab.name
                );
            }
        }
        return targets;
    }
}