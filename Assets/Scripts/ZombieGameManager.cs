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
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 0f);
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
        zombie.name = "Unit (Zombie)";

        var unitController = zombie.GetComponent<UnitController>();
        unitController.SetMaxHealth(50);
        unitController.SetMaxShield(0);
        unitController.angle = 180;
        zombie.AddComponent<AiZombieController>();
        NetworkServer.Spawn(zombie);
    }

    void GetAllZombieSpawnInScene()
    {
        ZombieSpawns = FindObjectsOfType<ZombieSpawnController>();
    }
}
