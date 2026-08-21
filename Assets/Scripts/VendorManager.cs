using Mirror;
using UnityEngine;

public class VendorManager : NetworkBehaviour
{
    public static VendorManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [Server]
    public void TryTransact(PlayerController buyer, string vendorId, VendorTab tab, string entryId, out bool success, out string message, out int timesBought)
    {
        success = false;
        message = string.Empty;
        timesBought = 0;

        if (buyer == null)
        {
            message = "Buyer is missing.";
            return;
        }

        var zone = buyer.InteractionZone;
        if (zone == null || zone.InteractionType != InteractionType.OpenVendor)
        {
            message = "You are not trading.";
            return;
        }

        var catalog = zone.VendorCatalog;
        if (catalog == null || catalog.vendorId != vendorId)
        {
            message = "This vendor is not available.";
            return;
        }

        if (buyer.Unit == null)
        {
            message = "Buyer unit is not ready.";
            return;
        }

        var unitController = buyer.Unit.GetComponent<UnitController>();
        if (unitController == null)
        {
            message = "Buyer does not have a unit.";
            return;
        }

        if (unitController.IsDead)
        {
            message = "Cannot trade while dead.";
            return;
        }

        switch (tab)
        {
            case VendorTab.Buy:
                TryBuyWeapon(buyer, unitController, catalog, entryId, out success, out message);
                return;
            case VendorTab.Upgrades:
                TryBuyUpgrade(buyer, catalog, entryId, out success, out message, out timesBought);
                return;
            default:
                message = "That tab cannot be used yet.";
                return;
        }
    }

    [Server]
    public void BuildSnapshot(PlayerController buyer, string vendorId, out string[] upgradeIds, out int[] counts)
    {
        upgradeIds = System.Array.Empty<string>();
        counts = System.Array.Empty<int>();

        if (buyer == null)
            return;

        var zone = buyer.InteractionZone;
        if (zone == null || zone.VendorCatalog == null || zone.VendorCatalog.vendorId != vendorId)
            return;

        var catalog = zone.VendorCatalog;
        if (catalog.upgradeEntries == null || catalog.upgradeEntries.Count == 0)
            return;

        var ids = new System.Collections.Generic.List<string>(catalog.upgradeEntries.Count);
        var values = new System.Collections.Generic.List<int>(catalog.upgradeEntries.Count);
        var upgradeManager = UpgradeManager.Instance;

        foreach (var upgrade in catalog.upgradeEntries)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.upgradeId))
                continue;

            ids.Add(upgrade.upgradeId);
            values.Add(upgradeManager != null ? upgradeManager.GetPurchaseCountFor(buyer, upgrade.upgradeId) : 0);
        }

        upgradeIds = ids.ToArray();
        counts = values.ToArray();
    }

    [Server]
    private static void TryBuyWeapon(
        PlayerController buyer,
        UnitController unitController,
        VendorDefinition catalog,
        string entryId,
        out bool success,
        out string message)
    {
        success = false;
        message = string.Empty;

        if (!catalog.TryGetBuyEntry(entryId, out var entry) || entry.weapon == null)
        {
            message = "That item is not sold here.";
            return;
        }

        if (entry.goldCost < 0)
        {
            message = "Item has invalid cost.";
            return;
        }

        if (!buyer.SpendGold(entry.goldCost))
        {
            message = "Not enough gold.";
            return;
        }

        unitController.EquipWeapon(entry.weapon.weaponName);
        success = true;
        message = entry.goldCost > 0
            ? $"Bought {entry.weapon.weaponName} for {entry.goldCost}"
            : $"Bought {entry.weapon.weaponName}";
    }

    [Server]
    private static void TryBuyUpgrade(
        PlayerController buyer,
        VendorDefinition catalog,
        string entryId,
        out bool success,
        out string message,
        out int timesBought)
    {
        success = false;
        timesBought = 0;
        message = string.Empty;

        if (!catalog.TryGetUpgrade(entryId, out var upgrade))
        {
            message = "That upgrade is not sold here.";
            return;
        }

        var wave = ZombieGameManager.Singleton != null ? Mathf.Max(1, ZombieGameManager.Singleton.CurrentWave) : 1;
        if (!upgrade.IsUnlockedAtWave(wave))
        {
            message = "Upgrade is not unlocked yet.";
            return;
        }

        if (UpgradeManager.Instance == null)
        {
            message = "Upgrades are not available.";
            return;
        }

        success = UpgradeManager.Instance.TryPurchase(buyer, upgrade, upgrade.baseGoldCost, out message, out timesBought);
        if (success)
        {
            message = upgrade.baseGoldCost > 0
                ? $"Bought {upgrade.DisplayName} for {upgrade.baseGoldCost}"
                : $"Bought {upgrade.DisplayName}";
        }
    }
}
