using System;
using System.Reflection;
using Gapa.Audio.Music;
using UnityEngine;
using VContainer;

namespace Gapa.Audio.VContainer
{
    /// <summary>
    /// VContainer adapter for <see href="https://github.com/VoxelCoreLab/gapa-audio">Gapa Audio</see>.
    /// Registers <see cref="IAudioService"/>, <see cref="IMusicService"/>, and
    /// <see cref="Gapa.Audio.Ambience.IAmbienceService"/> from a <see cref="GapaAudioRuntime"/> composition root.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override void Configure(IContainerBuilder builder)
    /// {
    ///     builder.RegisterGapaAudio(new GapaAudioInstallOptions
    ///     {
    ///         Runtime = gapaAudioRuntime,
    ///         Settings = gapaAudioSettings,
    ///     });
    /// }
    /// </code>
    /// </example>
    public static class GapaAudioContainerBuilderExtensions
    {
        public const string DefaultSettingsResourcePath = "Audio/GapaAudioSettings";

        /// <summary>
        /// Resolves or creates a <see cref="GapaAudioRuntime"/> and registers its services as
        /// singletons on <paramref name="builder"/>.
        /// </summary>
        public static GapaAudioRuntime RegisterGapaAudio(
            this IContainerBuilder builder,
            GapaAudioInstallOptions options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            options ??= new GapaAudioInstallOptions();
            var runtime = GapaAudioRuntimeResolver.ResolveOrCreate(options);
            RegisterResolvedRuntime(builder, runtime, options);
            return runtime;
        }

        /// <summary>
        /// Registers services from an already-placed <see cref="GapaAudioRuntime"/>.
        /// GameLifetimeScope runs before Gapa's default execution order, so this forces Awake
        /// first — otherwise <see cref="GapaAudioRuntime.Audio"/> would still be null.
        /// </summary>
        public static GapaAudioRuntime RegisterGapaAudio(
            this IContainerBuilder builder,
            GapaAudioRuntime runtime)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            var options = new GapaAudioInstallOptions { Runtime = runtime };
            GapaAudioRuntimeResolver.EnsureInitialized(runtime);
            RegisterResolvedRuntime(builder, runtime, options);
            return runtime;
        }

        /// <summary>
        /// Creates a DontDestroyOnLoad <see cref="GapaAudioRuntime"/> from
        /// <paramref name="settings"/> and registers its services.
        /// </summary>
        public static GapaAudioRuntime RegisterGapaAudio(
            this IContainerBuilder builder,
            GapaAudioSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return builder.RegisterGapaAudio(new GapaAudioInstallOptions { Settings = settings });
        }

        private static void RegisterResolvedRuntime(
            IContainerBuilder builder,
            GapaAudioRuntime runtime,
            GapaAudioInstallOptions options)
        {
            builder.RegisterInstance(runtime);
            builder.RegisterInstance(runtime.Audio);
            builder.RegisterInstance(runtime.Music);
            builder.RegisterInstance(runtime.Ambience);

            if (options.RegisterZoneCoordinators)
            {
                builder.RegisterInstance(runtime.MusicZones);
                builder.RegisterInstance(runtime.AmbienceZones);
            }

            if (options.RegisterSettings && runtime.Settings != null)
                builder.RegisterInstance(runtime.Settings);
        }
    }

    /// <summary>
    /// Finds, initializes, or constructs the Gapa Audio composition root. Kept out of the public
    /// extension surface so call sites stay <c>builder.RegisterGapaAudio(...)</c>.
    /// </summary>
    internal static class GapaAudioRuntimeResolver
    {
        public static GapaAudioRuntime ResolveOrCreate(GapaAudioInstallOptions options)
        {
            var runtime = options.Runtime;
            if (runtime == null)
                runtime = UnityEngine.Object.FindFirstObjectByType<GapaAudioRuntime>(FindObjectsInactive.Include);

            var settings = options.Settings;
            if (settings == null && (runtime == null || runtime.Settings == null))
                settings = Resources.Load<GapaAudioSettings>(GapaAudioContainerBuilderExtensions.DefaultSettingsResourcePath);

            if (runtime != null)
            {
                ApplySerializedOverrides(runtime, settings, options.MusicTransitionTable, options.DefaultMusicState);
                EnsureInitialized(runtime);
                return runtime;
            }

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "Gapa Audio is not installed: no GapaAudioRuntime in the scene, no settings " +
                    "passed to RegisterGapaAudio, and no asset at Resources/" +
                    GapaAudioContainerBuilderExtensions.DefaultSettingsResourcePath + ". " +
                    "Add a Gapa Audio Runtime to the bootstrap scene, or assign GapaAudioSettings " +
                    "on GameLifetimeScope.");
            }

            return CreateRuntime(
                settings,
                options.MusicTransitionTable,
                options.DefaultMusicState,
                options.DontDestroyOnLoad);
        }

        public static void EnsureInitialized(GapaAudioRuntime runtime)
        {
            if (runtime.Audio != null)
                return;

            var go = runtime.gameObject;
            if (go.activeSelf)
                go.SetActive(false);

            go.SetActive(true);

            if (runtime.Audio == null)
            {
                throw new InvalidOperationException(
                    "GapaAudioRuntime did not initialize. Ensure the GameObject is active and the " +
                    "component is enabled.");
            }
        }

        private static GapaAudioRuntime CreateRuntime(
            GapaAudioSettings settings,
            MusicTransitionTable musicTransitionTable,
            MusicState defaultMusicState,
            bool dontDestroyOnLoad)
        {
            var go = new GameObject(nameof(GapaAudioRuntime));
            go.SetActive(false);

            if (dontDestroyOnLoad)
                UnityEngine.Object.DontDestroyOnLoad(go);

            var runtime = go.AddComponent<GapaAudioRuntime>();
            ApplySerializedOverrides(runtime, settings, musicTransitionTable, defaultMusicState);
            SetSerializedField(runtime, "dontDestroyOnLoad", dontDestroyOnLoad);
            go.SetActive(true);
            return runtime;
        }

        private static void ApplySerializedOverrides(
            GapaAudioRuntime runtime,
            GapaAudioSettings settings,
            MusicTransitionTable musicTransitionTable,
            MusicState defaultMusicState)
        {
            if (runtime.Audio != null)
                return;

            if (settings != null && runtime.Settings == null)
                SetSerializedField(runtime, "settings", settings);

            if (musicTransitionTable != null)
                SetSerializedField(runtime, "musicTransitionTable", musicTransitionTable);

            if (defaultMusicState != null)
                SetSerializedField(runtime, "defaultMusicState", defaultMusicState);
        }

        private static void SetSerializedField<T>(GapaAudioRuntime runtime, string fieldName, T value)
        {
            var field = typeof(GapaAudioRuntime).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null || !field.FieldType.IsAssignableFrom(typeof(T)))
            {
                Debug.LogError(
                    $"[Gapa.Audio.VContainer] GapaAudioRuntime no longer has serialized field '{fieldName}'. " +
                    "Assign it on the component in the inspector instead.");
                return;
            }

            field.SetValue(runtime, value);
        }
    }
}
