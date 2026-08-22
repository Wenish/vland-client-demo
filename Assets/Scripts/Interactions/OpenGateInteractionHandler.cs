using MyGame.Events;

namespace ShadowInfection.Interactions
{
    public sealed class OpenGateInteractionHandler : IInteractionHandler
    {
        public InteractionType Type => InteractionType.OpenGate;

        public void Handle(InteractionZone zone, PlayerController player)
        {
            if (zone == null || player == null)
                return;

            if (!player.SpendGold(zone.GoldCost))
            {
                UnityEngine.Debug.Log("Not enough gold");
                return;
            }

            GameMessages.Publish(new OpenGateEvent(zone.InteractionId));
        }
    }
}
