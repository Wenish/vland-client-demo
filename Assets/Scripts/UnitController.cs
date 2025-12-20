using System;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class UnitController : NetworkBehaviour
{
    [SyncVar]
    public UnitType unitType;

    public event Action<UnitController> OnTeamChanged = delegate { };

    [SyncVar(hook = nameof(HookOnTeamChanged))]
    public int team;

    [Header("Team")]
    [Tooltip("Set the team number in the editor. During Play mode, if you're the server/host, changing this will update the networked team.")]
    [SerializeField, Min(0)]
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

    [SyncVar(hook = nameof(OnWeaponNameChanged))]
    public string weaponName;
    public WeaponData currentWeapon;
    public WeaponController weaponController;
    public event Action<UnitController> OnWeaponChange = delegate { };

    private void OnWeaponNameChanged(string oldWeaponName, string newWeaponName)
    {
        if (isServer) return;
        SetWeaponData(newWeaponName);
    }

    private void SetWeaponData(string weaponName)
    {
        WeaponData weaponData = DatabaseManager.Instance.weaponDatabase.GetWeaponByName(weaponName);
        if (weaponData == null)
        {
            Debug.LogError($"Weapon {weaponName} not found in database.");
            return;
        }
        currentWeapon = weaponData;
        weaponController.weaponData = weaponData;
        OnWeaponChange(this);
    }

    [Server]
    public void EquipWeapon(string weaponName)
    {
        this.weaponName = weaponName;
        SetWeaponData(weaponName);
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
        ModelData modelData = DatabaseManager.Instance.modelDatabase.GetModelByName(modelName);
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
        OnModelChange((this, modelInstance));
    }

    [Server]
    public void EquipModel(string modelName)
    {
        this.modelName = modelName;
        SetModelData(modelName);
    }

    public bool IsDead => health <= 0;
    private Rigidbody unitRigidbody;
    private Collider unitCollider;

    // Dash state (server-authoritative)
    private bool _isDashing = false;
    private Vector3 _dashDirection = Vector3.zero; // normalized XZ
    private float _dashSpeed = 0f;
    private float _dashDistance = 0f;
    private Vector3 _dashStartPosition = Vector3.zero;
    // Dash completion helpers
    private float _dashEndTime = 0f;               // absolute time when dash should end at the latest
    private float _lastDashTraveled = 0f;          // distance traveled along dash direction in previous FixedUpdate
    private int _dashStalledFrames = 0;            // consecutive frames with no meaningful progress

    public event Action<(int current, int max)> OnHealthChange = delegate { };
    public event Action<(int current, int max)> OnShieldChange = delegate { };
    public event Action<(UnitController unitController, int attackIndex)> OnAttackStart = delegate { };
    public event Action<(UnitController attacker, int attackIndex)> OnAttackSwing = delegate { };
    public event Action<(UnitController target, UnitController attacker)> OnAttackHitReceived = delegate { };
    public event Action<(UnitController target, UnitController attacker)> OnTakeDamage = delegate { };
    public event Action<UnitController> OnHealed = delegate { };
    public event Action<(UnitController caster, int amount)> OnShielded = delegate { };
    public event Action<(UnitController targetUnit, ProjectileData projectile)> OnProjectileHit = delegate { };
    public event Action OnDied = delegate { };
    public event Action OnRevive = delegate { };
    public UnitMediator unitMediator;
    public UnitActionState unitActionState;

    void Awake()
    {
        weaponController = GetComponent<WeaponController>();
        unitMediator = GetComponent<UnitMediator>();
        unitActionState = GetComponent<UnitActionState>();
    }
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            if (!string.IsNullOrEmpty(modelName))
            {
                EquipModel(modelName);
            }

            if (!string.IsNullOrEmpty(weaponName))
            {
                EquipWeapon(weaponName);
            }
        }
        unitRigidbody = GetComponent<Rigidbody>();
        unitCollider = GetComponent<Collider>();
        RaiseHealthChangeEvent();
        RaiseShieldChangeEvent();

        if (!isServer)
        {
            if (!string.IsNullOrEmpty(weaponName))
            {
                SetWeaponData(weaponName);
            }
            if (!string.IsNullOrEmpty(modelName))
            {
                SetModelData(modelName);
            }
        }

        if (isServer)
        {
            if (health <= 0)
            {
                Die();
            }
            else
            {
                Revive();
            }
        }
    }

    void FixedUpdate()
    {
        if (isServer)
        {
            MovePlayer();
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
            unitRigidbody.linearVelocity = Vector3.zero;
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

            bool timedOut = Time.time >= _dashEndTime && _dashEndTime > 0f;
            bool completed = remaining <= endEpsilon;
            bool progressed = (traveled - _lastDashTraveled) > stallEpsilon;
            _dashStalledFrames = progressed ? 0 : (_dashStalledFrames + 1);
            _lastDashTraveled = traveled;

            if (completed || timedOut || _dashStalledFrames >= maxStallFrames)
            {
                // End dash and stop dash velocity; normal movement resumes next frame
                _isDashing = false;
                unitRigidbody.linearVelocity = Vector3.zero;
            }
            else
            {
                float maxStep = _dashSpeed * Time.fixedDeltaTime;
                if (remaining < maxStep && Time.fixedDeltaTime > 0f)
                {
                    // Scale the final velocity so we land exactly at the end distance
                    float scaledSpeed = remaining / Time.fixedDeltaTime;
                    unitRigidbody.linearVelocity = _dashDirection * scaledSpeed;
                }
                else
                {
                    unitRigidbody.linearVelocity = _dashDirection * _dashSpeed;
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
        unitRigidbody.linearVelocity = moveDirection;
    }

    [SerializeField]
    private float baseTurnSpeed = 20f;

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

    [Server]
    public void TakeDamage(int damage, UnitController attacker)
    {
        if (IsDead) return;

        float damageMultiplier = GetIncomingDamageMultiplier();
        int reducedDamage = Mathf.CeilToInt(damage * damageMultiplier);
        damage = Mathf.Max(0, reducedDamage);

        OnTakeDamageEvent(damage, attacker);
        // If the unit has a shield, reduce the shield points first
        if (shield > 0)
        {
            shield -= damage;
            if (shield < 0)
            {
                damage = -shield;
                shield = 0;
            }
            else
            {
                return;
            }
        }

        // Reduce the health points by the remaining damage
        var newHealth = health - damage;

        health = Mathf.Clamp(newHealth, 0, maxHealth);

        if (health <= 0)
        {
            OnKillEvent(attacker);
            Die();
        }
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
        EventManager.Instance.Publish(new UnitDiedEvent(this, killer));
        RpcOnKill(this, killer);
    }

    [ClientRpc]
    public void RpcOnKill(UnitController victim, UnitController killer)
    {
        if (isServer) return;
        EventManager.Instance.Publish(new UnitDiedEvent(victim, killer));
    }

    [Server]
    public void OnTakeDamageEvent(int damage, UnitController attacker)
    {
        EventManager.Instance.Publish(new UnitDamagedEvent(this, attacker, damage));
        OnTakeDamage((this, attacker));
        RpcOnTakenDamage(damage, attacker);
    }

    [ClientRpc]
    public void RpcOnTakenDamage(int damage, UnitController attacker)
    {
        if (isServer) return;
        OnTakeDamage((this, attacker));
        EventManager.Instance.Publish(new UnitDamagedEvent(this, attacker, damage));
    }

    [Server]
    public void Attack()
    {
        if (IsDead) return;
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

        EventManager.Instance.Publish(new UnitHealedEvent(this, amount, oldHealth, health, healer));
        OnHealed(this);

        RpcOnHeal(amount, oldHealth, health, healer);
    }

    [ClientRpc]
    public void RpcOnHeal(int amount, int oldHealth, int newHealth, UnitController healer)
    {
        if (isServer) return;
        EventManager.Instance.Publish(new UnitHealedEvent(this, amount, oldHealth, newHealth, healer));
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
        EventManager.Instance.Publish(new UnitShieldedEvent(this, amount, oldShield, shield, shielder));
        OnShielded((this, amount));
        RpcOnShield(amount, oldShield, shield, shielder);
    }

    [ClientRpc]
    public void RpcOnShield(int amount, int oldShield, int newShield, UnitController shielder)
    {
        if (isServer) return;
        OnShielded((this, amount));
        EventManager.Instance.Publish(new UnitShieldedEvent(this, amount, oldShield, newShield, shielder));
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
        var projectile = DatabaseManager.Instance.projectileDatabase.GetProjectileByName(projectileName);
        OnProjectileHit((target, projectile));
    }

    private void Die()
    {
        if (unitCollider != null)
        {
            unitCollider.isTrigger = true;
        }
        RaiseOnDiedEvent();
    }

    private void Revive()
    {
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
        OnAttackStart((this, attackIndex));
        RpcRaiseOnAttackStartEvent(attackIndex);
    }

    [ClientRpc]
    public void RpcRaiseOnAttackStartEvent(int attackIndex)
    {
        if (isServer) return;
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
        if (IsDead) return;
        // Only allow flat XZ dashes and non-zero direction
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        _dashDirection = direction.normalized;
        _dashSpeed = Mathf.Max(0f, speed);
        _dashDistance = Mathf.Max(0f, distance);
        _dashStartPosition = transform.position;
        _dashStartPosition.y = 0f;
        _isDashing = _dashDistance > 0f && _dashSpeed > 0f;

        // Initialize dash completion helpers
        _lastDashTraveled = 0f;
        _dashStalledFrames = 0;
        // Safety timeout: expected dash duration + small fudge
        float expectedDuration = (_dashSpeed > 0f) ? (_dashDistance / _dashSpeed) : 0f;
        _dashEndTime = Time.time + Mathf.Max(0.05f, expectedDuration + 0.1f);
    }
}

public enum UnitType : byte
{
    Player,
    Zombie,
    Spirit
}