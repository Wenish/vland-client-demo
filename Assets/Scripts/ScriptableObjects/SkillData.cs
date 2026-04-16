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
    public bool canActivateWhileBusy;
    [Tooltip("If false, this skill can be used with any weapon.")]
    [SerializeField] private bool hasRequiredWeapon;
    [SerializeField] private WeaponType requiredWeapon;

    public WeaponType? RequiredWeapon
    {
        get => hasRequiredWeapon ? requiredWeapon : (WeaponType?)null;
        set
        {
            hasRequiredWeapon = value.HasValue;
            if (value.HasValue)
            {
                requiredWeapon = value.Value;
            }
        }
    }
    
    [Header("Restrictions")]
    public bool npcOnly;

    [Header("Effects")]
    public SkillEffectChainData initTrigger;
    public SkillEffectChainData castTrigger;

    [Header("Reactive Triggers")]
    [Tooltip("Event-driven triggers that will subscribe when the skill is initialized. Executed on the server by default.")]
    public List<SkillEventTriggerData> reactiveTriggers = new();

    [Header("UI")]
    public Texture2D iconTexture;

    public bool CanBeUsedWithWeapon(WeaponType? weaponType)
    {
        var required = RequiredWeapon;
        if (!required.HasValue)
        {
            return true;
        }

        return weaponType.HasValue && weaponType.Value == required.Value;
    }

    public string GetRequiredWeaponLabel()
    {
        return RequiredWeapon.HasValue ? RequiredWeapon.Value.ToString() : "Any";
    }

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