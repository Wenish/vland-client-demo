using Gapa.Audio;
using Gapa.Audio.Sfx;
using UnityEngine;

namespace ShadowInfection.Audio
{
    public sealed class SfxPlayer : ISfxPlayer
    {
        public static class Ids
        {
            public const string UiButtonHover = "UiButtonHover";
            public const string UiButtonClick = "UiButtonClick";
        }

        private readonly IAudioService _audio;
        private readonly SfxCatalog _catalog;

        public SfxPlayer(IAudioService audio, SfxCatalog catalog)
        {
            _audio = audio;
            _catalog = catalog;
        }

        public bool Play(string id)
        {
            if (!TryResolve(id, out var definition))
                return false;
            _audio.Play(definition);
            return true;
        }

        public bool Play(string id, Vector3 position)
        {
            if (!TryResolve(id, out var definition))
                return false;
            _audio.Play(definition, position);
            return true;
        }

        public bool PlayAttached(string id, Transform target)
        {
            if (target == null || !TryResolve(id, out var definition))
                return false;
            _audio.PlayAttached(definition, target);
            return true;
        }

        public bool Play(SfxDefinition definition)
        {
            if (!IsPlayable(definition))
                return false;
            _audio.Play(definition);
            return true;
        }

        public bool Play(SfxDefinition definition, Vector3 position)
        {
            if (!IsPlayable(definition))
                return false;
            _audio.Play(definition, position);
            return true;
        }

        public bool PlayAttached(SfxDefinition definition, Transform target)
        {
            if (target == null || !IsPlayable(definition))
                return false;
            _audio.PlayAttached(definition, target);
            return true;
        }

        public bool TryGetId(SfxDefinition definition, out string id)
        {
            if (_catalog != null && _catalog.TryGetId(definition, out id))
                return true;

            if (definition != null)
            {
                id = definition.name;
                return !string.IsNullOrEmpty(id);
            }

            id = null;
            return false;
        }

        private bool TryResolve(string id, out SfxDefinition definition)
        {
            definition = null;
            if (_catalog == null || string.IsNullOrEmpty(id))
                return false;

            if (_catalog.TryGet(id, out definition))
                return true;

            UnityEngine.Debug.LogWarning($"[SfxPlayer] No SFX catalog entry for id '{id}'.");
            return false;
        }

        private static bool IsPlayable(SfxDefinition definition) =>
            definition != null && definition.Clips != null && definition.Clips.Length > 0;
    }
}
