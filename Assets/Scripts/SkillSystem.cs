using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SkillSystem : NetworkBehaviour
{
    public event Action OnLoadoutReplaced = delegate { };

    public readonly SyncList<NetworkedSkillInstance> passiveSkills = new();
    public readonly SyncList<NetworkedSkillInstance> normalSkills = new();
    public readonly SyncList<NetworkedSkillInstance> ultimateSkills = new();

    private UnitController unit;
    [SerializeField]
    private GameObject skillPrefab;

    public override void OnStartServer()
    {
        unit = GetComponent<UnitController>();
        unit.OnRevive += OnUnitRevive;
        unit.OnActionInterrupted += OnActionInterrupted;
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
        unit.OnActionInterrupted -= OnActionInterrupted;
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

    public NetworkedSkillInstance GetSkillByName(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
            return null;

        if (TryFindSkillByName(normalSkills, skillName, out var skill)
            || TryFindSkillByName(ultimateSkills, skillName, out skill)
            || TryFindSkillByName(passiveSkills, skillName, out skill))
        {
            return skill;
        }

        return null;
    }

    /// <summary>
    /// Yaw the unit should face during an active cast with a fixed aim and locked turn speed.
    /// Skills that keep turn speed (e.g. Blazing Shot, Dragon's Flame) still follow the mouse.
    /// </summary>
    [Server]
    public bool TryGetLockedCastFacingYaw(out float yaw)
    {
        yaw = 0f;

        // Respect cast/channel turnSpeedPercent: only pin facing when turning is effectively disabled.
        float turnSpeed = unit != null && unit.unitMediator != null
            ? unit.unitMediator.Stats.GetStat(StatType.TurnSpeed)
            : 1f;
        if (!SkillAimUtil.IsTurnSpeedLocked(turnSpeed))
            return false;

        if (TryGetLockedCastFacingYaw(normalSkills, out yaw)
            || TryGetLockedCastFacingYaw(ultimateSkills, out yaw)
            || TryGetLockedCastFacingYaw(passiveSkills, out yaw))
        {
            return true;
        }

        return false;
    }

    [Server]
    private bool TryGetLockedCastFacingYaw(SyncList<NetworkedSkillInstance> list, out float yaw)
    {
        yaw = 0f;
        if (list == null || unit == null)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            var skill = list[i];
            if (skill == null)
                continue;

            if (!skill.TryGetRunningCastAim(out Vector3 aimPoint, out Quaternion aimRotation, out bool updatesAim))
                continue;

            // Live-aim casts and recast windows still follow the mouse.
            if (updatesAim || skill.IsRecastWindowOpen)
                continue;

            // Movement-based indicators (Evade) encode dash direction, not facing —
            // leave the model at its current yaw while turn speed is frozen.
            var indicator = SkillAimPreviewUtil.Resolve(skill);
            if (!SkillAimUtil.ShouldSnapFacingToCastAim(
                unit,
                new Vector2(unit.horizontalInput, unit.verticalInput),
                indicator))
            {
                continue;
            }

            yaw = SkillAimUtil.GetFacingAngleYaw(unit.transform.position, aimPoint);
            // Prefer rotation when aim point is essentially on the caster.
            Vector3 flat = aimPoint - unit.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                yaw = SkillAimUtil.GetFacingAngleYaw(aimRotation);

            return true;
        }

        return false;
    }

    private static bool TryFindSkillByName(
        SyncList<NetworkedSkillInstance> list,
        string skillName,
        out NetworkedSkillInstance skill)
    {
        skill = null;
        if (list == null)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            var candidate = list[i];
            if (candidate == null)
                continue;

            if (candidate.skillName == skillName)
            {
                skill = candidate;
                return true;
            }
        }

        return false;
    }

    [Server]
    public SkillCastResult CastSkill(
        SkillSlotType slot,
        int index,
        Vector3? aimPoint,
        bool forceSelfTarget = false,
        uint preferredTargetNetId = 0)
    {
        if (unit.IsDead)
        {
            Debug.Log("Unit is dead, cannot cast skill.");
            return SkillCastResult.Rejected;
        }

        if (unit.IsKnockedUp)
        {
            return SkillCastResult.Rejected;
        }

        var list = GetList(slot);
        if (index < 0 || index >= list.Count)
            return SkillCastResult.Rejected;
        return list[index].Cast(aimPoint, forceSelfTarget, preferredTargetNetId);
    }

    [Server]
    public void OnUnitRevive()
    {
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

    [Server]
    private void ClearSkills(SkillSlotType slot)
    {
        var list = GetList(slot);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            RemoveSkill(slot, i);
        }
    }

    [Server]
    public void RemoveSkillsIncompatibleWithWeapon(WeaponType? weaponType)
    {
        RemoveIncompatibleSkills(SkillSlotType.Passive, weaponType);
        RemoveIncompatibleSkills(SkillSlotType.Normal, weaponType);
        RemoveIncompatibleSkills(SkillSlotType.Ultimate, weaponType);
    }

    [Server]
    private void RemoveIncompatibleSkills(SkillSlotType slot, WeaponType? weaponType)
    {
        var list = GetList(slot);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var skill = list[i];
            var data = skill != null ? skill.skillData : null;
            if (data != null && !data.CanBeUsedWithWeapon(weaponType))
                RemoveSkill(slot, i);
        }
    }

    [Server]
    public void ClearAllSkills()
    {
        ClearSkills(SkillSlotType.Passive);
        ClearSkills(SkillSlotType.Normal);
        ClearSkills(SkillSlotType.Ultimate);
    }

    [Server]
    public void ReplaceLoadout(IEnumerable<string> passive, IEnumerable<string> normal, IEnumerable<string> ultimate)
    {
        OnLoadoutReplaced.Invoke();

        unit.InterruptAction();
        
        // Remove everything first
        ClearAllSkills();

        // Clean reset of any lingering stat modifiers (e.g., from channel mechanics)
        if (unit != null && unit.unitMediator != null && unit.unitMediator.Stats != null)
        {
            unit.unitMediator.Stats.ClearAllModifiers();
        }

        // Add passives
        if (passive != null)
        {
            foreach (var name in passive)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    AddSkill(SkillSlotType.Passive, name);
                }
            }
        }
        // Add normals
        if (normal != null)
        {
            foreach (var name in normal)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                AddSkill(SkillSlotType.Normal, name);
            }
        }


        // Add ultimates
        if (ultimate != null)
        {
            foreach (var name in ultimate)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                AddSkill(SkillSlotType.Ultimate, name);
            }
        }
    }

    /// <summary>
    /// Called when the unit's action is interrupted.
    /// Cancels only the skill that is currently casting or channeling.
    /// </summary>
    [Server]
    private void OnActionInterrupted((UnitController interruptedUnit, UnitActionState.ActionStateData interruptedAction) data)
    {
        if (data.interruptedUnit != unit) return;

        // Only cancel the skill that is currently casting or channeling
        var actionState = data.interruptedAction;
        if (actionState.type != UnitActionState.ActionType.Casting && 
            actionState.type != UnitActionState.ActionType.Channeling)
        {
            return;
        }

        var activeSkillName = actionState.name;
        if (string.IsNullOrEmpty(activeSkillName)) return;

        // Find and cancel only the matching skill
        foreach (var skill in normalSkills)
        {
            if (skill.skillName == activeSkillName)
            {
                skill.CancelCast();
                return;
            }
        }
        foreach (var skill in ultimateSkills)
        {
            if (skill.skillName == activeSkillName)
            {
                skill.CancelCast();
                return;
            }
        }
        // Passive skills typically don't have casts, but include for completeness
        foreach (var skill in passiveSkills)
        {
            if (skill.skillName == activeSkillName)
            {
                skill.CancelCast();
                return;
            }
        }
    }
}


public enum SkillSlotType
{
    Passive,
    Normal,
    Ultimate
}

public enum SkillCastResult
{
    Started,
    SignaledRunningCast,
    OnCooldown,
    Rejected,
    OutOfRange
}