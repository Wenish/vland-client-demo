using System;
using System.Collections.Generic;

namespace ShadowInfection.Units
{
    public interface IUnitRegistry
    {
        event Action<UnitController> UnitRegistered;
        event Action<UnitController> UnitUnregistered;

        void Register(UnitController unit);
        void Unregister(UnitController unit);
        IReadOnlyCollection<UnitController> Units { get; }
    }
}
