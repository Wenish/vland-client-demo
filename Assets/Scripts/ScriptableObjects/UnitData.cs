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