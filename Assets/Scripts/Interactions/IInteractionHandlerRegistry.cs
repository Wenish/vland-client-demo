namespace ShadowInfection.Interactions
{
    public interface IInteractionHandlerRegistry
    {
        bool TryGet(InteractionType type, out IInteractionHandler handler);
    }
}
