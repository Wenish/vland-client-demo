using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeStationInstanceConfig", menuName = "Game/Upgrades/Station Instance Config")]
public class UpgradeStationInstanceConfig : ScriptableObject
{
    [Header("Availability")]
    public bool isLocked;
    [Tooltip("-1 means no additional wave gate.")]
    public int minWaveOverride = -1;

    [Header("Pricing")]
    [Min(0.1f)]
    public float costMultiplier = 1f;

    [Header("Offer Overrides")]
    public bool useOfferOverrides;
    public List<UpgradeDefinition> allowedUpgrades = new List<UpgradeDefinition>();

    public bool IsUnlockedAtWave(int wave)
    {
        if (isLocked)
        {
            return false;
        }

        if (minWaveOverride > 0 && wave < minWaveOverride)
        {
            return false;
        }

        return true;
    }

    public bool IsUpgradeAllowed(UpgradeDefinition upgrade)
    {
        if (!useOfferOverrides)
        {
            return true;
        }

        return allowedUpgrades.Contains(upgrade);
    }
}
