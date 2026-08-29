using System;
using System.Collections.Generic;
using ShadowInfection.DI;

namespace ShadowInfection.Units
{
    public sealed class UnitRegistry : IUnitRegistry
    {
        private static readonly List<UnitController> pending = new List<UnitController>();
        private readonly HashSet<UnitController> units = new HashSet<UnitController>();

        public event Action<UnitController> UnitRegistered = delegate { };
        public event Action<UnitController> UnitUnregistered = delegate { };

        public UnitRegistry()
        {
            for (var i = 0; i < pending.Count; i++)
            {
                var unit = pending[i];
                if (unit != null)
                    units.Add(unit);
            }

            pending.Clear();
        }

        public IReadOnlyCollection<UnitController> Units => units;

        public void Register(UnitController unit)
        {
            if (unit != null && units.Add(unit))
                UnitRegistered(unit);
        }

        public void Unregister(UnitController unit)
        {
            if (unit != null && units.Remove(unit))
                UnitUnregistered(unit);
        }

        public static void RegisterOrDefer(UnitController unit)
        {
            if (unit == null)
                return;

            if (GameServices.TryGet<IUnitRegistry>(out var registry) && registry != null)
            {
                registry.Register(unit);
                return;
            }

            if (!pending.Contains(unit))
                pending.Add(unit);
        }

        public static void UnregisterOrDefer(UnitController unit)
        {
            if (unit == null)
                return;

            pending.Remove(unit);

            if (GameServices.TryGet<IUnitRegistry>(out var registry) && registry != null)
                registry.Unregister(unit);
        }
    }
}
