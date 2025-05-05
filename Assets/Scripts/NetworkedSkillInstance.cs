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
    public void Cast()
    {
        if (IsOnCooldown || skillData == null) return;

        lastCastTime = NetworkTime.time;
        skillData.TriggerCast(unit.gameObject);
    }

    [ContextMenu("Benchmark Cast 100,000x")]
    [Server]
    public void BenchmarkCast()
    {
        if (skillData == null) return;
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            skillData.TriggerCast(unit.gameObject);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"TriggerCast 100.000x: {stopwatch.ElapsedMilliseconds} ms");
    }
}
