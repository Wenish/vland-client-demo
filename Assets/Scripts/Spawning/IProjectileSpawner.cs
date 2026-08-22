using UnityEngine;

public interface IProjectileSpawner
{
    GameObject SpawnProjectile(ProjectileData projectileData, Vector3 position, Quaternion rotation);
}
