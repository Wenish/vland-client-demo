using System;
using System.Collections.Generic;

public class StatSystem
{
    private UnitMediator mediator;
    public event Action<StatType> OnStatChanged;
    private Dictionary<StatType, float> baseStats = new();
    private List<StatModifier> modifiers = new();

    public StatSystem(UnitMediator mediator)
    {
        this.mediator = mediator;

        // Example base stats
        baseStats[StatType.Health] = this.mediator.UnitController.maxHealth;
        baseStats[StatType.MovementSpeed] = this.mediator.UnitController.moveSpeed;
        baseStats[StatType.Shield] = this.mediator.UnitController.maxShield;
        baseStats[StatType.TurnSpeed] = 1f;
        baseStats[StatType.DamageReduction] = 0f;
        baseStats[StatType.AttackSpeed] = 1f;
        baseStats[StatType.AttackPower] = 10f;
    }

    public void SetBaseStat(StatType stat, float value)
    {
        if (baseStats.ContainsKey(stat))
        {
            baseStats[stat] = value;
            OnStatChanged?.Invoke(stat);
        }
        else
        {
            throw new ArgumentException($"Stat {stat} does not exist.");
        }
    }

    public void ApplyModifier(StatModifier mod)
    {
        modifiers.Add(mod);
        OnStatChanged?.Invoke(mod.Type);
    }

    public void RemoveModifier(StatModifier mod)
    {
        modifiers.Remove(mod);
        OnStatChanged?.Invoke(mod.Type);
    }

    public float GetStat(StatType stat)
    {
        float value = baseStats.ContainsKey(stat) ? baseStats[stat] : 0;

        foreach (var mod in modifiers)
        {
            if (mod.Type != stat) continue;

            if (mod.ModifierType == ModifierType.Flat)
                value += mod.Value;
            else if (mod.ModifierType == ModifierType.Percent)
                value *= mod.Value;
        }

        return value;
    }
}
