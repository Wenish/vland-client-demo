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
    public IVendorSession ResolveSession(PlayerController buyer, string vendorId)
    {
        if (buyer == null)
            return null;

        var session = buyer.ActiveVendor?.GetVendorSession();
        if (session == null || session.VendorId != vendorId || !session.IsAvailableTo(buyer))
            return null;

        return session;
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

        var session = ResolveSession(buyer, vendorId);
        if (session == null || session.Catalog == null)
        {
            message = "You are not trading.";
            return;
        }

        if (!session.Catalog.IsTabEnabled(tab))
        {
            message = "That tab cannot be used here.";
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
                TryBuyWeapon(buyer, unitController, session, entryId, out success, out message);
                return;
            case VendorTab.Upgrades:
                TryBuyUpgrade(buyer, session.Catalog, entryId, out success, out message, out timesBought);
                return;
            default:
                message = "That tab cannot be used yet.";
                return;
        }
    }

    [Server]
    public void BuildSnapshot(PlayerController buyer, string vendorId, out string[] upgradeIds, out int[] counts, out string[] buyIds, out int[] buyStocks, out int vendorGold)
    {
        upgradeIds = System.Array.Empty<string>();
        counts = System.Array.Empty<int>();
        buyIds = System.Array.Empty<string>();
        buyStocks = System.Array.Empty<int>();
        vendorGold = VendorStock.Unlimited;

        var session = ResolveSession(buyer, vendorId);
        if (session == null || session.Catalog == null)
            return;

        vendorGold = session.HasWallet ? session.Gold : VendorStock.Unlimited;
        var catalog = session.Catalog;

        if (catalog.buyEntries != null && catalog.buyEntries.Count > 0)
        {
            var listedIds = new System.Collections.Generic.List<string>(catalog.buyEntries.Count);
            var listedStock = new System.Collections.Generic.List<int>(catalog.buyEntries.Count);
            foreach (var entry in catalog.buyEntries)
            {
                if (entry == null || entry.weapon == null)
                    continue;

                listedIds.Add(entry.ResolvedId);
                listedStock.Add(session.GetBuyStock(entry.ResolvedId));
            }

            buyIds = listedIds.ToArray();
            buyStocks = listedStock.ToArray();
        }

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
        IVendorSession session,
        string entryId,
        out bool success,
        out string message)
    {
        success = false;
        message = string.Empty;

        var catalog = session.Catalog;
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

        var stock = session.GetBuyStock(entry.ResolvedId);
        if (stock == 0)
        {
            message = "Sold out.";
            return;
        }

        if (!buyer.SpendGold(entry.goldCost))
        {
            message = "Not enough gold.";
            return;
        }

        if (!session.TryCreditGold(entry.goldCost))
        {
            buyer.AddGold(entry.goldCost);
            message = "The vendor cannot take that payment.";
            return;
        }

        if (!session.TryConsumeBuyStock(entry.ResolvedId))
        {
            buyer.AddGold(entry.goldCost);
            session.TrySpendGold(entry.goldCost);
            message = "Sold out.";
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
