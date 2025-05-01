using System.Collections.Generic;
using UnityEngine;

public class BuffSystem
{
    private UnitMediator mediator;
    private List<Buff> activeBuffs = new();

    public BuffSystem(UnitMediator mediator)
    {
        this.mediator = mediator;
    }

    public void AddBuff(Buff buff)
    {
        activeBuffs.Add(buff);
        Debug.Log($"Buff {buff.BuffId} added to {mediator.UnitController.name}");

        foreach (var mod in buff.Modifiers)
            mediator.Stats.ApplyModifier(mod);

        buff.OnApply(mediator);
    }

    public void RemoveBuff(Buff buff)
    {
        if (activeBuffs.Remove(buff))
        {
            Debug.Log($"Buff {buff.BuffId} removed from {mediator.UnitController.name}");

            foreach (var mod in buff.Modifiers)
                mediator.Stats.RemoveModifier(mod);

            buff.OnRemove(mediator);
        }
        else
        {
            Debug.LogWarning($"Attempted to remove Buff {buff.BuffId}, but it was not found on {mediator.UnitController.name}");
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            if (mediator.UnitController.IsDead)
            {
                RemoveBuff(buff);
                continue;
            }
            bool finished = buff.Update(deltaTime, mediator);

            if (finished)
            {
                RemoveBuff(buff);
            }
        }
    }
}
