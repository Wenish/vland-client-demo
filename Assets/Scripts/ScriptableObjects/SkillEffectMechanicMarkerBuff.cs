using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicMarkerBuff",
    menuName = "Game/Skills/Effects/Mechanic/MarkerBuff")]
public class SkillEffectMechanicMarkerBuff : SkillEffectMechanic
{
    /// <summary>
    /// Minimal concrete Buff with no side effects. Used as a marker/gate for other mechanics.
    /// </summary>
    private class MarkerBuff : Buff
    {
        public MarkerBuff(string buffId, float duration, UniqueMode uniqueMode, UnitMediator caster, BuffType buffType)
            : base(buffId, duration, uniqueMode, caster, buffType)
        {
        }

        public override void OnApply(UnitMediator mediator) { }
        public override void OnRemove(UnitMediator mediator) { }
    }

    [Tooltip("Identifier for this marker buff (used by conditions to check if it's active).")]
    public string buffId;

    [Tooltip("How long the buff lasts in seconds.")]
    public float duration = 3f;

    [Tooltip("Uniqueness mode: None = stack unlimited, Global = one per target, PerCaster = one per caster.")]
    public UniqueMode uniqueMode = UniqueMode.Global;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            var mediator = target.unitMediator;
            if (mediator == null) continue;

            // Create and apply the marker buff
            var buff = new MarkerBuff(buffId, duration, uniqueMode, castContext.caster.unitMediator, null)
            {
                SkillName = castContext.skillInstance.skillData.skillName
            };
            mediator.Buffs.AddBuff(buff);
        }
        return targets;
    }
}
