using System;
using MessagePipe;
using MyGame.Events.Ui;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class LoadoutManagerStore : ILoadoutStore, IDisposable
    {
        private readonly IPublisher<LoadoutChangedEvent> loadoutChanged;
        private LoadoutManager boundManager;

        public LoadoutManagerStore(IPublisher<LoadoutChangedEvent> loadoutChanged)
        {
            this.loadoutChanged = loadoutChanged;
        }

        public LocalLoadout Get()
        {
            return ResolveManager()?.Get();
        }

        public void Set(LocalLoadout loadout)
        {
            ResolveManager()?.Set(loadout);
        }

        public void Dispose()
        {
            UnbindManager();
        }

        private LoadoutManager ResolveManager()
        {
            var manager = LoadoutManager.Instance;
            if (manager == null)
            {
                UnbindManager();
                return null;
            }

            if (boundManager == manager)
                return boundManager;

            UnbindManager();
            boundManager = manager;
            boundManager.OnLoadoutChanged += PublishChanged;
            return boundManager;
        }

        private void UnbindManager()
        {
            if (boundManager == null)
                return;

            boundManager.OnLoadoutChanged -= PublishChanged;
            boundManager = null;
        }

        private void PublishChanged(LocalLoadout loadout)
        {
            loadoutChanged.Publish(new LoadoutChangedEvent(loadout));
        }
    }
}
