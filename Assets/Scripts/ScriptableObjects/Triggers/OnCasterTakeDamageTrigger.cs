using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterTakeDamage", fileName = "OnCasterTakeDamageTrigger")]
public class OnCasterTakeDamageTrigger : SkillEventTriggerData
{
    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        var skill = runner.GetComponent<NetworkedSkillInstance>();
        if (skill == null) return;
        var caster = skill.Caster;
        if (caster == null) return;

        // Subscribe via UnitDamagedEvent so we also receive the damage amount for percent-based
        // mechanics such as SkillEffectMechanicReflectDamage in PercentOfIncoming mode.
        runner.Subscribe<UnitDamagedEvent>(e =>
        {
            if (e.Unit != caster) return;
            var targets = new List<UnitController> { caster };
            runner.FireWithInstigator(this, targets, e.Attacker, e.DamageAmount);
        });
    }
}