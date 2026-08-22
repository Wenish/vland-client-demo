using ShadowInfection.UI.ZombieLeaderboard;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class ZombieLeaderboardLifetimeScope : LifetimeScope
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
            var controller = GetComponent<ZombieLeaderboardController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "ZombieLeaderboardLifetimeScope requires ZombieLeaderboardController on the same GameObject.");
                return;
            }

            builder.Register<ZombieLeaderboardPresenter>(Lifetime.Scoped);
            builder.RegisterComponent(controller);
        }
    }
}
