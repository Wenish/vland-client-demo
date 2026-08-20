using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5100)]
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(container =>
                GlobalMessagePipe.SetProvider(container.AsServiceProvider()));
        }
    }
}
