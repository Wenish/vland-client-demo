namespace ShadowInfection.Interactions
{
    public interface IInteractionHandler
    {
        InteractionType Type { get; }

        /// <summary>Server-only interact handling for a zone the player is currently in.</summary>
        void Handle(InteractionZone zone, PlayerController player);
    }
}
