using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicBuffStat", menuName = "Game/Skills/Effects/Mechanic/BuffStat")]
public class SkillEffectMechanicBuffStatData : SkillEffectMechanic
{
    public string buffId;

    [Header("Stat Modifiers")] 
    [Tooltip("Configure one or more stat modifiers that this buff will apply.")]
    public List<StatModifier> statModifiers = new List<StatModifier>();
    public float duration = 5f;
    public UniqueMode uniqueMode = UniqueMode.None;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            UnitMediator mediator = target.unitMediator;
            if (mediator == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitMediator component.");
                continue;
            }
            if (statModifiers == null || statModifiers.Count == 0)
            {
                // Nothing to apply
                continue;
            }

            // Create fresh instances so multiple buffs don't share the same StatModifier references.
            var listStatModifiers = new List<StatModifier>(statModifiers.Count);
            foreach (var sm in statModifiers)
            {
                if (sm == null) continue;
                listStatModifiers.Add(new StatModifier
                {
                    Type = sm.Type,
                    ModifierType = sm.ModifierType,
                    Value = sm.Value
                });
            }

            if (listStatModifiers.Count == 0)
                continue;

            BuffStat buff = new BuffStat(buffId, duration, listStatModifiers, uniqueMode, castContext.caster.unitMediator);
            buff.SkillName = castContext.skillInstance.skillData.skillName;
            castContext.skillInstance.ManageBuff(mediator, buff, true);
        }
        return targets;
    }
}