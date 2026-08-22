using System.Collections.Generic;
using ShadowInfection.DI;

namespace ShadowInfection.World
{
    public sealed class GateRegistry : IGateRegistry
    {
        private static readonly List<GateController> pending = new List<GateController>();
        private readonly HashSet<GateController> gates = new HashSet<GateController>();

        public GateRegistry()
        {
            for (var i = 0; i < pending.Count; i++)
            {
                var gate = pending[i];
                if (gate != null)
                    gates.Add(gate);
            }

            pending.Clear();
        }

        public IReadOnlyCollection<GateController> Gates => gates;

        public void Register(GateController gate)
        {
            if (gate != null)
                gates.Add(gate);
        }

        public void Unregister(GateController gate)
        {
            if (gate != null)
                gates.Remove(gate);
        }

        public static void RegisterOrDefer(GateController gate)
        {
            if (gate == null)
                return;

            if (GameServices.TryGet<IGateRegistry>(out var registry) && registry != null)
            {
                registry.Register(gate);
                return;
            }

            if (!pending.Contains(gate))
                pending.Add(gate);
        }

        public static void UnregisterOrDefer(GateController gate)
        {
            if (gate == null)
                return;

            pending.Remove(gate);

            if (GameServices.TryGet<IGateRegistry>(out var registry) && registry != null)
                registry.Unregister(gate);
        }
    }
}
