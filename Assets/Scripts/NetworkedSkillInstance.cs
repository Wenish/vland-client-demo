using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;

public class NetworkedSkillInstance : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSkillNameChanged))]
    public string skillName;

    [SyncVar]
    public double lastCastTime = -Mathf.Infinity;

    public SkillData skillData;
    private UnitController unit;
    private SkillDatabase skillDatabase;

    [SerializeField]
    public readonly List<(UnitMediator target, Buff buff)> appliedBuffs = new();

    public void Initialize(string name, UnitController unitRef)
    {
        skillName = name;
        unit = unitRef;
        skillDatabase = DatabaseManager.Instance.skillDatabase;
        ResolveSkillData();
    }

    public void ResolveSkillData()
    {
        if (skillDatabase == null) {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

        skillData = skillDatabase.GetSkillByName(skillName);
    }

    public void OnSkillNameChanged(string oldName, string newName)
    {
        if (skillDatabase == null) {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

        skillData = skillDatabase.GetSkillByName(newName);
    }

    public bool IsOnCooldown => NetworkTime.time < lastCastTime + skillData.cooldown;
    public float CooldownRemaining => skillData.cooldown - (float)(NetworkTime.time - lastCastTime);
    public float CooldownProgress => (CooldownRemaining / skillData.cooldown) * 100f;

    [Server]
    public void TriggerInit() {
        if (skillData == null) return;
        skillData.TriggerInit(new CastContext(unit, this));
    }

    [Server]
    public void Cast()
    {
        if (IsOnCooldown || skillData == null) return;

        lastCastTime = NetworkTime.time;
        CastContext castContext = new CastContext(unit, this);
        skillData.TriggerCast(castContext);
    }

    [Server]
    public void ManageBuff(UnitMediator mediator, Buff buff, bool apply)
    {
        if (apply)
        {
            mediator.AddBuff(buff);
            appliedBuffs.Add((mediator, buff));

            // Subscribe to OnRemoved only once
            buff.OnRemoved += () => RemoveManagedBuff(mediator, buff);
        }
        else
        {
            mediator.Buffs.RemoveBuff(buff);

            // Unsubscribe from the OnRemoved event
            buff.OnRemoved -= () => RemoveManagedBuff(mediator, buff);
            appliedBuffs.Remove((mediator, buff));
        }
    }

    // Separate method for clean removal logic
    [Server]
    private void RemoveManagedBuff(UnitMediator mediator, Buff buff)
    {
        appliedBuffs.Remove((mediator, buff));
        buff.OnRemoved -= () => RemoveManagedBuff(mediator, buff);
    }

    [Server]
    public void Cleanup()
    {
         var buffsToRemove = new List<(UnitMediator target, Buff buff)>(appliedBuffs);

        foreach (var (target, buff) in buffsToRemove)
        {
            ManageBuff(target, buff, false);
        }

        appliedBuffs.Clear();
    }

    [ContextMenu("Benchmark Cast 100,000x")]
    [Server]
    public void BenchmarkCast()
    {
        if (skillData == null) return;
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            CastContext castContext = new CastContext(unit, this);
            skillData.TriggerCast(castContext);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"TriggerCast 100.000x: {stopwatch.ElapsedMilliseconds} ms");
    }
}
