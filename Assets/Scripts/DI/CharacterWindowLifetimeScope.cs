using ShadowInfection.UI.CharacterWindow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class CharacterWindowLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            parentReference = ParentReference.Create<GameLifetimeScope>();
            parentReference.Object = LifetimeScopeParents.Game();
            base.Awake();
        }

        protected override LifetimeScope FindParent()
        {
            return LifetimeScopeParents.Game();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            var controller = GetComponent<CharacterWindowController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "CharacterWindowLifetimeScope requires CharacterWindowController on the same GameObject.");
                return;
            }

            builder.Register<CharacterWindowPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
