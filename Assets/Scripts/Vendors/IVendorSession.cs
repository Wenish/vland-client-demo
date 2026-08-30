/// <summary>
/// Live shop the player is trading with. Catalog is the template; stock and gold live here.
/// </summary>
public interface IVendorSession
{
    string VendorId { get; }
    VendorDefinition Catalog { get; }
    VendorTab OpeningTab { get; }

    /// <summary>When false, the shop does not track gold (world stalls). When true, Gold is the NPC wallet.</summary>
    bool HasWallet { get; }
    int Gold { get; }

    /// <summary>-1 means unlimited. 0 means sold out.</summary>
    int GetBuyStock(string entryId);

    bool IsAvailableTo(PlayerController player);
    bool IsReachableBy(UnitController unit);
    bool BelongsToInteractable(IVendorInteractable interactable);

    bool TryCreditGold(int amount);
    bool TrySpendGold(int amount);
    bool TryConsumeBuyStock(string entryId, int quantity = 1);
}

public interface IVendorInteractable
{
    IVendorSession GetVendorSession();
    IVendorSession GetSessionFor(PlayerController player);
    void EndSessionFor(PlayerController player);
}

public static class VendorStock
{
    public const int Unlimited = -1;
}
