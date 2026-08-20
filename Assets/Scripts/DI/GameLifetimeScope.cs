using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5100)]
    public sealed class GameLifetimeScope : LifetimeScope
    {
        private static GameLifetimeScope instance;

        public static GameLifetimeScope FindOrCreate()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectsByType<GameLifetimeScope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
        }
    }
}
