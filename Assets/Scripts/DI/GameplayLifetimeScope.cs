using ShadowInfection.Interactions;
using ShadowInfection.Match;
using ShadowInfection.UI.ZombieMatch;
using ShadowInfection.Units;
using ShadowInfection.World;
using ShadowInfection.Zombie;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5050)]
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        private static GameplayLifetimeScope instance;
        private static bool isQuitting;
        private static bool isCreating;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
            isCreating = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CreateForLoadedScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CreateForLoadedScene();
        }

        private static void CreateForLoadedScene()
        {
            if (!Application.isPlaying || isQuitting)
                return;

            FindOrCreate();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPlayModeGuard()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                isQuitting = true;
            else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                instance = null;
                isQuitting = false;
                isCreating = false;
            }
        }
#endif

        public static GameplayLifetimeScope FindOrCreate()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectsByType<GameplayLifetimeScope>(FindObjectsInactive.Include);
            for (var i = 0; i < existing.Length; i++)
            {
                var candidate = existing[i];
                if (candidate == null)
                    continue;

                instance = candidate;
                return instance;
            }

            if (!Application.isPlaying || isQuitting || isCreating)
                return null;

            isCreating = true;
            GameObject go = null;
            try
            {
                go = new GameObject(nameof(GameplayLifetimeScope));
                return go.AddComponent<GameplayLifetimeScope>();
            }
            catch
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }

                return null;
            }
            finally
            {
                isCreating = false;
            }
        }

        public static bool TryResolve<T>(out T service)
        {
            service = default;
            var scope = instance;
            if (scope == null || scope.Container == null)
                return false;

            try
            {
                service = scope.Container.Resolve<T>();
                return service != null;
            }
            catch
            {
                return false;
            }
        }

        protected override void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            parentReference = ParentReference.Create<GameLifetimeScope>();
            parentReference.Object = GameLifetimeScope.FindOrCreate();
            base.Awake();
        }

        protected override LifetimeScope FindParent()
        {
            return GameLifetimeScope.FindOrCreate();
        }

        protected override void OnDestroy()
        {
            if (instance == this)
                instance = null;

            base.OnDestroy();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterIfPresent<UnitSpawner>(builder, out var unitSpawner);
            if (unitSpawner != null)
                builder.RegisterInstance<IUnitSpawner>(unitSpawner);

            RegisterIfPresent<ProjectileSpawner>(builder, out var projectileSpawner);
            if (projectileSpawner != null)
                builder.RegisterInstance<IProjectileSpawner>(projectileSpawner);

            RegisterIfPresent<AreaZoneSpawner>(builder, out var areaZoneSpawner);
            if (areaZoneSpawner != null)
                builder.RegisterInstance<IAreaZoneSpawner>(areaZoneSpawner);

            RegisterIfPresent<PlayerUnitsManager>(builder, out _);
            RegisterIfPresent<VendorManager>(builder, out _);
            RegisterIfPresent<UpgradeManager>(builder, out _);
            RegisterIfPresent<InteractionZoneManager>(builder, out _);
            RegisterIfPresent<SpawnManager>(builder, out _);
            RegisterIfPresent<ShadowInfection.Debug.DPSTracker>(builder, out _);

            builder.Register<IInteractionHandlerRegistry>(
                _ => new InteractionHandlerRegistry(new IInteractionHandler[]
                {
                    new OpenGateInteractionHandler(),
                    new BuyUpgradeInteractionHandler(),
                }),
                Lifetime.Singleton);
            builder.Register<InteractionZoneRegistry>(Lifetime.Singleton).As<IInteractionZoneRegistry>();
            builder.Register<UnitRegistry>(Lifetime.Singleton).As<IUnitRegistry>();
            builder.Register<GateRegistry>(Lifetime.Singleton).As<IGateRegistry>();
            builder.Register<ZombieSpawnRegistry>(Lifetime.Singleton).As<IZombieSpawnRegistry>();

            RegisterIfPresent<ZombieGameManager>(builder, out var zombie);
            RegisterIfPresent<CastleSiegeManager>(builder, out var castle);
            RegisterIfPresent<SkirmishGameManager>(builder, out var skirmish);

            if (zombie != null)
            {
                zombie.EnsureServices();
                builder.RegisterInstance(zombie.WaveDirector);
                builder.RegisterInstance(zombie.LeaderboardService);
                builder.RegisterInstance(zombie.RunEndService);
            }

            if (castle != null)
            {
                castle.EnsureServices();
                builder.RegisterInstance(castle.MatchDirector);
                builder.RegisterInstance(castle.PlayerService);
                builder.RegisterInstance(castle.ObjectiveService);
            }

            if (skirmish != null)
            {
                skirmish.EnsureServices();
                builder.RegisterInstance(skirmish.RoundDirector);
                builder.RegisterInstance(skirmish.PlayerService);
                builder.RegisterInstance(skirmish.ScoreService);
            }

            MatchGameManagerBase match = castle != null
                ? castle
                : skirmish;
            if (match != null)
                builder.RegisterInstance(match);

            RegisterMatchContracts(builder, zombie, castle, skirmish);
        }

        private static void RegisterMatchContracts(
            IContainerBuilder builder,
            ZombieGameManager zombie,
            CastleSiegeManager castle,
            SkirmishGameManager skirmish)
        {
            if (zombie != null)
            {
                builder.RegisterInstance<IMatchActivity>(new ZombieMatchActivity(zombie));
                builder.RegisterInstance<IUpgradeProgress>(new ZombieUpgradeProgress(zombie));
                var session = new ZombieGameManagerUiSession(zombie);
                builder.RegisterInstance<IZombieMatchUiSession>(session);
                builder.RegisterInstance<IZombieMatchCommands>(session);
                builder.RegisterInstance<IMatchCommands>(session);
                RegisterUnmatchedPvpSessions(builder);
                return;
            }

            if (castle != null)
            {
                builder.RegisterInstance<IMatchActivity>(new CastleSiegeMatchActivity(castle));
                builder.RegisterInstance<IPvpObjectives>(new CastleSiegePvpObjectives(castle));
                builder.RegisterInstance<IMatchCommands>(new MatchGameManagerCommands(castle));
                builder.RegisterInstance<IMatchUiSession>(new MatchGameManagerUiSession(castle));
                builder.RegisterInstance<ICastleSiegeUiSession>(new CastleSiegeUiSession(castle));
                builder.RegisterInstance<ISkirmishUiSession>(UnmatchedSkirmishUiSession.Instance);
                RegisterUnmatchedZombieSession(builder);
                return;
            }

            if (skirmish != null)
            {
                builder.RegisterInstance<IMatchActivity>(new SkirmishMatchActivity(skirmish));
                builder.RegisterInstance<IMatchCommands>(new MatchGameManagerCommands(skirmish));
                builder.RegisterInstance<IMatchUiSession>(new MatchGameManagerUiSession(skirmish));
                builder.RegisterInstance<ICastleSiegeUiSession>(UnmatchedCastleSiegeUiSession.Instance);
                builder.RegisterInstance<ISkirmishUiSession>(new SkirmishUiSession(skirmish));
                RegisterUnmatchedZombieSession(builder);
                return;
            }

            RegisterUnmatchedZombieSession(builder);
            RegisterUnmatchedPvpSessions(builder);
        }

        private static void RegisterUnmatchedPvpSessions(IContainerBuilder builder)
        {
            builder.RegisterInstance<IMatchUiSession>(UnmatchedMatchUiSession.Instance);
            builder.RegisterInstance<ICastleSiegeUiSession>(UnmatchedCastleSiegeUiSession.Instance);
            builder.RegisterInstance<ISkirmishUiSession>(UnmatchedSkirmishUiSession.Instance);
        }

        private static void RegisterUnmatchedZombieSession(IContainerBuilder builder)
        {
            var session = new ZombieGameManagerUiSession(null);
            builder.RegisterInstance<IZombieMatchUiSession>(session);
            builder.RegisterInstance<IZombieMatchCommands>(session);
        }

        private static void RegisterIfPresent<T>(IContainerBuilder builder, out T component)
            where T : Component
        {
            component = FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (component != null)
                builder.RegisterComponent(component);
        }
    }
}
