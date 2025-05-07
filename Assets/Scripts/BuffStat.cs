using System.Collections.Generic;

public class BuffStat : Buff
{
    private readonly List<StatModifier> _mods;

    public BuffStat(string buffId, float duration, bool isUnique, List<StatModifier> mods)
        : base(buffId, duration, isUnique)
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