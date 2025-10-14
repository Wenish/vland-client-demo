using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using UnityEngine;

/// <summary>
/// Attachable runtime that wires SkillEventTriggerData to the global EventManager and executes effect chains.
/// Expected to live alongside NetworkedSkillInstance on the spawned skill object.
/// </summary>
public class ReactiveTriggerRunner : NetworkBehaviour
{
    [SerializeField]
    private NetworkedSkillInstance _skill;

    [SerializeField]
    private List<SkillEventTriggerData> _triggers = new();

    private readonly Dictionary<SkillEventTriggerData, double> _lastFire = new();

    public void Initialize(NetworkedSkillInstance skill, IEnumerable<SkillEventTriggerData> triggers)
    {
        _skill = skill;
        _triggers = triggers != null ? new List<SkillEventTriggerData>(triggers) : new List<SkillEventTriggerData>();
        // Subscribe now if appropriate
        SubscribeAll();
    }

    private void OnEnable()
    {
        // In case assigned via inspector
        if (_skill == null) _skill = GetComponent<NetworkedSkillInstance>();
        if (_triggers != null && _triggers.Count > 0)
        {
            SubscribeAll();
        }
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    private void SubscribeAll()
    {
        if (_triggers == null) return;
        foreach (var t in _triggers)
        {
            if (t == null) continue;
            if (t.serverOnly && !isServer) continue;
            t.Subscribe(this);
        }
    }

    private readonly List<Action> _unsubscribers = new();
    private void UnsubscribeAll()
    {
        for (int i = _unsubscribers.Count - 1; i >= 0; i--)
        {
            try { _unsubscribers[i]?.Invoke(); }
            catch { /* ignore */ }
        }
        _unsubscribers.Clear();
    }

    /// <summary>
    /// Gate for concrete triggers to call when they want to execute their chain.
    /// Applies per-trigger cooldown and starts the effect chain coroutine.
    /// </summary>
    public void Fire(SkillEventTriggerData trigger, List<UnitController> targets)
    {
        if (trigger == null || trigger.onTrigger == null || _skill == null) return;

        // Cooldown gating (authority uses NetworkTime)
        if (trigger.triggerCooldown > 0f)
        {
            _lastFire.TryGetValue(trigger, out var last);
            if (NetworkTime.time < last + trigger.triggerCooldown)
                return;
            _lastFire[trigger] = NetworkTime.time;
        }

        var ctx = new CastContext(_skill.Caster, _skill);
        _skill.StartCoroutine(RunChain(trigger.onTrigger, ctx, targets));
    }

    private IEnumerator RunChain(SkillEffectChainData chain, CastContext ctx, List<UnitController> targets)
    {
        yield return _skill.StartCoroutine(chain.ExecuteCoroutine(ctx, targets));
    }

    // Convenience for subscribing to global events with method groups
    public void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        if (EventManager.Instance == null) return;
        EventManager.Instance.Subscribe(handler);
        _unsubscribers.Add(() => EventManager.Instance?.Unsubscribe(handler));
    }

    /// <summary>
    /// Convenience for subscribing to plain Action events (e.g., UnitController.OnAttackStart).
    /// Provide how to add and remove the handler; unsubscription is handled automatically on disable.
    /// Example:
    /// Subscribe(() => unit.OnAttackStart += OnAttackStart, () => unit.OnAttackStart -= OnAttackStart);
    /// </summary>
    public void Subscribe(Action addSubscription, Action removeSubscription)
    {
        if (addSubscription == null || removeSubscription == null) return;
        addSubscription();
        _unsubscribers.Add(() => {
            try { removeSubscription(); } catch { /* ignore */ }
        });
    }

    /// <summary>
    /// Convenience overload for Action events where you have the event adder/remover as functions.
    /// This keeps the handler in one place and wires up automatic unsubscription.
    /// Example:
    /// Subscribe(h => unit.OnAttackStart += h, h => unit.OnAttackStart -= h, OnAttackStart);
    /// </summary>
    public void Subscribe(Action<Action> adder, Action<Action> remover, Action handler)
    {
        if (adder == null || remover == null || handler == null) return;
        adder(handler);
        _unsubscribers.Add(() => {
            try { remover(handler); } catch { /* ignore */ }
        });
    }
}
