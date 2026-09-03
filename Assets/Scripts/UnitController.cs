using System;
using Mirror;
using MyGame.Events;
using ShadowInfection.DI;
using ShadowInfection.Items;
using ShadowInfection.Units;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class UnitController : NetworkBehaviour
{
    public enum DashSpeedProfile : byte
    {
        Constant = 0,
        EaseOut = 1,
        EaseIn = 2
    }

    [SyncVar]
    public UnitType unitType;

    public event Action<UnitController> OnTeamChanged = delegate { };

    [SyncVar(hook = nameof(HookOnTeamChanged))]
    public int team;

    [Header("Team")]
    [Tooltip("Set the team number in the editor. Use -1 for neutral (e.g., globally attackable objectives). During Play mode, if you're the server/host, changing this will update the networked team.")]
    [SerializeField]
    private int teamNumber = 0;

    [Server]
    public void SetTeam(int team)
    {
        this.team = team;
        // Keep inspector field in sync when changed via code/server
        teamNumber = team;
        OnTeamChanged(this);
    }

    [Client]
    public void HookOnTeamChanged(int oldTeam, int newTeam)
    {
        // Reflect networked value into the inspector field on clients
        teamNumber = newTeam;
        OnTeamChanged(this);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        // Keep the serialized inspector field and SyncVar in sync in editor
        // - Edit mode: writing the default/team on the component updates the initial SyncVar value
        // - Play mode server/host: changing the inspector field pushes a networked update via SetTeam
        // - Play mode client: the inspector mirrors the authoritative network value
        if (!Application.isPlaying)
        {
            team = teamNumber;
        }
        else
        {
            // In play mode, OnValidate can be called before Mirror wires up netIdentity.
            // Avoid accessing isServer/isClient unless netIdentity exists.
            if (netIdentity != null && NetworkServer.active && netIdentity.isServer)
            {
                if (team != teamNumber)
                {
                    SetTeam(teamNumber);
                }
            }
            else
            {
                // On clients (or when not fully initialized yet), mirror the networked value back to the inspector field.
                if (teamNumber != team)
                {
                    teamNumber = team;
                }
            }
        }
    }

    [SyncVar(hook = nameof(HookOnUnitNameChanged))]
    public string unitName;


    public event Action<UnitController> OnNameChanged = delegate { };

    [Server]
    public void SetUnitName(string name)
    {
        unitName = name;
        OnNameChanged(this);
    }
    [Client]
    public void HookOnUnitNameChanged(string oldValue, string newValue)
    {
        OnNameChanged(this);
    }

    [SyncVar]
    public float horizontalInput = 0f;

    [SyncVar]
    public float verticalInput = 0f;

    [SyncVar]
    public float angle = 0f;

    // Fire1 input state tracking
    [SyncVar]
    public bool isPressingFire1 = false;
    private bool _previousFire1State = false;
    private float _basicAttackCritMeter = 0f;

    /// <summary>
    /// Gets whether fire1 was just pressed this frame (rising edge detection).
    /// For use by skill effects that need to detect individual fire1 presses.
    /// </summary>
    public bool HasFire1PressThisFrame => isPressingFire1 && !_previousFire1State;

    [Server]
    public void ReceiveFire1Input(bool isPressed)
    {
        isPressingFire1 = isPressed;
    }

    /// <summary>
    /// Called by skill effects to update fire1 rising edge detection.
    /// Must be called once per frame from the context that checks fire1 input.
    /// </summary>
    public void UpdateFire1State()
    {
        _previousFire1State = isPressingFire1;
    }

    [SyncVar(hook = nameof(HookOnHealthChanged))]
    public int health = 100;

    [SyncVar(hook = nameof(HookOnMaxHealthChanged))]
    public int maxHealth = 100;

    [SyncVar(hook = nameof(HookOnShieldChanged))]
    public int shield = 50;

    [SyncVar(hook = nameof(HookOnMaxShieldChanged))]
    public int maxShield = 50;

    [SyncVar]
    public float moveSpeed = 5f;

    [SyncVar(hook = nameof(OnMainHandItemIdChanged))]
    public string mainHandItemId = "";
    [SyncVar(hook = nameof(OnOffHandItemIdChanged))]
    public string offHandItemId = "";
    public WeaponData currentWeapon;
    public WeaponData currentOffHandWeapon;
    public WeaponData offHandItemWeapon;
    public int LastAttackIndex { get; private set; }
    private WeaponController weaponController;
    public event Action<UnitController> OnWeaponChange = delegate { };

    private void OnMainHandItemIdChanged(string oldItemId, string newItemId)
    {
        RefreshHeldWeapons();
    }

    private void OnOffHandItemIdChanged(string oldItemId, string newItemId)
    {
        RefreshHeldWeapons();
    }

    [Server]
    public void EquipHeldItems(string mainItemId, string offItemId)
    {
        mainHandItemId = mainItemId ?? string.Empty;
        offHandItemId = offItemId ?? string.Empty;
        RefreshHeldWeapons();
    }

    private void RefreshHeldWeapons()
    {
        if (weaponController == null)
            weaponController = GetComponent<WeaponController>();

        var databases = GameServices.Databases;
        var items = databases?.Items;
        var weapons = databases?.Weapons;

        currentWeapon = HeldWeaponResolver.ResolveMain(items, weapons, mainHandItemId);
        offHandItemWeapon = HeldWeaponResolver.ResolveItemWeapon(items, offHandItemId);
        currentOffHandWeapon = HeldWeaponResolver.ResolveOffHandAttackWeapon(items, offHandItemId);

        if (weaponController != null)
            weaponController.SetHeldWeapons(currentWeapon, currentOffHandWeapon);

        OnWeaponChange(this);
    }

    public WeaponData GetWeaponForAttackIndex(int attackIndex)
    {
        if (attackIndex == 1 && currentOffHandWeapon != null)
            return currentOffHandWeapon;
        return currentWeapon;
    }

    [SyncVar(hook = nameof(OnModelNameChanged))]
    public string modelName;
    public ModelData modelData;
    public GameObject modelInstance;

    public void OnModelNameChanged(string oldModelName, string newModelName)
    {
        if (isServer) return;
        SetModelData(newModelName);
    }

    public event Action<(UnitController unitController, GameObject modelInstance)> OnModelChange = delegate { };

    private void SetModelData(string modelName)
    {
        ModelData modelData = GameServices.Databases?.Models?.GetModelByName(modelName);
        if (modelData == null)
        {
            Debug.LogError($"Model {modelName} not found in database.");
            return;
        }
        this.modelData = modelData;
        if (modelInstance != null)
        {
            Destroy(modelInstance);
        }
        modelInstance = Instantiate(modelData.prefab, transform.position, transform.rotation, transform);
        DisableRootMotion(modelInstance);
        OnModelChange((this, modelInstance));
    }

    [Server]
    public void EquipModel(string modelName)
    {
        this.modelName = modelName;
        SetModelData(modelName);
    }

    public bool IsDead => health <= 0;
    public bool IsKnockedUp => _isKnockedUp;
    private Rigidbody unitRigidbody;
    private Collider unitCollider;

    // Dash state (server-authoritative)
    private bool _isDashing = false;
    private Vector3 _dashDirection = Vector3.zero; // normalized XZ
    private float _dashSpeed = 0f;
    private float _dashDistance = 0f;
    private Vector3 _dashStartPosition = Vector3.zero;
    private DashSpeedProfile _dashSpeedProfile = DashSpeedProfile.Constant;
    // Dash completion helpers
    private float _dashEndTime = 0f;               // absolute time when dash should end at the latest
    private float _lastDashTraveled = 0f;          // distance traveled along dash direction in previous FixedUpdate
    private int _dashStalledFrames = 0;            // consecutive frames with no meaningful progress
    private const string WallTag = "Wall";
    private const float DashWallImpactDotThreshold = 0.70710678f;
    private const float DashWallSweepBuffer = 0.05f;

    // Knockup state (server-authoritative)
    private bool _isKnockedUp = false;
    private float _knockupStartTime = 0f;
    private float _knockupDuration = 0f;
    private float _knockupHeight = 0f;
    private float _knockupBaseY = 0f;
    private Vector3 _knockupPlanarAnchor = Vector3.zero;
    private RigidbodyConstraints _knockupSavedConstraints;
    private bool _knockupConstraintsOverridden = false;

    private void OnEnable()
    {
        UnitRegistry.RegisterOrDefer(this);
    }

    private void OnDisable()
    {
        UnitRegistry.UnregisterOrDefer(this);
        EndKnockupConstraintOverride();
        _isKnockedUp = false;
    }

    private void OnDestroy()
    {
        UnitRegistry.UnregisterOrDefer(this);
        EndKnockupConstraintOverride();
        _isKnockedUp = false;
    }

    public event Action<(int current, int max)> OnHealthChange = delegate { };
    public event Action<(int current, int max)> OnShieldChange = delegate { };
    public event Action<(UnitController unitController, int attackIndex)> OnAttackStart = delegate { };
    public event Action<(UnitController attacker, int attackIndex)> OnAttackSwing = delegate { };
    public event Action<(UnitController target, UnitController attacker)> OnAttackHitReceived = delegate { };
    public event Action<(UnitController target, UnitController attacker)> OnTakeDamage = delegate { };
    public event Action<(UnitController target, UnitActionState.ActionStateData interruptedAction)> OnActionInterrupted = delegate { };
    public event Action<(UnitController target, UnitController attacker)> OnAfterTakeDamage = delegate { };
    public event Action<UnitController> OnHealed = delegate { };
    public event Action<(UnitController caster, int amount)> OnShielded = delegate { };
    public event Action<(UnitController targetUnit, ProjectileData projectile)> OnProjectileHit = delegate { };
    public event Action OnDied = delegate { };
    public event Action OnRevive = delegate { };
    [HideInInspector]
    public UnitMediator unitMediator;
    [HideInInspector]
    public UnitActionState unitActionState;

    void Awake()
    {
        weaponController = GetComponent<WeaponController>();
        unitMediator = GetComponent<UnitMediator>();
        unitActionState = GetComponent<UnitActionState>();
        unitRigidbody = GetComponent<Rigidbody>();
        unitCollider = GetComponent<Collider>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ConfigureClientObserverPhysics();
        RefreshHeldWeapons();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (unitRigidbody == null)
            unitRigidbody = GetComponent<Rigidbody>();
        if (unitCollider == null)
            unitCollider = GetComponent<Collider>();

        ConfigureClientObserverPhysics();
        RefreshHeldWeapons();

        if (isServer)
        {
            if (!string.IsNullOrEmpty(modelName))
            {
                EquipModel(modelName);
            }
        }
        RaiseHealthChangeEvent();
        RaiseShieldChangeEvent();

        if (!isServer)
        {
            if (!string.IsNullOrEmpty(modelName))
            {
                SetModelData(modelName);
            }
        }

        if (isServer)
        {
            ResetBasicAttackCritMeter();

            // On initial spawn, set collider state directly without triggering events
            // Die()/Revive() will be called by gameplay methods (TakeDamage, Heal, SetHealth) when appropriate
            if (health <= 0)
            {
                if (unitCollider != null)
                {
                    unitCollider.isTrigger = true;
                }
            }
            else
            {
                if (unitCollider != null)
                {
                    unitCollider.isTrigger = false;
                }
            }
        }
    }

    private void ConfigureClientObserverPhysics()
    {
        if (isServer)
            return;

        if (unitRigidbody == null)
            unitRigidbody = GetComponent<Rigidbody>();
        if (unitRigidbody == null)
            return;

        // Velocity sync on a dynamic rigidbody fights NetworkTransform interpolation
        // and makes units jitter on clients even with low RTT.
        if (TryGetComponent<Mirror.Experimental.NetworkRigidbody>(out var networkRigidbody))
            networkRigidbody.enabled = false;

        unitRigidbody.linearVelocity = Vector3.zero;
        unitRigidbody.angularVelocity = Vector3.zero;
        unitRigidbody.interpolation = RigidbodyInterpolation.None;
        // Continuous sweep CCD is invalid on kinematic bodies; switch first.
        unitRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        unitRigidbody.isKinematic = true;
    }

    private static void DisableRootMotion(GameObject model)
    {
        if (model == null)
            return;

        var animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
            animators[i].applyRootMotion = false;
    }

    void FixedUpdate()
    {
        if (isServer)
        {
            MovePlayer();
            ApplyLockedCastFacing();
            RotatePlayer();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;
    if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
        {
            TakeDamage(20, this);
        }
    if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            Heal(maxHealth, this);
            Shield(maxShield, this);
        }
    }

    [Server]
    public bool RollBasicAttackCrit(float baseChance)
    {
        baseChance = Mathf.Clamp01(baseChance);
        if (baseChance <= 0f)
        {
            return false;
        }

        _basicAttackCritMeter += baseChance;
        bool isCritical = UnityEngine.Random.value < Mathf.Clamp01(_basicAttackCritMeter);

        if (isCritical)
        {
            _basicAttackCritMeter = Mathf.Max(0f, _basicAttackCritMeter - 1f);
        }

        return isCritical;
    }

    [Server]
    private void ResetBasicAttackCritMeter()
    {
        _basicAttackCritMeter = 0f;
    }

    [Server]
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    [Server]
    public void SetMaxShield(int newMaxShield)
    {
        maxShield = newMaxShield;
        if (shield > maxShield)
        {
            shield = maxShield;
        }
    }

    [Server]
    private void MovePlayer()
    {
        if (IsDead)
        {
            EndKnockupConstraintOverride();
            _isKnockedUp = false;
            SetLinearVelocitySafe(Vector3.zero);
            return;
        }

        if (_isKnockedUp)
        {
            UpdateKnockupMotion();
            return;
        }

        // If currently dashing, override normal movement until distance reached
        if (_isDashing)
        {
            // keep motion constrained to XZ plane
            Vector3 flatPos = transform.position; flatPos.y = 0f;
            Vector3 flatStart = _dashStartPosition; flatStart.y = 0f;
            float traveled = Vector3.Project(flatPos - flatStart, _dashDirection).magnitude;
            float remaining = _dashDistance - traveled;

            // End conditions: reached distance, timed out, or stalled against an obstacle
            const float endEpsilon = 0.01f;           // small tolerance for completion
            const float stallEpsilon = 0.001f;        // minimal delta to consider as movement progress
            const int maxStallFrames = 3;             // how many fixed frames of no-progress to allow

            float currentSpeed = GetCurrentDashSpeed(traveled);

            float maxStep = currentSpeed * Time.fixedDeltaTime;

            bool timedOut = Time.time >= _dashEndTime && _dashEndTime > 0f;
            bool completed = remaining <= endEpsilon;
            bool progressed = (traveled - _lastDashTraveled) > stallEpsilon;
            bool hitWallHeadOn = !completed && ShouldStopDashForWallImpact(maxStep + DashWallSweepBuffer);
            _dashStalledFrames = progressed ? 0 : (_dashStalledFrames + 1);
            _lastDashTraveled = traveled;

            if (completed || timedOut || hitWallHeadOn || _dashStalledFrames >= maxStallFrames)
            {
                // End dash and stop dash velocity; normal movement resumes next frame
                StopDash();
            }
            else
            {
                if (remaining < maxStep && Time.fixedDeltaTime > 0f)
                {
                    // Scale the final velocity so we land exactly at the end distance
                    float scaledSpeed = remaining / Time.fixedDeltaTime;
                    SetLinearVelocitySafe(_dashDirection * scaledSpeed);
                }
                else
                {
                    SetLinearVelocitySafe(_dashDirection * currentSpeed);
                }
            }
            return;
        }

        var currentMoveSpeed = unitMediator.Stats.GetStat(StatType.MovementSpeed);

        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * currentMoveSpeed;
        SetLinearVelocitySafe(moveDirection);
    }

    [Server]
    private void UpdateKnockupMotion()
    {
        if (!_isKnockedUp)
        {
            return;
        }

        float elapsed = Time.time - _knockupStartTime;
        float duration = Mathf.Max(0.05f, _knockupDuration);

        if (elapsed >= duration)
        {
            _isKnockedUp = false;
            Vector3 landingPos = new Vector3(_knockupPlanarAnchor.x, 0f, _knockupPlanarAnchor.z);

            if (unitRigidbody != null && !unitRigidbody.isKinematic)
            {
                unitRigidbody.linearVelocity = Vector3.zero;
                unitRigidbody.position = landingPos;
            }

            transform.position = landingPos;

            EndKnockupConstraintOverride();
            return;
        }

        float t = Mathf.Clamp01(elapsed / duration);
        float yOffset = 4f * _knockupHeight * t * (1f - t);
        Vector3 airbornePos = new Vector3(_knockupPlanarAnchor.x, _knockupBaseY + yOffset, _knockupPlanarAnchor.z);

        if (unitRigidbody != null && !unitRigidbody.isKinematic)
        {
            unitRigidbody.linearVelocity = Vector3.zero;
            unitRigidbody.MovePosition(airbornePos);
        }
        else
        {
            transform.position = airbornePos;
        }
    }

    [Server]
    private void CancelKnockup(bool snapToBaseHeight)
    {
        if (!_isKnockedUp)
        {
            EndKnockupConstraintOverride();
            return;
        }

        _isKnockedUp = false;

        if (!snapToBaseHeight)
        {
            EndKnockupConstraintOverride();
            return;
        }

        Vector3 pos = transform.position;
        Vector3 snapped = new Vector3(pos.x, ResolveLandingY(pos.x, pos.z), pos.z);

        if (unitRigidbody != null && !unitRigidbody.isKinematic)
        {
            unitRigidbody.linearVelocity = Vector3.zero;
            unitRigidbody.MovePosition(snapped);
        }
        else
        {
            transform.position = snapped;
        }

        EndKnockupConstraintOverride();
    }

    [Server]
    private void SetLinearVelocitySafe(Vector3 velocity)
    {
        if (unitRigidbody == null || unitRigidbody.isKinematic)
        {
            return;
        }

        unitRigidbody.linearVelocity = velocity;
    }

    private float GetCurrentDashSpeed(float traveled)
    {
        if (_dashDistance <= 0f)
        {
            return _dashSpeed;
        }

        float progress = Mathf.Clamp01(traveled / _dashDistance);
        float minSpeed = _dashSpeed * 0.05f;

        switch (_dashSpeedProfile)
        {
            case DashSpeedProfile.EaseOut:
                return Mathf.Max(_dashSpeed * (1f - progress), minSpeed);
            case DashSpeedProfile.EaseIn:
                return Mathf.Max(_dashSpeed * progress, minSpeed);
            default:
                return _dashSpeed;
        }
    }

    [Server]
    private void StopDash()
    {
        _isDashing = false;
        _dashEndTime = 0f;
        _dashStalledFrames = 0;
        _lastDashTraveled = 0f;
        SetLinearVelocitySafe(Vector3.zero);
    }

    [Server]
    private bool ShouldStopDashForWallImpact(float sweepDistance)
    {
        if (sweepDistance <= 0f || unitRigidbody == null || unitRigidbody.isKinematic)
        {
            return false;
        }

        if (!unitRigidbody.SweepTest(_dashDirection, out RaycastHit hit, sweepDistance, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (hit.collider == null || hit.collider == unitCollider || hit.collider.isTrigger)
        {
            return false;
        }

        if (!hit.collider.CompareTag(WallTag))
        {
            return false;
        }

        Vector3 wallNormal = hit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        wallNormal.Normalize();
        float impactAlignment = Vector3.Dot(_dashDirection, -wallNormal);
        return impactAlignment >= DashWallImpactDotThreshold;
    }

    [SerializeField]
    private float baseTurnSpeed = 20f;

    /// <summary>
    /// While a skill cast has a fixed aim and turn speed is 0, snap facing to that aim.
    /// Slow turn (e.g. 0.05) still lerps toward the mouse.
    /// </summary>
    [Server]
    private void ApplyLockedCastFacing()
    {
        if (IsDead || unitMediator?.Skills == null)
            return;

        if (!unitMediator.Skills.TryGetLockedCastFacingYaw(out float yaw))
            return;

        angle = yaw;
        ApplyFacingYawImmediate(yaw);
    }

    [Server]
    private void RotatePlayer()
    {
        if (IsDead) return;

        float turnSpeed = unitMediator.Stats.GetStat(StatType.TurnSpeed); // Should be in range [0,1]
        if (turnSpeed <= 0f) return; // Do not turn if turnSpeed is 0

        float currentY = transform.rotation.eulerAngles.y;
        float targetY = angle;
        float lerpedAngle = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * baseTurnSpeed * turnSpeed);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }

    /// <summary>
    /// Instantly faces <paramref name="aimPoint"/> using the same yaw path as <see cref="RotatePlayer"/>.
    /// </summary>
    [Server]
    public void SnapFacingToAimPoint(Vector3 aimPoint)
    {
        if (IsDead) return;

        angle = SkillAimUtil.GetFacingAngleYaw(transform.position, aimPoint);
        ApplyFacingYawImmediate(angle);
    }

    /// <summary>
    /// Instantly applies a horizontal aim rotation using the same yaw path as <see cref="RotatePlayer"/>.
    /// </summary>
    [Server]
    public void SnapFacingToAimRotation(Quaternion aimRotation)
    {
        if (IsDead) return;

        angle = SkillAimUtil.GetFacingAngleYaw(aimRotation);
        ApplyFacingYawImmediate(angle);
    }

    /// <summary>
    /// Local-player visual snap so facing matches confirm aim before NetworkTransform catches up.
    /// Host skips this — server <see cref="SnapFacingToAimPoint"/> already ran in the same command.
    /// </summary>
    public void ClientPredictFacingSnap(Vector3 aimPoint)
    {
        if (isServer || IsDead)
            return;

        float yaw = SkillAimUtil.GetFacingAngleYaw(transform.position, aimPoint);
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up);
    }

    [Server]
    private void ApplyFacingYawImmediate(float yaw)
    {
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up);
        // Do not ServerTeleport here: rotation-only snaps are synced through the normal
        // NetworkTransform path so clients interpolate smoothly. Teleport resets snapshot
        // buffers and hard-snaps position + rotation on every observer (host twice).
    }

    [Server]
    public void TakeDamage(int damage, UnitController attacker)
    {
        if (IsDead) return;

        float damageMultiplier = GetIncomingDamageMultiplier();
        int reducedDamage = Mathf.CeilToInt(damage * damageMultiplier);
        damage = Mathf.Max(0, reducedDamage);

        ApplyFinalDamage(damage, attacker, false);
    }

    [Server]
    public void TakeDamage(DamageInstance damage, UnitController attacker)
    {
        if (IsDead) return;

        int finalDamage = DamageCalculator.Calculate(damage, this, attacker, out bool wasCritical);
        ApplyFinalDamage(finalDamage, attacker, wasCritical);
    }

    private void ApplyFinalDamage(int damage, UnitController attacker, bool wasCritical)
    {
        int originalShield = shield;
        int originalHealth = health;
        int shieldDamage = Mathf.Min(originalShield, damage);
        int damageAfterShield = damage - shieldDamage;
        int healthDamage = Mathf.Min(originalHealth, damageAfterShield);
        int actualDamage = shieldDamage + healthDamage;

        OnTakeDamageEvent(damage, actualDamage, attacker, wasCritical);

        if (shieldDamage > 0)
        {
            shield = Mathf.Max(0, originalShield - shieldDamage);
        }

        if (healthDamage <= 0)
        {
            OnAfterTakeDamageEvent(actualDamage, attacker);
            return;
        }

        // Reduce the health points by the remaining damage that made it through the shield.
        var newHealth = originalHealth - healthDamage;

        health = Mathf.Clamp(newHealth, 0, maxHealth);

        if (health <= 0)
        {
            OnKillEvent(attacker);
            Die();
        }
        OnAfterTakeDamageEvent(actualDamage, attacker);
    }

    private float GetIncomingDamageMultiplier()
    {
        float dr = 0f;
        if (unitMediator != null)
        {
            dr = unitMediator.Stats.GetStat(StatType.DamageReduction);
        }
        float multiplier = 1f - Mathf.Clamp01(dr);
        return multiplier;
    }

    [Server]
    public void OnKillEvent(UnitController killer)
    {
        InterruptAction();
        GameMessages.Publish(new UnitDiedEvent(this, killer));
        RpcOnKill(this, killer);
    }

    [ClientRpc]
    public void RpcOnKill(UnitController victim, UnitController killer)
    {
        if (isServer) return;
        GameMessages.Publish(new UnitDiedEvent(victim, killer));
    }

    [Server]
    public void OnTakeDamageEvent(int damage, int appliedDamage, UnitController attacker, bool wasCritical)
    {
        GameMessages.Publish(new UnitDamagedEvent(this, attacker, damage, appliedDamage, wasCritical));
        OnTakeDamage((this, attacker));
        RpcOnTakenDamage(damage, appliedDamage, attacker, wasCritical);
    }

    [ClientRpc]
    public void RpcOnTakenDamage(int damage, int appliedDamage, UnitController attacker, bool wasCritical)
    {
        if (isServer) return;
        OnTakeDamage((this, attacker));
        GameMessages.Publish(new UnitDamagedEvent(this, attacker, damage, appliedDamage, wasCritical));
    }

    [Server]
    public void OnAfterTakeDamageEvent(int damage, UnitController attacker)
    {
        OnAfterTakeDamage((this, attacker));
        RpcOnAfterTakenDamage(damage, attacker);
    }

    [ClientRpc]
    public void RpcOnAfterTakenDamage(int damage, UnitController attacker)
    {
        if (isServer) return;
        OnAfterTakeDamage((this, attacker));
    }

    [Server]
    public void Attack()
    {
        if (IsDead || IsKnockedUp) return;
        _ = weaponController.Attack(this);
    }

    // Heal the unit
    [Server]
    public void Heal(int amount, UnitController healer)
    {
        var oldHealth = health;
        if (health == 0 && amount > 0)
        {
            Revive();
        }
        // Increase the health by the heal amount
        health = Mathf.Min(health + amount, maxHealth);

        GameMessages.Publish(new UnitHealedEvent(this, amount, oldHealth, health, healer));
        OnHealed(this);

        RpcOnHeal(amount, oldHealth, health, healer);
    }

    [ClientRpc]
    public void RpcOnHeal(int amount, int oldHealth, int newHealth, UnitController healer)
    {
        if (isServer) return;
        GameMessages.Publish(new UnitHealedEvent(this, amount, oldHealth, newHealth, healer));
        OnHealed(this);
    }

    // Shield the unit
    [Server]
    public void Shield(int amount, UnitController shielder)
    {
        if (IsDead) return;

        var oldShield = shield;

        // Increase the shield by the shield amount
        shield = Mathf.Min(shield + amount, maxShield);
        GameMessages.Publish(new UnitShieldedEvent(this, amount, oldShield, shield, shielder));
        OnShielded((this, amount));
        RpcOnShield(amount, oldShield, shield, shielder);
    }

    [ClientRpc]
    public void RpcOnShield(int amount, int oldShield, int newShield, UnitController shielder)
    {
        if (isServer) return;
        OnShielded((this, amount));
        GameMessages.Publish(new UnitShieldedEvent(this, amount, oldShield, newShield, shielder));
    }

    [Server]
    public void RaiseOnAttackHitReceivedEvent(UnitController attacker)
    {
        OnAttackHitReceived((this, attacker));
        RpcRaiseOnAttackHitReceivedEvent(attacker);
    }

    [ClientRpc]
    public void RpcRaiseOnAttackHitReceivedEvent(UnitController attacker)
    {
        if (isServer) return;
        OnAttackHitReceived((this, attacker));
    }

    [Server]
    public void RaiseOnProjectileHitEvent(UnitController target, ProjectileData projectile)
    {
        OnProjectileHit((target, projectile));
        RpcOnProjectileHit(target, projectile.name);
    }

    [ClientRpc]
    public void RpcOnProjectileHit(UnitController target, string projectileName)
    {
        if (isServer) return;
        var projectile = GameServices.Databases?.Projectiles?.GetProjectileByName(projectileName);
        OnProjectileHit((target, projectile));
    }

    private void Die()
    {
        if (isServer)
        {
            ResetBasicAttackCritMeter();
            CancelKnockup(true);
            StopDash();

            // Always interrupt ongoing actions on death so cast/channel coroutines are cancelled,
            // including death paths that bypass OnKillEvent (e.g. direct SetHealth(0)).
            InterruptAction();
        }

        SnapToGroundAtCurrentPosition();

        if (unitCollider != null)
        {
            unitCollider.isTrigger = true;
        }
        RaiseOnDiedEvent();
    }

    private void SnapToGroundAtCurrentPosition()
    {
        float landingY = ResolveLandingY(transform.position.x, transform.position.z);
        Vector3 snapped = new Vector3(transform.position.x, landingY, transform.position.z);

        if (unitRigidbody != null && !unitRigidbody.isKinematic)
        {
            unitRigidbody.linearVelocity = Vector3.zero;
            unitRigidbody.position = snapped;
        }

        transform.position = snapped;
    }

    private void Revive()
    {
        if (isServer)
        {
            ResetBasicAttackCritMeter();
        }

        if (unitCollider != null)
        {
            unitCollider.isTrigger = false;
        }
        RaiseOnReviveEvent();
    }

    void HookOnHealthChanged(int oldValue, int newValue)
    {
        if (!isServer && oldValue > 0 && newValue <= 0)
        {
            Die();
        }
        if (!isServer && oldValue == 0 && newValue > 0)
        {
            Revive();
        }
        RaiseHealthChangeEvent();
    }
    void HookOnMaxHealthChanged(int oldValue, int newValue)
    {
        RaiseHealthChangeEvent();
    }

    void HookOnShieldChanged(int oldValue, int newValue)
    {
        RaiseShieldChangeEvent();
    }
    void HookOnMaxShieldChanged(int oldValue, int newValue)
    {
        RaiseShieldChangeEvent();
    }

    private void RaiseHealthChangeEvent()
    {
        OnHealthChange((current: health, max: maxHealth));
    }

    private void RaiseShieldChangeEvent()
    {
        OnShieldChange((current: shield, max: maxShield));
    }

    [Server]
    public void RaiseOnAttackStartEvent(int attackIndex)
    {
        LastAttackIndex = attackIndex;
        OnAttackStart((this, attackIndex));
        RpcRaiseOnAttackStartEvent(attackIndex);
    }

    [ClientRpc]
    public void RpcRaiseOnAttackStartEvent(int attackIndex)
    {
        if (isServer) return;
        LastAttackIndex = attackIndex;
        OnAttackStart((this, attackIndex));
    }

    private void RaiseOnDiedEvent()
    {
        OnDied();
    }

    private void RaiseOnReviveEvent()
    {
        OnRevive();
    }

    [Server]
    public void RaiseOnAttackSwingEvent(int attackIndex)
    {
        OnAttackSwing((this, attackIndex));
        RpcRaiseOnAttackSwingEvent(attackIndex);
    }

    [ClientRpc]
    public void RpcRaiseOnAttackSwingEvent(int attackIndex)
    {
        if (isServer) return;
        OnAttackSwing((this, attackIndex));
    }

    [Server]
    public void SetHealth(int newHealth)
    {
        int oldHealth = health;
        health = Mathf.Clamp(newHealth, 0, maxHealth);

        if (oldHealth > 0 && health <= 0)
        {
            Die();
        }
        else if (oldHealth <= 0 && health > 0)
        {
            Revive();
        }

        RaiseHealthChangeEvent();
    }

    [Server]
    public void SetShield(int newShield)
    {
        shield = Mathf.Clamp(newShield, 0, maxShield);
        RaiseShieldChangeEvent();
    }

    [Server]
    public void StartDash(Vector3 direction, float speed, float distance)
    {
        StartDash(direction, speed, distance, DashSpeedProfile.Constant);
    }

    [Server]
    public void StartDash(Vector3 direction, float speed, float distance, bool decelerate)
    {
        StartDash(direction, speed, distance, decelerate ? DashSpeedProfile.EaseOut : DashSpeedProfile.Constant);
    }

    [Server]
    public void StartDash(Vector3 direction, float speed, float distance, DashSpeedProfile speedProfile)
    {
        if (IsDead) return;
        CancelKnockup(false);

        // Only allow flat XZ dashes and non-zero direction
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        _dashDirection = direction.normalized;
        _dashSpeed = Mathf.Max(0f, speed);
        _dashDistance = Mathf.Max(0f, distance);
        _dashStartPosition = transform.position;
        _dashStartPosition.y = 0f;
        _dashSpeedProfile = speedProfile;
        _isDashing = _dashDistance > 0f && _dashSpeed > 0f;

        // Initialize dash completion helpers
        _lastDashTraveled = 0f;
        _dashStalledFrames = 0;
        // Safety timeout: expected dash duration + generous fudge for deceleration
        float expectedDuration = (_dashSpeed > 0f) ? (_dashDistance / _dashSpeed) : 0f;
        bool variableSpeedProfile = speedProfile != DashSpeedProfile.Constant;
        float timeoutFudge = variableSpeedProfile ? 0.5f : 0.1f;
        _dashEndTime = Time.time + Mathf.Max(0.05f, expectedDuration * (variableSpeedProfile ? 2f : 1f) + timeoutFudge);
    }

    [Server]
    public void StartKnockup(float height, float duration, bool interruptCurrentAction = true)
    {
        if (IsDead) return;

        CancelKnockup(false);

        if (interruptCurrentAction)
        {
            InterruptAction();
        }

        StopDash();

        _knockupHeight = Mathf.Max(0f, height);
        _knockupDuration = Mathf.Max(0.05f, duration);
        _knockupStartTime = Time.time;

        Vector3 currentPos = transform.position;
        _knockupBaseY = currentPos.y;
        _knockupPlanarAnchor = new Vector3(currentPos.x, 0f, currentPos.z);
        _isKnockedUp = _knockupHeight > 0f;

        if (_isKnockedUp && unitRigidbody != null && !unitRigidbody.isKinematic)
        {
            BeginKnockupConstraintOverride();
            unitRigidbody.linearVelocity = Vector3.zero;
        }
    }

    [Server]
    private void BeginKnockupConstraintOverride()
    {
        if (unitRigidbody == null || unitRigidbody.isKinematic)
        {
            return;
        }

        if (_knockupConstraintsOverridden)
        {
            return;
        }

        _knockupSavedConstraints = unitRigidbody.constraints;
        unitRigidbody.constraints = _knockupSavedConstraints & ~RigidbodyConstraints.FreezePositionY;
        _knockupConstraintsOverridden = true;
    }

    private void EndKnockupConstraintOverride()
    {
        if (!_knockupConstraintsOverridden)
        {
            return;
        }

        if (unitRigidbody != null && !unitRigidbody.isKinematic)
        {
            unitRigidbody.constraints = _knockupSavedConstraints;
        }

        _knockupConstraintsOverridden = false;
    }

    private float ResolveLandingY(float x, float z)
    {
        return 0f;
    }

    /// <summary>
    /// Interrupts any ongoing action (attack, skill cast, channel, etc.)
    /// Only callable by the server.
    /// </summary>
    [Server]
    public void InterruptAction()
    {
        var isActionToInterrupt = unitActionState != null && unitActionState.IsActive;

        // Dont interrupt if there's no action ongoing
        if (!isActionToInterrupt) return;
        
        var interruptedAction = unitActionState.state;
        // Clear the current action state
        if (unitActionState != null && unitActionState.IsActive)
        {
            unitActionState.SetUnitActionStateToIdle();
        }

        // Signal the weapon controller to stop attacking
        if (weaponController != null)
        {
            weaponController.CancelAttack();
        }

        // Raise the interrupt event for any subscribed listeners (e.g., skills, abilities)
        OnActionInterrupted((this, interruptedAction));

        // Network the interrupt to all clients
        RpcOnActionInterrupted(interruptedAction);
    }

    [ClientRpc]
    private void RpcOnActionInterrupted(UnitActionState.ActionStateData interruptedAction)
    {
        if (isServer) return;
        OnActionInterrupted((this, interruptedAction));
    }
}

public enum UnitType : byte
{
    Player = 0,
    Zombie = 1,
    Spirit = 2,
    Structure = 3
}