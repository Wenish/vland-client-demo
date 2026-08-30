namespace ShadowInfection.Targeting
{
    public sealed class NullPlayerTarget : IPlayerTarget
    {
        public static readonly NullPlayerTarget Instance = new();

        public UnitController Current => null;

        public bool HasTarget => false;

        public void Set(UnitController unit)
        {
        }

        public void Clear()
        {
        }

        public bool TryGetSnapshot(out PlayerTargetSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }
    }
}
