using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SkillEffectMechanicManualChannel",
    menuName = "Game/Skills/Effects/Mechanic/ManualChannel"
)]
public class SkillEffectMechanicManualChannelData : SkillEffectData
{
    public enum TriggerInputType
    {
        Attack,
        SkillButton,
    }

    [Tooltip("Which input triggers the skill effect chain during the channel.")]
    public TriggerInputType triggerInput = TriggerInputType.SkillButton;

    [Tooltip("Maximum number of times the effect chain can be triggered during the channel.")]
    [Min(1)]
    public int maxTriggers = 3;

    [Tooltip("Minimum seconds between consecutive triggers.")]
    [Min(0f)]
    public float triggerCooldown = 0.5f;

    [Tooltip("Maximum seconds the channel lasts. Ends early if all triggers are used.")]
    [Min(0f)]
    public float maxChannelDuration = 5f;

    [Tooltip("Percentage (0–1) of the caster's base move speed allowed during channel.")]
    [Range(0f, 1f)]
    public float moveSpeedPercent = 0f;

    [Tooltip("Percentage (0-1) of the caster's base turn speed allowed during channel.")]
    [Range(0f, 1f)]
    public float turnSpeedPercent = 0f;

    [Header("Trigger Effect")]
    [Tooltip("The effect chain to execute each time the player triggers.")]
    public SkillEffectChainData triggerEffect;

    public override SkillEffectType EffectType => SkillEffectType.Mechanic;

    public override IEnumerator Execute(
        CastContext ctx,
        List<UnitController> targets,
        Action<List<UnitController>> onComplete
    )
    {
        var caster = ctx.caster;
        caster.unitActionState.SetUnitActionState(
            UnitActionState.ActionType.Channeling,
            NetworkTime.time,
            maxChannelDuration,
            ctx.skillInstance.skillName
        );

        // Apply move/turn speed modifiers
        var moveSpeedModifier = new StatModifier
        {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = moveSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(moveSpeedModifier);

        var turnSpeedModifier = new StatModifier
        {
            Type = StatType.TurnSpeed,
            ModifierType = ModifierType.Percent,
            Value = turnSpeedPercent
        };
        caster.unitMediator.Stats.ApplyModifier(turnSpeedModifier);

        float elapsed = 0f;
        int triggersUsed = 0;
        float timeSinceLastTrigger = triggerCooldown; // allow immediate first trigger
        bool prevFire1 = false;
        Coroutine activeTriggerCoroutine = null;

        Debug.Log($"[ManualChannel] Started | triggerInput={triggerInput} maxTriggers={maxTriggers} triggerCooldown={triggerCooldown} maxDuration={maxChannelDuration} hasEffect={triggerEffect != null}");

        // For Attack trigger mode, find the PlayerInput that owns this caster
        PlayerInput playerInput = null;
        if (triggerInput == TriggerInputType.Attack)
        {
            var allInputs = UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude);
            foreach (var pi in allInputs)
            {
                if (pi.myUnit == caster.gameObject)
                {
                    playerInput = pi;
                    break;
                }
            }
            if (playerInput != null)
                prevFire1 = playerInput.IsPressingFire1;
            else
                Debug.LogWarning($"[ManualChannel] No PlayerInput found for caster {caster.name}");
        }

        while (elapsed < maxChannelDuration && triggersUsed < maxTriggers)
        {
            if (ctx.IsCancelled)
            {
                Debug.Log($"[ManualChannel] Cancelled at elapsed={elapsed:F2}");
                break;
            }

            elapsed += Time.deltaTime;
            timeSinceLastTrigger += Time.deltaTime;

            bool shouldTrigger = false;

            if (triggerInput == TriggerInputType.SkillButton)
            {
                shouldTrigger = ctx.ConsumePendingTrigger();
                if (shouldTrigger)
                    Debug.Log($"[ManualChannel] SkillButton trigger consumed at elapsed={elapsed:F2}");
            }
            else // Attack - detect rising edge of fire1
            {
                if (playerInput != null)
                {
                    bool currentFire1 = playerInput.IsPressingFire1;
                    if (currentFire1 && !prevFire1)
                    {
                        shouldTrigger = true;
                        Debug.Log($"[ManualChannel] Attack trigger (fire1 press) at elapsed={elapsed:F2}");
                    }
                    prevFire1 = currentFire1;
                }
            }

            if (shouldTrigger)
            {
                if (timeSinceLastTrigger < triggerCooldown)
                    Debug.Log($"[ManualChannel] Trigger blocked by cooldown (timeSince={timeSinceLastTrigger:F2} < cd={triggerCooldown:F2})");
                else if (triggerEffect == null)
                    Debug.Log($"[ManualChannel] Trigger blocked: triggerEffect is null");
                else
                {
                    triggersUsed++;
                    timeSinceLastTrigger = 0f;
                    Debug.Log($"[ManualChannel] Firing trigger {triggersUsed}/{maxTriggers}");
                    activeTriggerCoroutine = ctx.skillInstance.StartCoroutine(triggerEffect.ExecuteCoroutine(ctx, targets));
                }
            }

            yield return null;
        }

        Debug.Log($"[ManualChannel] Ended | elapsed={elapsed:F2} triggersUsed={triggersUsed} cancelled={ctx.IsCancelled}");

        // Wait for the last trigger effect to finish before ending the channel.
        // On interrupt (IsCancelled), skip the wait — the trigger chain unwinds
        // itself via the IsCancelled checks in SkillEffectNodeData / SkillEffectChainData.
        if (activeTriggerCoroutine != null && !ctx.IsCancelled)
            yield return activeTriggerCoroutine;

        caster.unitActionState.SetUnitActionStateToIdle();

        // Remove modifiers
        caster.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        caster.unitMediator.Stats.RemoveModifier(turnSpeedModifier);

        if (ctx.IsCancelled) yield break;

        onComplete(targets);
    }
}
