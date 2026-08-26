using UnityEngine;

public interface ISkillIndicatorService
{
    void BeginPreview(UnitController caster, SkillIndicatorDisplayParams display, Vector3 aimPoint);
    void BeginPreview(
        UnitController caster,
        SkillIndicatorDisplayParams display,
        Vector3 aimPoint,
        SkillIndicatorData visualSource);
    void UpdateAim(Vector3 aimPoint);
    void EndPreview();

    void BeginSession(int sessionId, UnitController caster, SkillIndicatorDisplayParams display, Vector3 aimPoint);
    void EndSession(int sessionId);
    void EndAllSessions();
}
