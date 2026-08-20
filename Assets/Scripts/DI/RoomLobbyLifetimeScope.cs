using ShadowInfection.UI.RoomLobby;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ShadowInfection.DI
{
    [DefaultExecutionOrder(-5000)]
    public sealed class RoomLobbyLifetimeScope : LifetimeScope
    {
        [SerializeField]
        [Min(0.05f)]
        private float refreshIntervalSeconds = 0.2f;

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
            builder.RegisterInstance(new RoomLobbySettings(refreshIntervalSeconds));
            builder.Register<IRoomLobbySession, MirrorRoomLobbySession>(Lifetime.Scoped);
            builder.Register<MirrorRoomLobbyPresenter>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<RoomLobbyController>();
        }
    }
}
