namespace ShadowInfection.Input
{
    public readonly struct InputBindingsChangedEvent
    {
        public readonly int Revision;

        public InputBindingsChangedEvent(int revision)
        {
            Revision = revision;
        }
    }
}
