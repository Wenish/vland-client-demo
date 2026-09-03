using System;
using System.Collections.Generic;
using UnityEngine;

public enum VendorTab : byte
{
    Buy = 0,
    Sell = 1,
    Upgrades = 2
}

public enum VendorCatalogSource : byte
{
    BuyEntries = 0,
    ItemDatabase = 1
}

[Serializable]
public class VendorBuyEntry
{
    public string entryId;
    [Min(0)]
    public int goldCost;
    [Tooltip("Initial listed stock copied into a runtime session. 0 or less means unlimited. Do not treat this asset as live NPC inventory.")]
    public int stock;

    public string ResolvedId => entryId ?? string.Empty;

    public bool IsUnlimitedStock => stock <= 0;
}

[Serializable]
public class VendorSellEntry
{
    public string entryId;
    public string displayName;
    [Min(0)]
    public int goldValue;
}

[CreateAssetMenu(fileName = "Vendor", menuName = "Game/Vendors/Vendor")]
public class VendorDefinition : ScriptableObject
{
    [Header("Identity")]
    public string vendorId = "vendor_default";
    public string displayName = "Trader";
    public string subtitle = "Weapons and upgrades";
    public Texture2D portrait;

    [Header("Tabs")]
    [Tooltip("Which tab opens first. Disabled tabs are skipped.")]
    public VendorTab defaultTab = VendorTab.Buy;
    public bool showBuyTab = true;
    public bool showSellTab = true;
    public bool showUpgradesTab = true;

    [Header("Catalog")]
    [Tooltip("Template offers. Live stock and NPC gold belong on IVendorSession, not this asset.")]
    public List<VendorBuyEntry> buyEntries = new List<VendorBuyEntry>();
    public List<VendorSellEntry> sellEntries = new List<VendorSellEntry>();
    public List<UpgradeDefinition> upgradeEntries = new List<UpgradeDefinition>();

    [Header("Catalog Source")]
    [Tooltip("ItemDatabase lists every ItemDefinition (debug stall). BuyEntries are unused for in-match weapon shops.")]
    public VendorCatalogSource catalogSource = VendorCatalogSource.BuyEntries;

    public bool UsesItemCatalog => catalogSource == VendorCatalogSource.ItemDatabase;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public bool IsTabEnabled(VendorTab tab)
    {
        switch (tab)
        {
            case VendorTab.Buy:
                return showBuyTab;
            case VendorTab.Sell:
                return showSellTab;
            case VendorTab.Upgrades:
                return showUpgradesTab;
            default:
                return false;
        }
    }

    public VendorTab ResolveDefaultTab()
    {
        if (IsTabEnabled(defaultTab))
            return defaultTab;
        if (showBuyTab)
            return VendorTab.Buy;
        if (showUpgradesTab)
            return VendorTab.Upgrades;
        if (showSellTab)
            return VendorTab.Sell;
        return defaultTab;
    }

    public bool TryGetBuyEntry(string id, out VendorBuyEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(id) || buyEntries == null)
            return false;

        foreach (var candidate in buyEntries)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.ResolvedId))
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
