using System;
using UnityEngine;

public abstract class Buff
{
    public string BuffId { get; }
    public float Duration { get; }
    public UniqueMode UniqueMode { get; }
    public UnitMediator Caster { get; }

    public string SkillName { get; set; }
    public event Action OnRemoved;

    private float _elapsed;

    public float Elapsed => _elapsed;
    public float Remaining => Mathf.Max(0f, Duration - _elapsed);
    public float NormalizedRemaining => Duration <= 0f ? 0f : Mathf.Clamp01((Duration - _elapsed) / Duration);

    protected Buff(string buffId, float duration, UniqueMode uniqueMode = UniqueMode.None,
        UnitMediator caster = null)
    {
        if (string.IsNullOrWhiteSpace(buffId))
            throw new ArgumentException("Buff must have a non-empty Id.", nameof(buffId));

        if (uniqueMode == UniqueMode.PerCaster && caster == null)
            throw new ArgumentException("PerCaster buffs need a non-null caster", nameof(caster));

        BuffId = buffId;
        Duration = duration;
        UniqueMode = uniqueMode;
        Caster = caster;
    }

    public virtual void OnApply(UnitMediator mediator) { }

    public virtual void OnRemove(UnitMediator mediator)
    {
        OnRemoved?.Invoke();
    }

    public virtual bool Update(float deltaTime, UnitMediator mediator)
    {
        _elapsed += deltaTime;

        var hasDurationElapsed = _elapsed >= Duration;
        return hasDurationElapsed;
    }
}

public enum UniqueMode
{
    None,
    Global,     // only one buff.Id on the target
    PerCaster   // only one buff.Id *from the same Caster* on the target
}