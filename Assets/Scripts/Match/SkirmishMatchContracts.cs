namespace ShadowInfection.Match
{
    public sealed class SkirmishMatchActivity : IMatchActivity
    {
        private readonly SkirmishGameManager manager;

        public SkirmishMatchActivity(SkirmishGameManager manager)
        {
            this.manager = manager;
        }

        public bool CanCombatantsAct =>
            manager != null && manager.CurrentRoundState == SkirmishGameManager.RoundState.InRound;
    }
}
