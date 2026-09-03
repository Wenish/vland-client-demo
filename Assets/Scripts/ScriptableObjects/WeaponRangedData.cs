using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponRanged", menuName = "Game/Weapon/Ranged")]
public class WeaponRangedData : WeaponData
{
    [Header("Ranged Specific")]
    public float spawnDistance = 0f;

    [Header("Projectile")]
    public ProjectileData projectile;

    public override void PerformAttack(UnitController attacker, float damageMultiplier = 1f)
    {
        PerformAttack(attacker, null, damageMultiplier);
    }

    public void PerformAttack(UnitController attacker, IProjectileSpawner projectileSpawner, float damageMultiplier = 1f)
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

        var multiplier = damageMultiplier;
        System.Action<(UnitController target, UnitController attacker)> onHit = hit =>
        {
            var damage = CalculateDamage(hit.attacker, multiplier);
            hit.target.TakeDamage(DamageInstance.Physical(damage, DamageSourceKind.BasicAttack), hit.attacker);
            hit.target.RaiseOnAttackHitReceivedEvent(hit.attacker);
        };

        projectileController.OnProjectileUnitHit += onHit;
        projectileController.OnProjectileDestroyed += proj =>
        {
            proj.OnProjectileUnitHit -= onHit;
        };
    }
}
