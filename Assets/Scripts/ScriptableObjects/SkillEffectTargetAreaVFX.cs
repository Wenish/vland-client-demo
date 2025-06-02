using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/VFX/TargetArea")]
public class SkillEffectTargetAreaVFX : SkillEffectData
{
    [Tooltip("Same as your TargetLinear range/width")]
    public float range = 5f;
    public float width = 1f;
    public Vector2 offset = Vector2.zero;

    [Tooltip("Path under Resources to your unlit transparent VFX material")]
    public string materialResourcePath = "Materials/VFX/LinearAreaHighlight";

    [Tooltip("How long the spawned mesh should live")]
    public float duration = 1f;

    [Tooltip("Shape of the area VFX")]
    public AreaVFXShape shape = AreaVFXShape.Rectangle;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(CastContext ctx, List<UnitController> targets, Action<List<UnitController>> onComplete)
    {
        foreach (var target in targets)
        {
            // Only the server (or host) should send the RPC out
            if (NetworkServer.active)
            {
                Vector3 origin = target.transform.position;
                Vector3 direction = target.transform.forward;
            
                ctx.skillInstance.Rpc_SpawnAreaVFX(
                    origin,
                    direction,
                    range,
                    width,
                    materialResourcePath,
                    duration,
                    target.transform,
                    shape,
                    offset
                );
            }
        }

        // pass the (unchanged) targets back into the pipeline
        onComplete(targets);
        yield break;
    }
}