using System.Collections.Generic;
using NaughtyAttributes;
using NPCBehaviour;
using ShadowInfection.Items;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit/Unit")]
public class UnitData : ScriptableObject
{
    [BoxGroup("Identity")]
    public string unitName;
    [BoxGroup("Identity")]
    public UnitType unitType;
    [BoxGroup("Identity")]
    public int team;

    [BoxGroup("Stats")]
    [MinValue(0)]
    public int health;
    [BoxGroup("Stats")]
    [MinValue(0)]
    public int maxHealth;
    [BoxGroup("Stats")]
    [MinValue(0)]
    public int shield;
    [BoxGroup("Stats")]
    [MinValue(0)]
    public int maxShield;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float moveSpeed;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float turnSpeed = 1f;
    [BoxGroup("Stats")]
    public float damageReduction = 0f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float attackSpeed = 1f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float attackPower = 10f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float abilityPower = 0f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float armor = 0f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float magicResist = 0f;
    [BoxGroup("Stats")]
    [MinValue(0f)]
    public float critChance = 0f;

    public IEnumerable<StatModifier> GetBaseStats()
    {
        yield return CreateBaseStat(StatType.Health, maxHealth);
        yield return CreateBaseStat(StatType.MovementSpeed, moveSpeed);
        yield return CreateBaseStat(StatType.Shield, maxShield);
        yield return CreateBaseStat(StatType.TurnSpeed, turnSpeed);
        yield return CreateBaseStat(StatType.DamageReduction, damageReduction);
        yield return CreateBaseStat(StatType.AttackSpeed, attackSpeed);
        yield return CreateBaseStat(StatType.AttackPower, attackPower);
        yield return CreateBaseStat(StatType.AbilityPower, abilityPower);
        yield return CreateBaseStat(StatType.Armor, armor);
        yield return CreateBaseStat(StatType.MagicResist, magicResist);
        yield return CreateBaseStat(StatType.CritChance, critChance);
    }

    private static StatModifier CreateBaseStat(StatType type, float value)
    {
        return new StatModifier
        {
            Type = type,
            Value = value,
            ModifierType = ModifierType.Flat
        };
    }

    [BoxGroup("Loadout")]
    [Expandable]
    public ItemDefinition mainHandItem;
    [BoxGroup("Loadout")]
    [Expandable]
    public ItemDefinition offHandItem;
    [BoxGroup("Loadout")]
    [Expandable]
    public ModelData modelData;

    [BoxGroup("Skills")]
    [Expandable]
    public List<SkillData> passiveSkills = new List<SkillData>();
    [BoxGroup("Skills")]
    [Expandable]
    public List<SkillData> normalSkills = new List<SkillData>();
    [BoxGroup("Skills")]
    [Expandable]
    public List<SkillData> ultimateSkills = new List<SkillData>();

    [BoxGroup("AI")]
    [ShowIf(nameof(IsNpc))]
    [Expandable]
    public BehaviourProfile behaviourProfile;

    private bool IsNpc() => unitType != UnitType.Player;
}
