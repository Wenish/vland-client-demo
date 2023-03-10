using System;
using UnityEngine;

public class UnitController : MonoBehaviour
{

    Rigidbody unitRigidbody;
    public float horizontalInput = 0f;
    public float verticalInput = 0f;
    public float angle = 0f;

    public int health = 100;
    public int maxHealth = 100;
    public int shield = 50;
    public int maxShield = 50;
    public float moveSpeed = 5f;

    public bool isDead => health <= 0;

    public Weapon weapon;

    public event Action<(int current, int max)> OnHealthChange = delegate {};
    public event Action<(int current, int max)> OnShieldChange = delegate {};
    public event Action<UnitController> OnAttackStart = delegate {};
    public event Action<UnitController> OnTakeDamage = delegate {};
    public event Action OnDied = delegate {};

    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        unitRigidbody = GetComponent<Rigidbody>();
        RaiseHealthChangeEvent();
        RaiseShieldChangeEvent();
    }

    void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Heal(maxHealth);
            Shield(maxShield);
        }
    }

    private void MovePlayer()
    {
        if(isDead) {
            unitRigidbody.velocity = Vector3.zero;
            return;
        };

        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * moveSpeed;
        unitRigidbody.velocity = moveDirection;
    }

    private void RotatePlayer()
    {
        if(isDead) return;

        float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, angle, Time.deltaTime * 10);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }

    public void TakeDamage(int damage)
    {
        RaiseOnTakeDamageEvent();
        // If the unit has a shield, reduce the shield points first
        if (shield > 0)
        {
            shield -= damage;
            if (shield < 0)
            {
                damage = -shield;
                shield = 0;
                RaiseShieldChangeEvent();
            }
            else
            {
                RaiseShieldChangeEvent();
                return;
            }
        }

        // Reduce the health points by the remaining damage
        health -= damage;

        health = Mathf.Clamp(health, 0, maxHealth);

        // Check if the unit is dead
        if (health <= 0)
        {
            Die();
        }
        RaiseHealthChangeEvent();
    }

    public void Attack() {
        weapon.Attack(this);
    }

    // Heal the unit
    public void Heal(int amount)
    {
        unitRigidbody.detectCollisions = true;
        // Increase the health by the heal amount
        health = Mathf.Min(health + amount, maxHealth);
        RaiseHealthChangeEvent();
    }

    // Shield the unit
    public void Shield(int amount)
    {
        // Increase the shield by the shield amount
        shield = Mathf.Min(shield + amount, maxShield);
        RaiseShieldChangeEvent();
    }

    private void Die()
    {
        unitRigidbody.detectCollisions = false;
        RaiseOnDiedEvent();
    }

    private void RaiseHealthChangeEvent()
    {
        OnHealthChange((current: health, max: maxHealth));
    }

    private void RaiseShieldChangeEvent()
    {
        OnShieldChange((current: shield, max: maxShield));
    }

    public void RaiseOnAttackStartEvent()
    {
        OnAttackStart(this);
    }

    private void RaiseOnDiedEvent()
    {
        OnDied();
    }

    private void RaiseOnTakeDamageEvent()
    {
        OnTakeDamage(this);
    }
}
