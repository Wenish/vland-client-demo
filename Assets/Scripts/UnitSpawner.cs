using Mirror;
using UnityEngine;

public class UnitSpawner : NetworkBehaviour
{
    public static UnitSpawner Instance { get; private set; }
    public UnitDatabase unitDatabase;
    public GameObject unitPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        unitPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
    }

    [Server]
    public GameObject SpawnUnit(string unitName, Vector3 position, Quaternion rotation)
    {
        UnitData unitData = unitDatabase.GetUnitByName(unitName);
        if (unitData == null)
        {
            Debug.LogError($"Unit {unitName} not found in database.");
            return null;
        }

        GameObject unitInstance = Instantiate(unitPrefab, position, rotation);
        unitInstance.name = $"Unit ({unitName})";
        UnitController unitController = unitInstance.GetComponent<UnitController>();

        if (unitController != null)
        {
            unitController.health = unitData.health;
            unitController.maxHealth = unitData.maxHealth;
            unitController.shield = unitData.shield;
            unitController.maxShield = unitData.maxShield;
            unitController.moveSpeed = unitData.moveSpeed;
            unitController.team = unitData.team;
            unitController.unitType = unitData.unitType;
            unitController.unitName = unitData.unitName;
            unitController.weaponName = unitData.weapon.weaponName;
            unitController.currentWeapon = unitData.weapon;
        }

        NetworkServer.Spawn(unitInstance);
        return unitInstance;
    }
}