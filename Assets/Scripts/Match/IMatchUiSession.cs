namespace ShadowInfection.Match
{
    public readonly struct MatchUiSnapshot
    {
        public readonly string ManagerTypeName;
        public readonly bool TeamSelectionLocked;
        public readonly int TeamCount;
        public readonly MatchGameManagerBase.MatchLifecycleState LifecycleState;
        public readonly float ReturnToLobbyCountdownRemaining;
        public readonly int WinnerTeamId;

        public MatchUiSnapshot(
            string managerTypeName,
            bool teamSelectionLocked,
            int teamCount,
            MatchGameManagerBase.MatchLifecycleState lifecycleState,
            float returnToLobbyCountdownRemaining,
            int winnerTeamId)
        {
            ManagerTypeName = managerTypeName ?? string.Empty;
            TeamSelectionLocked = teamSelectionLocked;
            TeamCount = teamCount;
            LifecycleState = lifecycleState;
            ReturnToLobbyCountdownRemaining = returnToLobbyCountdownRemaining;
            WinnerTeamId = winnerTeamId;
        }

        public string Signature =>
            $"{ManagerTypeName}|{TeamSelectionLocked}|{TeamCount}|{(int)LifecycleState}|{ReturnToLobbyCountdownRemaining:F1}|{WinnerTeamId}";
    }

    public interface IMatchUiSession
    {
        bool TryGetSnapshot(out MatchUiSnapshot snapshot);
    }
}
