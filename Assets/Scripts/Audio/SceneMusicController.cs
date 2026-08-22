using System;
using Gapa.Audio.Music;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace ShadowInfection.Audio
{
    /// <summary>
    /// Plays / transitions scene music through <see cref="IMusicService"/> using a
    /// <see cref="SceneMusicTable"/>. Registered as a VContainer entry point.
    /// </summary>
    public sealed class SceneMusicController : IStartable, IDisposable
    {
        private readonly IMusicService _music;
        private readonly SceneMusicTable _table;
        private MusicState _current;

        public SceneMusicController(IMusicService music, SceneMusicTable table)
        {
            _music = music;
            _table = table;
        }

        public void Start()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyForScene(SceneManager.GetActiveScene(), immediate: true);
        }

        public void Dispose()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            ApplyForScene(next, immediate: false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single || scene == SceneManager.GetActiveScene())
                ApplyForScene(scene, immediate: false);
        }

        private void ApplyForScene(Scene scene, bool immediate)
        {
            if (!scene.IsValid() || _table == null)
                return;

            ApplyForScene(scene.name, immediate);
        }

        private void ApplyForScene(string sceneName, bool immediate)
        {
            if (_table == null || string.IsNullOrEmpty(sceneName))
                return;

            var desired = _table.Resolve(sceneName);
            if (desired == null || desired == _current)
                return;

            if (immediate || _current == null)
                _music.Play(desired);
            else
                _music.TransitionTo(desired);

            _current = desired;
        }
    }
}
