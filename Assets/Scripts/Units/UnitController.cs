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

    public Weapon weapon;

    public event Action<int, int> OnHealthChange = delegate {};

    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        unitRigidbody = GetComponent<Rigidbody>();
        OnHealthChange(health, maxHealth);
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
            ModifyHealth(-20);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ModifyHealth(maxHealth);
        }
    }

    public void ModifyHealth(int amount)
    {
        var newHealth = health + amount;
        health = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChange(health, maxHealth);
    }

    private void MovePlayer()
    {
        
        Vector3 inputs = Vector3.zero;
        inputs.x = horizontalInput;
        inputs.z = verticalInput;
        inputs = Vector3.ClampMagnitude(inputs, 1f);
        Vector3 moveDirection = inputs * moveSpeed;
        unitRigidbody.velocity = moveDirection;
    }

    private void RotatePlayer()
    {
        float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, angle, Time.deltaTime * 10);
        transform.rotation = Quaternion.AngleAxis(lerpedAngle, Vector3.up);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("Heyy dmg" + damage);
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
        health -= damage;

        // Check if the unit is dead
        if (health <= 0)
        {
            Die();
        }
        OnHealthChange(health, maxHealth);
    }

    private void Die()
    {
        // Destroy the unit game object
        Debug.Log("Unit Dead");
        // Destroy(gameObject);
    }
}
