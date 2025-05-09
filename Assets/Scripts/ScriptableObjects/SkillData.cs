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

    public void TriggerInit(CastContext castContext)
    {
        initTrigger?.Execute(castContext, new List<UnitController> { castContext.caster });
    }

    public void TriggerCast(CastContext castContext)
    {
        castTrigger?.Execute(castContext, new List<UnitController> { castContext.caster });
    }
}

public enum SkillType
{
    Normal,
    Passive,
    Ultimate
}