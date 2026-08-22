namespace ShadowInfection.Match
{
    public sealed class AlwaysAllowMatchActivity : IMatchActivity
    {
        public static readonly AlwaysAllowMatchActivity Instance = new AlwaysAllowMatchActivity();

        private AlwaysAllowMatchActivity()
        {
        }

        public bool CanCombatantsAct => true;
    }

    public sealed class FixedUpgradeProgress : IUpgradeProgress
    {
        public static readonly FixedUpgradeProgress Default = new FixedUpgradeProgress(1);

        private readonly int unlockLevel;

        public FixedUpgradeProgress(int unlockLevel)
        {
            this.unlockLevel = unlockLevel < 1 ? 1 : unlockLevel;
        }

        public int UnlockLevel => unlockLevel;
    }

    public sealed class NoPvpObjectives : IPvpObjectives
    {
        public static readonly NoPvpObjectives Instance = new NoPvpObjectives();

        private NoPvpObjectives()
        {
        }

        public bool TryGetPriorityTarget(UnitController seeker, out UnitController target)
        {
            target = null;
            return false;
        }
    }

    public sealed class UnmatchedMatchUiSession : IMatchUiSession
    {
        public static readonly UnmatchedMatchUiSession Instance = new UnmatchedMatchUiSession();

        private UnmatchedMatchUiSession()
        {
        }

        public bool TryGetSnapshot(out MatchUiSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }
    }

    public sealed class UnmatchedCastleSiegeUiSession : ICastleSiegeUiSession
    {
        public static readonly UnmatchedCastleSiegeUiSession Instance = new UnmatchedCastleSiegeUiSession();

        private UnmatchedCastleSiegeUiSession()
        {
        }

        public bool TryGetSnapshot(out CastleSiegeUiSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }
    }

    public sealed class UnmatchedSkirmishUiSession : ISkirmishUiSession
    {
        public static readonly UnmatchedSkirmishUiSession Instance = new UnmatchedSkirmishUiSession();

        private UnmatchedSkirmishUiSession()
        {
        }

        public bool TryGetSnapshot(out SkirmishUiSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }
    }
}
