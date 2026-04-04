using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InteractionZone))]
public class UpgradeStationZone : MonoBehaviour
{
    [SerializeField]
    private UpgradeStationProfile stationProfile;

    [SerializeField]
    private UpgradeStationInstanceConfig instanceConfig;

    [SerializeField]
    private UpgradeDefinition defaultUpgradeOverride;

    [SerializeField]
    private bool useInteractionZoneGoldCostAsFallback = true;

    private InteractionZone _interactionZone;

    public string StationName => stationProfile != null ? stationProfile.DisplayName : "Upgrade Station";
    public bool HasMultipleOffers => GetValidOfferCount() > 1;

    private void Awake()
    {
        _interactionZone = GetComponent<InteractionZone>();
        if (_interactionZone != null && _interactionZone.InteractionType != InteractionType.BuyUpgrade)
        {
            Debug.LogWarning($"UpgradeStationZone on {name} should use InteractionType.BuyUpgrade.", this);
        }
    }

    public string GetTooltipPurchaseSummary()
    {
        if (stationProfile == null || stationProfile.offers == null || stationProfile.offers.Count == 0)
        {
            return defaultUpgradeOverride != null ? defaultUpgradeOverride.DisplayName : string.Empty;
        }

        var names = new System.Collections.Generic.List<string>();
        foreach (var offer in stationProfile.offers)
        {
            if (offer == null || offer.upgrade == null)
            {
                continue;
            }

            names.Add(offer.upgrade.DisplayName);
            if (names.Count >= 3)
            {
                break;
            }
        }

        if (names.Count == 0)
        {
            return defaultUpgradeOverride != null ? defaultUpgradeOverride.DisplayName : string.Empty;
        }

        var suffix = stationProfile.offers.Count > names.Count ? ", ..." : string.Empty;
        return string.Join(", ", names) + suffix;
    }

    public string GetTooltipOfferLines()
    {
        if (stationProfile == null || stationProfile.offers == null)
        {
            return string.Empty;
        }

        var lines = new System.Collections.Generic.List<string>();
        var displayIndex = 1;

        foreach (var offer in stationProfile.offers)
        {
            if (offer == null || offer.upgrade == null)
            {
                continue;
            }

            var cost = ResolveCost(offer.upgrade);
            lines.Add($"[{displayIndex}] {offer.upgrade.DisplayName} ({cost} Gold)");
            displayIndex += 1;

            if (displayIndex > 9)
            {
                break;
            }
        }

        return string.Join("\n", lines);
    }

    public bool TryGetUpgradeIdAtOfferIndex(int offerIndex, out string upgradeId)
    {
        upgradeId = string.Empty;
        if (offerIndex < 0 || stationProfile == null || stationProfile.offers == null)
        {
            return false;
        }

        var validIndex = 0;
        foreach (var offer in stationProfile.offers)
        {
            if (offer == null || offer.upgrade == null)
            {
                continue;
            }

            if (validIndex == offerIndex)
            {
                upgradeId = offer.upgrade.upgradeId;
                return !string.IsNullOrWhiteSpace(upgradeId);
            }

            validIndex += 1;
        }

        return false;
    }

    public bool TryBuildPurchaseOffer(string requestedUpgradeId, out UpgradeDefinition upgrade, out int finalCost, out string reason)
    {
        upgrade = ResolveUpgrade(requestedUpgradeId);
        finalCost = 0;

        if (upgrade == null)
        {
            reason = "No upgrade configured on this station.";
            return false;
        }

        var wave = ZombieGameManager.Singleton != null ? Mathf.Max(1, ZombieGameManager.Singleton.CurrentWave) : 1;

        if (!upgrade.IsUnlockedAtWave(wave))
        {
            reason = "Upgrade is not unlocked yet.";
            return false;
        }

        if (instanceConfig != null)
        {
            if (!instanceConfig.IsUnlockedAtWave(wave))
            {
                reason = "This station is currently locked.";
                return false;
            }

            if (!instanceConfig.IsUpgradeAllowed(upgrade))
            {
                reason = "Upgrade is not available at this station.";
                return false;
            }
        }

        finalCost = ResolveCost(upgrade);
        if (finalCost <= 0)
        {
            reason = "Upgrade has invalid cost.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private UpgradeDefinition ResolveUpgrade(string requestedUpgradeId)
    {
        if (!string.IsNullOrWhiteSpace(requestedUpgradeId) && stationProfile != null)
        {
            if (stationProfile.TryGetOfferByUpgradeId(requestedUpgradeId, out var requestedOffer))
            {
                return requestedOffer.upgrade;
            }
        }

        if (defaultUpgradeOverride != null)
        {
            return defaultUpgradeOverride;
        }

        if (stationProfile == null)
        {
            return null;
        }

        var defaultOffer = stationProfile.GetDefaultOffer();
        return defaultOffer != null ? defaultOffer.upgrade : null;
    }

    private int GetValidOfferCount()
    {
        if (stationProfile == null || stationProfile.offers == null)
        {
            return defaultUpgradeOverride != null ? 1 : 0;
        }

        var count = 0;
        foreach (var offer in stationProfile.offers)
        {
            if (offer != null && offer.upgrade != null)
            {
                count += 1;
            }
        }

        if (count == 0 && defaultUpgradeOverride != null)
        {
            return 1;
        }

        return count;
    }

    private int ResolveCost(UpgradeDefinition upgrade)
    {
        var cost = upgrade.baseGoldCost;

        if (stationProfile != null && stationProfile.TryGetOfferByUpgradeId(upgrade.upgradeId, out var offer))
        {
            if (offer.costOverride >= 0)
            {
                cost = offer.costOverride;
            }
        }

        if (instanceConfig != null)
        {
            cost = Mathf.RoundToInt(cost * instanceConfig.costMultiplier);
        }

        if (cost <= 0 && useInteractionZoneGoldCostAsFallback && _interactionZone != null)
        {
            cost = _interactionZone.GoldCost;
        }

        return Mathf.Max(0, cost);
    }
}
