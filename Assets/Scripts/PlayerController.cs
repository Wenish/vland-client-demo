using System.Collections;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;
using MyGame.Events;
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
        EventManager.Instance.Publish(new PlayerGoldChangedEvent(this, oldValue, newValue));
    }

    [SerializeField]
    private InteractionZone _interactionZone;

    public InteractionZone InteractionZone => _interactionZone;

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
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                CmdInteract();
            }
        }
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
        }
    }

    void OnUnitExitedInteractionZone(UnitExitedInteractionZone unitExitedInteractionZone)
    {
        var hasThisPlayerExitedInteractionZone = unitExitedInteractionZone.Unit == _unitController;
        if (hasThisPlayerExitedInteractionZone)
        {
            _interactionZone = null;
        }
    }

    [Command]
    public void CmdInteract()
    {
        if (_interactionZone == null) return;

        var canAffordInteraction = SpendGold(_interactionZone.goldCost);

        if (!canAffordInteraction)
        {
            Debug.Log("Not enough gold");
            return;
        }

        switch (_interactionZone.interactionType)
        {
            case InteractionType.OpenGate:
                Debug.Log("Open Gate");
                EventManager.Instance.Publish(new OpenGateEvent(_interactionZone.interactionId));
                break;
            case InteractionType.BuyWeapon:
                Debug.Log("Buy Weapon");
                EventManager.Instance.Publish(new BuyWeaponEvent(_interactionZone.interactionId, this));
                break;
        }
    }
}
