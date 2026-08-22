namespace ShadowInfection.UI.Session
{
    internal interface ISessionFlowCommands
    {
        bool TryEndMatch();
        void LeaveClient();
        void StopHost();
        void ExitGame();
    }
}
