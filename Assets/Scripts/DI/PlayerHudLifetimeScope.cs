using ShadowInfection.UI.PlayerHud;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class PlayerHudLifetimeScope : LifetimeScope
    {
        [SerializeField]
        [Min(0f)]
        private float goldTweenSeconds = 1f;

        [SerializeField]
        [Min(0f)]
        private float bannerFadeInSeconds = 1f;

        [SerializeField]
        [Min(0f)]
        private float bannerHoldSeconds = 1f;

        [SerializeField]
        [Min(0f)]
        private float bannerFadeOutSeconds = 3f;

        [SerializeField]
        [Min(0f)]
        private float castBarInterruptFadeSeconds = 0.3f;

        [SerializeField]
        [Min(0f)]
        private float castBarSuccessFadeSeconds = 1f;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<GameLifetimeScope>();
            parentReference.Object = GameLifetimeScope.FindOrCreate();
            base.Awake();
        }

        protected override LifetimeScope FindParent()
        {
            return GameLifetimeScope.FindOrCreate();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            var controller = GetComponent<PlayerHudController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError("PlayerHudLifetimeScope requires PlayerHudController on the same GameObject.");
                return;
            }

            builder.RegisterInstance(new PlayerHudSettings(
                goldTweenSeconds,
                bannerFadeInSeconds,
                bannerHoldSeconds,
                bannerFadeOutSeconds,
                castBarInterruptFadeSeconds,
                castBarSuccessFadeSeconds));
            builder.Register<PlayerHudPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
