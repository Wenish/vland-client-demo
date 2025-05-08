using System.Collections.Generic;

public class BuffStat : Buff
{
    private readonly List<StatModifier> _mods;

    public BuffStat(
        string buffId,
        float duration,
        List<StatModifier> mods,
        UniqueMode           uniqueMode = UniqueMode.None,
        UnitMediator         caster     = null)
        : base(buffId, duration, uniqueMode, caster)
    {
        _mods = mods;
    }
    public override void OnApply(UnitMediator mediator)
    {
        base.OnApply(mediator);
        foreach (var m in _mods) {
            mediator.Stats.ApplyModifier(m);
        }
    }
    public override void OnRemove(UnitMediator mediator)
    {
        base.OnRemove(mediator);
        foreach (var m in _mods) {
            mediator.Stats.RemoveModifier(m);
        }
    }
}