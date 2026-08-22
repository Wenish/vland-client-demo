using System;
using Gapa.Audio.Music;
using UnityEngine;

namespace ShadowInfection.Audio
{
    /// <summary>
    /// Maps Unity scene names to Gapa <see cref="MusicState"/>s for automatic scene music.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Audio/Scene Music Table", fileName = "SceneMusicTable")]
    public sealed class SceneMusicTable : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string sceneName;
            public MusicState musicState;
        }

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        [SerializeField]
        private MusicState fallbackState;

        public MusicState Resolve(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return fallbackState;

            // Mirror often stores full paths ("Assets/Scenes/Foo.unity"); Unity scene.name is short.
            var shortName = sceneName;
            if (sceneName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var slash = Math.Max(sceneName.LastIndexOf('/'), sceneName.LastIndexOf('\\'));
                shortName = slash >= 0
                    ? sceneName.Substring(slash + 1, sceneName.Length - slash - 1 - ".unity".Length)
                    : sceneName.Substring(0, sceneName.Length - ".unity".Length);
            }

            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry == null || entry.musicState == null || string.IsNullOrEmpty(entry.sceneName))
                        continue;

                    if (string.Equals(entry.sceneName, sceneName, StringComparison.Ordinal)
                        || string.Equals(entry.sceneName, shortName, StringComparison.Ordinal))
                    {
                        return entry.musicState;
                    }
                }
            }

            return fallbackState;
        }
    }
}
