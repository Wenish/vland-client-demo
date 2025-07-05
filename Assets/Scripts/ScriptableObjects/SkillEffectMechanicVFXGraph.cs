using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/VFX/Graph")]
public class SkillEffectMechanicVFXGraph : SkillEffectMechanic
{
    [Tooltip("Prefab with a VisualEffect component (assign in inspector)")]
    public GameObject vfxPrefab;

    [Tooltip("How long the VFX should live")] 
    public float duration = 1f;

    public bool attachToTarget = true;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            if (Mirror.NetworkServer.active && vfxPrefab != null)
            {
                Vector3 position = target.transform.position;
                Quaternion rotation = target.transform.rotation;
                uint targetNetId = target.netId;

                // Call a networked RPC to spawn the VFX on all clients
                castContext.skillInstance.Rpc_SpawnVFXGraphPrefab(
                    position,
                    rotation,
                    duration,
                    attachToTarget,
                    targetNetId,
                    vfxPrefab.name
                );
            }
        }
        return targets;
    }
}