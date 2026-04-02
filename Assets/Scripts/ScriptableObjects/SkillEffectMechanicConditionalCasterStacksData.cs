using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicConditionalCasterStacks",
    menuName = "Game/Skills/Effects/Mechanic/Conditional Caster Stacks")]
public class SkillEffectMechanicConditionalCasterStacksData : SkillEffectData
{
    public enum ComparisonMode
    {
        AtLeast,
        AtMost,
        Exactly,
    }

    [Header("Stack Gate")]
    [Tooltip("Buff id used as stack token on the caster.")]
    public string buffId = "echo_cleave_stack";

    [Min(0)]
    public int stackThreshold = 3;

    public ComparisonMode comparisonMode = ComparisonMode.AtLeast;

    [Header("Consumption")]
    [Tooltip("If true, consume stacks from the caster when the condition passes.")]
    public bool consumeStacksOnSuccess = false;

    [Tooltip("How many stacks to consume. Set to 0 to consume stackThreshold amount.")]
    [Min(0)]
    public int stacksToConsume = 0;

    [Header("Branch Effects")]
    [Tooltip("Executed when stack condition passes.")]
    public SkillEffectChainData onConditionMet;

    [Tooltip("Optional fallback effect when stack condition fails.")]
    public SkillEffectChainData onConditionFailed;

    [Header("Timing")]
    [Tooltip("Optional delay after branch execution before continuing node chain.")]
    [Min(0f)]
    public float postDelaySeconds = 0f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(CastContext castContext, List<UnitController> targets, Action<List<UnitController>> onComplete)
    {
        if (castContext?.caster?.unitMediator?.Buffs == null || castContext.skillInstance == null)
        {
            onComplete(targets);
            yield break;
        }

        var buffSystem = castContext.caster.unitMediator.Buffs;
        var matchingStacks = buffSystem.ActiveBuffs.Where(b => b.BuffId == buffId).ToList();
        var stackCount = matchingStacks.Count;
        var conditionMet = IsConditionMet(stackCount);

        if (conditionMet)
        {
            if (consumeStacksOnSuccess && stackCount > 0)
            {
                var consumeCount = stacksToConsume > 0 ? stacksToConsume : stackThreshold;
                consumeCount = Mathf.Min(consumeCount, stackCount);
                for (int i = 0; i < consumeCount; i++)
                {
                    buffSystem.RemoveBuff(matchingStacks[i]);
                }
            }

            if (onConditionMet != null)
            {
                yield return castContext.skillInstance.StartCoroutine(onConditionMet.ExecuteCoroutine(castContext, targets));
            }
        }
        else
        {
            if (onConditionFailed != null)
            {
                yield return castContext.skillInstance.StartCoroutine(onConditionFailed.ExecuteCoroutine(castContext, targets));
            }
        }

        if (postDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(postDelaySeconds);
        }

        onComplete(targets);
    }

    private bool IsConditionMet(int stackCount)
    {
        return comparisonMode switch
        {
            ComparisonMode.AtLeast => stackCount >= stackThreshold,
            ComparisonMode.AtMost => stackCount <= stackThreshold,
            ComparisonMode.Exactly => stackCount == stackThreshold,
            _ => false,
        };
    }
}