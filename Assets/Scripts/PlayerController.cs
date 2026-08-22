using System.Collections;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;
using MyGame.Events;
using MyGame.Events.Ui;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Interactions;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public GameObject Unit;

    private UnitController _unitController;
    private DisposableBag serverSubscriptions;
    private DisposableBag peerSubscriptions;

    [SyncVar(hook = nameof(OnGoldChanged))]
    public int Gold = 0;

    private void OnGoldChanged(int oldValue, int newValue)
    {
        GameMessages.Publish(new PlayerGoldChangedEvent(this, oldValue, newValue));
    }

    [SerializeField]
    private InteractionZone _interactionZone;

    public InteractionZone InteractionZone => _interactionZone;
    public IVendorInteractable ActiveVendor { get; private set; }
    public IVendorSession ServerVendorSession { get; private set; }

    public UnitController GetControlledUnit()
    {
        if (_unitController != null)
            return _unitController;

        CacheControlledUnit();
        return _unitController;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        BindControlledUnit();
        GameMessages.Subscribe<WaveStartedEvent>(ref serverSubscriptions, OnWaveStartedHealPlayerUnitFull);
        GameMessages.Subscribe<PlayerReceivesGoldEvent>(ref serverSubscriptions, OnPlayerReceivesGold);
        GameMessages.Subscribe<PlayerUnitSpawnedEvent>(ref serverSubscriptions, OnPlayerUnitSpawned);
    }

    void Start()
    {
        GameMessages.Subscribe<UnitEnteredInteractionZone>(ref peerSubscriptions, OnUnitEnteredInteractionZone);
        GameMessages.Subscribe<UnitExitedInteractionZone>(ref peerSubscriptions, OnUnitExitedInteractionZone);
    }

    void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent e)
    {
        if (connectionToClient != null && e.ConnectionId == connectionToClient.connectionId)
            BindControlledUnit(e.Unit);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        StartCoroutine(WaitForUnit());
    }

    public IEnumerator WaitForUnit()
    {
        while (Unit == null)
        {
            yield return null;
        }
        var unitController = Unit.GetComponent<UnitController>();
        _unitController = unitController;
    }

    private void OnDestroy()
    {
        if (isServer)
            EndVendorTrade();

        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
        peerSubscriptions.Dispose();
        peerSubscriptions = new DisposableBag();

        StopCoroutine(WaitForUnit());
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (TryHandleUpgradeQuickBuy(Keyboard.current))
            {
                return;
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (TryOpenVendorWindow())
                    return;

                CmdInteract();
            }
        }
    }

    private bool TryHandleUpgradeQuickBuy(Keyboard keyboard)
    {
        if (_interactionZone == null || _interactionZone.InteractionType != InteractionType.BuyUpgrade)
        {
            return false;
        }

        if (!_interactionZone.TryGetComponent<UpgradeStationZone>(out var upgradeStationZone) || !upgradeStationZone.HasMultipleOffers)
        {
            return false;
        }

        var offerIndex = GetPressedOfferIndex(keyboard);
        if (offerIndex < 0)
        {
            return false;
        }

        if (!upgradeStationZone.TryGetUpgradeIdAtOfferIndex(offerIndex, out var upgradeId))
        {
            return false;
        }

        CmdBuyUpgrade(upgradeId);
        return true;
    }

    private static int GetPressedOfferIndex(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame) return 3;
        if (keyboard.digit5Key.wasPressedThisFrame) return 4;
        if (keyboard.digit6Key.wasPressedThisFrame) return 5;
        if (keyboard.digit7Key.wasPressedThisFrame) return 6;
        if (keyboard.digit8Key.wasPressedThisFrame) return 7;
        if (keyboard.digit9Key.wasPressedThisFrame) return 8;

        return -1;
    }

    [Server]
    public void OnPlayerReceivesGold(PlayerReceivesGoldEvent playerReceivesGoldEvent)
    {
        if (!_unitController) return;
        var hasThisPlayerReceivedGold = playerReceivesGoldEvent.Player == _unitController;
        if (hasThisPlayerReceivedGold)
        {
            AddGold(playerReceivesGoldEvent.GoldAmount);
        }
    }

    [Server]
    public void OnWaveStartedHealPlayerUnitFull(WaveStartedEvent waveStartedEvent)
    {
        if (!_unitController) return;
        _unitController.Heal(_unitController.maxHealth / 2, _unitController);
        _unitController.Shield(_unitController.maxShield / 2, _unitController);
    }


    [Server]
    public void AddGold(int amount)
    {
        Gold += amount;
    }

    [Server]
    public bool SpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            return true;
        }
        return false;
    }

    void OnUnitEnteredInteractionZone(UnitEnteredInteractionZone unitEnteredInteractionZone)
    {
        if (unitEnteredInteractionZone.Unit != GetControlledUnit())
            return;

        _interactionZone = unitEnteredInteractionZone.Zone;
        if (_interactionZone != null && _interactionZone.InteractionType == InteractionType.OpenVendor)
            ActiveVendor = _interactionZone;
    }

    void OnUnitExitedInteractionZone(UnitExitedInteractionZone unitExitedInteractionZone)
    {
        if (unitExitedInteractionZone.Unit != GetControlledUnit())
            return;

        if (isLocalPlayer)
            GameMessages.Publish(new CloseVendorWindowIfInteractableEvent(unitExitedInteractionZone.Zone));
        if (isServer && ReferenceEquals(ActiveVendor, unitExitedInteractionZone.Zone))
            EndVendorTrade();
        if (ReferenceEquals(ActiveVendor, unitExitedInteractionZone.Zone))
            ActiveVendor = null;
        _interactionZone = null;
    }

    private bool TryOpenVendorWindow()
    {
        if (_interactionZone == null || _interactionZone.InteractionType != InteractionType.OpenVendor)
            return false;

        var session = _interactionZone.GetVendorSession();
        if (session == null)
        {
            Debug.LogWarning("OpenVendor zone is missing a VendorDefinition.", _interactionZone);
            return true;
        }

        GameMessages.Publish(new OpenVendorWindowEvent(session, this));
        return true;
    }

    [Command]
    public void CmdBeginVendorTrade(string vendorId)
    {
        TryEnsureVendorTrade(vendorId);
    }

    [Command]
    public void CmdEndVendorTrade()
    {
        EndVendorTrade();
    }

    [Server]
    public bool TryEnsureVendorTrade(string vendorId)
    {
        var unit = GetControlledUnit();
        if (unit == null)
            return false;

        var zone = VendorManager.FindReachableVendor(unit, vendorId);
        if (zone == null && _interactionZone != null && _interactionZone.InteractionType == InteractionType.OpenVendor)
        {
            if (string.IsNullOrEmpty(vendorId) ||
                (_interactionZone.VendorCatalog != null && _interactionZone.VendorCatalog.vendorId == vendorId))
            {
                zone = _interactionZone;
            }
        }

        if (zone == null || zone.VendorCatalog == null)
        {
            EndVendorTrade();
            return false;
        }

        _interactionZone = zone;
        ActiveVendor = zone;
        ServerVendorSession = zone.GetSessionFor(this);
        return ServerVendorSession != null && ServerVendorSession.IsAvailableTo(this);
    }

    [Server]
    private void EndVendorTrade()
    {
        ActiveVendor?.EndSessionFor(this);
        ServerVendorSession = null;
        ActiveVendor = null;
    }

    private void BindControlledUnit(GameObject unitOverride = null)
    {
        if (unitOverride != null)
            Unit = unitOverride;
        else if (GameServices.PlayerUnits != null && connectionToClient != null)
            Unit = GameServices.PlayerUnits.GetPlayerUnit(connectionToClient.connectionId);

        CacheControlledUnit();
    }

    private void CacheControlledUnit()
    {
        _unitController = Unit != null ? Unit.GetComponent<UnitController>() : null;
    }

    [Command]
    public void CmdVendorTransact(string vendorId, byte tab, string entryId)
    {
        if (GameServices.Vendors == null)
        {
            TargetVendorTransactResult(false, "Vendor is not available.", entryId, 0);
            return;
        }

        GameServices.Vendors.TryTransact(this, vendorId, (VendorTab)tab, entryId, out var success, out var message, out var timesBought);
        TargetVendorTransactResult(success, message, entryId, timesBought);
    }

    [Command]
    public void CmdRequestVendorSnapshot(string vendorId)
    {
        if (GameServices.Vendors == null)
            return;

        GameServices.Vendors.BuildSnapshot(this, vendorId, out var ids, out var counts, out var buyIds, out var buyStocks, out var vendorGold);
        TargetVendorSnapshot(ids, counts, buyIds, buyStocks, vendorGold);
    }

    [TargetRpc]
    private void TargetVendorTransactResult(bool success, string message, string entryId, int timesBought)
    {
        GameMessages.Publish(new VendorTransactResultEvent(this, success, message, entryId, timesBought));
        if (!isLocalPlayer)
            return;

        PlayerActionFeedback.Show(
            message,
            "vendor:" + entryId,
            kind: success ? PlayerHudInfoKind.Info : PlayerHudInfoKind.Error);
    }

    [TargetRpc]
    private void TargetVendorSnapshot(string[] upgradeIds, int[] purchaseCounts, string[] buyIds, int[] buyStocks, int vendorGold)
    {
        GameMessages.Publish(new VendorSnapshotEvent(this, upgradeIds, purchaseCounts, buyIds, buyStocks, vendorGold));
    }

    [Command]
    public void CmdInteract()
    {
        if (_interactionZone == null) return;

        // Vendor UI opens client-side; server trade goes through VendorManager cmds.
        if (_interactionZone.InteractionType == InteractionType.OpenVendor)
            return;

        var registry = GameServices.Get<IInteractionHandlerRegistry>();
        if (registry == null || !registry.TryGet(_interactionZone.InteractionType, out var handler))
            return;

        handler.Handle(_interactionZone, this);
    }

    [Command]
    public void CmdBuyUpgrade(string upgradeId)
    {
        if (_interactionZone == null) return;
        if (_interactionZone.InteractionType != InteractionType.BuyUpgrade) return;

        GameMessages.Publish(new BuyUpgradeEvent(_interactionZone, this, upgradeId));
    }
}
