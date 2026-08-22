using ShadowInfection.UI.HostAdmin;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class HostAdminLifetimeScope : LifetimeScope
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
            var controller = GetComponent<HostAdminOverlayController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "HostAdminLifetimeScope requires HostAdminOverlayController on the same GameObject.");
                return;
            }

            builder.Register<HostAdminOverlayPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
