using System;
using Mirror;
using MyGame.Events;
using UnityEngine;

public class UnitController : NetworkBehaviour
{

    Rigidbody unitRigidbody;
    [SyncVar]
    public float horizontalInput = 0f;
    [SyncVar]
    public float verticalInput = 0f;
    [SyncVar]
    public float angle = 0f;
    [SyncVar(hook = nameof(HookOnHealthChanged))]
    public int Health = 100;
    [SyncVar(hook = nameof(HookOnMaxHealthChanged))]
    public int maxHealth = 100;
    [SyncVar(hook = nameof(HookOnShieldChanged))]
    public int shield = 50;
    [SyncVar(hook = nameof(HookOnMaxShieldChanged))]
    public int maxShield = 50;
    [SyncVar]
    public float moveSpeed = 5f;
    [SyncVar(hook = nameof(HookOnRaceChanged))]
    public Race Race = Race.Ninja;
    public bool IsDead => Health <= 0;
    public Weapon weapon;
    public UnitType unitType;
    public event Action<(int current, int max)> OnHealthChange = delegate {};
    public event Action<(int current, int max)> OnShieldChange = delegate {};
    public event Action<UnitController> OnAttackStart = delegate {};
    public event Action<UnitController> OnTakeDamage = delegate {};
    public event Action OnDied = delegate {};
    public event Action OnRevive = delegate {};
    public static event Action<(UnitController killer, UnitController victim)> OnKill = delegate {};

    // Start is called before the first frame update
    void Start()
    {
        if (isServer) {
            Health = maxHealth;
        }
        unitRigidbody = GetComponent<Rigidbody>();
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20, this);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Heal(maxHealth);
            Shield(maxShield);
        }
    }

    [Server]
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (Health > maxHealth)
        {
            Health = maxHealth;
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

        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * moveSpeed;
        unitRigidbody.linearVelocity = moveDirection;
    }

    [Server]
    private void RotatePlayer()
    {
        if(IsDead) return;

        float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, angle, Time.deltaTime * 10);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }

    [Server]
    public void TakeDamage(int damage, UnitController attacker)
    {
        RaiseOnTakeDamageEvent();
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
        Health -= damage;

        Health = Mathf.Clamp(Health, 0, maxHealth);

        if (Health <= 0)
        {
            RaiseOnKillEvent(attacker, this);
            OnKillEvent(attacker);
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
        RpcOnTakenDamage(damage, attacker);
    }

    [ClientRpc]
    public void RpcOnTakenDamage(int damage, UnitController attacker)
    {
        if(isServer) return;
        EventManager.Instance.Publish(new UnitDamagedEvent(this, attacker, damage));
    }

    [Server]
    public void Attack() {
        if (IsDead) return;

        _ = weapon.Attack(this);
    }

    // Heal the unit
    [Server]
    public void Heal(int amount)
    {
        if (Health == 0)
        {
            Revive();
        }
        // Increase the health by the heal amount
        Health = Mathf.Min(Health + amount, maxHealth);

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
        unitRigidbody.detectCollisions = false;
        RaiseOnDiedEvent();
    }

    private void Revive()
    {
        unitRigidbody.detectCollisions = true;
        RaiseOnReviveEvent();
    }

    void HookOnHealthChanged(int oldValue, int newValue)
    {
        if (oldValue > 0 && newValue <= 0)
        {
            Die();
        }
        if(oldValue == 0 && newValue > 0)
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

    void HookOnRaceChanged(Race oldValue, Race newValue)
    {
        Debug.Log(newValue);
    }

    private void RaiseHealthChangeEvent()
    {
        OnHealthChange((current: Health, max: maxHealth));
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

    private void RaiseOnKillEvent(UnitController killer, UnitController victim)
    {
        OnKill((killer, victim));
    }

    [ClientRpc]
    private void RaiseOnTakeDamageEvent()
    {
        OnTakeDamage(this);
    }
}

public enum Race
{
    Ninja,
    Grunt,
    Warrior,
    Sensei
}

public enum UnitType : byte
{
    Player,
    Zombie,
}