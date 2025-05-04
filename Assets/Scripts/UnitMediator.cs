using System;
using Mirror;
using UnityEngine;

public class UnitMediator : NetworkBehaviour
{
    public StatSystem Stats { get; private set; }
    public BuffSystem Buffs { get; private set; }
    public SkillSystem Skills { get; private set; }
    public UnitController UnitController { get; private set; }

    private void Awake()
    {
        UnitController = GetComponent<UnitController>();
        Skills = GetComponent<SkillSystem>();
        Stats = new StatSystem(this);
        Buffs = new BuffSystem(this);

        Stats.OnStatChanged += OnStatChanged;
    }

    private void OnStatChanged(StatType type)
    {
        switch (type)
        {
            case StatType.Health:
                UnitController.maxHealth = (int)Stats.GetStat(StatType.Health);
                break;
            case StatType.MovementSpeed:
                UnitController.moveSpeed = Stats.GetStat(StatType.MovementSpeed);
                break;
            case StatType.Shield:
                UnitController.maxShield = (int)Stats.GetStat(StatType.Shield);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void Update()
    {
        if (!isServer) return;

        Buffs.Update(Time.deltaTime);
    }

    public void AddBuff(Buff buff)
    {
        Debug.Log($"Adding buff {buff.BuffId} to {UnitController.name}");
        if (!isServer) return;
        Buffs.AddBuff(buff);
    }
}