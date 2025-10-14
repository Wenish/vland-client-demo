using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterStartAttack", fileName = "OnCasterStartAttackTrigger")]
public class OnCasterStartAttackTrigger : SkillEventTriggerData
{
    public override void Subscribe(ReactiveTriggerRunner runner)
    {

        var skill = runner.GetComponent<NetworkedSkillInstance>();
        if (skill == null) return;
        var caster = skill.Caster;
        if (caster == null) return;

        // Use runner's convenience API so unsubscription is automatic
        Action<(UnitController unitController, int attackIndex)> handler = obj =>
        {
            // At attack start we only know the attacker, not the hit target
            var attacker = obj.unitController;
            var targets = new List<UnitController> { attacker };
            runner.Fire(this, targets);
        };

        runner.Subscribe(
            () => caster.OnAttackStart += handler,
            () => caster.OnAttackStart -= handler
        );
    }
}