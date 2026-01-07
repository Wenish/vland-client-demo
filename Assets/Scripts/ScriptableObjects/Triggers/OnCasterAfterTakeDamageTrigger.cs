using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterAfterTakeDamage", fileName = "OnCasterAfterTakeDamageTrigger")]
public class OnCasterAfterTakeDamageTrigger : SkillEventTriggerData
{
    // Handler is stored to ensure the same instance is added and removed
    // This prevents duplicate triggers when the runner is re-enabled (e.g., on unit revival)
    private Action<(UnitController target, UnitController attacker)> _handler;

    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        var skill = runner.GetComponent<NetworkedSkillInstance>();
        if (skill == null) return;
        var caster = skill.Caster;
        if (caster == null) return;

        // Create handler only once to avoid accumulation on re-subscription
        _handler = obj =>
        {
            var targets = new List<UnitController> { caster };
            Debug.Log("OnCasterAfterTakeDamageTrigger fired");
            runner.Fire(this, targets);
        };

        runner.Subscribe(
            () => caster.OnAfterTakeDamage += _handler,
            () => caster.OnAfterTakeDamage -= _handler
        );
    }
}