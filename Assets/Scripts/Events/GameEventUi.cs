using MyGame.Events;

namespace MyGame.Events.Ui
{
    /// <summary>
    /// Fired when the in-game UI needs to be updated.
    /// </summary>
    public class OpenMultiplayerMenu : GameEvent
    {
        public OpenMultiplayerMenu() { }
    }

    public class OpenFormJoinGame : GameEvent
    {
        public OpenFormJoinGame() { }
    }

    public class RequestCloseVendorWindowEvent : GameEvent
    {
        public RequestCloseVendorWindowEvent() { }
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
}
