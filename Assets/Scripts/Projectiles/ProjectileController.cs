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

    public int maxHits = 1;

    private int hitCount = 0;

    private Vector3 spawn;


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
            CheckProjectileTravel();
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
    void CheckProjectileTravel()
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


    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Projectile Trigger Enter");
        Debug.Log(other.gameObject.name);
        if (isServer)
        {
            TriggerEnter(other);
        }
    }

    [Server]
    void TriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Wall"))
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        UnitController unit = other.GetComponent<UnitController>();

        if (unit != null && unit != shooter)
        {
            hitCount++;
            unit.TakeDamage(damage, shooter);
        }
        if (hitCount >= maxHits)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}