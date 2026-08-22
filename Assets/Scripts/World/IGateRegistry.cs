using System.Collections.Generic;

namespace ShadowInfection.World
{
    public interface IGateRegistry
    {
        void Register(GateController gate);
        void Unregister(GateController gate);
        IReadOnlyCollection<GateController> Gates { get; }
    }
}
