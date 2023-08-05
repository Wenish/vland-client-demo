using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Game.Scripts.Controllers;

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

    Plane _plane;
    Camera _cameraMain;
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            SpawnPlayerUnit();
        }

        if (isLocalPlayer)
        {
            _controllerCamera = Camera.main.GetComponent<ControllerCamera>();
            SetCameraTargetToPlayerUnit();
            _plane = new Plane(Vector3.up, 0);
            _cameraMain = Camera.main;
        }
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
        }

        if (isServer)
        {
            ControlUnit();
        }
    }

    [Server]
    void SpawnPlayerUnit()
    {
        var unit = NetworkManager.Instantiate(CustomNetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit"));
        unit.name = "Unit (Player)";
        NetworkServer.Spawn(unit);
        Unit = unit;
        _unitController = Unit.GetComponent<UnitController>();
    }

    [Client]
    void WeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            UnitEquipSword();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UnitEquipBow();
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
    void UnitEquipSword()
    {
        if (!_unitController) return;
        if (_unitController.weapon.attackCooldown > 0) return;

        WeaponMelee weaponMelee = _unitController.GetComponent<WeaponMelee>();
        if (!weaponMelee) return;
        _unitController.weapon = weaponMelee;
    }

    [Command]
    void UnitEquipBow()
    {
        if (!_unitController) return;
        if (_unitController.weapon.attackCooldown > 0) return;
        
        WeaponRanged weaponRanged = _unitController.GetComponent<WeaponRanged>();
        if (!weaponRanged) return;
        _unitController.weapon = weaponRanged;
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
}
