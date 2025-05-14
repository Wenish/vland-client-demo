using System.Collections;
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

    public IEnumerator ExecuteInitCoroutine(CastContext castContext)
    {
        if (initTrigger == null) yield break;

        var targets = new List<UnitController> { castContext.caster };
        yield return castContext.skillInstance.StartCoroutine(
            initTrigger.ExecuteCoroutine(castContext, targets)
        );
    }

    public IEnumerator ExecuteCastCoroutine(CastContext castContext)
    {
        if (castTrigger == null) yield break;

        var targets = new List<UnitController> { castContext.caster };
        yield return castContext.skillInstance.StartCoroutine(
            castTrigger.ExecuteCoroutine(castContext, targets)
        );
    }
}

public enum SkillType
{
    Normal,
    Passive,
    Ultimate
}