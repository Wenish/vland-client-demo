using UnityEngine;
using Mirror;
using System;

public class ProjectileController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnProjectileNameChanged))]
    public string projectileName;
    public ProjectileData projectileData;
    // The unit that shot the projectile
    public UnitController shooter;
    public event Action<(UnitController target, UnitController attacker)> OnProjectileUnitHit = delegate { };
    public event Action<ProjectileController> OnProjectileDestroyed = delegate { };

    private int hitCount = 0;

    private Vector3 spawn;

    Rigidbody rb;

    private GameObject projectileBodyInstance;

    public void OnProjectileNameChanged(string oldName, string newName)
    {
        if (isServer) return;
        SetProjectileData(newName);
    }

    private void SetProjectileData(string projectileName)
    {
        var database = DatabaseManager.Instance.projectileDatabase;
        projectileData = database.GetProjectileByName(projectileName);

        if (projectileData == null)
        {
            Debug.LogError($"Projectile data not found for projectile name: {projectileName}");
            return;
        }

        if (projectileBodyInstance != null)
        {
            Destroy(projectileBodyInstance);
        }
        projectileBodyInstance = Instantiate(projectileData.prefab, transform);
    }

    [Server]
    public void SetProjectileName(string name)
    {
        projectileName = name;
        SetProjectileData(name);
    }

    public void Start()
    {
        SetProjectileData(projectileName);
    }
    

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
            rb.linearVelocity = transform.forward * projectileData.speed;
        }
    }

    [Server]
    void CheckProjectileTravel()
    {
        // Move the projectile
        // transform.position += transform.forward * speed * Time.fixedDeltaTime;

        var distanceTravelled = Vector3.Distance(spawn, transform.position);

        // If the projectile has travelled its range, destroy it
        if (distanceTravelled >= projectileData.range)
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

        if (shooter == null) return;

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
        return hitCount >= projectileData.maxHits;
    }

    [Server]
    private void DestroySelf()
    {
        OnProjectileDestroyed(this);
        NetworkServer.Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        // Draw a line showing the range of the projectile
        Gizmos.color = Color.red;
        Gizmos.DrawLine(spawn, spawn + transform.forward * projectileData.range);

        Gizmos.DrawWireSphere(spawn + transform.forward * projectileData.range, 0.5f);

        var col = GetComponent<Collider>();
        Gizmos.color = Color.cyan;

        float radius = 0.5f;
        if (col != null)
        {
            // approximate radius from collider bounds (use largest extent)
            radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
        }
        // wireframe and semi-transparent fill
        Gizmos.DrawWireSphere(transform.position, radius / 2);
        var prevColor = Gizmos.color;
        Gizmos.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0.15f);
        Gizmos.DrawSphere(transform.position, radius / 2);
        Gizmos.color = prevColor;
    }
}