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

        if (upgradeDatabase != null && !upgradeDatabase.ContainsUpgrade(upgrade.upgradeId))
        {
            PublishResult(buyer, false, "Upgrade is not registered in database.", upgrade.upgradeId, 0);
            return;
        }

        if (!CanPurchaseUpgrade(buyer, upgrade))
        {
            PublishResult(buyer, false, "Upgrade purchase limit reached.", upgrade.upgradeId, 0);
            return;
        }

        if (buyer.Unit == null)
        {
            PublishResult(buyer, false, "Buyer unit is not ready.", upgrade.upgradeId, 0);
            return;
        }

        var unitController = buyer.Unit.GetComponent<UnitController>();
        if (unitController == null || unitController.unitMediator == null)
        {
            PublishResult(buyer, false, "Buyer does not have a valid UnitMediator.", upgrade.upgradeId, 0);
            return;
        }

        if (unitController.IsDead)
        {
            PublishResult(buyer, false, "Cannot buy upgrades while dead.", upgrade.upgradeId, 0);
            return;
        }

        if (!buyer.SpendGold(finalCost))
        {
            PublishResult(buyer, false, "Not enough gold.", upgrade.upgradeId, 0);
            return;
        }

        var buff = upgrade.CreateBuff(unitController.unitMediator);
        unitController.unitMediator.AddBuff(buff);
        RegisterPurchase(buyer, upgrade.upgradeId);

        PublishResult(buyer, true, $"Purchased {upgrade.DisplayName}", upgrade.upgradeId, finalCost);
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
