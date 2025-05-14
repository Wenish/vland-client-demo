using System;
using System.Collections;
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
        if (skillDatabase == null)
        {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

        skillData = skillDatabase.GetSkillByName(skillName);
    }

    public void OnSkillNameChanged(string oldName, string newName)
    {
        if (skillDatabase == null)
        {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

        skillData = skillDatabase.GetSkillByName(newName);
    }

    public bool IsOnCooldown => NetworkTime.time < lastCastTime + skillData.cooldown;
    public float CooldownRemaining => skillData.cooldown - (float)(NetworkTime.time - lastCastTime);
    public float CooldownProgress => (CooldownRemaining / skillData.cooldown) * 100f;

    private Coroutine _runningInitCoroutine;
    private CastContext _runningInitContext;

    [Server]
    public void TriggerInit()
    {
        if (skillData == null) return;
        if (_runningInitCoroutine != null)
        {
            StopCoroutine(_runningInitCoroutine);
        }
        _runningInitContext = new CastContext(unit, this); ;
        _runningInitCoroutine = StartCoroutine(skillData.ExecuteInitCoroutine(_runningInitContext));
    }

    private Coroutine _runningCastCoroutine;
    private CastContext _runningCastContext;

    [Server]
    public void Cast()
    {
        if (IsOnCooldown || skillData == null) return;

        lastCastTime = NetworkTime.time;
        if (_runningCastCoroutine != null)
        {
            StopCoroutine(_runningCastCoroutine);
        }
        _runningCastContext = new CastContext(unit, this);
        _runningCastCoroutine = StartCoroutine(skillData.ExecuteCastCoroutine(_runningCastContext));
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

    [ContextMenu("Benchmark Cast 10,000x (Coroutine)")]
    [Server]
    public void BenchmarkCast()
    {
        // turn your void into a coroutine
        StartCoroutine(BenchmarkCastCoroutine());
    }

    private IEnumerator BenchmarkCastCoroutine()
    {
        if (skillData == null)
            yield break;

        int total = 10000;
        int finished = 0;

        // Stopwatch can’t start/stop inside the main thread yield,
        // so we start it here; we’ll Stop() it after all coroutines.
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < total; i++)
        {
            var ctx = new CastContext(unit, this);
            // launch each wrapped cast
            StartCoroutine(WrapAndCount(ctx, () => finished++));
        }

        // wait until every single one has fired its callback
        yield return new WaitUntil(() => finished >= total);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"TriggerCast {total:N0}×: {stopwatch.ElapsedMilliseconds} ms");
    }

    // helper that runs the real cast, then fires the onDone callback
    private IEnumerator WrapAndCount(CastContext ctx, Action onDone)
    {
        // run your original coroutine to completion
        yield return skillData.ExecuteCastCoroutine(ctx);
        // notify
        onDone();
    }
}
