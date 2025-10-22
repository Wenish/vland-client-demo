using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponRanged", menuName = "Game/Weapon/Ranged")]
public class WeaponRangedData : WeaponData
{
    [Header("Ranged Specific")]
    public float projectileSpeed = 10.0f;
    public float spawnDistance = 1f;

    [Header("Projectile")]
    public ProjectileData projectile;

    public override void PerformAttack(UnitController attacker)
    {
        // Get the position and rotation of the attacker
        Vector3 attackerPosition = attacker.transform.position;
        Quaternion attackerRotation = attacker.transform.rotation;

        // Calculate the position to spawn the projectile
        Vector3 spawnPosition = attackerPosition + attackerRotation * Vector3.forward * spawnDistance;
        var prefabProjectile = projectile.prefab;
        // Instantiate the projectile
        GameObject projectileObject = NetworkManager.Instantiate(prefabProjectile, spawnPosition + Vector3.up, attackerRotation);
        NetworkServer.Spawn(projectileObject);

        // Get the projectile component of the spawned projectile
        ProjectileController projectileController = projectileObject.GetComponent<ProjectileController>();

        projectileController.OnProjectileUnitHit += OnProjectileUnitHit;
        projectileController.OnProjectileDestroyed += (proj) => {
            proj.OnProjectileUnitHit -= OnProjectileUnitHit;
            proj.OnProjectileDestroyed -= (p) => { };
        };

        // Set the projectile's damage
        projectileController.damage = attackPower;

        // Set the projectile's speed
        projectileController.speed = projectileSpeed;

        projectileController.range = attackRange;
        projectileController.shooter = attacker;

        // Set the projectile's max hits
        projectileController.maxHits = projectile.maxHits;
    }

    private void OnProjectileUnitHit((UnitController target, UnitController attacker) obj)
    {
        var damage = CalculateDamage(obj.attacker);
        obj.target.TakeDamage(damage, obj.attacker);
        obj.target.RaiseOnAttackHitReceivedEvent(obj.attacker);
    }
}