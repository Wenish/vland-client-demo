using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skills/Skill")]
public class SkillData : ScriptableObject
{
    [BoxGroup("Identity")]
    public string skillName;
    [BoxGroup("Identity")]
    public SkillType skillType;
    [BoxGroup("Identity")]
    [ResizableTextArea]
    public string description;

    [BoxGroup("Timing")]
    [MinValue(0)]
    public int cooldown;
    [BoxGroup("Timing")]
    [MinValue(0)]
    public int castCost;
    [BoxGroup("Timing")]
    public bool canActivateWhileBusy;

    [BoxGroup("Restrictions")]
    [Tooltip("If false, this skill can be used with any weapon.")]
    [SerializeField]
    private bool hasRequiredWeapon;
    [BoxGroup("Restrictions")]
    [SerializeField]
    [ShowIf(nameof(hasRequiredWeapon))]
    private WeaponType requiredWeapon;
    [BoxGroup("Restrictions")]
    public bool npcOnly;

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

    [BoxGroup("Effects")]
    [Expandable]
    public SkillEffectChainData initTrigger;
    [BoxGroup("Effects")]
    [Expandable]
    public SkillEffectChainData castTrigger;

    [BoxGroup("Reactive Triggers")]
    [Tooltip("Event-driven triggers that will subscribe when the skill is initialized. Executed on the server by default.")]
    [Expandable]
    public List<SkillEventTriggerData> reactiveTriggers = new();

    [BoxGroup("UI")]
    [ShowAssetPreview(64)]
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
