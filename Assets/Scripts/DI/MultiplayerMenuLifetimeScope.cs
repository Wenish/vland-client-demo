using ShadowInfection.UI.MultiplayerMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class MultiplayerMenuLifetimeScope : LifetimeScope
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
            var controller = GetComponent<MultiplayerMenuController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "MultiplayerMenuLifetimeScope requires MultiplayerMenuController on the same GameObject.");
                return;
            }

            builder.RegisterComponent(controller);
        }
    }
}
