using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SkillSystem : NetworkBehaviour
{
    public readonly SyncList<NetworkedSkillInstance> passiveSkills = new();
    public readonly SyncList<NetworkedSkillInstance> normalSkills = new();
    public readonly SyncList<NetworkedSkillInstance> ultimateSkills = new();

    private UnitController unit;
    [SerializeField]
    private GameObject skillPrefab;

    public override void OnStartServer()
    {
        unit = GetComponent<UnitController>();
        InitializeSlots();
        unit.OnRevive += OnUnitRevive;
    }

    private void Awake()
    {
        skillPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "SkillInstance");
        if (skillPrefab == null)
        {
            Debug.LogError("Skill prefab not assigned!");
            return;
        }
    }

    private void OnDestroy()
    {
        if (!isServer) return;
        unit.OnRevive -= OnUnitRevive;
    }

    [Server]
    private void InitializeSlots()
    {
        AddSkill(SkillSlotType.Passive, "HealingLeaf");
        AddSkill(SkillSlotType.Normal, "ConeOfCold");
        AddSkill(SkillSlotType.Normal, "IceBlast");
        AddSkill(SkillSlotType.Normal, "Swiftness");
        AddSkill(SkillSlotType.Ultimate, "HealingLeaf");
        RemoveSkill(SkillSlotType.Passive, 0);
    }

    [Server]
    public void AddSkill(SkillSlotType slotType, string skillName)
    {
        if (skillPrefab == null)
        {
            Debug.LogError("Skill prefab not assigned!");
            return;
        }

        GameObject go = Instantiate(skillPrefab, unit.transform); // Parent to unit
        var netSkill = go.GetComponent<NetworkedSkillInstance>();

        if (netSkill == null)
        {
            Debug.LogError("Skill prefab is missing NetworkedSkillInstance component!");
            return;
        }

        netSkill.Initialize(skillName, GetComponent<UnitController>());
        NetworkServer.Spawn(go);

        switch (slotType)
        {
            case SkillSlotType.Passive:
                passiveSkills.Add(netSkill);
                break;
            case SkillSlotType.Normal:
                normalSkills.Add(netSkill);
                break;
            case SkillSlotType.Ultimate:
                ultimateSkills.Add(netSkill);
                break;
        }
        netSkill.TriggerInit();
    }

    [Server]
    public void RemoveSkill(SkillSlotType slotType, int index)
    {
        var list = GetList(slotType);
        if (index < 0 || index >= list.Count) return;

        var skillToRemove = list[index];
        list.RemoveAt(index);
        skillToRemove.Cleanup();

        if (skillToRemove != null && skillToRemove.gameObject != null)
        {
            NetworkServer.Destroy(skillToRemove.gameObject);
        }
        Debug.Log($"Removed skill {skillToRemove.skillName} from {unit.name}");
        Debug.Log($"{ultimateSkills.Count} ultimate skills found.");
    }

    private SyncList<NetworkedSkillInstance> GetList(SkillSlotType type)
    {
        return type switch
        {
            SkillSlotType.Passive => passiveSkills,
            SkillSlotType.Normal => normalSkills,
            SkillSlotType.Ultimate => ultimateSkills,
            _ => null,
        };
    }

    public NetworkedSkillInstance GetSkill(SkillSlotType slot, int index)
    {
        var list = GetList(slot);
        if (index < 0 || index >= list.Count) return null;
        return list[index];
    }

    [Server]
    public void CastSkill(SkillSlotType slot, int index)
    {
        var isUnitDead = unit.IsDead;
        if (isUnitDead)
        {
            Debug.Log("Unit is dead, cannot cast skill.");
            return;
        }
        var list = GetList(slot);
        if (index < 0 || index >= list.Count) return;
        list[index].Cast();
    }

    [Server]
    public void OnUnitRevive()
    {
        Debug.Log("Unit revived, reinitializing skills.");
        foreach (var skill in passiveSkills)
        {
            skill.TriggerInit();
        }
        foreach (var skill in normalSkills)
        {
            skill.TriggerInit();
        }
        foreach (var skill in ultimateSkills)
        {
            skill.TriggerInit();
        }
    }
}


public enum SkillSlotType
{
    Passive,
    Normal,
    Ultimate
}