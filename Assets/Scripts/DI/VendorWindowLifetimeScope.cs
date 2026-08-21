using ShadowInfection.UI.VendorWindow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class VendorWindowLifetimeScope : LifetimeScope
    {
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
            var controller = GetComponent<VendorWindowController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError("VendorWindowLifetimeScope requires VendorWindowController on the same GameObject.");
                return;
            }

            builder.Register<VendorWindowPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
