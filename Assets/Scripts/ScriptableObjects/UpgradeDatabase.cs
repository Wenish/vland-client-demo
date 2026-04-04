using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrades/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [SerializeField]
    private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    private Dictionary<string, UpgradeDefinition> _lookup;

    private void EnsureLookup()
    {
        if (_lookup != null)
        {
            return;
        }

        _lookup = new Dictionary<string, UpgradeDefinition>();
        foreach (var upgrade in upgrades)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.upgradeId))
            {
                continue;
            }

            _lookup[upgrade.upgradeId] = upgrade;
        }
    }

    public bool TryGetUpgrade(string upgradeId, out UpgradeDefinition upgrade)
    {
        EnsureLookup();
        return _lookup.TryGetValue(upgradeId, out upgrade);
    }

    public bool ContainsUpgrade(string upgradeId)
    {
        EnsureLookup();
        return _lookup.ContainsKey(upgradeId);
    }
}
