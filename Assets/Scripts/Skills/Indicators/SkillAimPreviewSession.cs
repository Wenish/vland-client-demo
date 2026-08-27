using R3;
using UnityEngine;

public sealed class SkillAimPreviewSession : ISkillAimPreviewSession, ISkillAimPreviewSessionNotifier
{
    private readonly ReactiveProperty<SkillAimPreviewState?> _state = new(null);

    public ReadOnlyReactiveProperty<SkillAimPreviewState?> State => _state;

    public void Begin(SkillAimPreviewState state)
    {
        _state.Value = state;
    }

    public void Update(Vector3 aimPoint, UnitController followTarget)
    {
        if (_state.Value is not { } current)
            return;

        _state.Value = new SkillAimPreviewState(
            current.SnapsToTarget,
            followTarget,
            aimPoint,
            current.Skill);
    }

    public void End()
    {
        _state.Value = null;
    }
}
