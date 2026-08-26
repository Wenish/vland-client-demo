using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillIndicator", menuName = "Game/Skills/Skill Indicator")]
public class SkillIndicatorData : ScriptableObject
{
    public enum IndicatorShape : byte
    {
        None = 0,
        Circle = 1,
        Directional = 2,
    }

    public enum IndicatorPlacement : byte
    {
        Self = 0,
        AtAimPoint = 1,
        FromCasterTowardAim = 2,
    }

    public enum AimFollowMode : byte
    {
        LockOnConfirm = 0,
        FollowWhileActive = 1,
    }

    [BoxGroup("Shape")]
    public IndicatorShape shape = IndicatorShape.Circle;

    [BoxGroup("Shape")]
    public IndicatorPlacement placement = IndicatorPlacement.AtAimPoint;

    [BoxGroup("Shape")]
    public AimFollowMode aimFollowMode = AimFollowMode.LockOnConfirm;

    [BoxGroup("Shape")]
    [Tooltip("Draw a cast-range ring around the caster during Shift+skill aim preview (uses SkillData.castRange).")]
    public bool showRangeRing = true;

    [BoxGroup("Shape")]
    [Tooltip("If true, also show the range ring while the skill is actively casting. Default is preview-only.")]
    [ShowIf(nameof(showRangeRing))]
    public bool showRangeRingDuringCast = false;

    [BoxGroup("Shape")]
    [Tooltip(
        "Optional targeting effect used to snap the placement indicator onto a unit "
            + "(e.g. SkillEffectTargetSmart). Preview snaps live; cast locks/follows that unit.")]
    [Expandable]
    public SkillEffectTarget snapToTarget;

    [BoxGroup("Size")]
    [Tooltip("Optional Circle/Linear target to inherit radius or range/width from.")]
    [Expandable]
    public SkillEffectTarget sizeSource;

    [BoxGroup("Size")]
    [Tooltip("If > 0, overrides circle radius (and ignores linked Circle radius).")]
    [MinValue(0f)]
    public float overrideRadius;

    [BoxGroup("Size")]
    [Tooltip("If > 0, overrides directional length (and ignores linked Linear range).")]
    [MinValue(0f)]
    public float overrideRange;

    [BoxGroup("Size")]
    [Tooltip("If > 0, overrides directional width (and ignores linked Linear width).")]
    [MinValue(0f)]
    public float overrideWidth;

    [BoxGroup("Visuals")]
    [Tooltip("Texture for the cast-range ring around the caster. Falls back to Resources/SkillIndicators/rangeskillindicator when null.")]
    [ShowAssetPreview(64)]
    public Texture2D rangeRingTexture;

    [BoxGroup("Visuals")]
    [Tooltip("Optional material override for the range ring. When null, a runtime transparent material is used.")]
    public Material rangeRingMaterial;

    [BoxGroup("Visuals")]
    [Tooltip("Texture for the skill placement shape (circle / directional). Falls back to Resources/SkillIndicators/aoeskillindicator_nobackground when null.")]
    [ShowAssetPreview(64)]
    public Texture2D placementTexture;

    [BoxGroup("Visuals")]
    [Tooltip("Optional material override for the placement shape. When null, a runtime transparent material is used.")]
    public Material placementMaterial;

    public float ResolveRadius()
    {
        if (overrideRadius > 0f)
            return overrideRadius;

        if (sizeSource is SkillEffectTargetCircle circle)
            return circle.radius;

        return 0f;
    }

    public float ResolveRange()
    {
        if (overrideRange > 0f)
            return overrideRange;

        if (sizeSource is SkillEffectTargetLinear linear)
            return linear.range;

        return 0f;
    }

    public float ResolveWidth()
    {
        if (overrideWidth > 0f)
            return overrideWidth;

        if (sizeSource is SkillEffectTargetLinear linear)
            return linear.width;

        return 1f;
    }

    public SkillIndicatorDisplayParams ToDisplayParams(float castRange, bool forPreview = false)
    {
        bool showRing = showRangeRing && (forPreview || showRangeRingDuringCast);

        return new SkillIndicatorDisplayParams
        {
            shape = shape,
            placement = placement,
            aimFollowMode = aimFollowMode,
            showRangeRing = showRing,
            castRange = Mathf.Max(0f, castRange),
            effectRadius = ResolveRadius(),
            effectRange = ResolveRange(),
            effectWidth = ResolveWidth(),
            indicatorAssetName = name ?? string.Empty,
            snapToTarget = snapToTarget != null,
        };
    }
}

/// <summary>
/// Network-friendly snapshot of what the owner client should draw.
/// Textures are resolved client-side from <see cref="SkillIndicatorData"/> by asset name.
/// </summary>
[System.Serializable]
public struct SkillIndicatorDisplayParams
{
    public SkillIndicatorData.IndicatorShape shape;
    public SkillIndicatorData.IndicatorPlacement placement;
    public SkillIndicatorData.AimFollowMode aimFollowMode;
    public bool showRangeRing;
    public float castRange;
    public float effectRadius;
    public float effectRange;
    public float effectWidth;
    public string indicatorAssetName;
    public bool snapToTarget;
}
