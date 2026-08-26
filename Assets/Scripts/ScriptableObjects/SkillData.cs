using System;
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
    [EnumFlags]
    public SkillTag tags;
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

    [BoxGroup("Aim")]
    [Tooltip("Max horizontal distance from the caster where the skill may be aimed/placed. 0 = no clamp.")]
    [MinValue(0f)]
    public float castRange;

    [BoxGroup("Aim")]
    [Tooltip("Optional indicator shown locally while holding Shift+skill before confirm. Reuse the same asset as the first Show Indicator effect when possible.")]
    [Expandable]
    public SkillIndicatorData aimPreviewIndicator;

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

    public bool HasTag(SkillTag tag)
    {
        if (tag == SkillTag.None)
            return tags == SkillTag.None;

        return (tags & tag) != 0;
    }

    public string GetOneLineSummary(int maxLength = 88)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var text = description.Trim();
        var period = text.IndexOf('.');
        if (period >= 0)
            text = text.Substring(0, period + 1);

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, Mathf.Max(1, maxLength - 3)).TrimEnd() + "...";
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

[Flags]
public enum SkillTag
{
    None = 0,
    Damage = 1 << 0,
    Support = 1 << 1,
    Defense = 1 << 2,
    Control = 1 << 3,
    Mobility = 1 << 4,
}

public static class SkillTagUtil
{
    public static readonly SkillTag[] FilterTags =
    {
        SkillTag.Damage,
        SkillTag.Support,
        SkillTag.Defense,
        SkillTag.Control,
        SkillTag.Mobility,
    };

    public static string GetLabel(SkillTag tag)
    {
        return tag switch
        {
            SkillTag.Damage => "Damage",
            SkillTag.Support => "Support",
            SkillTag.Defense => "Defense",
            SkillTag.Control => "Control",
            SkillTag.Mobility => "Mobility",
            _ => tag.ToString(),
        };
    }

    public static List<string> GetLabels(SkillTag tags)
    {
        var labels = new List<string>(FilterTags.Length);
        foreach (var tag in FilterTags)
        {
            if ((tags & tag) != 0)
                labels.Add(GetLabel(tag));
        }

        return labels;
    }
}
