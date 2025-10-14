using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnSelfDied", fileName = "OnSelfDiedTrigger")]
public class OnSelfDiedTrigger : SkillEventTriggerData
{
    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        runner.Subscribe<UnitDiedEvent>((evt) =>
        {
            var skill = runner.GetComponent<NetworkedSkillInstance>();
            if (skill == null) return;
            var caster = skill.Caster;
            if (caster == null) return;

            if (evt.Unit != caster) return;

            // Target: self
            var targets = new List<UnitController> { caster };
            runner.Fire(this, targets);
        });
    }
}
