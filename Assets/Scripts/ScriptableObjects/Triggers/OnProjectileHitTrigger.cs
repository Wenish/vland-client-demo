using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OnProjectileHitTrigger", menuName = "Game/Skills/Triggers/OnProjectileHit")]
public class OnProjectileHitTrigger : SkillEventTriggerData
{
    public override void Subscribe(ReactiveTriggerRunner runner)
    {
        var skill = runner.GetComponent<NetworkedSkillInstance>();
        if (skill == null) return;
        var caster = skill.Caster;
        if (caster == null) return;

        // Use runner's convenience API so unsubscription is automatic
        Action<(UnitController targetUnit, ProjectileData projectile)> handler = obj =>
        {
            var target = obj.targetUnit;
            var targets = new List<UnitController> { target };
            runner.Fire(this, targets);
        };

        runner.Subscribe(
            () => caster.OnProjectileHit += handler,
            () => caster.OnProjectileHit -= handler
        );
    }
}