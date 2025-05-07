using System;

public abstract class Buff
{
    public string BuffId { get; }
    public float Duration { get; }
    public bool IsUnique { get; }

    private float _elapsed;

    protected Buff(string buffId, float duration, bool isUnique = false)
    {
        if (string.IsNullOrWhiteSpace(buffId))
            throw new ArgumentException("Buff must have a non-empty Id.", nameof(buffId));

        BuffId = buffId;
        Duration = duration;
        IsUnique = isUnique;
    }

    public virtual void OnApply(UnitMediator mediator) { }

    public virtual void OnRemove(UnitMediator mediator) { }

    public virtual bool Update(float deltaTime, UnitMediator mediator)
    {
        _elapsed += deltaTime;
        if (_elapsed >= Duration)
            return true;
        return false;
    }
}