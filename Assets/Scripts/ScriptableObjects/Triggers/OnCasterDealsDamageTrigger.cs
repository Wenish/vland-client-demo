using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterDealsDamage", fileName = "OnCasterDealsDamageTrigger")]
public class OnCasterDealsDamageTrigger : SkillEventTriggerData
{
    [Tooltip("Optional minimum damage to fire. 0 = any damage")] public int minDamage = 0;

    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        runner.Subscribe<UnitDamagedEvent>((evt) =>
        {
            var skill = runner.GetComponent<NetworkedSkillInstance>();
            if (skill == null) return;
            var caster = skill.Caster;
            if (caster == null) return;

            if (evt.Attacker != caster) return;
            if (evt.DamageAmount < minDamage) return;

            // Target: damaged unit
            var targets = new List<UnitController> { evt.Unit };
            runner.Fire(this, targets);
        });
    }
}
