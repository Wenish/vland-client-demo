using System;
using System.Collections.Generic;
using UnityEngine;

public enum VendorTab : byte
{
    Buy = 0,
    Sell = 1,
    Upgrades = 2
}

[Serializable]
public class VendorBuyEntry
{
    public string entryId;
    public WeaponData weapon;
    [Min(0)]
    public int goldCost;
    [Tooltip("0 or less means unlimited stock.")]
    public int stock;

    public string ResolvedId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(entryId))
                return entryId;
            return weapon != null ? weapon.weaponName : string.Empty;
        }
    }

    public bool IsUnlimitedStock => stock <= 0;
}

[CreateAssetMenu(fileName = "Vendor", menuName = "Game/Vendors/Vendor")]
public class VendorDefinition : ScriptableObject
{
    [Header("Identity")]
    public string vendorId = "vendor_default";
    public string displayName = "Trader";
    public string subtitle = "Weapons and upgrades";
    public Texture2D portrait;

    [Header("Catalog")]
    public VendorTab defaultTab = VendorTab.Buy;
    public List<VendorBuyEntry> buyEntries = new List<VendorBuyEntry>();
    public List<UpgradeDefinition> upgradeEntries = new List<UpgradeDefinition>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public bool TryGetBuyEntry(string id, out VendorBuyEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(id) || buyEntries == null)
            return false;

        foreach (var candidate in buyEntries)
        {
            if (candidate == null || candidate.weapon == null)
                continue;
            if (candidate.ResolvedId == id)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryGetUpgrade(string upgradeId, out UpgradeDefinition upgrade)
    {
        upgrade = null;
        if (string.IsNullOrWhiteSpace(upgradeId) || upgradeEntries == null)
            return false;

        foreach (var candidate in upgradeEntries)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.upgradeId))
                continue;
            if (candidate.upgradeId == upgradeId)
            {
                upgrade = candidate;
                return true;
            }
        }

        return false;
    }
}
