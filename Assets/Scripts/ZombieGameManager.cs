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
            SpawnWave();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Server]
    void SpawnWave()
    {

        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 0f);
        SpawnZombie(ZombieSpawns[0].transform.position, spawnRotation);
        SpawnZombie(ZombieSpawns[1].transform.position, spawnRotation);
        SpawnZombie(ZombieSpawns[2].transform.position, spawnRotation);
        SpawnZombie(ZombieSpawns[3].transform.position, spawnRotation);
    }

    [Server]
    void SpawnZombie(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        var zombie = NetworkManager.Instantiate(ZombiePrefab, new Vector3(spawnPosition.x, 0, spawnPosition.z), spawnRotation);
        zombie.name = "Unit (Zombie)";

        var unitController = zombie.GetComponent<UnitController>();
        unitController.SetMaxHealth(50);
        unitController.SetMaxShield(0);
        unitController.moveSpeed = 3;
        UnitEquipSword(unitController);
        NetworkServer.Spawn(zombie);
        zombie.AddComponent<AiZombieController>();
        var weaponMelee = zombie.GetComponent<WeaponMelee>();
        weaponMelee.attackPower = 40;
        weaponMelee.attackRange = 1.5f;
        weaponMelee.moveSpeedPercentWhileAttacking = 0.8f;
        weaponMelee.attackTime = 0.1f;
        weaponMelee.attackSpeed = 0.4f;
        weaponMelee.coneAngleRadians = 75;
        weaponMelee.numRays = 15;
    }

    void GetAllZombieSpawnInScene()
    {
        ZombieSpawns = FindObjectsOfType<ZombieSpawnController>();
    }

    void UnitEquipSword(UnitController unitController)
    {
        if (!unitController) return;
        if (unitController.weapon.attackCooldown > 0) return;

        WeaponMelee weaponMelee = unitController.GetComponent<WeaponMelee>();
        if (!weaponMelee) return;
        unitController.weapon = weaponMelee;
    }
}
