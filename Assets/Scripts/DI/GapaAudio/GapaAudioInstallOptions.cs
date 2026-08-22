using Gapa.Audio.Music;

namespace Gapa.Audio.VContainer
{
    /// <summary>
    /// Optional wiring for <see cref="GapaAudioContainerBuilderExtensions.RegisterGapaAudio"/>.
    /// Leave fields null to fall back to a scene <see cref="GapaAudioRuntime"/>, then a
    /// <see cref="GapaAudioSettings"/> assigned on the installer, then
    /// <c>Resources/Audio/GapaAudioSettings</c>.
    /// </summary>
    public sealed class GapaAudioInstallOptions
    {
        /// <summary>
        /// Existing composition root. When set, this instance is reused instead of creating one.
        /// </summary>
        public GapaAudioRuntime Runtime;

        /// <summary>
        /// Settings used when a runtime must be created, or applied to a not-yet-awoken runtime
        /// that has no settings assigned in the inspector.
        /// </summary>
        public GapaAudioSettings Settings;

        /// <summary>
        /// Optional per-pair music transition table forwarded to <see cref="GapaAudioRuntime"/>.
        /// </summary>
        public MusicTransitionTable MusicTransitionTable;

        /// <summary>
        /// Fallback music state used by <see cref="Gapa.Audio.Zones.MusicZoneCoordinator"/> when no
        /// music zone is active.
        /// </summary>
        public MusicState DefaultMusicState;

        /// <summary>
        /// When a runtime is created by the adapter, whether it survives scene loads.
        /// Matches <see cref="GapaAudioRuntime"/>'s default of true.
        /// </summary>
        public bool DontDestroyOnLoad = true;

        /// <summary>
        /// Registers <see cref="Gapa.Audio.Zones.MusicZoneCoordinator"/> and
        /// <see cref="Gapa.Audio.Zones.AmbienceZoneCoordinator"/> so gameplay can inject them
        /// instead of calling <c>zone.Initialize</c> from a scene lookup.
        /// </summary>
        public bool RegisterZoneCoordinators = true;

        /// <summary>
        /// Registers the runtime's <see cref="GapaAudioSettings"/> when it is non-null.
        /// </summary>
        public bool RegisterSettings = true;
    }
}
