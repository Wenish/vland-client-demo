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
        // Get the position and rotation of the attacker
        Vector3 attackerPosition = attacker.transform.position;
        Quaternion attackerRotation = attacker.transform.rotation;

        // Calculate the position to spawn the projectile
        Vector3 spawnPosition = attackerPosition + attackerRotation * Vector3.forward * spawnDistance;

        // Spawn the projectile
        var projectileInstance = ProjectileSpawner.Instance.SpawnProjectile(projectile, spawnPosition + Vector3.up, attackerRotation);

        // Get the projectile component of the spawned projectile
        ProjectileController projectileController = projectileInstance.GetComponent<ProjectileController>();
        projectileController.shooter = attacker;

        projectileController.OnProjectileUnitHit += OnProjectileUnitHit;
        projectileController.OnProjectileDestroyed += (proj) => {
            proj.OnProjectileUnitHit -= OnProjectileUnitHit;
            proj.OnProjectileDestroyed -= (p) => { };
        };

    }

    private void OnProjectileUnitHit((UnitController target, UnitController attacker) obj)
    {
        var damage = CalculateDamage(obj.attacker);
        obj.target.TakeDamage(damage, obj.attacker);
        obj.target.RaiseOnAttackHitReceivedEvent(obj.attacker);
    }
}