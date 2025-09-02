using System.Collections;
using Game.Scripts.Controllers;
using Mirror;
using MyGame.Events;
using UnityEngine;

public class PlayerInput : NetworkBehaviour
{
    [SyncVar]
    public GameObject myUnit;
    [SyncVar]
    public float HorizontalInput = 0f;
    [SyncVar]
    public float VerticalInput = 0f;
    [SyncVar]
    public float Angle = 0f;
    [SyncVar]
    public bool IsPressingFire1 = false;

    [SyncVar]
    public bool isThisInputActive = false;

    private UnitController _myUnitController;
    private Vector3 _mouseWorldPosition;
    Plane _plane;
    Camera _cameraMain;

    private ControllerCamera _controllerCamera;

    void Start()
    {
        if (isServer)
        {
            var unit = PlayerUnitsManager.Instance.GetPlayerUnit(connectionToClient.connectionId);
            SetMyUnit(unit);
            EventManager.Instance.Subscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        }
    }

    void OnDestroy()
    {
        if (isServer)
        {
            EventManager.Instance.Unsubscribe<PlayerUnitSpawnedEvent>(OnPlayerUnitSpawned);
        }
    }

    [Server]
    private void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent e)
    {
        ;
        if (e.ConnectionId == connectionToClient.connectionId)
        {
            SetMyUnit(e.Unit);
        }
    }

    [Server]
    public void SetMyUnit(GameObject unit)
    {
        myUnit = unit;
        _myUnitController = unit.GetComponent<UnitController>();

        if (isLocalPlayer)
        {
            EventManager.Instance.Publish(new MyPlayerUnitSpawnedEvent(_myUnitController));
        }
    }

    void Update()
    {
        if (!isThisInputActive) return;
        if (isLocalPlayer && myUnit != null)
        {
            SetMouseWorldPosition();
            InputWorldPing();
            InputAxis();
            InputPressingFire1();
            CalculateAngle();
            InputUseSkills();
        }

        if (isServer)
        {
            ControlMyUnit();
        }
    }

    [Client]
    void InputWorldPing()
    {
        if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Mouse0))
        {
            CmdWorldPing(_mouseWorldPosition);
        }
    }

    [Command]
    public void CmdWorldPing(Vector3 position)
    {
        RpcWorldPing(position);
        EventManager.Instance.Publish(new WorldPingEvent(position));
    }

    [ClientRpc]
    void RpcWorldPing(Vector3 position)
    {
        if (isServer) return;
        EventManager.Instance.Publish(new WorldPingEvent(position));
    }

    private Coroutine _delaySendSetFire1InputCoroutine;

    [Client]
    void InputPressingFire1()
    {
        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) && Input.GetButtonDown("Fire1"))
        {
            if (_delaySendSetFire1InputCoroutine != null)
            {
                StopCoroutine(_delaySendSetFire1InputCoroutine);
            }
            CmdSetFire1(true);
        }
        if (Input.GetButtonUp("Fire1"))
        {
            _delaySendSetFire1InputCoroutine = StartCoroutine(DelaySendSetFire1Input(0.15f, false));
        }
    }

    private IEnumerator DelaySendSetFire1Input(float delay, bool isPressingFire1)
    {
        yield return new WaitForSeconds(delay);
        CmdSetFire1(isPressingFire1);
    }

    [Command]
    void CmdSetFire1(bool isPressingFire1)
    {
        IsPressingFire1 = isPressingFire1;
    }


    [Client]
    void SetMouseWorldPosition()
    {
        float distance;
        if (_cameraMain == null) return;
        Ray ray = _cameraMain.ScreenPointToRay(Input.mousePosition);
        if (_plane.Raycast(ray, out distance))
        {
            _mouseWorldPosition = ray.GetPoint(distance);
        }
    }

    [Client]
    void CalculateAngle()
    {
        if (!myUnit) return;
        Vector3 pos = myUnit.transform.position - _mouseWorldPosition;
        var angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90;
        CmdSetAngle(angle);
    }


    [Command]
    void CmdSetAngle(float angle)
    {
        Angle = angle;
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

    [Command]
    void CmdSetInput(float horizontal, float vertical)
    {
        HorizontalInput = horizontal;
        VerticalInput = vertical;
    }

    [Server]
    void ControlMyUnit()
    {
        if (_myUnitController == null) return;
        _myUnitController.horizontalInput = HorizontalInput;
        _myUnitController.verticalInput = VerticalInput;
        _myUnitController.angle = Angle;

        if (IsPressingFire1)
        {
            _myUnitController.Attack();
        }

    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _plane = new Plane(Vector3.up, 0);
        _cameraMain = Camera.main;
        CmdSetInputActive(true);
        StartCoroutine(WaitForUnit());
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        CmdSetInputActive(false);
        StopCoroutine(WaitForUnit());
    }

    [Command]
    public void CmdSetInputActive(bool isActive)
    {
        isThisInputActive = isActive;
    }

    public IEnumerator WaitForUnit()
    {
        while (myUnit == null)
        {
            yield return null;
        }

        _controllerCamera = Camera.main.GetComponent<ControllerCamera>();
        var unitController = myUnit.GetComponent<UnitController>();
        _myUnitController = unitController;
        SetCameraTargetToPlayerUnit();
        EventManager.Instance.Publish(new MyPlayerUnitSpawnedEvent(unitController));
    }


    [Client]
    void SetCameraTargetToPlayerUnit()
    {
        if (!_myUnitController) return;
        _controllerCamera.CameraTarget = _myUnitController.transform;
    }

    [Client]
    public void InputUseSkills() 
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CmdUseSkill(SkillSlotType.Normal, 0, _mouseWorldPosition);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CmdUseSkill(SkillSlotType.Normal, 1, _mouseWorldPosition);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            CmdUseSkill(SkillSlotType.Normal, 2, _mouseWorldPosition);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            CmdUseSkill(SkillSlotType.Ultimate, 0, _mouseWorldPosition);
        }
    }

    [Command]
    public void CmdUseSkill(SkillSlotType slot, int index, Vector3? aimPoint)
    {
        _myUnitController.unitMediator.Skills.CastSkill(slot, index, aimPoint);
    }
}