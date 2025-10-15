using System;
using System.Collections.Generic;
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

        // Use runner's convenience API so unsubscription is automatic
        Action<(UnitController target, UnitController attacker)> handler = obj =>
        {
            var targets = new List<UnitController> { caster };
            runner.Fire(this, targets);
        };

        runner.Subscribe(
            () => caster.OnTakeDamage += handler,
            () => caster.OnTakeDamage -= handler
        );
    }
}