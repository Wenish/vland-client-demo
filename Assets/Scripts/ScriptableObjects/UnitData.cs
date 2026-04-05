using System.Collections.Generic;
using UnityEngine;
using NPCBehaviour;

[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit/Unit")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public UnitType unitType;
    public int team;

    [Header("Stats")]
    public int health;
    public int maxHealth;
    public int shield;
    public int maxShield;
    public float moveSpeed;
    public float turnSpeed = 1f;
    public float damageReduction = 0f;
    public float attackSpeed = 1f;
    public float attackPower = 10f;
    public float abilityPower = 0f;
    public float armor = 0f;
    public float magicResist = 0f;
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

    [Header("Weapon")]
    public WeaponData weapon;

    [Header("Model")]
    public ModelData modelData;

    [Header("Skills")]
    public List<SkillData> passiveSkills = new List<SkillData>();
    public List<SkillData> normalSkills = new List<SkillData>();
    public List<SkillData> ultimateSkills = new List<SkillData>();

    [Header("AI Behaviour")]
    public BehaviourProfile behaviourProfile;
}