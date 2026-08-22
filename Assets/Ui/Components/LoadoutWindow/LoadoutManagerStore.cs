using System;
using MessagePipe;
using MyGame.Events.Ui;

namespace ShadowInfection.UI.LoadoutWindow
{
    internal sealed class LoadoutManagerStore : ILoadoutStore, IDisposable
    {
        private readonly IPublisher<LoadoutChangedEvent> loadoutChanged;
        private readonly LoadoutManager manager;

        public LoadoutManagerStore(LoadoutManager manager, IPublisher<LoadoutChangedEvent> loadoutChanged)
        {
            this.manager = manager;
            this.loadoutChanged = loadoutChanged;
            if (this.manager != null)
                this.manager.OnLoadoutChanged += PublishChanged;
        }

        public LocalLoadout Get()
        {
            return manager != null ? manager.Get() : null;
        }

        public void Set(LocalLoadout loadout)
        {
            manager?.Set(loadout);
        }

        public void Dispose()
        {
            if (manager != null)
                manager.OnLoadoutChanged -= PublishChanged;
        }

        private void PublishChanged(LocalLoadout loadout)
        {
            loadoutChanged.Publish(new LoadoutChangedEvent(loadout));
        }
    }
}
