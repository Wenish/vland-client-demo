namespace ShadowInfection.Match
{
    public interface IPvpObjectives
    {
        bool TryGetPriorityTarget(UnitController seeker, out UnitController target);
    }
}
