using System;

public abstract class PeriodicBuff : Buff
{
    private readonly float _tickInterval;
    private float _tickTimer;
    protected PeriodicBuff(
        string buffId,
        float duration,
        float tickInterval,
        UniqueMode     uniqueMode   = UniqueMode.None,
        UnitMediator   caster       = null
    ) : base(buffId, duration, uniqueMode, caster)
    {
        if (tickInterval <= 0f)
            throw new ArgumentException("Tick interval must be > 0.", nameof(tickInterval));
        _tickInterval = tickInterval;
    }
    public abstract void OnTick(UnitMediator mediator);

    public override bool Update(float deltaTime, UnitMediator mediator)
    {
        // advance the base elapsed time & see if we’ve expired
        bool expired = base.Update(deltaTime, mediator);

        // accumulate tick timer and fire as many OnTick calls as needed
        _tickTimer += deltaTime;
        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            OnTick(mediator);
        }

        return expired;
    }
}