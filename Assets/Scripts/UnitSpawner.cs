using Mirror;
using ShadowInfection.DI;
using UnityEngine;

public class UnitSpawner : NetworkBehaviour, IUnitSpawner
{
    public GameObject unitPrefab;
    public GameObject unitNpcPrefab;

    private void Awake()
    {
        unitPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
        unitNpcPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "UnitNpc");
    }

    [Server]
    public GameObject SpawnUnit(string unitName, Vector3 position, Quaternion rotation, bool isNpc = false)
    {
        var units = GameServices.Databases?.Units;
        UnitData unitData = units != null ? units.GetUnitByName(unitName) : null;
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
            unitController.unitMediator.Stats.SetBaseStats(unitData.GetBaseStats());
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

                    

        if (isNpc && unitData.behaviourProfile != null)
        {
            var npcBehaviourExecutor = unitInstance.GetComponent<NPCBehaviour.BehaviourExecutor>();
            if (npcBehaviourExecutor != null)
            {
                npcBehaviourExecutor.SetBehaviourProfile(unitData.behaviourProfile);
            }
        }

        return unitInstance;
    }
}