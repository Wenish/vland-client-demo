using UnityEngine;

public interface ISkillAimPreviewSessionNotifier
{
    void Begin(SkillAimPreviewState state);
    void Update(Vector3 aimPoint, UnitController followTarget);
    void End();
}
