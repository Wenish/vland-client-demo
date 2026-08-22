using ShadowInfection.UI.SkirmishHud;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class SkirmishHudLifetimeScope : LifetimeScope
    {
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
            var controller = GetComponent<SkirmishHudController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "SkirmishHudLifetimeScope requires SkirmishHudController on the same GameObject.");
                return;
            }

            builder.Register<SkirmishHudPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
