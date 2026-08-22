using Gapa.Audio.Sfx;
using UnityEngine;

namespace ShadowInfection.Audio
{
    /// <summary>
    /// Game-facing SFX facade: catalog lookup + Gapa playback in one place.
    /// Prefer this over resolving <c>IAudioService</c> / <c>SfxCatalog</c> separately.
    /// </summary>
    public interface ISfxPlayer
    {
        bool Play(string id);
        bool Play(string id, Vector3 position);
        bool PlayAttached(string id, Transform target);

        bool Play(SfxDefinition definition);
        bool Play(SfxDefinition definition, Vector3 position);
        bool PlayAttached(SfxDefinition definition, Transform target);

        /// <summary>
        /// Catalog id for networking. Falls back to <see cref="Object.name"/> if uncatalogued.
        /// </summary>
        bool TryGetId(SfxDefinition definition, out string id);
    }
}
