using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class WeaponRanged : Weapon
{
    // The projectile scriptable object
    public Projectile projectile;

    // The speed of the projectile
    public float projectileSpeed = 10.0f;

    // The distance to spawn the projectile in front of the shooter
    public float spawnDistance = 1f;

    [Server]
    protected override void PerformAttack(UnitController attacker)
    {
        // Get the position and rotation of the attacker
        Vector3 attackerPosition = attacker.transform.position;
        Quaternion attackerRotation = attacker.transform.rotation;

        // Calculate the position to spawn the projectile
        Vector3 spawnPosition = attackerPosition + attackerRotation * Vector3.forward * spawnDistance;
        var prefabArrow = MyNetworkRoomManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Arrow");
        // Instantiate the projectile
        GameObject projectileObject = NetworkManager.Instantiate(prefabArrow, spawnPosition + Vector3.up, attackerRotation);
        NetworkServer.Spawn(projectileObject);

        // Get the projectile component of the spawned projectile
        ProjectileController projectile = projectileObject.GetComponent<ProjectileController>();

        // Set the projectile's damage
        projectile.damage = attackPower;

        // Set the projectile's speed
        projectile.speed = projectileSpeed;
        
        projectile.range = attackRange;
        projectile.shooter = attacker;
    }

        void OnDrawGizmos()
    {
        // Set the color of the gizmo
        Gizmos.color = Color.red;

        Vector3 unitPosition = transform.position + Vector3.up;
        Gizmos.DrawRay(unitPosition, transform.forward * attackRange);
    }
}
