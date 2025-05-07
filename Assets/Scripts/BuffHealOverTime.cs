using UnityEngine;

public class BuffHealOverTime : PeriodicBuff
{
    private readonly float _healAmount;
    private readonly ModifierType _modifierType = ModifierType.Flat;

    public BuffHealOverTime(
        string buffId,
        float duration,
        bool isUnique,
        float tickInterval,
        float healAmount
    ) : base(
        buffId,
        duration,
        tickInterval,
        isUnique
      )
    {
        _healAmount = healAmount;
    }

    public override void OnApply(UnitMediator mediator)
    {
        base.OnApply(mediator);
        // maybe show VFX, etc.
    }

    public override void OnRemove(UnitMediator mediator)
    {
        base.OnApply(mediator);
        // cleanup VFX, if any
    }

    private float _residual;

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