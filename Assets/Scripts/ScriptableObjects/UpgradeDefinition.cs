using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Game/Upgrades/Upgrade")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string upgradeId = "upgrade_default";
    public string displayName = "New Upgrade";
    [TextArea]
    public string description;

    [Header("Cost and Unlock")]
    [Min(0)]
    public int baseGoldCost = 100;
    [Min(1)]
    public int minWaveToUnlock = 1;

    [Header("Purchase Limits")]
    [Tooltip("-1 means unlimited purchases per player per match.")]
    public int maxPurchasesPerPlayer = 1;

    [Header("Buff Configuration")]
    public string buffId = "upgrade_buff";
    public BuffType buffType;
    public UniqueMode uniqueMode = UniqueMode.Global;
    public float duration = Mathf.Infinity;
    [Tooltip("If enabled, this purchased upgrade remains active after the unit dies.")]
    public bool persistsThroughDeath = true;
    public List<StatModifier> statModifiers = new List<StatModifier>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public bool IsUnlockedAtWave(int wave)
    {
        return wave >= minWaveToUnlock;
    }

    public BuffStat CreateBuff(UnitMediator caster)
    {
        var clonedModifiers = new List<StatModifier>(statModifiers.Count);
        foreach (var modifier in statModifiers)
        {
            if (modifier == null)
            {
                continue;
            }

            clonedModifiers.Add(new StatModifier
            {
                Type = modifier.Type,
                ModifierType = modifier.ModifierType,
                Value = modifier.Value
            });
        }

        var resolvedBuffId = string.IsNullOrWhiteSpace(buffId) ? upgradeId : buffId;
        var buff = new BuffStat(resolvedBuffId, duration, clonedModifiers, uniqueMode, caster, buffType)
        {
            PersistsThroughDeath = persistsThroughDeath
        };
        return buff;
    }
}
