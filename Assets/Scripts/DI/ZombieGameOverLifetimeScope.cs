using ShadowInfection.UI.ZombieGameOver;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class ZombieGameOverLifetimeScope : LifetimeScope
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
            var controller = GetComponent<ZombieGameOverController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "ZombieGameOverLifetimeScope requires ZombieGameOverController on the same GameObject.");
                return;
            }

            builder.Register<ZombieGameOverPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
