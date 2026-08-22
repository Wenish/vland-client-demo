using MyGame.Events;

namespace MyGame.Events.Ui
{
    /// <summary>
    /// Fired when the in-game UI needs to be updated.
    /// </summary>
    public class OpenMultiplayerMenuEvent : GameEvent
    {
        public OpenMultiplayerMenuEvent() { }
    }

    public class OpenFormJoinGameEvent : GameEvent
    {
        public OpenFormJoinGameEvent() { }
    }

    public class RequestCloseVendorWindowEvent : GameEvent
    {
        public RequestCloseVendorWindowEvent() { }
    }

    public class OpenVendorWindowEvent : GameEvent
    {
        public IVendorSession Session { get; }
        public PlayerController Player { get; }

        public OpenVendorWindowEvent(IVendorSession session, PlayerController player)
        {
            Session = session;
            Player = player;
        }
    }

    public class CloseVendorWindowIfInteractableEvent : GameEvent
    {
        public IVendorInteractable Interactable { get; }

        public CloseVendorWindowIfInteractableEvent(IVendorInteractable interactable)
        {
            Interactable = interactable;
        }
    }

    public class VendorWindowVisibilityChangedEvent : GameEvent
    {
        public bool IsOpen { get; }

        public VendorWindowVisibilityChangedEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    public class SetLoadoutWindowOpenEvent : GameEvent
    {
        public bool IsOpen { get; }

        public SetLoadoutWindowOpenEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    public class LoadoutChangedEvent : GameEvent
    {
        public LocalLoadout Loadout { get; }

        public LoadoutChangedEvent(LocalLoadout loadout)
        {
            Loadout = loadout;
        }
    }
}
