using System.Collections.Generic;
using ShadowInfection.DI;

namespace ShadowInfection.World
{
    public sealed class ZombieSpawnRegistry : IZombieSpawnRegistry
    {
        private static readonly List<ZombieSpawnController> pending = new List<ZombieSpawnController>();
        private readonly HashSet<ZombieSpawnController> spawns = new HashSet<ZombieSpawnController>();

        public ZombieSpawnRegistry()
        {
            for (var i = 0; i < pending.Count; i++)
            {
                var spawn = pending[i];
                if (spawn != null)
                    spawns.Add(spawn);
            }

            pending.Clear();
        }

        public IReadOnlyCollection<ZombieSpawnController> Spawns => spawns;

        public void Register(ZombieSpawnController spawn)
        {
            if (spawn != null)
                spawns.Add(spawn);
        }

        public void Unregister(ZombieSpawnController spawn)
        {
            if (spawn != null)
                spawns.Remove(spawn);
        }

        public static void RegisterOrDefer(ZombieSpawnController spawn)
        {
            if (spawn == null)
                return;

            if (GameServices.TryGet<IZombieSpawnRegistry>(out var registry) && registry != null)
            {
                registry.Register(spawn);
                return;
            }

            if (!pending.Contains(spawn))
                pending.Add(spawn);
        }

        public static void UnregisterOrDefer(ZombieSpawnController spawn)
        {
            if (spawn == null)
                return;

            pending.Remove(spawn);

            if (GameServices.TryGet<IZombieSpawnRegistry>(out var registry) && registry != null)
                registry.Unregister(spawn);
        }
    }
}
