using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class NameplateLayerLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private ShadowInfection.UI.Nameplates.NameplateLayerSettings settings;

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

            if (settings == null)
            {
                UnityEngine.Debug.LogError("NameplateLayerLifetimeScope requires NameplateLayerSettings.");
                return;
            }

            builder.RegisterInstance(settings);
            builder.Register<ShadowInfection.UI.Nameplates.NameplateLayerPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
