using UnityEngine;

public interface ISkillIndicatorService
{
    void BeginPreview(UnitController caster, SkillIndicatorDisplayParams display, Vector3 aimPoint);
    void BeginPreview(
        UnitController caster,
        SkillIndicatorDisplayParams display,
        Vector3 aimPoint,
        SkillIndicatorData visualSource,
        UnitController followTarget = null,
        NetworkedSkillInstance skillInstance = null);
    void UpdateAim(Vector3 aimPoint, UnitController followTarget = null, Vector2 moveInput = default);
    void EndPreview();

    void BeginSession(
        int sessionId,
        UnitController caster,
        SkillIndicatorDisplayParams display,
        Vector3 aimPoint,
        UnitController followTarget = null,
        NetworkedSkillInstance skillInstance = null);
    void EndSession(int sessionId);
    void EndAllSessions();
}
