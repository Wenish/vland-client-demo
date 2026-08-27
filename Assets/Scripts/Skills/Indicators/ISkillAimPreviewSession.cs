using R3;

public interface ISkillAimPreviewSession
{
    ReadOnlyReactiveProperty<SkillAimPreviewState?> State { get; }
}
