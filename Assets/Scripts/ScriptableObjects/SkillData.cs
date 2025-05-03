using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skills/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;
    public string description;
    public int cooldown;
    public int castCost;
    public WeaponType? requiredWeapon;

    [Header("Effects")]
    public SkillEffectTriggerData initalizeEffect;
    public SkillEffectTriggerData castEffect;

    [Header("UI")]
    public Texture2D iconTexture;
}

public enum SkillType
{
    Normal,
    Passive,
    Ultimate
}