using UnityEngine;

public readonly struct SkillAimPreviewState
{
    public bool SnapsToTarget { get; }
    public UnitController FollowTarget { get; }
    public Vector3 AimPoint { get; }
    public SkillData Skill { get; }

    public SkillAimPreviewState(
        bool snapsToTarget,
        UnitController followTarget,
        Vector3 aimPoint,
        SkillData skill)
    {
        SnapsToTarget = snapsToTarget;
        FollowTarget = followTarget;
        AimPoint = aimPoint;
        Skill = skill;
    }

    public bool ShouldOverrideHoverHighlight =>
        SnapsToTarget && FollowTarget != null;
}
