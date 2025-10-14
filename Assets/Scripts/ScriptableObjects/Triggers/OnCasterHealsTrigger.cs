using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Triggers/OnCasterHeals", fileName = "OnCasterHealsTrigger")]
public class OnCasterHealsTrigger : SkillEventTriggerData
{
    public bool includeSelfHeals = true;

    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        runner.Subscribe<UnitHealedEvent>((evt) =>
        {
            var skill = runner.GetComponent<NetworkedSkillInstance>();
            if (skill == null) return;
            var caster = skill.Caster;
            if (caster == null) return;

            var isCasterTheHealer = evt.Healer == caster;

            if (!isCasterTheHealer) return;

            if (!includeSelfHeals && evt.Unit == caster) return;

            // Default targets: healed unit
            var targets = new List<UnitController> { evt.Unit };
            runner.Fire(this, targets);
        });
    }
}
