using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeStationProfile", menuName = "Game/Upgrades/Station Profile")]
public class UpgradeStationProfile : ScriptableObject
{
    [Serializable]
    public class UpgradeOffer
    {
        public UpgradeDefinition upgrade;
        [Tooltip("Use -1 to keep upgrade base cost.")]
        public int costOverride = -1;
    }

    public string stationId = "station_default";
    public string stationDisplayName = "Upgrade Station";
    public List<UpgradeOffer> offers = new List<UpgradeOffer>();

    public string DisplayName => string.IsNullOrWhiteSpace(stationDisplayName) ? name : stationDisplayName;

    public UpgradeOffer GetDefaultOffer()
    {
        foreach (var offer in offers)
        {
            if (offer != null && offer.upgrade != null)
            {
                return offer;
            }
        }

        return null;
    }

    public bool TryGetOfferByUpgradeId(string upgradeId, out UpgradeOffer result)
    {
        foreach (var offer in offers)
        {
            if (offer == null || offer.upgrade == null)
            {
                continue;
            }

            if (offer.upgrade.upgradeId == upgradeId)
            {
                result = offer;
                return true;
            }
        }

        result = null;
        return false;
    }
}
