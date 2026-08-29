using UnityEngine;

public static class SkillAimUtil
{
    /// <summary>
    /// True 0% turn — facing snaps; mouse cannot steer.
    /// Values like 0.05 still lerp (slow aim adjust).
    /// </summary>
    public static bool IsTurnSpeedLocked(float turnSpeed)
    {
        return turnSpeed <= 0.0001f;
    }

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
    /// Movement-based indicators (Evade) bake dash direction into a world point.
    /// Facing must not: a chasing point along <c>transform.forward</c> fights mouse yaw
    /// and dirties the live-aim pipeline every frame.
    /// </summary>
    public static bool ProjectsDirectionIntoAimPoint(
        SkillIndicatorData.IndicatorShape shape,
        SkillIndicatorData.DirectionSource directionSource)
    {
        if (shape != SkillIndicatorData.IndicatorShape.Directional
            && shape != SkillIndicatorData.IndicatorShape.Cone)
            return false;

        return directionSource == SkillIndicatorData.DirectionSource.MovementThenAimPoint
            || directionSource == SkillIndicatorData.DirectionSource.MovementThenFacing;
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
    /// Spawn rotation for projectiles / VFX / units.
    /// Default is the unit's current facing (turn speed can make this differ from mouse aim).
    /// <paramref name="atAimPoint"/> uses cast aim for ground-targeted spawns.
    /// </summary>
    public static Quaternion ResolveSpawnRotation(
        CastContext castContext,
        UnitController target,
        bool atAimPoint = false)
    {
        if (atAimPoint && castContext != null)
        {
            if (castContext.aimRotation.HasValue)
                return castContext.aimRotation.Value;

            if (castContext.aimPoint.HasValue && castContext.caster != null)
                return GetAimRotation(castContext.caster, castContext.aimPoint.Value);
        }

        UnitController facingUnit = target != null ? target : castContext?.caster;
        if (facingUnit == null)
            return Quaternion.identity;

        return Quaternion.LookRotation(GetFacingDirection(facingUnit), Vector3.up);
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
    /// Movement-based indicators (Evade-style) encode skill direction, not where the unit should face.
    /// Skip cast-start facing snap while moving so mouse aim is not briefly overridden.
    /// </summary>
    public static bool ShouldSnapFacingToCastAim(
        UnitController caster,
        Vector2 moveInput,
        SkillIndicatorData indicator)
    {
        if (indicator == null)
            return true;

        if (indicator.shape != SkillIndicatorData.IndicatorShape.Directional
            && indicator.shape != SkillIndicatorData.IndicatorShape.Cone)
            return true;

        switch (indicator.directionSource)
        {
            case SkillIndicatorData.DirectionSource.MovementThenFacing:
            case SkillIndicatorData.DirectionSource.MovementThenAimPoint:
                return !TryGetMoveDirection(moveInput, out _);
            default:
                return true;
        }
    }

    /// <summary>
    /// Combat forward for linear/cone targeting and unit-spawned shots — the caster's look direction.
    /// Mouse aim can differ while the body is still turning.
    /// </summary>
    public static Vector3 ResolveCombatDirection(CastContext castContext)
    {
        if (castContext?.caster == null)
            return Vector3.forward;

        return GetFacingDirection(castContext.caster);
    }

    public static bool PlacementUsesAimPoint(SkillIndicatorData.IndicatorPlacement placement)
    {
        return placement == SkillIndicatorData.IndicatorPlacement.AtAimPoint;
    }

    /// <summary>
    /// Shared circle / ground AoE position for skill mechanics (VFX, area zones, etc.).
    /// At aim point = mouse aim (or snapped unit). Otherwise = effect target position
    /// (not caster — IndicatorPlacement.Self is only for indicator previews).
    /// </summary>
    public static Vector3 ResolveCirclePlacement(
        CastContext castContext,
        UnitController effectTarget,
        bool atAimPoint,
        UnitController snapTarget = null)
    {
        // Snap wins for aim-point placement; for non-aim, prefer the effect target.
        // Dead units are valid when smart-target LifeMask includes Dead (e.g. Resurrection).
        UnitController placementTarget = atAimPoint ? snapTarget : (snapTarget ?? effectTarget);
        if (!atAimPoint && placementTarget != null)
            return placementTarget.transform.position;

        Vector3 aim = castContext?.aimPoint ?? effectTarget?.transform.position ?? Vector3.zero;
        return ResolveCirclePlacement(
            castContext?.caster,
            aim,
            atAimPoint
                ? SkillIndicatorData.IndicatorPlacement.AtAimPoint
                : SkillIndicatorData.IndicatorPlacement.Self,
            atAimPoint ? snapTarget : null);
    }

    /// <summary>
    /// Indicator-side circle placement (preview + cast overlay).
    /// </summary>
    public static Vector3 ResolveCirclePlacement(
        UnitController caster,
        Vector3 aimPoint,
        SkillIndicatorData.IndicatorPlacement placement,
        UnitController snapTarget = null,
        float visualYOffset = 0.05f)
    {
        float groundY = caster != null
            ? caster.transform.position.y + visualYOffset
            : aimPoint.y;

        if (snapTarget != null)
        {
            Vector3 pos = snapTarget.transform.position;
            pos.y = groundY;
            return pos;
        }

        if (placement == SkillIndicatorData.IndicatorPlacement.Self)
        {
            return caster != null
                ? caster.transform.position + Vector3.up * visualYOffset
                : aimPoint;
        }

        Vector3 aim = aimPoint;
        aim.y = groundY;
        return aim;
    }

    /// <summary>
    /// Origin for cones/lines — always anchored on the caster (original TargetAreaVFX behavior).
    /// </summary>
    public static Vector3 ResolveDirectionalOrigin(CastContext castContext, float yOffset = 0.01f)
    {
        if (castContext?.caster == null)
            return Vector3.zero;

        return castContext.caster.transform.position + Vector3.up * yOffset;
    }

    public static Vector3 ResolveDirectionalOrigin(UnitController caster, float yOffset = 0.05f)
    {
        if (caster == null)
            return Vector3.zero;

        return caster.transform.position + Vector3.up * yOffset;
    }

    /// <summary>
    /// Indicator cones/lines follow mouse / directionSource, not body facing.
    /// </summary>
    public static Vector3 ResolveDirectionalFacing(
        CastContext castContext,
        UnitController caster,
        Vector3 aimPoint,
        SkillIndicatorData.DirectionSource directionSource = SkillIndicatorData.DirectionSource.TowardAimPoint,
        Vector2 moveInput = default)
    {
        UnitController from = caster != null ? caster : castContext?.caster;
        Vector3 aim = aimPoint;
        if (castContext != null && castContext.aimPoint.HasValue)
            aim = castContext.aimPoint.Value;

        if (from == null)
            return Vector3.forward;

        return ResolveDirection(from, aim, moveInput, directionSource);
    }

    /// <summary>
    /// World origin for area VFX meshes.
    /// </summary>
    public static Vector3 ResolveAreaVfxOrigin(
        CastContext castContext,
        UnitController target,
        AreaVFXShape shape,
        bool spawnAtAimPoint = false)
    {
        if (shape == AreaVFXShape.Rectangle || shape == AreaVFXShape.Cone)
            return ResolveDirectionalOrigin(castContext);

        return ResolveCirclePlacement(castContext, target, spawnAtAimPoint);
    }

    /// <summary>
    /// Horizontal forward for area VFX spawned from the caster — unit facing, same as projectiles.
    /// </summary>
    public static Vector3 ResolveAreaVfxDirection(CastContext castContext, UnitController target)
    {
        if (castContext?.caster != null)
            return GetFacingDirection(castContext.caster);

        if (target != null)
            return GetFacingDirection(target);

        return Vector3.forward;
    }

    /// <summary>
    /// Parent transform for attached area VFX. Directional shapes attach to the caster so they
    /// turn with the unit. Aim-placed circles stay in world space.
    /// </summary>
    public static Transform ResolveAreaVfxAttachTransform(
        CastContext castContext,
        UnitController target,
        AreaVFXShape shape,
        bool attachToTarget,
        bool spawnAtAimPoint)
    {
        if (!attachToTarget)
            return null;

        if (shape == AreaVFXShape.Circle
            && spawnAtAimPoint
            && castContext?.aimPoint.HasValue == true)
        {
            return null;
        }

        if (castContext?.caster != null
            && (shape == AreaVFXShape.Rectangle || shape == AreaVFXShape.Cone))
        {
            return castContext.caster.transform;
        }

        if (target != null)
            return target.transform;

        return castContext?.caster != null ? castContext.caster.transform : null;
    }
}
