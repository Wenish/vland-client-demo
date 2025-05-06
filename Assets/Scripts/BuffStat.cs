public class BuffStat : Buff
{
    public StatType StatType;
    public ModifierType ModifierType;
    public float Value;
    private StatModifier _statModifier;
    public BuffStat(float duration, StatType statType, ModifierType modifierType, float value)
    {
        BuffId = "BuffStat";
        Duration = duration;
        StatType = statType;
        ModifierType = modifierType;
        Value = value;

        _statModifier = new StatModifier
        {
            Type = StatType,
            Value = Value,
            ModifierType = ModifierType
        };
    }
    public override void OnApply(UnitMediator mediator)
    {
        base.OnApply(mediator);
        mediator.Stats.ApplyModifier(_statModifier);
    }
    public override void OnRemove(UnitMediator mediator)
    {
        base.OnRemove(mediator);
        mediator.Stats.RemoveModifier(_statModifier);
    }
}