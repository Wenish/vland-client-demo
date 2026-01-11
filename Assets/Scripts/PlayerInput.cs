using System.Collections;
using Game.Scripts.Controllers;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField]
    private UnitController _myUnitController;
    [SerializeField]
    private Vector3 _mouseWorldPosition;
    private Plane _plane;
    [SerializeField]
    private Camera _cameraMain;

    private ControllerCamera _controllerCamera;

    // When aiming, account for projectile visual spawning 1 unit above the floor
    // so the forward direction from that height aligns with the cursor on screen.
    [SerializeField]
    [Tooltip("Vertical offset (in world units) used for the aim plane when computing yaw. Match projectile spawn height.")]
    private float aimPlaneHeightOffset = 1f;

    void Start()
    {
        _plane = new Plane(Vector3.up, 0);
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
        if (isLocalPlayer && !isThisInputActive)
        {
            CmdSetInputActive(true);
        }

        if (isLocalPlayer && _myUnitController == null && myUnit != null)
        {
            _myUnitController = myUnit.GetComponent<UnitController>();
        }
        if (isLocalPlayer && _cameraMain == null)
        {
            _cameraMain = Camera.main;
        }

        if (!isThisInputActive) return;
        if (isLocalPlayer && myUnit != null)
        {
            SetMouseWorldPosition();
            InputWorldPing();
            InputAxis();
            InputPressingFire1();
            CalculateAngle();
            InputUseSkills();
            InputInterrupt();
        }

        if (isServer)
        {
            ControlMyUnit();
        }
    }

    [Client]
    void InputWorldPing()
    {

        if ((IsAltPressed()) && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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
        // Fire1 mapped to primary action; ignore when Alt is held
        // When pointer is over LoadoutPanel, block only the mouse-based press (allow gamepad/keyboard)
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        bool overUi = UiPointerState.IsPointerOverBlockingElement;

        bool firePressed = (mousePressed && !overUi) || gamepadPressed || keyboardPressed;
        if (!IsAltPressed() && firePressed)
        {
            if (_delaySendSetFire1InputCoroutine != null)
            {
                StopCoroutine(_delaySendSetFire1InputCoroutine);
            }
            CmdSetFire1(true);
        }
        bool fireReleased = (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame)
                    || (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame);
        if (fireReleased)
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

        Vector2 pointerPos = Vector2.zero;
        if (Mouse.current != null)
        {
            pointerPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        Ray ray = _cameraMain.ScreenPointToRay(pointerPos);
        if (_plane.Raycast(ray, out distance))
        {
            _mouseWorldPosition = ray.GetPoint(distance);
        }
    }

    [Client]
    void CalculateAngle()
    {
        if (!myUnit) return;
        // Cast the cursor ray against a plane at the projectile's spawn height
        if (_cameraMain == null) return;

        // Build a plane parallel to the ground, passing through (unit.y + offset)
        float planeY = myUnit.transform.position.y + aimPlaneHeightOffset;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        Vector2 pointerPos = Vector2.zero;
        if (Mouse.current != null)
        {
            pointerPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        Ray ray = _cameraMain.ScreenPointToRay(pointerPos);
        Vector3 aimPoint = _mouseWorldPosition; // fallback to ground-plane point if ray misses
        if (aimPlane.Raycast(ray, out float t))
        {
            aimPoint = ray.GetPoint(t);
        }

        // Compute yaw using the same orientation as before (unit - aim) to preserve model-facing convention
        Vector3 pos = myUnit.transform.position - aimPoint;
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
        float newHorizontalInput = 0f;
        float newVerticalInput = 0f;
        // Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) newHorizontalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) newHorizontalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) newVerticalInput -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) newVerticalInput += 1f;
        }
        // Gamepad
        if (Gamepad.current != null)
        {
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(leftStick.x) > Mathf.Abs(newHorizontalInput)) newHorizontalInput = leftStick.x;
            if (Mathf.Abs(leftStick.y) > Mathf.Abs(newVerticalInput)) newVerticalInput = leftStick.y;
        }

        var hasHorizontalInputChanged = !Mathf.Approximately(newHorizontalInput, HorizontalInput);
        var hasVerticalInputChanged = !Mathf.Approximately(newVerticalInput, VerticalInput);
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
        _cameraMain = Camera.main;
        CmdSetInputActive(true);
        StartCoroutine(WaitForUnit());
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        CmdSetInputActive(false);
        CmdResetInput();
        StopCoroutine(WaitForUnit());
        _myUnitController = null;
        _cameraMain = null;
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
        if (

            Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame

            )
        {
            CmdUseSkill(SkillSlotType.Normal, 0, _mouseWorldPosition);
        }
        if (

            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame

            )
        {
            CmdUseSkill(SkillSlotType.Normal, 1, _mouseWorldPosition);
        }
        if (
            Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame

            )
        {
            CmdUseSkill(SkillSlotType.Normal, 2, _mouseWorldPosition);
        }
        if (

            Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame
            )
        {
            CmdUseSkill(SkillSlotType.Ultimate, 0, _mouseWorldPosition);
        }
    }

    [Client]
    public void InputInterrupt()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            CmdInterruptMyUnit();
        }
    }

    // Helpers
    [Client]
    private static bool IsAltPressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
    }

    [Command]
    public void CmdUseSkill(SkillSlotType slot, int index, Vector3? aimPoint)
    {
        _myUnitController.unitMediator.Skills.CastSkill(slot, index, aimPoint);
    }

    [Command]
    public void CmdInterruptMyUnit()
    {
        if (_myUnitController != null)
        {
            _myUnitController.InterruptAction();
        }
    }

    [Command]
    public void CmdResetInput()
    {
        ResetInput();
    }

    [Server]
    public void ResetInput()
    {
        myUnit = null;
        _myUnitController = null;
        HorizontalInput = 0f;
        VerticalInput = 0f;
        Angle = 0f;
        IsPressingFire1 = false;
    }
}