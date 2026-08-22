using ShadowInfection.UI.LoadoutWindow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class LoadoutWindowLifetimeScope : LifetimeScope
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
            var controller = GetComponent<LoadoutWindowController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "LoadoutWindowLifetimeScope requires LoadoutWindowController on the same GameObject.");
                return;
            }

            builder.Register<LoadoutWindowPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
