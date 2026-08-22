using ShadowInfection.UI.FormJoinGame;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class FormJoinGameLifetimeScope : LifetimeScope
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
            var controller = GetComponent<FormJoinGameController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogError(
                    "FormJoinGameLifetimeScope requires FormJoinGameController on the same GameObject.");
                return;
            }

            builder.RegisterComponent(controller);
        }
    }
}
