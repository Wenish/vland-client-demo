using Mirror;
using UnityEngine;

public class ProjectileSpawner : NetworkBehaviour
{
    public static ProjectileSpawner Instance { get; private set; }
    public GameObject projectilePrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        projectilePrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Projectile");
    }

    [Server]
    public GameObject SpawnProjectile(ProjectileData projectileData, Vector3 position, Quaternion rotation)
    {
        GameObject projectileInstance = Instantiate(projectilePrefab, position, rotation);

        // Initialize the projectile's data
        ProjectileController projectileController = projectileInstance.GetComponent<ProjectileController>();
        projectileController.SetProjectileName(projectileData.projectileName);

        NetworkServer.Spawn(projectileInstance);

        return projectileInstance;
    }
}