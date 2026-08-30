using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-stall session backed by a <see cref="VendorDefinition"/>.
/// Unlimited stock, no NPC wallet. Safe to use until real NPC inventory exists.
/// </summary>
public sealed class CatalogVendorSession : IVendorSession
{
    private readonly VendorDefinition catalog;
    private readonly IVendorInteractable source;
    private readonly Dictionary<string, int> buyStock = new Dictionary<string, int>();

    public CatalogVendorSession(VendorDefinition catalog, IVendorInteractable source = null)
    {
        this.catalog = catalog;
        this.source = source;
        CacheTemplateStock();
    }

    public string VendorId => catalog != null ? catalog.vendorId : string.Empty;
    public VendorDefinition Catalog => catalog;
    public bool HasWallet => false;
    public int Gold => 0;

    public VendorTab OpeningTab
    {
        get
        {
            if (catalog == null)
                return VendorTab.Buy;

            if (source is InteractionZone zone && catalog.IsTabEnabled(zone.DefaultTab))
                return zone.DefaultTab;

            return catalog.ResolveDefaultTab();
        }
    }

    public bool IsAvailableTo(PlayerController player)
    {
        if (catalog == null || player == null)
            return false;

        return IsReachableBy(player.GetControlledUnit());
    }

    public bool IsReachableBy(UnitController unit)
    {
        if (source == null)
            return true;

        if (unit == null)
            return false;

        if (source is InteractionZone zone)
            return VendorManager.IsUnitInVendorRange(unit, zone);

        return true;
    }

    public bool BelongsToInteractable(IVendorInteractable interactable)
    {
        return source != null && ReferenceEquals(source, interactable);
    }

    public int GetBuyStock(string entryId)
    {
        if (catalog != null && catalog.UsesItemCatalog)
            return VendorStock.Unlimited;

        if (string.IsNullOrEmpty(entryId) || !buyStock.TryGetValue(entryId, out var stock))
            return 0;
        return stock;
    }

    public bool TryCreditGold(int amount)
    {
        return amount >= 0;
    }

    public bool TrySpendGold(int amount)
    {
        return amount >= 0;
    }

    public bool TryConsumeBuyStock(string entryId, int quantity = 1)
    {
        if (catalog != null && catalog.UsesItemCatalog)
            return quantity > 0;

        if (quantity <= 0 || string.IsNullOrEmpty(entryId))
            return false;
        if (!buyStock.TryGetValue(entryId, out var stock))
            return false;
        if (stock == VendorStock.Unlimited)
            return true;
        if (stock < quantity)
            return false;

        buyStock[entryId] = stock - quantity;
        return true;
    }

    private void CacheTemplateStock()
    {
        buyStock.Clear();
        if (catalog?.buyEntries == null || catalog.UsesItemCatalog)
            return;

        foreach (var entry in catalog.buyEntries)
        {
            if (entry == null || entry.weapon == null)
                continue;

            buyStock[entry.ResolvedId] = entry.IsUnlimitedStock
                ? VendorStock.Unlimited
                : Mathf.Max(0, entry.stock);
        }
    }
}
