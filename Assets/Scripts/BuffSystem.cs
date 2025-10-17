using System;
using System.Collections.Generic;
using System.Linq;

public class BuffSystem
{
    private readonly UnitMediator _target;
    private readonly List<Buff> _active = new();

    public IReadOnlyList<Buff> ActiveBuffs => _active;
    public event Action<Buff> OnBuffAdded;
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
        // 1) Global uniqueness?
        if (buff.UniqueMode == UniqueMode.Global)
        {
            foreach (var old in _active.Where(b => b.BuffId == buff.BuffId).ToList())
                RemoveBuff(old);
        }
        // 2) Per-caster uniqueness?
        else if (buff.UniqueMode == UniqueMode.PerCaster)
        {
            var old = _active
                .FirstOrDefault(b => b.BuffId == buff.BuffId
                                  && b.Caster == buff.Caster);
            if (old != null)
                RemoveBuff(old);
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
            if (b.Update(deltaTime, _target))
                RemoveBuff(b);
        }
    }
}