using ShadowInfection.UI.InventoryWindow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class InventoryWindowLifetimeScope : LifetimeScope
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
            var controller = GetComponent<InventoryWindowController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "InventoryWindowLifetimeScope requires InventoryWindowController on the same GameObject.");
                return;
            }

            builder.Register<InventoryWindowPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
