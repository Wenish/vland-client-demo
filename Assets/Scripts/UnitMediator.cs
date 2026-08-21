using System;
using Mirror;
using UnityEngine;

public class UnitMediator : NetworkBehaviour
{
    private static readonly StatType[] AllStatTypes = (StatType[])Enum.GetValues(typeof(StatType));

    public StatSystem Stats { get; private set; }
    public BuffSystem Buffs { get; private set; }
    public SkillSystem Skills { get; private set; }
    public UnitController UnitController { get; private set; }

    private readonly SyncList<float> replicatedStats = new SyncList<float>();

    private void Awake()
    {
        UnitController = GetComponent<UnitController>();
        Skills = GetComponent<SkillSystem>();
        Stats = new StatSystem(this);
        Buffs = new BuffSystem(this);

        Stats.OnStatChanged += OnStatChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureReplicatedStatsSize();
        ReplicateAllStats();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        replicatedStats.Callback += OnReplicatedStatChanged;
        if (!isServer)
            ApplyAllReplicatedStats();
    }

    public override void OnStopClient()
    {
        replicatedStats.Callback -= OnReplicatedStatChanged;
        base.OnStopClient();
    }

    private void OnStatChanged(StatType type)
    {
        var value = Stats.GetStat(type);
        if (!isClientOnly)
        {
            switch (type)
            {
                case StatType.Health:
                    UnitController.maxHealth = (int)value;
                    break;
                case StatType.MovementSpeed:
                    UnitController.moveSpeed = value;
                    break;
                case StatType.Shield:
                    UnitController.maxShield = (int)value;
                    break;
            }
        }

        if (isServer)
            SetReplicatedStat(type, value);
    }

    private void Update()
    {
        if (!isServer) return;

        Buffs.Update(Time.deltaTime);
    }

    public void AddBuff(Buff buff)
    {
        if (!isServer) return;
        Buffs.AddBuff(buff);
    }

    private void EnsureReplicatedStatsSize()
    {
        while (replicatedStats.Count < AllStatTypes.Length)
            replicatedStats.Add(0f);
    }

    private void ReplicateAllStats()
    {
        if (Stats == null)
            return;

        EnsureReplicatedStatsSize();
        for (var i = 0; i < AllStatTypes.Length; i++)
            SetReplicatedStat(AllStatTypes[i], Stats.GetStat(AllStatTypes[i]));
    }

    private void SetReplicatedStat(StatType type, float value)
    {
        var index = (int)type;
        if (index < 0 || index >= replicatedStats.Count)
            return;
        if (Mathf.Approximately(replicatedStats[index], value))
            return;

        replicatedStats[index] = value;
    }

    private void OnReplicatedStatChanged(SyncList<float>.Operation op, int index, float oldItem, float newItem)
    {
        if (isServer)
            return;

        if (op != SyncList<float>.Operation.OP_SET && op != SyncList<float>.Operation.OP_ADD)
            return;

        if (index < 0 || index >= AllStatTypes.Length)
            return;

        ApplyReplicatedStat(AllStatTypes[index], newItem);
    }

    private void ApplyAllReplicatedStats()
    {
        var count = Mathf.Min(replicatedStats.Count, AllStatTypes.Length);
        for (var i = 0; i < count; i++)
            ApplyReplicatedStat(AllStatTypes[i], replicatedStats[i]);
    }

    private void ApplyReplicatedStat(StatType type, float value)
    {
        if (Stats == null)
            return;

        Stats.SetBaseStat(type, value);
    }
}
