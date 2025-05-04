using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SkillSystem : NetworkBehaviour
{

    public readonly SyncList<SkillInstance> passiveSkills = new SyncList<SkillInstance>();
    public readonly SyncList<SkillInstance> normalSkills = new SyncList<SkillInstance>();
    public readonly SyncList<SkillInstance> ultimateSkills = new SyncList<SkillInstance>();

    private UnitController unit;
    private SkillDatabase skillDatabase;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
        skillDatabase = DatabaseManager.Instance.skillDatabase;
    }

    void Start()
    {
        if (!isServer) return;
        InitializeSlots();
        ActivatePassives();
    }



    void Update()
    {
        if (!isServer) return;
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (normalSkills.Count > 0)
            {
                SkillInstance skill = normalSkills[0];
                skill.lastCastTime = NetworkTime.time;
                normalSkills[0] = skill;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        normalSkills.OnSet += (int index, SkillInstance item) =>
        {
            ResolveSkillData(SkillSlotType.Normal, index);
        };

        normalSkills.OnChange += (SyncList<SkillInstance>.Operation op, int index, SkillInstance item) =>
        {
            ResolveSkillData(SkillSlotType.Normal, index);
        };

        for (int i = 0; i < passiveSkills.Count; i++)
        {
            ResolveSkillData(SkillSlotType.Passive, i);
        }
        for (int i = 0; i < normalSkills.Count; i++)
        {
            ResolveSkillData(SkillSlotType.Normal, i);
        }
        for (int i = 0; i < ultimateSkills.Count; i++)
        {
            ResolveSkillData(SkillSlotType.Ultimate, i);
        }
    }


    [Server]
    public void InitializeSlots()
    {
        Debug.Log("Initializing skill slots");
        AddSkill(SkillSlotType.Normal, "TestSkill");
    }

    [Server]
    public void AddSkill(SkillSlotType slotType, string skillName)
    {
        var skillData = skillDatabase.GetSkillByName(skillName);
        if (skillData == null)
        {
            Debug.LogError($"Skill {skillName} not found");
            return;
        }
        var skillInstance = new SkillInstance();
        skillInstance.skillName = skillData.name;

        switch (slotType)
        {
            case SkillSlotType.Passive:
                passiveSkills.Add(skillInstance);
                break;
            case SkillSlotType.Normal:
                normalSkills.Add(skillInstance);
                break;
            case SkillSlotType.Ultimate:
                ultimateSkills.Add(skillInstance);
                break;
            default:
                Debug.LogError($"Invalid slot type: {slotType}");
                break;
        }
        ResolveSkillData(slotType, GetListByType(slotType).Count - 1);

        skillInstance.skillData.TriggerInit(unit.gameObject);
        Debug.Log($"Added skill {skillName} to {slotType}");
    }

    public void ResolveSkillData(SkillSlotType slotType, int index)
    {
        Debug.Log($"Resolving skill data for {slotType} at index {index}");
        var skillList = GetListByType(slotType);
        if (index >= skillList.Count) return;

        var instance = skillList[index];
        var skillData = skillDatabase.GetSkillByName(instance.skillName);
        if (skillData == null)
        {
            Debug.LogError($"Skill {instance.skillName} not found");
            return;
        }
        instance.skillData = skillData;
    }

    [Server]
    private void ActivatePassives()
    {
        foreach (var skillInstance in passiveSkills)
        {
            skillInstance.skillData.TriggerInit(unit.gameObject);
        }
    }

    [Server]
    public void UseSkill(SkillSlotType slotType, int index)
    {
        var skillList = GetListByType(slotType);
        if (index >= skillList.Count) return;

        var instance = skillList[index];
        if (instance.skillData == null) return;

        if (instance.IsOnCooldown) return;

        instance.lastCastTime = Time.time;
        instance.skillData.TriggerCast(unit.gameObject);
    }

    private SyncList<SkillInstance> GetListByType(SkillSlotType type)
    {
        return type switch
        {
            SkillSlotType.Passive => passiveSkills,
            SkillSlotType.Normal => normalSkills,
            SkillSlotType.Ultimate => ultimateSkills,
            _ => null,
        };
    }

    [Command]
    public void CmdUseSkill(SkillSlotType type, int index)
    {
        UseSkill(type, index);
    }
}

public enum SkillSlotType
{
    Passive,
    Normal,
    Ultimate
}