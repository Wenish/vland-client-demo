using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicPlayAnimation",
    menuName = "Game/Skills/Effects/Mechanic/Play Animation"
)]
public class SkillEffectMechanicPlayAnimationData : SkillEffectData
{
    public enum AnimationMode
    {
        Trigger,
        State,
    }

    [Header("Selection")]
    public bool applyToCaster = false;
    public bool applyToTargets = true;

    [Header("Animation")]
    public AnimationMode mode = AnimationMode.Trigger;
    public string triggerName = "Attack";
    public string stateName = "";
    [Min(0)] public int layer = 0;
    [Min(0f)] public float transitionDuration = 0.05f;
    [Range(0f, 1f)] public float normalizedTime = 0f;
    [Min(0.01f)] public float speedMultiplier = 1f;
    public bool resetTriggerBeforeSet = true;

    [Header("Timing")]
    [Tooltip("Optional delay after firing animation before next node executes.")]
    [Min(0f)] public float postDelaySeconds = 0f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(CastContext castContext, List<UnitController> targets, Action<List<UnitController>> onComplete)
    {
        if (castContext?.skillInstance == null)
        {
            onComplete(targets);
            yield break;
        }

        var affected = new HashSet<UnitController>();

        if (applyToCaster && castContext.caster != null)
        {
            affected.Add(castContext.caster);
        }

        if (applyToTargets && targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                affected.Add(target);
            }
        }

        foreach (var unit in affected)
        {
            if (unit == null) continue;

            castContext.skillInstance.Rpc_PlayUnitAnimation(
                unit.netId,
                mode == AnimationMode.Trigger,
                triggerName,
                stateName,
                layer,
                transitionDuration,
                normalizedTime,
                speedMultiplier,
                resetTriggerBeforeSet
            );
        }

        if (postDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(postDelaySeconds);
        }

        onComplete(targets);
    }
}