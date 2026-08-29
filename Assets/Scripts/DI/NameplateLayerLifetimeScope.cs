using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class NameplateLayerLifetimeScope : LifetimeScope
    {
        [SerializeField]
        [Min(0.01f)]
        private float healthLerpSeconds = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float headWorldOffset = 0.15f;

        [SerializeField]
        [Min(0f)]
        private float screenOffsetPixels = 12f;

        [SerializeField]
        private Color localPlayerHealthColor = new Color(0f, 0.6509804f, 0.24313727f, 1f);

        protected override void Awake()
        {
            parentReference = ParentReference.Create<GameplayLifetimeScope>();
            parentReference.Object = LifetimeScopeParents.GameplayOrGame();
            base.Awake();
        }

        protected override LifetimeScope FindParent()
        {
            return LifetimeScopeParents.GameplayOrGame();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            var controller = GetComponent<ShadowInfection.UI.Nameplates.NameplateLayerController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError("NameplateLayerLifetimeScope requires NameplateLayerController.");
                return;
            }

            builder.RegisterInstance(new ShadowInfection.UI.Nameplates.NameplateLayerSettings(
                healthLerpSeconds,
                headWorldOffset,
                screenOffsetPixels,
                localPlayerHealthColor));
            builder.Register<ShadowInfection.UI.Nameplates.NameplateLayerPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
