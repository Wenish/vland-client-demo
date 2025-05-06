using System.Collections.Generic;
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
    }

    private void Awake()
    {
        skillPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "SkillInstance");
    }

    [Server]
    private void InitializeSlots()
    {
        AddSkill(SkillSlotType.Normal, "ConeOfCold");
        AddSkill(SkillSlotType.Normal, "IceBlast");
        AddSkill(SkillSlotType.Normal, "Dash");
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
        var list = GetList(slot);
        if (index < 0 || index >= list.Count) return;
        list[index].Cast();
    }
}


public enum SkillSlotType
{
    Passive,
    Normal,
    Ultimate
}