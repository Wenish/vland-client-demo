using System.Collections.Generic;
using Mirror;
using ShadowInfection.DI;
using ShadowInfection.Match;
using UnityEngine;

public class VendorManager : NetworkBehaviour
{
    private const float VendorRangePadding = 1.5f;

    private UpgradeManager Upgrades => GameServices.Upgrades;

    [Server]
    public IVendorSession ResolveSession(PlayerController buyer, string vendorId)
    {
        if (buyer == null)
            return null;

        if (!buyer.TryEnsureVendorTrade(vendorId))
            return null;

        var session = buyer.ServerVendorSession;
        if (session == null || session.Catalog == null)
            return null;

        if (!string.IsNullOrEmpty(vendorId) && session.VendorId != vendorId)
            return null;

        if (!session.IsAvailableTo(buyer))
            return null;

        return session;
    }

    public static InteractionZone FindReachableVendor(UnitController unit, string vendorId)
    {
        if (unit == null)
            return null;

        InteractionZone best = null;
        var bestSqr = float.MaxValue;
        var zones = FindObjectsByType<InteractionZone>();
        foreach (var zone in zones)
        {
            if (zone == null || zone.InteractionType != InteractionType.OpenVendor || zone.VendorCatalog == null)
                continue;
            if (!string.IsNullOrEmpty(vendorId) && zone.VendorCatalog.vendorId != vendorId)
                continue;
            if (!IsUnitInVendorRange(unit, zone))
                continue;

            var sqr = (zone.transform.position - unit.transform.position).sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            best = zone;
        }

        return best;
    }

    public static bool IsUnitInVendorRange(UnitController unit, InteractionZone zone)
    {
        if (unit == null || zone == null)
            return false;

        var zoneCollider = zone.GetComponent<Collider>();
        if (zoneCollider == null)
            return Vector3.Distance(unit.transform.position, zone.transform.position) <= 3f + VendorRangePadding;

        var closest = zoneCollider.ClosestPoint(unit.transform.position);
        return (closest - unit.transform.position).sqrMagnitude <= VendorRangePadding * VendorRangePadding;
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

        var unitController = buyer.GetControlledUnit();
        if (unitController == null)
        {
            message = "Buyer unit is not ready.";
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
            var listedIds = new List<string>(catalog.buyEntries.Count);
            var listedStock = new List<int>(catalog.buyEntries.Count);
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

        var ids = new List<string>(catalog.upgradeEntries.Count);
        var values = new List<int>(catalog.upgradeEntries.Count);
        var upgradeManager = Upgrades;

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
    private void TryBuyUpgrade(
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

        var progress = GameServices.Get<IUpgradeProgress>();
        var wave = progress != null ? Mathf.Max(1, progress.UnlockLevel) : 1;
        if (!upgrade.IsUnlockedAtWave(wave))
        {
            message = "Upgrade is not unlocked yet.";
            return;
        }

        var upgrades = Upgrades;
        if (upgrades == null)
        {
            message = "Upgrades are not available.";
            return;
        }

        success = upgrades.TryPurchase(buyer, upgrade, upgrade.baseGoldCost, out message, out timesBought);
        if (success)
        {
            message = upgrade.baseGoldCost > 0
                ? $"Bought {upgrade.DisplayName} for {upgrade.baseGoldCost}"
                : $"Bought {upgrade.DisplayName}";
        }
    }
}
