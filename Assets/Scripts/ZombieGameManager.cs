using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ZombieGameManager : NetworkBehaviour
{
    public ZombieSpawnController[] ZombieSpawns;
    public GameObject ZombiePrefab;
    // Start is called before the first frame update
    void Awake()
    {
        GetAllZombieSpawnInScene();
        ZombiePrefab = CustomNetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
    }
    void Start()
    {
        if(isServer)
        {
            Quaternion spawnRotation = Quaternion.Euler(0f, 45f, 0f);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[1].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[2].transform.position, spawnRotation);
            SpawnZombie(ZombieSpawns[3].transform.position, spawnRotation);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Server]
    void SpawnZombie(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        var zombie = NetworkManager.Instantiate(ZombiePrefab, spawnPosition, spawnRotation);

        var unitController = zombie.GetComponent<UnitController>();
        unitController.maxHealth = 100;
        unitController.health = 100;
        unitController.maxShield = 0;
        unitController.shield = 0;
        NetworkServer.Spawn(zombie);
    }

    void GetAllZombieSpawnInScene()
    {
        ZombieSpawns = FindObjectsOfType<ZombieSpawnController>();
    }
}
