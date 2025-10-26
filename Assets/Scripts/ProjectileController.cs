using UnityEngine;
using Mirror;
using System;
using System.Collections;
using UnityEngine.VFX;

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
    Collider cachedCollider;

    private GameObject projectileBodyInstance;

    private GameObject projectileTrailInstance;

    [SyncVar(hook = nameof(OnIsDyingChanged))]
    private bool isDying;

    private Coroutine destroyCoroutine;

    public void OnProjectileNameChanged(string oldName, string newName)
    {
        if (isServer) return;
        SetProjectileData(newName);
    }

    void OnIsDyingChanged(bool _old, bool nowDying)
    {
        if (isServer) return; // server handles its own visuals immediately
        if (nowDying)
        {
            HideBodyLocal();
            StopTrailEmissionLocal();
        }
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

        if (projectileData.prefabBody != null)
        {
            projectileBodyInstance = Instantiate(projectileData.prefabBody, transform);
        }

        if (projectileTrailInstance != null)
        {
            Destroy(projectileTrailInstance);
        }
        if (projectileData.prefabTrail != null)
        {
            projectileTrailInstance = Instantiate(projectileData.prefabTrail, transform);
        }
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
        rb = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();
    }
    

    // Called when the projectile is spawned
    void OnEnable()
    {
        // rb cached in Start; keep a defensive fetch in case Start hasn't run yet
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (cachedCollider == null) cachedCollider = GetComponent<Collider>();
        spawn = transform.position;
        if(isServer && !isDying) {
            ApplyForce();
        }
    }

    void FixedUpdate()
    {
        if(isServer && !isDying) {
            ApplyForce();
        }
    }

    // Called every frame
    void LateUpdate()
    {
        if (isServer && !isDying) {
            CheckProjectileTravel();
        }
    }

    [Server]
    void ApplyForce()
    {
        if (rb != null && projectileData != null && !isDying)
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
            EnterDyingState();
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
            EnterDyingState();
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

    [Server]
    private void EnterDyingState()
    {
        if (isDying) return;
        isDying = true; // triggers client hooks

        // Stop further physics interactions
        StopPhysics();

        // Hide the body on server as well (clients will via hook)
        HideBodyLocal();
    StopTrailEmissionLocal();

        // Schedule destruction after the configured delay
        float delay = projectileData != null ? projectileData.destroyDelayAfterMaxHits : 0.35f;
        if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
        destroyCoroutine = StartCoroutine(DestroyAfterDelay(delay));
    }

    private void HideBodyLocal()
    {
        if (projectileBodyInstance != null)
        {
            projectileBodyInstance.SetActive(false);
        }
        // Keep trail alive for visual niceness
        // Optionally, we could stop emission if it's a ParticleSystem, but request is to keep it visible.
    }

    private void StopTrailEmissionLocal()
    {
        if (projectileTrailInstance == null) return;

        // Stop ALL VisualEffect (VFX Graph) components
        var vfxs = projectileTrailInstance.GetComponentsInChildren<VisualEffect>(true);
        if (vfxs != null && vfxs.Length > 0)
        {
            foreach (var vfx in vfxs)
            {
                if (vfx == null) continue;
                // Stop spawners; existing particles die naturally
                vfx.Stop();
            }
        }

        // Stop ALL ParticleSystem components
        var particleSystems = projectileTrailInstance.GetComponentsInChildren<ParticleSystem>(true);
        if (particleSystems != null && particleSystems.Length > 0)
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                emission.enabled = false; // stop spawning new particles
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Stop ALL TrailRenderers, if present (visual nicety)
        var trails = projectileTrailInstance.GetComponentsInChildren<TrailRenderer>(true);
        if (trails != null && trails.Length > 0)
        {
            foreach (var tr in trails)
            {
                if (tr == null) continue;
                tr.emitting = false;
            }
        }
    }

    [Server]
    private IEnumerator DestroyAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DestroySelf();
    }

    [Server]
    private void StopPhysics()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        if (cachedCollider != null)
        {
            cachedCollider.enabled = false; // prevent further triggers
        }
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