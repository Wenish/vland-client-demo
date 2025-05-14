using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicDelay",
    menuName = "Game/Skills/Effects/Mechanic/Delay"
)]
public class SkillEffectMechanicDelayData : SkillEffectData
{
    [Tooltip("Seconds to wait before continuing the chain.")]
    public float delaySeconds = 1f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext castContext,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete
    )
    {
        // 1) Wait for the specified delay
        yield return new WaitForSeconds(delaySeconds);

        // 2) Hand back the same targets so the chain continues
        onComplete(targets);
    }
}