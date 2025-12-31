using Mirror;
using UnityEngine;

public class UnitSpawner : NetworkBehaviour
{
    public static UnitSpawner Instance { get; private set; }
    public GameObject unitPrefab;
    public GameObject unitNpcPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        unitPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
        unitNpcPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "UnitNpc");
    }

    [Server]
    public GameObject SpawnUnit(string unitName, Vector3 position, Quaternion rotation, bool isNpc = false)
    {
        UnitData unitData = DatabaseManager.Instance.unitDatabase.GetUnitByName(unitName);
        if (unitData == null)
        {
            Debug.LogError($"Unit {unitName} not found in database.");
            return null;
        }
        return Spawn(unitData, position, rotation, isNpc);
    }

    [Server]
    public GameObject Spawn(UnitData unitData, Vector3 position, Quaternion rotation, bool isNpc = false)
    {
        GameObject prefabToUse = isNpc ? unitNpcPrefab : unitPrefab;
        GameObject unitInstance = Instantiate(prefabToUse, position, rotation);
        unitInstance.name = $"{prefabToUse.name} ({unitData.unitName})";
        UnitController unitController = unitInstance.GetComponent<UnitController>();

        if (unitController != null)
        {
            unitController.unitMediator.Stats.SetBaseStat(StatType.Health, unitData.maxHealth);
            unitController.unitMediator.Stats.SetBaseStat(StatType.MovementSpeed, unitData.moveSpeed);
            unitController.unitMediator.Stats.SetBaseStat(StatType.Shield, unitData.maxShield);
            unitController.health = unitData.health;
            unitController.shield = unitData.shield;
            unitController.team = unitData.team;
            unitController.unitType = unitData.unitType;
            unitController.unitName = unitData.unitName;
            unitController.weaponName = unitData.weapon?.weaponName ?? "";
            unitController.currentWeapon = unitData.weapon;
            unitController.EquipModel(unitData.modelData.modelName);
        }

        NetworkServer.Spawn(unitInstance);

        foreach (var skill in unitData.passiveSkills)
            unitController.unitMediator.Skills.AddSkill(SkillSlotType.Passive, skill.skillName);
        foreach (var skill in unitData.normalSkills)
            unitController.unitMediator.Skills.AddSkill(SkillSlotType.Normal, skill.skillName);
        foreach (var skill in unitData.ultimateSkills)
            unitController.unitMediator.Skills.AddSkill(SkillSlotType.Ultimate, skill.skillName);

        return unitInstance;
    }
}