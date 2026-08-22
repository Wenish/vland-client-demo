using System;
using System.Collections.Generic;
using Gapa.Audio.Sfx;
using UnityEngine;

namespace ShadowInfection.Audio
{
    /// <summary>
    /// Maps stable string ids (legacy sound names / asset names) to Gapa <see cref="SfxDefinition"/>s
    /// for network RPCs and UI Toolkit call sites that cannot hold direct asset references.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Audio/SFX Catalog", fileName = "SfxCatalog")]
    public sealed class SfxCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public SfxDefinition definition;
        }

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<string, SfxDefinition> _lookup;
        private Dictionary<SfxDefinition, string> _idsByDefinition;

        public SfxDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            EnsureLookup();
            return _lookup.TryGetValue(id, out var definition) ? definition : null;
        }

        public bool TryGet(string id, out SfxDefinition definition)
        {
            definition = Get(id);
            return definition != null;
        }

        public bool TryGetId(SfxDefinition definition, out string id)
        {
            id = null;
            if (definition == null)
                return false;

            EnsureLookup();
            return _idsByDefinition.TryGetValue(definition, out id);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, SfxDefinition>(StringComparer.Ordinal);
            _idsByDefinition = new Dictionary<SfxDefinition, string>();
            if (entries == null)
                return;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.definition == null || string.IsNullOrEmpty(entry.id))
                    continue;

                _lookup[entry.id] = entry.definition;
                _idsByDefinition[entry.definition] = entry.id;
            }
        }

        private void OnEnable()
        {
            _lookup = null;
            _idsByDefinition = null;
        }
    }
}
