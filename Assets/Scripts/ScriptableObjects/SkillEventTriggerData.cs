using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base asset for event-driven skill triggers.
/// Derived assets subscribe to a specific GameEvent type and decide whether to fire.
/// When fired, their configured effect chain executes using a fresh CastContext.
/// </summary>
public abstract class SkillEventTriggerData : ScriptableObject
{
    [Tooltip("Optional: limit how frequently this trigger can fire per skill instance (seconds). 0 = no limit")]
    public float triggerCooldown = 0f;

    [Tooltip("Effects to execute when this trigger fires.")]
    public SkillEffectChainData onTrigger;

    [Tooltip("If true, only server will subscribe and execute. Recommended for authoritative gameplay.")]
    public bool serverOnly = true;

    /// <summary>
    /// Called by ReactiveTriggerRunner to set up subscriptions for this trigger.
    /// Implementations should call runner.Subscribe<T>(...) for their event type and keep no runtime state here.
    /// </summary>
    public abstract void Subscribe(ReactiveTriggerRunner runner);

    /// <summary>
    /// Optional hook for editors/helpers to preview what targets an event would yield.
    /// </summary>
    public virtual IEnumerable<UnitController> PreviewTargets(UnitController caster)
    {
        yield break;
    }
}
