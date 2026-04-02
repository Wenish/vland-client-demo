using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens a recast window after Phase 1 without locking the unit.
/// The unit remains free to move, attack, and use other abilities.
/// Pressing the same skill button again within the window fires onRecast.
/// Uses the existing CastContext.SignalTrigger / ConsumePendingTrigger mechanism.
/// </summary>
[CreateAssetMenu(
    fileName = "SkillEffectMechanicRecastWindow",
    menuName = "Game/Skills/Effects/Mechanic/Recast Window")]
public class SkillEffectMechanicRecastWindowData : SkillEffectData
{
    [Header("Window")]
    [Tooltip("Seconds the player has to press the skill again before the window closes.")]
    [Min(0.1f)]
    public float windowDuration = 1.8f;

    [Tooltip("Minimum seconds after window opens before a recast is accepted. Prevents accidental immediate recast from the initial press.")]
    [Min(0f)]
    public float minDelayBeforeRecast = 0.15f;

    [Header("Cancellation")]
    [Tooltip("Close the window early if the caster is knocked up.")]
    public bool cancelOnKnockup = true;

    [Tooltip("Close the window early if the caster dies.")]
    public bool cancelOnDeath = true;

    [Header("Branch Effects")]
    [Tooltip("Effect chain to execute when the player recasts within the window.")]
    public SkillEffectChainData onRecast;

    [Tooltip("Optional effect chain to execute when the window expires without a recast.")]
    public SkillEffectChainData onExpire;

    [Header("Timing")]
    [Tooltip("Optional delay after branch execution before continuing the parent chain.")]
    [Min(0f)]
    public float postDelaySeconds = 0f;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(CastContext ctx, List<UnitController> targets, Action<List<UnitController>> onComplete)
    {
        if (ctx?.caster == null || ctx.skillInstance == null)
        {
            onComplete(targets);
            yield break;
        }

        var caster = ctx.caster;

        // Consume any pending trigger that was already queued from the initial press
        // so we don't immediately fire the recast.
        ctx.ConsumePendingTrigger();

        // --- Anti-misfire delay ---
        float elapsed = 0f;
        while (elapsed < minDelayBeforeRecast)
        {
            if (ctx.IsCancelled) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Recast window loop ---
        bool recastFired = false;
        while (elapsed < windowDuration)
        {
            if (ctx.IsCancelled) yield break;

            if (cancelOnDeath && caster.IsDead) break;
            if (cancelOnKnockup && caster.IsKnockedUp) break;

            if (ctx.ConsumePendingTrigger())
            {
                recastFired = true;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Branch ---
        if (recastFired && onRecast != null)
        {
            yield return ctx.skillInstance.StartCoroutine(onRecast.ExecuteCoroutine(ctx, targets));
        }
        else if (!recastFired && onExpire != null)
        {
            yield return ctx.skillInstance.StartCoroutine(onExpire.ExecuteCoroutine(ctx, targets));
        }

        if (postDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(postDelaySeconds);
        }

        onComplete(targets);
    }
}
