using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/BuffStat")]
public class SkillEffectMechanicBuffStatData : SkillEffectData
{

    public StatType StatType;
    public ModifierType ModifierType;
    public float Value;
    public float duration = 5f;
    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        foreach (var target in targets)
        {
            var unitController = target.GetComponent<UnitController>();
            if (unitController == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitController component.");
                continue;
            }
            UnitMediator mediator = unitController.unitMediator;
            if (mediator == null)
            {
                Debug.LogWarning($"Target {target.name} does not have a UnitMediator component.");
                continue;
            }
            BuffStat buff = new BuffStat(duration, StatType, ModifierType, Value);
            mediator.AddBuff(buff);
            Debug.Log($"Applied BuffStat to {target.name}: {StatType}, {ModifierType}, {Value}");
        }
        return targets;
    }
}