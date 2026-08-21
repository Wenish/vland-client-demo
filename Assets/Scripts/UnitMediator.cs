using Mirror;
using UnityEngine;

public class UnitMediator : NetworkBehaviour
{
    public StatSystem Stats { get; private set; }
    public BuffSystem Buffs { get; private set; }
    public SkillSystem Skills { get; private set; }
    public UnitController UnitController { get; private set; }

    [SyncVar(hook = nameof(OnSyncHealth))] private float syncHealth;
    [SyncVar(hook = nameof(OnSyncMovementSpeed))] private float syncMovementSpeed;
    [SyncVar(hook = nameof(OnSyncShield))] private float syncShield;
    [SyncVar(hook = nameof(OnSyncTurnSpeed))] private float syncTurnSpeed;
    [SyncVar(hook = nameof(OnSyncDamageReduction))] private float syncDamageReduction;
    [SyncVar(hook = nameof(OnSyncAttackSpeed))] private float syncAttackSpeed;
    [SyncVar(hook = nameof(OnSyncAttackPower))] private float syncAttackPower;
    [SyncVar(hook = nameof(OnSyncAbilityPower))] private float syncAbilityPower;
    [SyncVar(hook = nameof(OnSyncArmor))] private float syncArmor;
    [SyncVar(hook = nameof(OnSyncMagicResist))] private float syncMagicResist;
    [SyncVar(hook = nameof(OnSyncCritChance))] private float syncCritChance;

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
        ReplicateAllStats();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
            ApplyAllReplicatedStats();
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
        if (!isServer)
            return;

        Buffs.Update(Time.deltaTime);
    }

    public void AddBuff(Buff buff)
    {
        if (!isServer)
            return;

        Buffs.AddBuff(buff);
        ReplicateAllStats();
        PushStatsToClients();
    }

    [Server]
    public void ReplicateAllStats()
    {
        if (Stats == null)
            return;

        SetReplicatedStat(StatType.Health, Stats.GetStat(StatType.Health));
        SetReplicatedStat(StatType.MovementSpeed, Stats.GetStat(StatType.MovementSpeed));
        SetReplicatedStat(StatType.Shield, Stats.GetStat(StatType.Shield));
        SetReplicatedStat(StatType.TurnSpeed, Stats.GetStat(StatType.TurnSpeed));
        SetReplicatedStat(StatType.DamageReduction, Stats.GetStat(StatType.DamageReduction));
        SetReplicatedStat(StatType.AttackSpeed, Stats.GetStat(StatType.AttackSpeed));
        SetReplicatedStat(StatType.AttackPower, Stats.GetStat(StatType.AttackPower));
        SetReplicatedStat(StatType.AbilityPower, Stats.GetStat(StatType.AbilityPower));
        SetReplicatedStat(StatType.Armor, Stats.GetStat(StatType.Armor));
        SetReplicatedStat(StatType.MagicResist, Stats.GetStat(StatType.MagicResist));
        SetReplicatedStat(StatType.CritChance, Stats.GetStat(StatType.CritChance));
    }

    [Server]
    public void PushStatsToClients()
    {
        ReplicateAllStats();
        if (netId == 0)
            return;

        RpcApplyAllStats(
            syncHealth,
            syncMovementSpeed,
            syncShield,
            syncTurnSpeed,
            syncDamageReduction,
            syncAttackSpeed,
            syncAttackPower,
            syncAbilityPower,
            syncArmor,
            syncMagicResist,
            syncCritChance);
    }

    private void SetReplicatedStat(StatType type, float value)
    {
        switch (type)
        {
            case StatType.Health: syncHealth = value; break;
            case StatType.MovementSpeed: syncMovementSpeed = value; break;
            case StatType.Shield: syncShield = value; break;
            case StatType.TurnSpeed: syncTurnSpeed = value; break;
            case StatType.DamageReduction: syncDamageReduction = value; break;
            case StatType.AttackSpeed: syncAttackSpeed = value; break;
            case StatType.AttackPower: syncAttackPower = value; break;
            case StatType.AbilityPower: syncAbilityPower = value; break;
            case StatType.Armor: syncArmor = value; break;
            case StatType.MagicResist: syncMagicResist = value; break;
            case StatType.CritChance: syncCritChance = value; break;
        }
    }

    [ClientRpc]
    private void RpcApplyAllStats(
        float health,
        float movementSpeed,
        float shield,
        float turnSpeed,
        float damageReduction,
        float attackSpeed,
        float attackPower,
        float abilityPower,
        float armor,
        float magicResist,
        float critChance)
    {
        if (isServer)
            return;

        ApplyReplicatedStat(StatType.Health, health);
        ApplyReplicatedStat(StatType.MovementSpeed, movementSpeed);
        ApplyReplicatedStat(StatType.Shield, shield);
        ApplyReplicatedStat(StatType.TurnSpeed, turnSpeed);
        ApplyReplicatedStat(StatType.DamageReduction, damageReduction);
        ApplyReplicatedStat(StatType.AttackSpeed, attackSpeed);
        ApplyReplicatedStat(StatType.AttackPower, attackPower);
        ApplyReplicatedStat(StatType.AbilityPower, abilityPower);
        ApplyReplicatedStat(StatType.Armor, armor);
        ApplyReplicatedStat(StatType.MagicResist, magicResist);
        ApplyReplicatedStat(StatType.CritChance, critChance);
    }

    private void ApplyAllReplicatedStats()
    {
        ApplyReplicatedStat(StatType.Health, syncHealth);
        ApplyReplicatedStat(StatType.MovementSpeed, syncMovementSpeed);
        ApplyReplicatedStat(StatType.Shield, syncShield);
        ApplyReplicatedStat(StatType.TurnSpeed, syncTurnSpeed);
        ApplyReplicatedStat(StatType.DamageReduction, syncDamageReduction);
        ApplyReplicatedStat(StatType.AttackSpeed, syncAttackSpeed);
        ApplyReplicatedStat(StatType.AttackPower, syncAttackPower);
        ApplyReplicatedStat(StatType.AbilityPower, syncAbilityPower);
        ApplyReplicatedStat(StatType.Armor, syncArmor);
        ApplyReplicatedStat(StatType.MagicResist, syncMagicResist);
        ApplyReplicatedStat(StatType.CritChance, syncCritChance);
    }

    private void OnSyncHealth(float _, float value) => ApplyReplicatedStat(StatType.Health, value);
    private void OnSyncMovementSpeed(float _, float value) => ApplyReplicatedStat(StatType.MovementSpeed, value);
    private void OnSyncShield(float _, float value) => ApplyReplicatedStat(StatType.Shield, value);
    private void OnSyncTurnSpeed(float _, float value) => ApplyReplicatedStat(StatType.TurnSpeed, value);
    private void OnSyncDamageReduction(float _, float value) => ApplyReplicatedStat(StatType.DamageReduction, value);
    private void OnSyncAttackSpeed(float _, float value) => ApplyReplicatedStat(StatType.AttackSpeed, value);
    private void OnSyncAttackPower(float _, float value) => ApplyReplicatedStat(StatType.AttackPower, value);
    private void OnSyncAbilityPower(float _, float value) => ApplyReplicatedStat(StatType.AbilityPower, value);
    private void OnSyncArmor(float _, float value) => ApplyReplicatedStat(StatType.Armor, value);
    private void OnSyncMagicResist(float _, float value) => ApplyReplicatedStat(StatType.MagicResist, value);
    private void OnSyncCritChance(float _, float value) => ApplyReplicatedStat(StatType.CritChance, value);

    private void ApplyReplicatedStat(StatType type, float value)
    {
        if (isServer || Stats == null)
            return;

        Stats.SetBaseStat(type, value);
    }
}
