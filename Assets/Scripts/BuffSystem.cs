using System;
using System.Collections.Generic;
using System.Linq;

public class BuffSystem
{
    private readonly UnitMediator _target;
    private readonly List<Buff> _active = new();

    public IReadOnlyList<Buff> ActiveBuffs => _active;
    public event Action<Buff> OnBuffAdded;
    public event Action<Buff> OnBuffUpdated;
    public event Action<Buff> OnBuffRemoved;

    public BuffSystem(UnitMediator target)
    {
        _target = target;
    }

    public Buff GetBuffById(string buffId)
    {
        return _active.FirstOrDefault(b => b.BuffId == buffId);
    }

    public void AddBuff(Buff buff)
    {
        // Capture tick remainder from any displaced periodic buff so the new one can inherit it
        float? donorTickRemainder = null;
        float? donorTickInterval = null;

        // 1) Global uniqueness?
        if (buff.UniqueMode == UniqueMode.Global)
        {
            var duplicates = _active.Where(b => b.BuffId == buff.BuffId).ToList();
            // Capture from first periodic duplicate, if any
            var donor = duplicates.OfType<PeriodicBuff>().FirstOrDefault();
            if (donor != null)
            {
                donorTickRemainder = donor.TickRemainder;
                donorTickInterval = donor.TickInterval;
            }

            foreach (var old in duplicates)
                RemoveBuff(old);
        }
        // 2) Per-caster uniqueness?
        else if (buff.UniqueMode == UniqueMode.PerCaster)
        {
            var old = _active
                .FirstOrDefault(b => b.BuffId == buff.BuffId
                                  && b.Caster == buff.Caster);
            if (old != null)
            {
                if (old is PeriodicBuff oldPeriodic)
                {
                    donorTickRemainder = oldPeriodic.TickRemainder;
                    donorTickInterval = oldPeriodic.TickInterval;
                }
                RemoveBuff(old);
            }
        }

        // If applicable, transfer tick timing to the new periodic buff before applying
        if (buff is PeriodicBuff newPeriodic && donorTickRemainder.HasValue)
        {
            // Transfer only if intervals match (within epsilon); otherwise keep the new schedule
            if (!donorTickInterval.HasValue || Math.Abs(newPeriodic.TickInterval - donorTickInterval.Value) < 0.0001f)
            {
                newPeriodic.TickRemainder = donorTickRemainder.Value;
                if (newPeriodic.TickOnApply == false) {
                    newPeriodic.SetDuration(newPeriodic.Duration + (newPeriodic.TickInterval - donorTickRemainder.Value));
                }
            }
        }

        // 3) Now add the new one
        _active.Add(buff);
        buff.OnApply(_target);
        OnBuffAdded?.Invoke(buff);
    }

    public void RemoveBuff(Buff buff)
    {   
        if (_active.Remove(buff))
        {
            buff.OnRemove(_target);
            OnBuffRemoved?.Invoke(buff);
        }
    }

    public void Update(float deltaTime)
    {
        if (_target.UnitController.IsDead)
        {
            // clear all buffs if dead
            foreach (var b in _active.ToList())
                RemoveBuff(b);
            return;
        }

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var b = _active[i];
            var isExpired = b.Update(deltaTime, _target);
            if (isExpired)
            {
                RemoveBuff(b);
                continue;
            }

            OnBuffUpdated?.Invoke(b);
        }
    }
}