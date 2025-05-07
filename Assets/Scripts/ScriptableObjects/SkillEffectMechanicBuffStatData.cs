using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/BuffStat")]
public class SkillEffectMechanicBuffStatData : SkillEffectMechanic
{
    public string buffId;
    public StatType StatType;
    public ModifierType ModifierType;
    public float Value;
    public float duration = 5f;
    public bool isUnique = true;

    public override List<UnitController> DoMechanic(UnitController caster, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            UnitMediator mediator = target.unitMediator;
            if (mediator == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitMediator component.");
                continue;
            }

            var listStatModifiers = new List<StatModifier>
            {
                new StatModifier() {
                    Type = StatType,
                    ModifierType = ModifierType,
                    Value = Value
                }
            };

            BuffStat buff = new BuffStat(buffId, duration, isUnique, listStatModifiers);
            mediator.AddBuff(buff);
            Debug.Log($"Applied BuffStat to {target.name}: {StatType}, {ModifierType}, {Value}");
        }
        return targets;
    }
}