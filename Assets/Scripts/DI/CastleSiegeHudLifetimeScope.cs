using ShadowInfection.UI.CastleSiegeHud;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class CastleSiegeHudLifetimeScope : LifetimeScope
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
            var controller = GetComponent<CastleSiegeHudController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "CastleSiegeHudLifetimeScope requires CastleSiegeHudController on the same GameObject.");
                return;
            }

            builder.Register<CastleSiegeHudPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
