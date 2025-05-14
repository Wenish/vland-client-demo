using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/VFX/TargetLinearArea")]
public class SkillEffectTargetLinearVFX : SkillEffectData
{
    [Tooltip("Same as your TargetLinear range/width")]
    public float range = 5f;
    public float width = 1f;

    [Tooltip("Path under Resources to your unlit transparent VFX material")]
    public string materialResourcePath = "Materials/VFX/LinearAreaHighlight";

    [Tooltip("How long the spawned mesh should live")]
    public float duration = 1f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(CastContext ctx, List<UnitController> targets, Action<List<UnitController>> onComplete)
    {
        foreach (var target in targets)
        {
            // Only the server (or host) should send the RPC out
            if (NetworkServer.active)
            {
                Vector3 origin = ctx.caster.transform.position + Vector3.up * 0.01f;
                Vector3 direction = ctx.caster.transform.forward;

                ctx.skillInstance.Rpc_SpawnLinearAreaVFX(
                    origin,
                    direction,
                    range,
                    width,
                    materialResourcePath,
                    duration,
                    target.transform
                );
            }
        }

        // pass the (unchanged) targets back into the pipeline
        onComplete(targets);
        yield break;
    }
}