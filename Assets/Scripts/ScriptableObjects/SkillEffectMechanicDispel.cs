using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicDispel", menuName = "Game/Skills/Effects/Mechanic/Dispel")]
public class SkillEffectMechanicDispel : SkillEffectMechanic
{
    public enum DispelMode
    {
        [Tooltip("Remove beneficial buffs from enemies (offensive dispel).")]
        RemovePositive,

        [Tooltip("Remove negative debuffs from allies (cleanse).")]
        RemoveNegative
    }

    [Header("Dispel Settings")]
    public DispelMode mode = DispelMode.RemoveNegative;

    [Tooltip("Maximum number of buffs to remove per target. 0 = remove all matching.")]
    public int maxBuffsToRemove = 1;

    [Tooltip("Skip targets on the same team as the caster when removing positive buffs, " +
             "or skip enemies when removing negative buffs.")]
    public bool autoFilterByTeam = true;

    [Tooltip("If set, only buffs with these BuffTypes will be removed. Leave empty for generic dispel/cleanse.")]
    public List<BuffType> buffTypesToRemove = new();

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        if (castContext == null || castContext.skillInstance == null) return targets;
        var caster = castContext.caster;
        if (caster == null) return targets;

        if (!castContext.skillInstance.isServer) return targets;

        if (targets == null || targets.Count == 0) return targets;

        var dispelledTargets = new List<UnitController>();

        foreach (var target in targets)
        {
            if (target == null) continue;

            if (autoFilterByTeam)
            {
                bool sameTeam = target.team == caster.team;
                // Offensive dispel targets enemies; cleanse targets allies
                if (mode == DispelMode.RemovePositive && sameTeam) continue;
                if (mode == DispelMode.RemoveNegative && !sameTeam) continue;
            }

            var mediator = target.unitMediator;
            if (mediator == null) continue;

            var buffSystem = mediator.Buffs;
            if (buffSystem == null) continue;

            bool wantNegative = mode == DispelMode.RemoveNegative;


            var matching = buffSystem.ActiveBuffs
                .Where(b =>
                    b.BuffType != null &&
                    b.BuffType.IsNegative == wantNegative &&
                    b.BuffType.IsDispellable &&
                    (buffTypesToRemove == null || buffTypesToRemove.Count == 0 || buffTypesToRemove.Contains(b.BuffType))
                )
                .ToList();

            int removeCount = maxBuffsToRemove > 0
                ? Mathf.Min(maxBuffsToRemove, matching.Count)
                : matching.Count;

            if (removeCount == 0) continue;

            for (int i = 0; i < removeCount; i++)
            {
                buffSystem.RemoveBuff(matching[i]);
            }

            dispelledTargets.Add(target);
        }

        return dispelledTargets;
    }
}
