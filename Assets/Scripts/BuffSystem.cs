using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffSystem
{
    private readonly UnitMediator _mediator;
    private readonly List<Buff> _active = new();

    public BuffSystem(UnitMediator mediator)
    {
        _mediator = mediator;
    }

    public void AddBuff(Buff buff)
    {
        if (buff.IsUnique)
        {
            // find and remove any existing buff with the same Id
            var existing = _active.FirstOrDefault(b => b.BuffId == buff.BuffId);
            if (existing != null)
            {
                Debug.Log($"Overwriting unique buff {buff.BuffId} on {_mediator.UnitController.name}");
                RemoveBuff(existing);
            }
        }

        _active.Add(buff);
        Debug.Log($"Buff {buff.BuffId} applied to {_mediator.UnitController.name}");
        buff.OnApply(_mediator);
    }

    public void RemoveBuff(Buff buff)
    {
        if (_active.Remove(buff))
        {
            Debug.Log($"Buff {buff.BuffId} removed from {_mediator.UnitController.name}");
            buff.OnRemove(_mediator);
        }
        else
        {
            Debug.LogWarning($"Tried to remove buff {buff.BuffId}, but it wasn’t active.");
        }
    }

    public void Update(float deltaTime)
    {
        if (_mediator.UnitController.IsDead)
        {
            // clear all buffs if dead
            foreach (var b in _active.ToList())
                RemoveBuff(b);
            return;
        }

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var b = _active[i];
            if (b.Update(deltaTime, _mediator))
                RemoveBuff(b);
        }
    }
}