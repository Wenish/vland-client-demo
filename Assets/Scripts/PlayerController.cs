using System.Collections;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;
using MyGame.Events;
using ShadowInfection.UI.VendorWindow;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public GameObject Unit;

    private UnitController _unitController;

    [SyncVar(hook = nameof(OnGoldChanged))]
    public int Gold = 0;

    private void OnGoldChanged(int oldValue, int newValue)
    {
        GameEventPublish.ToBoth(new PlayerGoldChangedEvent(this, oldValue, newValue));
    }

    [SerializeField]
    private InteractionZone _interactionZone;

    public InteractionZone InteractionZone => _interactionZone;
    public IVendorInteractable ActiveVendor { get; private set; }

    void Start()
    {
        if (isServer)
        {
            Unit = PlayerUnitsManager.Instance.GetPlayerUnit(connectionToClient.connectionId);
            if (Unit != null)
            {
                _unitController = Unit.GetComponent<UnitController>();
            }
            EventManager.Instance.Subscribe<WaveStartedEvent>(OnWaveStartedHealPlayerUnitFull);
            EventManager.Instance.Subscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
            EventManager.Instance.Subscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        }

        EventManager.Instance.Subscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Subscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);
    }

    void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent e)
    {
        if (e.ConnectionId == connectionToClient.connectionId)
        {
            Unit = e.Unit;
            _unitController = Unit.GetComponent<UnitController>();
        }
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
        {
            EventManager.Instance.Unsubscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
            EventManager.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStartedHealPlayerUnitFull);
            EventManager.Instance.Unsubscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
        }
        EventManager.Instance.Unsubscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Unsubscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);

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
        var hasThisPlayerEnteredInteractionZone = unitEnteredInteractionZone.Unit == _unitController;
        if (hasThisPlayerEnteredInteractionZone)
        {
            _interactionZone = unitEnteredInteractionZone.Zone;
            if (_interactionZone != null && _interactionZone.InteractionType == InteractionType.OpenVendor)
                ActiveVendor = _interactionZone;
        }
    }

    void OnUnitExitedInteractionZone(UnitExitedInteractionZone unitExitedInteractionZone)
    {
        var hasThisPlayerExitedInteractionZone = unitExitedInteractionZone.Unit == _unitController;
        if (hasThisPlayerExitedInteractionZone)
        {
            if (isLocalPlayer)
                VendorWindowController.Instance?.CloseIfInteractable(unitExitedInteractionZone.Zone);
            if (ReferenceEquals(ActiveVendor, unitExitedInteractionZone.Zone))
                ActiveVendor = null;
            _interactionZone = null;
        }
    }

    private bool TryOpenVendorWindow()
    {
        if (_interactionZone == null || _interactionZone.InteractionType != InteractionType.OpenVendor)
            return false;

        var session = ActiveVendor != null
            ? ActiveVendor.GetVendorSession()
            : _interactionZone.GetVendorSession();
        if (session == null)
        {
            Debug.LogWarning("OpenVendor zone is missing a VendorDefinition.", _interactionZone);
            return true;
        }

        VendorWindowController.Instance?.Open(session, this);
        return true;
    }

    [Command]
    public void CmdVendorTransact(string vendorId, byte tab, string entryId)
    {
        if (VendorManager.Instance == null)
        {
            TargetVendorTransactResult(false, "Vendor is not available.", entryId, 0);
            return;
        }

        VendorManager.Instance.TryTransact(this, vendorId, (VendorTab)tab, entryId, out var success, out var message, out var timesBought);
        TargetVendorTransactResult(success, message, entryId, timesBought);
    }

    [Command]
    public void CmdRequestVendorSnapshot(string vendorId)
    {
        if (VendorManager.Instance == null)
            return;

        VendorManager.Instance.BuildSnapshot(this, vendorId, out var ids, out var counts, out var buyIds, out var buyStocks, out var vendorGold);
        TargetVendorSnapshot(ids, counts, buyIds, buyStocks, vendorGold);
    }

    [TargetRpc]
    private void TargetVendorTransactResult(bool success, string message, string entryId, int timesBought)
    {
        GameEventPublish.ToBoth(new VendorTransactResultEvent(this, success, message, entryId, timesBought));
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
        GameEventPublish.ToBoth(new VendorSnapshotEvent(upgradeIds, purchaseCounts, buyIds, buyStocks, vendorGold));
    }

    [Command]
    public void CmdInteract()
    {
        if (_interactionZone == null) return;

        if (_interactionZone.InteractionType == InteractionType.OpenVendor)
            return;

        if (_interactionZone.InteractionType == InteractionType.BuyUpgrade)
        {
            EventManager.Instance.Publish(new BuyUpgradeEvent(_interactionZone, this));
            return;
        }

        var canAffordInteraction = SpendGold(_interactionZone.GoldCost);

        if (!canAffordInteraction)
        {
            Debug.Log("Not enough gold");
            return;
        }

        switch (_interactionZone.InteractionType)
        {
            case InteractionType.OpenGate:
                Debug.Log("Open Gate");
                EventManager.Instance.Publish(new OpenGateEvent(_interactionZone.InteractionId));
                break;
            case InteractionType.BuyWeapon:
                Debug.Log("Buy Weapon");
                EventManager.Instance.Publish(new BuyWeaponEvent(_interactionZone.InteractionId, this));
                break;
        }
    }

    [Command]
    public void CmdBuyUpgrade(string upgradeId)
    {
        if (_interactionZone == null) return;
        if (_interactionZone.InteractionType != InteractionType.BuyUpgrade) return;

        EventManager.Instance.Publish(new BuyUpgradeEvent(_interactionZone, this, upgradeId));
    }
}
