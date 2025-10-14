using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterKillsUnit", fileName = "OnCasterKillsUnitTrigger")]
public class OnUnitKilledTrigger : SkillEventTriggerData
{
    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        runner.Subscribe<UnitDiedEvent>((evt) =>
        {
            var skill = runner.GetComponent<NetworkedSkillInstance>();
            if (skill == null) return;
            var caster = skill.Caster;
            if (caster == null) return;

            // Only fire when our caster is the killer
            if (evt.Killer != caster) return;

            // Targets: the killer (self) by default
            var targets = new List<UnitController> { caster };
            runner.Fire(this, targets);
        });
    }
}
