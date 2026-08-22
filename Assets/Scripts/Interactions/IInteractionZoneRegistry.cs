using System.Collections.Generic;

namespace ShadowInfection.Interactions
{
    public interface IInteractionZoneRegistry
    {
        void Register(InteractionZone zone);
        void Unregister(InteractionZone zone);
        IReadOnlyCollection<InteractionZone> Zones { get; }
    }
}
