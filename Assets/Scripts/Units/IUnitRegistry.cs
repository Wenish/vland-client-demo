using System.Collections.Generic;

namespace ShadowInfection.Units
{
    public interface IUnitRegistry
    {
        void Register(UnitController unit);
        void Unregister(UnitController unit);
        IReadOnlyCollection<UnitController> Units { get; }
    }
}
