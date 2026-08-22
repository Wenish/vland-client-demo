using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponRanged", menuName = "Game/Weapon/Ranged")]
public class WeaponRangedData : WeaponData
{
    [Header("Ranged Specific")]
    public float spawnDistance = 0f;

    [Header("Projectile")]
    public ProjectileData projectile;

    public override void PerformAttack(UnitController attacker)
    {
        PerformAttack(attacker, null);
    }

    public void PerformAttack(UnitController attacker, IProjectileSpawner projectileSpawner)
    {
        if (projectile == null)
        {
            Debug.LogWarning("Projectile data is missing. Cannot spawn projectile.");
            return;
        }

        if (projectileSpawner == null)
        {
            Debug.LogWarning("Projectile spawner is missing. Cannot spawn projectile.");
            return;
        }

        Vector3 attackerPosition = attacker.transform.position;
        Quaternion attackerRotation = attacker.transform.rotation;
        Vector3 spawnPosition = attackerPosition + attackerRotation * Vector3.forward * spawnDistance;

        var projectileInstance = projectileSpawner.SpawnProjectile(
            projectile,
            spawnPosition + Vector3.up,
            attackerRotation);
        if (projectileInstance == null)
        {
            Debug.LogWarning("Projectile spawner returned no projectile.");
            return;
        }

        ProjectileController projectileController = projectileInstance.GetComponent<ProjectileController>();
        projectileController.shooter = attacker;

        projectileController.OnProjectileUnitHit += OnProjectileUnitHit;
        projectileController.OnProjectileDestroyed += (proj) =>
        {
            proj.OnProjectileUnitHit -= OnProjectileUnitHit;
            proj.OnProjectileDestroyed -= (p) => { };
        };
    }

    private void OnProjectileUnitHit((UnitController target, UnitController attacker) obj)
    {
        var damage = CalculateDamage(obj.attacker);
        obj.target.TakeDamage(DamageInstance.Physical(damage, DamageSourceKind.BasicAttack), obj.attacker);
        obj.target.RaiseOnAttackHitReceivedEvent(obj.attacker);
    }
}
