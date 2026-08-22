using System.Collections.Generic;

namespace ShadowInfection.Interactions
{
    public sealed class InteractionZoneRegistry : IInteractionZoneRegistry
    {
        private static readonly List<InteractionZone> pending = new List<InteractionZone>();
        private readonly HashSet<InteractionZone> zones = new HashSet<InteractionZone>();

        public InteractionZoneRegistry()
        {
            for (var i = 0; i < pending.Count; i++)
            {
                var zone = pending[i];
                if (zone != null)
                    zones.Add(zone);
            }

            pending.Clear();
        }

        public IReadOnlyCollection<InteractionZone> Zones => zones;

        public void Register(InteractionZone zone)
        {
            if (zone != null)
                zones.Add(zone);
        }

        public void Unregister(InteractionZone zone)
        {
            if (zone != null)
                zones.Remove(zone);
        }

        public static void RegisterOrDefer(InteractionZone zone)
        {
            if (zone == null)
                return;

            if (ShadowInfection.DI.GameServices.TryGet<IInteractionZoneRegistry>(out var registry) && registry != null)
            {
                registry.Register(zone);
                return;
            }

            if (!pending.Contains(zone))
                pending.Add(zone);
        }

        public static void UnregisterOrDefer(InteractionZone zone)
        {
            if (zone == null)
                return;

            pending.Remove(zone);

            if (ShadowInfection.DI.GameServices.TryGet<IInteractionZoneRegistry>(out var registry) && registry != null)
                registry.Unregister(zone);
        }
    }
}
