using ShadowInfection.UI.InGameMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class InGameMenuLifetimeScope : LifetimeScope
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
            var controller = GetComponent<InGameMenuController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "InGameMenuLifetimeScope requires InGameMenuController on the same GameObject.");
                return;
            }

            builder.Register<InGameMenuPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
