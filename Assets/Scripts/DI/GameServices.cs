using UnityEngine;

namespace ShadowInfection.DI
{
    /// <summary>
    /// Locator for spawned NetworkBehaviours, ScriptableObjects, and other types
    /// that cannot take constructor / [Inject] dependencies.
    /// Prefers the scene gameplay scope, then the app-wide game scope.
    /// </summary>
    public static class GameServices
    {
        public static bool TryGet<T>(out T service)
        {
            if (GameplayLifetimeScope.TryResolve(out service) && service != null)
                return true;

            if (GameLifetimeScope.TryResolve(out service) && service != null)
                return true;

            service = default;
            return false;
        }

        public static T Get<T>() where T : class
        {
            TryGet(out T service);
            return service;
        }

        public static IGameDatabases Databases => Get<IGameDatabases>();
        public static ApplicationSettings Settings => Get<ApplicationSettings>();
        public static LoadoutManager Loadout => Get<LoadoutManager>();
        public static PlayerUnitsManager PlayerUnits => Get<PlayerUnitsManager>();
        public static IUnitSpawner Units => Get<IUnitSpawner>();
        public static IProjectileSpawner Projectiles => Get<IProjectileSpawner>();
        public static IAreaZoneSpawner AreaZones => Get<IAreaZoneSpawner>();
        public static VendorManager Vendors => Get<VendorManager>();
        public static UpgradeManager Upgrades => Get<UpgradeManager>();
        public static ZombieGameManager ZombieMatch => Get<ZombieGameManager>();
        public static CastleSiegeManager CastleSiege => Get<CastleSiegeManager>();
        public static SkirmishGameManager Skirmish => Get<SkirmishGameManager>();
        public static MatchGameManagerBase Match => Get<MatchGameManagerBase>();
    }
}
