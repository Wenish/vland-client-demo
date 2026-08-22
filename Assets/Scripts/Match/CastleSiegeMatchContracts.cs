namespace ShadowInfection.Match
{
    public sealed class CastleSiegeMatchActivity : IMatchActivity
    {
        private readonly CastleSiegeManager manager;

        public CastleSiegeMatchActivity(CastleSiegeManager manager)
        {
            this.manager = manager;
        }

        public bool CanCombatantsAct => manager != null && manager.IsInGame;
    }

    public sealed class CastleSiegePvpObjectives : IPvpObjectives
    {
        private readonly CastleSiegeManager manager;

        public CastleSiegePvpObjectives(CastleSiegeManager manager)
        {
            this.manager = manager;
        }

        public bool TryGetPriorityTarget(UnitController seeker, out UnitController target)
        {
            target = null;
            if (seeker == null || manager == null || !manager.IsInGame)
                return false;

            target = manager.ServerGetAliveEnemyLordForTeam(seeker.team, seeker.transform.position);
            return target != null;
        }
    }
}
