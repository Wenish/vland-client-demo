using System.Collections.Generic;
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
    public SkillEffectChainData initTrigger;
    public SkillEffectChainData castTrigger;

    [Header("UI")]
    public Texture2D iconTexture;

    public void TriggerInit(GameObject caster)
    {
        initTrigger?.Execute(caster, new List<GameObject> { caster });
    }

    public void TriggerCast(GameObject caster)
    {
        castTrigger?.Execute(caster, new List<GameObject> { caster });
    }
}

public enum SkillType
{
    Normal,
    Passive,
    Ultimate
}