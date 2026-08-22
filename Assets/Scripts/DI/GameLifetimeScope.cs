using Gapa.Audio;
using Gapa.Audio.Music;
using Gapa.Audio.VContainer;
using MessagePipe;
using ShadowInfection.Audio;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5100)]
    public sealed class GameLifetimeScope : LifetimeScope
    {
        private static GameLifetimeScope instance;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            FindOrCreate();
        }

        public static GameLifetimeScope FindOrCreate()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectsByType<GameLifetimeScope>(FindObjectsInactive.Exclude);
            for (var i = 0; i < existing.Length; i++)
            {
                var candidate = existing[i];
                if (candidate != null && candidate.Container != null)
                {
                    instance = candidate;
                    return instance;
                }
            }

            var go = new GameObject(nameof(GameLifetimeScope));
            DontDestroyOnLoad(go);
            return go.AddComponent<GameLifetimeScope>();
        }

        /// <summary>
        /// Resolves a service from the live game scope. Used by UI Toolkit elements, Mirror RPCs,
        /// and other types that cannot take constructor / [Inject] dependencies cleanly.
        /// </summary>
        public static bool TryResolve<T>(out T service)
        {
            service = default;
            var scope = instance != null ? instance : FindOrCreate();
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
        }
    }
}
