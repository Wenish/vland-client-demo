using UnityEngine;

public static class SkillAimUtil
{
    /// <summary>
    /// Clamps <paramref name="aimPoint"/> to a horizontal circle of <paramref name="castRange"/>
    /// around <paramref name="casterPosition"/>. If castRange &lt;= 0, returns aim unchanged.
    /// Preserves aim Y (ground height).
    /// </summary>
    public static Vector3 ClampAimPoint(Vector3 casterPosition, Vector3 aimPoint, float castRange)
    {
        if (castRange <= 0f)
            return aimPoint;

        Vector3 offset = aimPoint - casterPosition;
        offset.y = 0f;
        float sqr = offset.sqrMagnitude;
        float maxSqr = castRange * castRange;
        if (sqr <= maxSqr || sqr < 0.0001f)
            return aimPoint;

        Vector3 clamped = casterPosition + offset.normalized * castRange;
        clamped.y = aimPoint.y;
        return clamped;
    }

    public static Vector3 ClampAimPoint(UnitController caster, Vector3 aimPoint, SkillData skillData)
    {
        if (caster == null || skillData == null)
            return aimPoint;

        return ClampAimPoint(caster.transform.position, aimPoint, skillData.castRange);
    }

    /// <summary>
    /// Builds a world aim point used by indicators so directional shapes match skill direction rules
    /// (e.g. Evade: movement first, then mouse/facing fallback).
    /// </summary>
    public static Vector3 ResolveIndicatorAimPoint(
        UnitController caster,
        Vector3 mouseAimPoint,
        Vector2 moveInput,
        SkillIndicatorData.DirectionSource directionSource,
        float directionLength)
    {
        if (caster == null)
            return mouseAimPoint;

        Vector3 casterPos = caster.transform.position;
        Vector3 dir = ResolveDirection(caster, mouseAimPoint, moveInput, directionSource);
        float length = directionLength > 0f ? directionLength : 1f;

        Vector3 point = casterPos + dir * length;
        point.y = mouseAimPoint.y;
        return point;
    }

    public static Vector3 ResolveDirection(
        UnitController caster,
        Vector3 mouseAimPoint,
        Vector2 moveInput,
        SkillIndicatorData.DirectionSource directionSource)
    {
        if (caster == null)
            return Vector3.forward;

        switch (directionSource)
        {
            case SkillIndicatorData.DirectionSource.MovementThenAimPoint:
            {
                if (TryGetMoveDirection(moveInput, out Vector3 moveDir))
                    return moveDir;
                return GetAimPointDirection(caster, mouseAimPoint);
            }
            case SkillIndicatorData.DirectionSource.MovementThenFacing:
            {
                if (TryGetMoveDirection(moveInput, out Vector3 moveDir))
                    return moveDir;
                return GetFacingDirection(caster);
            }
            case SkillIndicatorData.DirectionSource.Facing:
                return GetFacingDirection(caster);
            case SkillIndicatorData.DirectionSource.TowardAimPoint:
            default:
                return GetAimPointDirection(caster, mouseAimPoint);
        }
    }

    public static bool TryGetMoveDirection(Vector2 moveInput, out Vector3 moveDir)
    {
        moveDir = Vector3.zero;
        if (moveInput.sqrMagnitude < 0.0001f)
            return false;

        moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        return true;
    }

    public static Vector3 GetFacingDirection(UnitController caster)
    {
        Vector3 forward = caster.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return forward.normalized;
    }

    public static Vector3 GetAimPointDirection(UnitController caster, Vector3 mouseAimPoint)
    {
        if (caster == null)
            return Vector3.forward;

        return GetAimPointDirection(caster.transform.position, mouseAimPoint, GetFacingDirection(caster));
    }

    public static Vector3 GetAimPointDirection(Vector3 casterPosition, Vector3 mouseAimPoint, Vector3 fallbackDirection)
    {
        Vector3 dir = mouseAimPoint - casterPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            fallbackDirection.y = 0f;
            if (fallbackDirection.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return fallbackDirection.normalized;
        }

        return dir.normalized;
    }

    /// <summary>
    /// Horizontal aim rotation used by cast context + combat targeting (matches indicator math).
    /// </summary>
    public static Quaternion GetAimRotation(Vector3 casterPosition, Vector3 aimPoint, Vector3 fallbackForward)
    {
        Vector3 dir = GetAimPointDirection(casterPosition, aimPoint, fallbackForward);
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    public static Quaternion GetAimRotation(UnitController caster, Vector3 aimPoint)
    {
        if (caster == null)
            return Quaternion.identity;

        return GetAimRotation(caster.transform.position, aimPoint, caster.transform.forward);
    }

    /// <summary>
    /// Yaw used by <see cref="PlayerInput"/> / <see cref="UnitController.angle"/> so snap + lerp share one convention.
    /// </summary>
    public static float GetFacingAngleYaw(Vector3 casterPosition, Vector3 aimPoint)
    {
        Vector3 pos = casterPosition - aimPoint;
        return -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90f;
    }

    public static float GetFacingAngleYaw(Quaternion aimRotation)
    {
        Vector3 forward = aimRotation * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return 0f;

        // Match GetFacingAngleYaw(position, aimPoint): pos = -forward.
        Vector3 pos = -forward.normalized;
        return -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90f;
    }

    /// <summary>
    /// Combat forward for linear/cone targeting — same horizontal aim rules as indicators.
    /// </summary>
    public static Vector3 ResolveCombatDirection(CastContext castContext)
    {
        if (castContext?.caster == null)
            return Vector3.forward;

        if (castContext.aimPoint.HasValue)
        {
            return GetAimPointDirection(
                castContext.caster,
                castContext.aimPoint.Value);
        }

        if (castContext.aimRotation.HasValue)
        {
            Vector3 fromRot = castContext.aimRotation.Value * Vector3.forward;
            fromRot.y = 0f;
            if (fromRot.sqrMagnitude > 0.0001f)
                return fromRot.normalized;
        }

        return GetFacingDirection(castContext.caster);
    }
}
