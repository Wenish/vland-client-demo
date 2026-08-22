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
}
