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
}