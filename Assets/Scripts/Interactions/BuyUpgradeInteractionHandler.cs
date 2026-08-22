using MyGame.Events;

namespace ShadowInfection.Interactions
{
    public sealed class BuyUpgradeInteractionHandler : IInteractionHandler
    {
        public InteractionType Type => InteractionType.BuyUpgrade;

        public void Handle(InteractionZone zone, PlayerController player)
        {
            if (zone == null || player == null)
                return;

            GameMessages.Publish(new BuyUpgradeEvent(zone, player));
        }
    }
}
