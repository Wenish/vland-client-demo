using System.Collections.Generic;

public abstract class Buff
{
    public string BuffId;
    public float Duration;
    protected float elapsedTime;

    public List<StatModifier> Modifiers = new();
    public bool IsPeriodic;
    public float TickInterval;
    private float tickTimer;

    public virtual void OnApply(UnitMediator mediator) { }
    public virtual void OnRemove(UnitMediator mediator) { }
    public virtual void OnTick(UnitMediator mediator) { }

    public bool Update(float deltaTime, UnitMediator mediator)
    {
        elapsedTime += deltaTime;

        if (IsPeriodic)
        {
            tickTimer += deltaTime;
            if (tickTimer >= TickInterval)
            {
                tickTimer = 0f;
                OnTick(mediator);
            }
        }

        return elapsedTime >= Duration;
    }
}
