using System;
using Mirror;
using MyGame.Events;
using UnityEngine;

public class UnitController : NetworkBehaviour
{
    [SyncVar]
    public UnitType unitType;

    [SyncVar]
    public int team;

    [SyncVar]
    public string unitName;

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
    public event Action<UnitController> OnWeaponChange = delegate {};

    private void OnWeaponNameChanged(string oldWeaponName, string newWeaponName)
    {
        if (isServer) return;
        SetWeaponData(newWeaponName);
    }

    private void SetWeaponData(string weaponName) {
        WeaponData weaponData = DatabaseManager.Instance.weaponDatabase.GetWeaponByName(weaponName);
        if (weaponData == null) {
            Debug.LogError($"Weapon {weaponName} not found in database.");
            return;
        }
        currentWeapon = weaponData;
        weaponController.weaponData = weaponData;
        OnWeaponChange(this);
    }

    [Server]
    public void EquipWeapon(string weaponName) {
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

    private void SetModelData(string modelName) {
        ModelData modelData = DatabaseManager.Instance.modelDatabase.GetModelByName(modelName);
        if (modelData == null) {
            Debug.LogError($"Model {modelName} not found in database.");
            return;
        }
        this.modelData = modelData;
        if (modelInstance != null) {
            Destroy(modelInstance);
        }
        modelInstance = Instantiate(modelData.prefab, transform.position, transform.rotation, transform);
    }

    [Server]
    public void EquipModel(string modelName) {
        this.modelName = modelName;
        SetModelData(modelName);
    }

    public bool IsDead => health <= 0;
    private Rigidbody unitRigidbody;
    private Collider unitCollider;

    public event Action<(int current, int max)> OnHealthChange = delegate { };
    public event Action<(int current, int max)> OnShieldChange = delegate {};
    public event Action<UnitController> OnAttackStart = delegate {};
    public event Action<UnitController> OnTakeDamage = delegate {};
    public event Action OnDied = delegate {};
    public event Action OnRevive = delegate {};
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
        if (isServer) {
            health = maxHealth;
        }
        unitRigidbody = GetComponent<Rigidbody>();
        unitCollider = GetComponent<Collider>();
        RaiseHealthChangeEvent();
        RaiseShieldChangeEvent();
    }

    void FixedUpdate()
    {
        if (isServer) {
            MovePlayer();
            RotatePlayer();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;
        if (Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(20, this);
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            Heal(maxHealth);
            Shield(maxShield);
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
        if(IsDead) {
            unitRigidbody.linearVelocity = Vector3.zero;
            return;
        };

        var currentMoveSpeed = unitMediator.Stats.GetStat(StatType.MovementSpeed);

        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * currentMoveSpeed;
        unitRigidbody.linearVelocity = moveDirection;
    }

    [Server]
    private void RotatePlayer()
    {
        if(IsDead) return;

        float turnSpeed = unitMediator.Stats.GetStat(StatType.TurnSpeed); // Should be in range [0,1]
        if (turnSpeed <= 0f) return; // Do not turn if turnSpeed is 0

        float currentY = transform.rotation.eulerAngles.y;
        float targetY = angle;
        float lerpedAngle = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * 10 * turnSpeed);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }

    [Server]
    public void TakeDamage(int damage, UnitController attacker)
    {
        if (IsDead) return;

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
        OnTakeDamage(this);
        RpcOnTakenDamage(damage, attacker);
    }

    [ClientRpc]
    public void RpcOnTakenDamage(int damage, UnitController attacker)
    {
        if(isServer) return;
        OnTakeDamage(this);
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
    public void Heal(int amount)
    {
        if (health == 0 && amount > 0)
        {
            Revive();
        }
        // Increase the health by the heal amount
        health = Mathf.Min(health + amount, maxHealth);

        RpcOnHeal(amount);
    }

    [ClientRpc]
    public void RpcOnHeal(int amount)
    {
        EventManager.Instance.Publish(new UnitHealedEvent(this, amount));
    }

    // Shield the unit
    [Server]
    public void Shield(int amount)
    {
        if (IsDead) return;

        // Increase the shield by the shield amount
        shield = Mathf.Min(shield + amount, maxShield);
        RpcOnShield(amount);
    }

    [ClientRpc]
    public void RpcOnShield(int amount)
    {
        EventManager.Instance.Publish(new UnitShieldedEvent(this, amount));
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
        if(!isServer && oldValue == 0 && newValue > 0)
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

    [ClientRpc]
    public void RaiseOnAttackStartEvent()
    {
        OnAttackStart(this);
    }

    private void RaiseOnDiedEvent()
    {
        OnDied();
    }

    private void RaiseOnReviveEvent()
    {
        OnRevive();
    }
}

public enum UnitType : byte
{
    Player,
    Zombie,
}