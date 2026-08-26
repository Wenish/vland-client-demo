using System.Collections;
using Game.Scripts.Controllers;
using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
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
    private DisposableBag serverSubscriptions;

    private bool _isAimPreviewActive;
    private SkillSlotType _aimPreviewSlot;
    private int _aimPreviewIndex;
    private SkillData _aimPreviewSkill;

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
            var playerUnits = GameServices.PlayerUnits;
            var unit = playerUnits != null
                ? playerUnits.GetPlayerUnit(connectionToClient.connectionId)
                : null;
            if (unit != null)
                SetMyUnit(unit);
            GameMessages.Subscribe<PlayerUnitSpawnedEvent>(ref serverSubscriptions, OnPlayerUnitSpawned);
        }
    }

    void OnDestroy()
    {
        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
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
        _myUnitController = unit != null ? unit.GetComponent<UnitController>() : null;

        if (isLocalPlayer && _myUnitController != null)
        {
            GameMessages.Publish(new MyPlayerUnitSpawnedEvent(_myUnitController));
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
            if (UiModalInputBlock.IsBlocked)
            {
                // Inputs already cancelled when the modal opened; keep reading blocked.
                return;
            }

            SetMouseWorldPosition();
            InputWorldPing();
            InputAxis();
            InputPressingFire1();
            CalculateAngle();
            InputUseSkills();
            InputInterrupt();
            UpdateAimPreview();
            UpdateAimDuringCast();
        }

        if (isServer)
        {
            ControlMyUnit();
        }
    }

    /// <summary>
    /// Stops movement/fire on the server and interrupts casts/attacks.
    /// Call when opening character select or other gameplay-blocking UI.
    /// </summary>
    public static void CancelLocalGameplayInput()
    {
        if (NetworkClient.localPlayer == null)
            return;

        var input = NetworkClient.localPlayer.GetComponent<PlayerInput>();
        input?.CancelGameplayInputForUi();
    }

    [Client]
    public void CancelGameplayInputForUi()
    {
        if (!isLocalPlayer)
            return;

        if (_isAimPreviewActive)
            CancelAimPreview();

        if (_delaySendSetFire1InputCoroutine != null)
        {
            StopCoroutine(_delaySendSetFire1InputCoroutine);
            _delaySendSetFire1InputCoroutine = null;
        }

        CmdCancelGameplayInput();
    }

    [Command]
    private void CmdCancelGameplayInput()
    {
        HorizontalInput = 0f;
        VerticalInput = 0f;
        IsPressingFire1 = false;

        if (_myUnitController == null)
            return;

        _myUnitController.horizontalInput = 0f;
        _myUnitController.verticalInput = 0f;
        _myUnitController.ReceiveFire1Input(false);
        _myUnitController.InterruptAction();
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
        GameMessages.Publish(new WorldPingEvent(position));
    }

    [ClientRpc]
    void RpcWorldPing(Vector3 position)
    {
        if (isServer) return;
        GameMessages.Publish(new WorldPingEvent(position));
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
            PlayerActionFeedback.TryNotifyAttackCooldown(_myUnitController);
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
        // Signal the unit to track fire1 input for skill effects
        if (_myUnitController != null)
        {
            _myUnitController.ReceiveFire1Input(isPressingFire1);
        }

        if (isPressingFire1)
            NotifyOwnerIfAttackOnCooldown();
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
        GameMessages.Publish(new MyPlayerUnitSpawnedEvent(unitController));
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
        if (Keyboard.current == null)
            return;

        // Cancel aim preview without casting.
        if (_isAimPreviewActive
            && (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
        {
            CancelAimPreview();
            return;
        }

        HandleSkillKey(Keyboard.current.qKey, SkillSlotType.Normal, 0);
        HandleSkillKey(Keyboard.current.eKey, SkillSlotType.Normal, 1);
        HandleSkillKey(Keyboard.current.cKey, SkillSlotType.Normal, 2);
        HandleSkillKey(Keyboard.current.xKey, SkillSlotType.Ultimate, 0);
    }

    [Client]
    void HandleSkillKey(UnityEngine.InputSystem.Controls.KeyControl key, SkillSlotType slot, int index)
    {
        if (key == null)
            return;

        if (key.wasPressedThisFrame)
        {
            if (IsShiftPressed())
            {
                TryBeginAimPreview(slot, index);
            }
            else if (!_isAimPreviewActive)
            {
                TryUseSkill(slot, index);
            }
            return;
        }

        if (_isAimPreviewActive
            && _aimPreviewSlot == slot
            && _aimPreviewIndex == index
            && key.wasReleasedThisFrame)
        {
            ConfirmAimPreview();
        }
    }

    [Client]
    void TryBeginAimPreview(SkillSlotType slot, int index)
    {
        if (_myUnitController == null || _myUnitController.unitMediator == null)
            return;

        var skills = _myUnitController.unitMediator.Skills;
        if (skills == null)
            return;

        var instance = skills.GetSkill(slot, index);
        if (instance == null)
            return;

        if (instance.skillData == null)
            instance.ResolveSkillData();

        if (instance.IsOnCooldown && !instance.IsRecastWindowOpen)
        {
            PlayerActionFeedback.TryNotifySkillCooldown(_myUnitController, slot, index);
            return;
        }

        var data = instance.skillData;
        if (data == null || data.aimPreviewIndicator == null)
        {
            // No preview configured — cast immediately like a normal press.
            TryUseSkill(slot, index);
            return;
        }

        if (_isAimPreviewActive)
            CancelAimPreview();

        _isAimPreviewActive = true;
        _aimPreviewSlot = slot;
        _aimPreviewIndex = index;
        _aimPreviewSkill = data;

        Vector3 aim = ResolveIndicatorAim(_myUnitController, data, data.aimPreviewIndicator);
        var display = data.aimPreviewIndicator.ToDisplayParams(data.castRange, forPreview: true);
        var followTarget = SkillIndicatorTargetSnap.Resolve(
            data.aimPreviewIndicator,
            _myUnitController,
            instance,
            aim);
        GameMessages.Publish(
            new SkillAimPreviewStartedEvent(
                _myUnitController,
                data,
                display,
                aim,
                followTarget,
                instance));
    }

    [Client]
    void UpdateAimPreview()
    {
        if (!_isAimPreviewActive || _aimPreviewSkill == null || _myUnitController == null)
            return;

        Vector3 aim = ResolveIndicatorAim(
            _myUnitController,
            _aimPreviewSkill,
            _aimPreviewSkill.aimPreviewIndicator);

        NetworkedSkillInstance instance = null;
        var skills = _myUnitController.unitMediator != null
            ? _myUnitController.unitMediator.Skills
            : null;
        if (skills != null)
            instance = skills.GetSkill(_aimPreviewSlot, _aimPreviewIndex);

        var followTarget = SkillIndicatorTargetSnap.Resolve(
            _aimPreviewSkill.aimPreviewIndicator,
            _myUnitController,
            instance,
            aim);

        if (GameplayLifetimeScope.TryResolve<ISkillIndicatorService>(out var service))
            service.UpdateAim(aim, followTarget);
        else
            GameMessages.Publish(new SkillAimPreviewUpdatedEvent(aim, followTarget));
    }

    [Client]
    void UpdateAimDuringCast()
    {
        if (_isAimPreviewActive || _myUnitController == null)
            return;

        var actionState = _myUnitController.unitActionState;
        if (actionState == null)
            return;

        if (!TryGetActiveCastSkillName(actionState, out string skillName))
            return;

        var skills = _myUnitController.unitMediator != null
            ? _myUnitController.unitMediator.Skills
            : null;
        if (skills == null)
            return;

        var instance = skills.GetSkillByName(skillName);
        if (instance == null)
            return;

        if (instance.skillData == null)
            instance.ResolveSkillData();

        var data = instance.skillData;
        if (data == null || !SkillEffectChainUtil.UpdatesAimDuringCast(data))
            return;

        Vector3 aim = ResolveIndicatorAim(_myUnitController, data, data.aimPreviewIndicator);

        UnitController followTarget = null;
        var indicator = data.aimPreviewIndicator;
        if (indicator != null && indicator.snapToTarget != null)
        {
            followTarget = SkillIndicatorTargetSnap.Resolve(
                indicator,
                _myUnitController,
                instance,
                aim);
        }

        if (GameplayLifetimeScope.TryResolve<ISkillIndicatorService>(out var service))
            service.UpdateAim(aim, followTarget);

        CmdUpdateCastAim(skillName, aim);
    }

    [Client]
    static bool TryGetActiveCastSkillName(UnitActionState actionState, out string skillName)
    {
        skillName = null;
        if (actionState == null)
            return false;

        if (actionState.childState.type == UnitActionState.ActionType.Casting
            && !string.IsNullOrEmpty(actionState.childState.name))
        {
            skillName = actionState.childState.name;
            return true;
        }

        if (actionState.state.type == UnitActionState.ActionType.Casting
            && !string.IsNullOrEmpty(actionState.state.name))
        {
            skillName = actionState.state.name;
            return true;
        }

        return false;
    }

    [Command]
    void CmdUpdateCastAim(string skillName, Vector3 aimPoint)
    {
        if (_myUnitController == null || _myUnitController.unitMediator == null)
            return;

        var skills = _myUnitController.unitMediator.Skills;
        if (skills == null)
            return;

        var instance = skills.GetSkillByName(skillName);
        if (instance == null)
            return;

        instance.ServerUpdateRunningCastAim(aimPoint);
    }

    [Client]
    void ConfirmAimPreview()
    {
        if (!_isAimPreviewActive)
            return;

        var slot = _aimPreviewSlot;
        var index = _aimPreviewIndex;
        var skill = _aimPreviewSkill;
        Vector3 aim = _mouseWorldPosition;
        if (skill != null && _myUnitController != null)
            aim = ResolveIndicatorAim(_myUnitController, skill, skill.aimPreviewIndicator);

        GameMessages.Publish(new SkillAimPreviewEndedEvent(confirmedCast: true));
        ClearAimPreviewState();

        PlayerActionFeedback.TryNotifySkillCooldown(_myUnitController, slot, index);
        CmdUseSkill(slot, index, aim);
    }

    [Client]
    void CancelAimPreview()
    {
        if (!_isAimPreviewActive)
            return;

        GameMessages.Publish(new SkillAimPreviewEndedEvent(confirmedCast: false));
        ClearAimPreviewState();
    }

    [Client]
    void ClearAimPreviewState()
    {
        _isAimPreviewActive = false;
        _aimPreviewSkill = null;
        _aimPreviewIndex = -1;
    }

    [Client]
    void TryUseSkill(SkillSlotType slot, int index)
    {
        if (_isAimPreviewActive)
            CancelAimPreview();

        PlayerActionFeedback.TryNotifySkillCooldown(_myUnitController, slot, index);
        CmdUseSkill(slot, index, _mouseWorldPosition);
    }

    [Client]
    public void InputInterrupt()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (_isAimPreviewActive)
                CancelAimPreview();
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

    [Client]
    private static bool IsShiftPressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }

    [Client]
    Vector2 ReadLocalMoveInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(leftStick.x) > Mathf.Abs(horizontal)) horizontal = leftStick.x;
            if (Mathf.Abs(leftStick.y) > Mathf.Abs(vertical)) vertical = leftStick.y;
        }

        return new Vector2(horizontal, vertical);
    }

    [Client]
    Vector3 ResolveIndicatorAim(UnitController caster, SkillData skill, SkillIndicatorData indicator)
    {
        Vector3 mouseAim = SkillAimUtil.ClampAimPoint(caster, _mouseWorldPosition, skill);
        if (indicator == null)
            return mouseAim;

        if (indicator.shape != SkillIndicatorData.IndicatorShape.Directional
            || indicator.directionSource == SkillIndicatorData.DirectionSource.TowardAimPoint)
        {
            return mouseAim;
        }

        float length = indicator.ResolveRange();
        if (length <= 0f)
            length = Mathf.Max(1f, skill != null ? skill.castRange : 1f);

        return SkillAimUtil.ResolveIndicatorAimPoint(
            caster,
            mouseAim,
            ReadLocalMoveInput(),
            indicator.directionSource,
            length);
    }

    [Command]
    public void CmdUseSkill(SkillSlotType slot, int index, Vector3? aimPoint)
    {
        if (_myUnitController == null || _myUnitController.unitMediator == null || _myUnitController.unitMediator.Skills == null)
            return;

        var skills = _myUnitController.unitMediator.Skills;
        var skill = skills.GetSkill(slot, index);
        var result = skills.CastSkill(slot, index, aimPoint);
        if (result == SkillCastResult.OnCooldown
            && PlayerActionFeedback.ShouldShowSkillCooldown(_myUnitController, slot, index))
            NotifyOwnerActionFailed(PlayerActionFailReason.OnCooldown, PlayerActionFeedback.ResolveSkillName(skill));
    }

    [Server]
    void NotifyOwnerIfAttackOnCooldown()
    {
        if (!PlayerActionFeedback.ShouldShowAttackCooldown(_myUnitController))
            return;

        NotifyOwnerActionFailed(PlayerActionFailReason.OnCooldown, "Attack");
    }

    [Server]
    void NotifyOwnerActionFailed(PlayerActionFailReason reason, string actionName)
    {
        if (connectionToClient == null)
            return;

        TargetNotifyActionFailed(reason, actionName);
    }

    [TargetRpc]
    void TargetNotifyActionFailed(PlayerActionFailReason reason, string actionName)
    {
        PlayerActionFeedback.Notify(reason, actionName);
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
        // Clear fire1 state from the unit before disconnecting
        if (_myUnitController != null)
        {
            _myUnitController.ReceiveFire1Input(false);
        }
        myUnit = null;
        _myUnitController = null;
        HorizontalInput = 0f;
        VerticalInput = 0f;
        Angle = 0f;
        IsPressingFire1 = false;
    }
}