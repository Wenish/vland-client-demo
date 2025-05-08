using UnityEngine;

public class BuffHealOverTime : PeriodicBuff
{
    private readonly float _healAmount;
    private readonly ModifierType _modifierType = ModifierType.Flat;
    private float _residual;

    public BuffHealOverTime(
        string buffId,
        float duration,
        float tickInterval,
        float healAmount,
        ModifierType   modifierType  = ModifierType.Flat,
        UniqueMode     uniqueMode    = UniqueMode.None,
        UnitMediator   caster        = null
    ) : base(
        buffId,
        duration,
        tickInterval,
        uniqueMode,
        caster
      )
    {
        _healAmount = healAmount;
        _modifierType   = modifierType;
    }

    public override void OnApply(UnitMediator mediator)
    {
        base.OnApply(mediator);
        // maybe show VFX, etc.
    }

    public override void OnRemove(UnitMediator mediator)
    {
        base.OnRemove(mediator);
        // cleanup VFX, if any
    }


    public override void OnTick(UnitMediator mediator)
    {
        float rawHeal = (_modifierType == ModifierType.Flat)
            ? _healAmount
            : mediator.UnitController.maxHealth * _healAmount;

        rawHeal += _residual;
        int toHeal = Mathf.FloorToInt(rawHeal);
        _residual = rawHeal - toHeal;

        if (toHeal > 0)
            mediator.UnitController.Heal(toHeal);
    }
}