using System.Collections.Generic;

namespace ShadowInfection.World
{
    public interface IZombieSpawnRegistry
    {
        void Register(ZombieSpawnController spawn);
        void Unregister(ZombieSpawnController spawn);
        IReadOnlyCollection<ZombieSpawnController> Spawns { get; }
    }
}
