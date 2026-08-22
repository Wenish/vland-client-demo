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
