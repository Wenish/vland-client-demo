using UnityEngine;
using Mirror;

public class ProjectileController : NetworkBehaviour
{
    // The projectile scriptable object
    public Projectile projectile;

    // The unit that shot the projectile
    public UnitController shooter;

    // The current speed of the projectile
    public float speed;

    // The current damage of the projectile
    public int damage;

    public float range;

    private Vector3 spawn;

    private bool hasCollidedWithUnit;


    Rigidbody rb;
    

    // Called when the projectile is spawned
    void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        spawn = transform.position;
        if(isServer) {
            ApplyForce();
        }
    }

    void FixedUpdate()
    {
        if(isServer) {
            ApplyForce();
        }
    }

    // Called every frame
    void LateUpdate()
    {
        if (isServer) {
            MoveProjectile();
        }
    }

    [Server]
    void ApplyForce()
    {
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }

    [Server]
    void MoveProjectile()
    {
        // Move the projectile
        // transform.position += transform.forward * speed * Time.fixedDeltaTime;

        var distanceTravelled = Vector3.Distance(spawn, transform.position);

        // If the projectile has travelled its range, destroy it
        if (distanceTravelled >= range)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    // Called when the projectile collides with another collider
    void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            CollisionEnter(collision);
        }
    }

    [Server]
    void CollisionEnter(Collision collision)
    {
        if (hasCollidedWithUnit) return;

        ProjectileController projectileController = collision.collider.GetComponent<ProjectileController>();

        if (projectileController != null) return;

        // Get the unit controller component of the collided game object
        UnitController unit = collision.collider.GetComponent<UnitController>();

        // If the collided game object has a unit controller, deal damage to the unit
        if (unit != null && unit != shooter)
        {
            hasCollidedWithUnit = true;
            unit.TakeDamage(damage, shooter);
        }


        // Destroy the projectile
        NetworkServer.Destroy(gameObject);
    }
}