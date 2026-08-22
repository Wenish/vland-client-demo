namespace MyGame.Events.Ui
{
    /// <summary>
    /// Fired when the in-game UI needs to be updated.
    /// </summary>
    public class OpenMultiplayerMenuEvent
    {
        public OpenMultiplayerMenuEvent() { }
    }

    public class OpenFormJoinGameEvent
    {
        public OpenFormJoinGameEvent() { }
    }

    public class RequestCloseVendorWindowEvent
    {
        public RequestCloseVendorWindowEvent() { }
    }

    public class OpenVendorWindowEvent
    {
        public IVendorSession Session { get; }
        public PlayerController Player { get; }

        public OpenVendorWindowEvent(IVendorSession session, PlayerController player)
        {
            Session = session;
            Player = player;
        }
    }

    public class CloseVendorWindowIfInteractableEvent
    {
        public IVendorInteractable Interactable { get; }

        public CloseVendorWindowIfInteractableEvent(IVendorInteractable interactable)
        {
            Interactable = interactable;
        }
    }

    public class VendorWindowVisibilityChangedEvent
    {
        public bool IsOpen { get; }

        public VendorWindowVisibilityChangedEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    public class SetLoadoutWindowOpenEvent
    {
        public bool IsOpen { get; }

        public SetLoadoutWindowOpenEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    public class LoadoutChangedEvent
    {
        public LocalLoadout Loadout { get; }

        public LoadoutChangedEvent(LocalLoadout loadout)
        {
            Loadout = loadout;
        }
    }
}
