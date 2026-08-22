using System.Collections.Generic;
using ShadowInfection.DI;
using ShadowInfection.Units;

namespace ShadowInfection.Units
{
    public static class UnitRegistryAccess
    {
        public static IReadOnlyCollection<UnitController> GetUnits()
        {
            var registry = GameServices.Get<IUnitRegistry>();
            return registry != null ? registry.Units : System.Array.Empty<UnitController>();
        }
    }
}
