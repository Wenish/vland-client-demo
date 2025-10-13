using UnityEngine;
using Mirror;
using System;

public class ProjectileController : NetworkBehaviour
{
    // The unit that shot the projectile
    public UnitController shooter;

    public event Action<(UnitController target, UnitController attacker)> OnProjectileUnitHit = delegate { };
    public event Action<ProjectileController> OnProjectileDestroyed = delegate { };

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
            DestroySelf();
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (isServer)
        {
            TriggerEnter(other);
        }
    }

    [Server]
    void TriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Wall"))
        {
            DestroySelf();
            return;
        }

        UnitController unit = other.GetComponent<UnitController>();

        if (unit == null) return;

        var isShooter = unit == shooter;
        var isSameTeam = unit.team == shooter.team;
        var isDead = unit.IsDead;

        if (!isShooter && !isSameTeam && !isDead && !HasMaxHitCountReached())
        {
            hitCount++;
            OnProjectileUnitHit((unit, shooter));
        }
        
        if (HasMaxHitCountReached())
        {
            DestroySelf();
        }
    }

    bool HasMaxHitCountReached()
    {
        return hitCount >= maxHits;
    }

    [Server]
    private void DestroySelf()
    {
        OnProjectileDestroyed(this);
        NetworkServer.Destroy(gameObject);
    }
}