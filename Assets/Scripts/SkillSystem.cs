using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SkillSystem : NetworkBehaviour
{
    public List<NetworkedSkillInstance> passiveSkills = new();
    public List<NetworkedSkillInstance> normalSkills = new();
    public List<NetworkedSkillInstance> ultimateSkills = new();

    private UnitController unit;

    public override void OnStartServer()
    {
        unit = GetComponent<UnitController>();
        InitializeSlots();
    }

    [Server]
    private void InitializeSlots()
    {
        AddSkill(SkillSlotType.Normal, "TestSkill");
    }

    [Server]
    public void AddSkill(SkillSlotType slotType, string skillName)
    {
        GameObject go = new GameObject($"Skill_{skillName}");
        go.transform.SetParent(transform);

        // Add NetworkIdentity before spawning
        var identity = go.AddComponent<NetworkIdentity>();
        var netSkill = go.AddComponent<NetworkedSkillInstance>();

        NetworkServer.Spawn(go, connectionToClient);

        netSkill.Initialize(skillName, GetComponent<UnitController>());

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

    [Command]
    public void CmdUseSkill(SkillSlotType slot, int index)
    {
        var list = GetList(slot);
        if (index < 0 || index >= list.Count) return;
        list[index].Cast();
    }

    private List<NetworkedSkillInstance> GetList(SkillSlotType type)
    {
        return type switch
        {
            SkillSlotType.Passive => passiveSkills,
            SkillSlotType.Normal => normalSkills,
            SkillSlotType.Ultimate => ultimateSkills,
            _ => null,
        };
    }

    private void Update()
    {
        if (!isServer) return;

        if (Input.GetKeyDown(KeyCode.Q) && normalSkills.Count > 0)
        {
            normalSkills[0].Cast();
        }
    }
}


public enum SkillSlotType
{
    Passive,
    Normal,
    Ultimate
}