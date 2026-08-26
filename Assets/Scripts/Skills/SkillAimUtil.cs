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
}
