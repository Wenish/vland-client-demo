using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;
using MyGame.Events;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public GameObject Unit;
    [SyncVar]
    public float HorizontalInput = 0f;
    [SyncVar]
    public float VerticalInput = 0f;
    [SyncVar]
    public float Angle = 0f;
    [SyncVar]
    public bool IsPressingFire1 = false;

    private UnitController _unitController;

    private ControllerCamera _controllerCamera;
    private Vector3 _mouseWorldPosition;

    [SyncVar(hook = nameof(OnGoldChanged))]
    public int Gold = 0;

    private void OnGoldChanged(int oldValue, int newValue)
    {
        EventManager.Instance.Publish(new PlayerGoldChangedEvent(this, oldValue, newValue));
    }

    [SerializeField]
    public InteractionZone _interactionZone { get; private set; }

    Plane _plane;
    Camera _cameraMain;
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            SpawnPlayerUnit();
            EventManager.Instance.Subscribe<WaveStartedEvent>(OnWaveStartedHealPlayerUnitFull);
            EventManager.Instance.Subscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
        }

        if (isLocalPlayer)
        {
            _controllerCamera = Camera.main.GetComponent<ControllerCamera>();
            SetCameraTargetToPlayerUnit();
            _plane = new Plane(Vector3.up, 0);
            _cameraMain = Camera.main;
            var unitController = Unit.GetComponent<UnitController>();
            _unitController = unitController;
            EventManager.Instance.Publish(new MyPlayerUnitSpawnedEvent(unitController));
        }
        EventManager.Instance.Subscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Subscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);
    }

    private void OnDestroy()
    {
        if (isServer)
        {
            EventManager.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStartedHealPlayerUnitFull);
            EventManager.Instance.Unsubscribe<PlayerReceivesGoldEvent>(OnPlayerReceivesGold);
        }
        EventManager.Instance.Unsubscribe<UnitEnteredInteractionZone>(OnUnitEnteredInteractionZone);
        EventManager.Instance.Unsubscribe<UnitExitedInteractionZone>(OnUnitExitedInteractionZone);
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            SetMouseWorldPosition();
            InputAxis();
            InputPressingFire1();
            CalculateAngle();
            WeaponSwitch();
            if (Input.GetKeyDown(KeyCode.F))
            {
                CmdInteract();
            }
        }

        if (isServer)
        {
            ControlUnit();
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
        _unitController.Heal(_unitController.maxHealth);
        _unitController.Shield(_unitController.maxShield);
    }

    [Server]
    void SpawnPlayerUnit()
    {
        var unit = UnitSpawner.Instance.SpawnUnit("player", Vector3.zero, Quaternion.Euler(0f, 0f, 0f));
        Unit = unit;
        _unitController = Unit.GetComponent<UnitController>();
    }

    [Client]
    void WeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            UnitEquipWeapon("sword");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UnitEquipWeapon("shortBow");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            UnitEquipWeapon("daggers");
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            UnitEquipWeapon("gun");
        }
    }

    [Client]
    void SetCameraTargetToPlayerUnit()
    {
        _controllerCamera.CameraTarget = Unit.transform;
    }

    [Client]
    void SetMouseWorldPosition()
    {
        float distance;
        Ray ray = _cameraMain.ScreenPointToRay(Input.mousePosition);
        if (_plane.Raycast(ray, out distance))
        {
            _mouseWorldPosition = ray.GetPoint(distance);
        }
    }

    [Client]
    void InputAxis()
    {
        var newHorizontalInput = Input.GetAxisRaw("Horizontal");
        var hasHorizontalInputChanged = newHorizontalInput != HorizontalInput;
        var newVerticalInput = Input.GetAxisRaw("Vertical");
        var hasVerticalInputChanged = newVerticalInput != VerticalInput;
        if (hasHorizontalInputChanged || hasVerticalInputChanged)
        {
            CmdSetInput(newHorizontalInput, newVerticalInput);
        }
    }

    [Client]
    void CalculateAngle()
    {
        Vector3 pos = Unit.transform.position - _mouseWorldPosition;
        var angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90;
        CmdSetAngle(angle);
    }


    [Client]
    void InputPressingFire1()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            CmdSetFire1(true);
        }
        if (Input.GetButtonUp("Fire1"))
        {
            CmdSetFire1(false);
        }
    }

    [Command]
    void CmdSetInput(float horizontal, float vertical)
    {
        HorizontalInput = horizontal;
        VerticalInput = vertical;
    }

    [Command]
    void CmdSetAngle(float angle)
    {
        Angle = angle;
    }

    [Command]
    void CmdSetFire1(bool isPressingFire1)
    {
        IsPressingFire1 = isPressingFire1;
    }

    [Command]
    void UnitEquipWeapon(string weaponName)
    {
        if (!_unitController) return;

        WeaponController weaponController = _unitController.GetComponent<WeaponController>();
        if (!weaponController) return;
        if (weaponController.IsAttackOnCooldown) return;

        _unitController.EquipWeapon(weaponName);
    }

    [Server]
    void ControlUnit()
    {
        if (!_unitController) return;
        _unitController.horizontalInput = HorizontalInput;
        _unitController.verticalInput = VerticalInput;
        _unitController.angle = Angle;


        if (IsPressingFire1)
        {
            _unitController.Attack();
        }
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
                break;
        }
    }
}
