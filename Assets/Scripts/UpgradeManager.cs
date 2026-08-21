using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using UnityEngine;

public class UpgradeManager : NetworkBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField]
    private UpgradeDatabase upgradeDatabase;

    private readonly Dictionary<uint, Dictionary<string, int>> _purchaseCountsByPlayer = new Dictionary<uint, Dictionary<string, int>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (isServer)
        {
            EventManager.Instance.Subscribe<BuyUpgradeEvent>(OnBuyUpgradeEvent);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (isServer)
        {
            EventManager.Instance.Unsubscribe<BuyUpgradeEvent>(OnBuyUpgradeEvent);
        }
    }

    [Server]
    private void OnBuyUpgradeEvent(BuyUpgradeEvent buyUpgradeEvent)
    {
        var buyer = buyUpgradeEvent.Buyer;
        if (buyer == null)
        {
            return;
        }

        var zone = buyUpgradeEvent.Zone != null ? buyUpgradeEvent.Zone : buyer.InteractionZone;
        if (zone == null)
        {
            PublishResult(buyer, false, "No interaction zone selected.", string.Empty, 0);
            return;
        }

        var station = zone.GetComponent<UpgradeStationZone>();
        if (station == null)
        {
            PublishResult(buyer, false, "Upgrade station is not configured.", string.Empty, 0);
            return;
        }

        if (!station.TryBuildPurchaseOffer(buyUpgradeEvent.UpgradeId, out var upgrade, out var finalCost, out var reason))
        {
            PublishResult(buyer, false, reason, string.Empty, 0);
            return;
        }

        if (!TryPurchase(buyer, upgrade, finalCost, out var message, out _))
        {
            PublishResult(buyer, false, message, upgrade != null ? upgrade.upgradeId : string.Empty, 0);
            return;
        }

        PublishResult(buyer, true, message, upgrade.upgradeId, finalCost);
    }

    [Server]
    public bool TryPurchase(PlayerController buyer, UpgradeDefinition upgrade, int finalCost, out string message, out int timesBought)
    {
        message = string.Empty;
        timesBought = 0;

        if (buyer == null)
        {
            message = "Buyer is missing.";
            return false;
        }

        if (upgrade == null)
        {
            message = "Upgrade is not configured.";
            return false;
        }

        if (upgradeDatabase != null && !upgradeDatabase.ContainsUpgrade(upgrade.upgradeId))
        {
            message = "Upgrade is not registered in database.";
            return false;
        }

        if (!CanPurchaseUpgrade(buyer, upgrade))
        {
            message = "Upgrade purchase limit reached.";
            return false;
        }

        if (buyer.Unit == null)
        {
            message = "Buyer unit is not ready.";
            return false;
        }

        var unitController = buyer.Unit.GetComponent<UnitController>();
        if (unitController == null || unitController.unitMediator == null)
        {
            message = "Buyer does not have a valid UnitMediator.";
            return false;
        }

        if (unitController.IsDead)
        {
            message = "Cannot buy upgrades while dead.";
            return false;
        }

        if (finalCost < 0)
        {
            message = "Upgrade has invalid cost.";
            return false;
        }

        if (!buyer.SpendGold(finalCost))
        {
            message = "Not enough gold.";
            return false;
        }

        var buff = upgrade.CreateBuff(unitController.unitMediator);
        unitController.unitMediator.AddBuff(buff);
        RegisterPurchase(buyer, upgrade.upgradeId);
        timesBought = GetPurchaseCount(buyer, upgrade.upgradeId);
        message = $"Purchased {upgrade.DisplayName}";
        return true;
    }

    [Server]
    public int GetPurchaseCountFor(PlayerController buyer, string upgradeId)
    {
        return GetPurchaseCount(buyer, upgradeId);
    }

    [Server]
    private bool CanPurchaseUpgrade(PlayerController buyer, UpgradeDefinition upgrade)
    {
        if (upgrade.maxPurchasesPerPlayer < 0)
        {
            return true;
        }

        var count = GetPurchaseCount(buyer, upgrade.upgradeId);
        return count < upgrade.maxPurchasesPerPlayer;
    }

    [Server]
    private int GetPurchaseCount(PlayerController buyer, string upgradeId)
    {
        if (buyer == null)
        {
            return 0;
        }

        if (!_purchaseCountsByPlayer.TryGetValue(buyer.netId, out var perUpgrade))
        {
            return 0;
        }

        if (!perUpgrade.TryGetValue(upgradeId, out var count))
        {
            return 0;
        }

        return count;
    }

    [Server]
    private void RegisterPurchase(PlayerController buyer, string upgradeId)
    {
        if (!_purchaseCountsByPlayer.TryGetValue(buyer.netId, out var perUpgrade))
        {
            perUpgrade = new Dictionary<string, int>();
            _purchaseCountsByPlayer[buyer.netId] = perUpgrade;
        }

        if (!perUpgrade.ContainsKey(upgradeId))
        {
            perUpgrade[upgradeId] = 0;
        }

        perUpgrade[upgradeId] += 1;
    }

    [Server]
    private void PublishResult(PlayerController buyer, bool success, string message, string upgradeId, int cost)
    {
        EventManager.Instance.Publish(new UpgradePurchaseResultEvent(buyer, success, message, upgradeId, cost));
    }
}
