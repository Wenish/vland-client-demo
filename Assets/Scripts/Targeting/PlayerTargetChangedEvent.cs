namespace ShadowInfection.Targeting
{
    public readonly struct PlayerTargetChangedEvent
    {
        public PlayerTargetChangedEvent(UnitController previous, UnitController current)
        {
            Previous = previous;
            Current = current;
        }

        public UnitController Previous { get; }
        public UnitController Current { get; }
    }
}
