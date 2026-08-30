using Gapa.Audio;
using Gapa.Audio.Music;
using Gapa.Audio.VContainer;
using MessagePipe;
using ShadowInfection.Audio;
using ShadowInfection.Input;
using ShadowInfection.Items;
using ShadowInfection.Match;
using ShadowInfection.Targeting;
using ShadowInfection.UI.InventoryWindow;
using ShadowInfection.UI.LoadoutWindow;
using ShadowInfection.UI.Session;
using ShadowInfection.UI.ZombieMatch;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5100)]
    public sealed class GameLifetimeScope : LifetimeScope
    {
        private static GameLifetimeScope instance;
        private static bool isQuitting;
        private static bool isCreating;

        [Header("Gapa Audio")]
        [SerializeField]
        private GapaAudioRuntime gapaAudioRuntime;

        [SerializeField]
        private GapaAudioSettings gapaAudioSettings;

        [SerializeField]
        private MusicTransitionTable gapaMusicTransitionTable;

        [SerializeField]
        private MusicState gapaDefaultMusicState;

        [SerializeField]
        private SfxCatalog sfxCatalog;

        [SerializeField]
        private SceneMusicTable sceneMusicTable;

        [Header("Team Colors")]
        [SerializeField]
        private TeamColorTable teamColorTable;

        [Header("Databases")]
        [SerializeField]
        private WeaponDatabase weaponDatabase;

        [SerializeField]
        private SkillDatabase skillDatabase;

        [SerializeField]
        private UnitDatabase unitDatabase;

        [SerializeField]
        private ModelDatabase modelDatabase;

        [SerializeField]
        private ProjectileDatabase projectileDatabase;

        [SerializeField]
        private AreaZoneDatabase areaZoneDatabase;

        [SerializeField]
        private ItemDatabase itemDatabase;

        [Header("Input")]
        [SerializeField]
        private PlayerActionCatalog playerActionCatalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
            isCreating = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Application.quitting -= HandleQuitting;
            Application.quitting += HandleQuitting;
            isQuitting = false;
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

        private static void HandleQuitting()
        {
            isQuitting = true;
        }

        private static bool CanCreateRuntimeInstance()
        {
            if (isQuitting || isCreating)
                return false;
            if (!Application.isPlaying)
                return false;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return false;
#endif
            return true;
        }

        public static GameLifetimeScope FindOrCreate()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectsByType<GameLifetimeScope>(FindObjectsInactive.Include);
            GameLifetimeScope fallback = null;
            for (var i = 0; i < existing.Length; i++)
            {
                var candidate = existing[i];
                if (candidate == null)
                    continue;

                if (candidate.Container != null)
                {
                    instance = candidate;
                    return instance;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            if (fallback != null)
            {
                instance = fallback;
                return instance;
            }

            if (!CanCreateRuntimeInstance())
                return null;

            isCreating = true;
            GameObject go = null;
            try
            {
                go = new GameObject(nameof(GameLifetimeScope));
                DontDestroyOnLoad(go);
                return go.AddComponent<GameLifetimeScope>();
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

        /// <summary>
        /// Resolves a service from the live game scope. Used by UI Toolkit elements, Mirror RPCs,
        /// and other types that cannot take constructor / [Inject] dependencies cleanly.
        /// Does not create a scope: hover/click audio must not boot DI in edit mode or during shutdown.
        /// </summary>
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
                if (GetComponent<UnityEngine.UIElements.UIDocument>() != null)
                {
                    UnityEngine.Debug.LogError(
                        "GameLifetimeScope must be on a dedicated GameObject, not the HUD. Removing this component.");
                    Destroy(this);
                    return;
                }

                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            if (instance == this)
                instance = null;

            base.OnDestroy();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(container =>
                GlobalMessagePipe.SetProvider(container.AsServiceProvider()));

            var runtime = gapaAudioRuntime != null
                ? gapaAudioRuntime
                : GetComponent<GapaAudioRuntime>();

            builder.RegisterGapaAudio(new GapaAudioInstallOptions
            {
                Runtime = runtime,
                Settings = gapaAudioSettings,
                MusicTransitionTable = gapaMusicTransitionTable,
                DefaultMusicState = gapaDefaultMusicState,
            });

            var catalog = sfxCatalog != null
                ? sfxCatalog
                : Resources.Load<SfxCatalog>("Audio/SfxCatalog");
            if (catalog != null)
            {
                builder.RegisterInstance(catalog);
                builder.Register<ISfxPlayer, SfxPlayer>(Lifetime.Singleton);
            }

            var musicTable = sceneMusicTable != null
                ? sceneMusicTable
                : Resources.Load<SceneMusicTable>("Audio/SceneMusicTable");
            if (musicTable != null)
            {
                builder.RegisterInstance(musicTable);
                builder.RegisterEntryPoint<SceneMusicController>();
            }

            var colorTable = teamColorTable != null
                ? teamColorTable
                : Resources.Load<TeamColorTable>("TeamColors/TeamColorTable");
            if (colorTable == null)
            {
                UnityEngine.Debug.LogError("TeamColorTable is missing; team colors will use generated hues only.");
                colorTable = ScriptableObject.CreateInstance<TeamColorTable>();
            }

            builder.RegisterInstance(colorTable);
            builder.Register<ITeamColorService, TeamColorService>(Lifetime.Singleton);

            RegisterDatabases(builder);
            RegisterCharacterManager(builder);
            RegisterLoadoutManager(builder);
            RegisterApplicationSettings(builder);
            RegisterInputBindings(builder);

            builder.Register<MirrorSessionFlowCommands>(Lifetime.Singleton)
                .As<ISessionFlowCommands>();
            builder.Register<LoadoutManagerStore>(Lifetime.Singleton)
                .As<ILoadoutStore>();
            builder.Register<DatabaseLoadoutCatalog>(Lifetime.Singleton)
                .As<ILoadoutCatalog>();
            builder.Register<DatabaseItemCatalog>(Lifetime.Singleton)
                .As<IItemCatalog>();
            builder.Register<ActiveCharacterInventory>(Lifetime.Singleton)
                .As<IItemInventory>();
            builder.RegisterEntryPoint<InventoryWindowBootstrap>();

            var unmatchedSession = new ZombieGameManagerUiSession(null);
            builder.RegisterInstance<IZombieMatchUiSession>(unmatchedSession);
            builder.RegisterInstance<IZombieMatchCommands>(unmatchedSession);
            builder.RegisterInstance<IMatchCommands>(unmatchedSession);
            builder.RegisterInstance<IMatchActivity>(AlwaysAllowMatchActivity.Instance);
            builder.RegisterInstance<IUpgradeProgress>(FixedUpgradeProgress.Default);
            builder.RegisterInstance<IPvpObjectives>(NoPvpObjectives.Instance);
            builder.RegisterInstance<IMatchUiSession>(UnmatchedMatchUiSession.Instance);
            builder.RegisterInstance<ICastleSiegeUiSession>(UnmatchedCastleSiegeUiSession.Instance);
            builder.RegisterInstance<ISkirmishUiSession>(UnmatchedSkirmishUiSession.Instance);
            builder.RegisterInstance<IPlayerTarget>(NullPlayerTarget.Instance);
        }

        private void RegisterDatabases(IContainerBuilder builder)
        {
            var weapons = LoadDatabase(weaponDatabase, "ScriptableObjects/WeaponDatabase")
                ?? ScriptableObject.CreateInstance<WeaponDatabase>();
            var skills = LoadDatabase(skillDatabase, "ScriptableObjects/SkillDatabase")
                ?? ScriptableObject.CreateInstance<SkillDatabase>();
            var units = LoadDatabase(unitDatabase, "ScriptableObjects/UnitDatabase")
                ?? ScriptableObject.CreateInstance<UnitDatabase>();
            var models = LoadDatabase(modelDatabase, "ScriptableObjects/ModelDatabase")
                ?? ScriptableObject.CreateInstance<ModelDatabase>();
            var projectiles = LoadDatabase(projectileDatabase, "ScriptableObjects/ProjectileDatabase")
                ?? ScriptableObject.CreateInstance<ProjectileDatabase>();
            var areaZones = LoadDatabase(areaZoneDatabase, "ScriptableObjects/AreaZoneDatabase")
                ?? ScriptableObject.CreateInstance<AreaZoneDatabase>();
            var items = LoadDatabase(itemDatabase, "ScriptableObjects/ItemDatabase")
                ?? ScriptableObject.CreateInstance<ItemDatabase>();

            builder.RegisterInstance(weapons);
            builder.RegisterInstance(skills);
            builder.RegisterInstance(units);
            builder.RegisterInstance(models);
            builder.RegisterInstance(projectiles);
            builder.RegisterInstance(areaZones);
            builder.RegisterInstance(items);
            builder.RegisterInstance<IGameDatabases>(new GameDatabases(
                weapons, skills, units, models, projectiles, areaZones, items));
        }

        private static T LoadDatabase<T>(T serialized, string resourcePath) where T : ScriptableObject
        {
            if (serialized != null)
                return serialized;

            var loaded = Resources.Load<T>(resourcePath);
            if (loaded == null)
                UnityEngine.Debug.LogError($"Missing {typeof(T).Name} at Resources/{resourcePath}.");

            return loaded;
        }

        private void RegisterCharacterManager(IContainerBuilder builder)
        {
            var characters = GetComponent<CharacterManager>();
            if (characters == null)
                characters = FindAnyObjectByType<CharacterManager>(FindObjectsInactive.Include);
            if (characters == null)
                characters = gameObject.AddComponent<CharacterManager>();

            builder.RegisterComponent(characters);
        }

        private void RegisterLoadoutManager(IContainerBuilder builder)
        {
            var loadout = GetComponent<LoadoutManager>();
            if (loadout == null)
                loadout = FindAnyObjectByType<LoadoutManager>(FindObjectsInactive.Include);
            if (loadout == null)
                loadout = gameObject.AddComponent<LoadoutManager>();

            builder.RegisterComponent(loadout);
        }

        private void RegisterApplicationSettings(IContainerBuilder builder)
        {
            builder.Register<ApplicationSettings>(_ =>
            {
                var settings = FindAnyObjectByType<ApplicationSettings>(FindObjectsInactive.Include);
                if (settings != null)
                    return settings;

                var host = instance != null ? instance.gameObject : gameObject;
                return host.GetComponent<ApplicationSettings>()
                    ?? host.AddComponent<ApplicationSettings>();
            }, Lifetime.Singleton);
        }

        private void RegisterInputBindings(IContainerBuilder builder)
        {
            var catalog = playerActionCatalog != null
                ? playerActionCatalog
                : Resources.Load<PlayerActionCatalog>("ScriptableObjects/Input/PlayerActionCatalog");
            if (catalog == null || !catalog.HasActions)
                catalog = PlayerActionCatalog.CreateRuntime();

            builder.RegisterInstance(catalog);
            builder.Register<InputBindingsService>(Lifetime.Singleton)
                .As<IInputReader>()
                .As<IInputBindingSession>()
                .As<IInputBindingCommands>();
        }
    }
}
