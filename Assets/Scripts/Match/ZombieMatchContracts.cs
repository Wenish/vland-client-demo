using UnityEngine;

namespace ShadowInfection.Match
{
    public sealed class ZombieMatchActivity : IMatchActivity
    {
        private readonly ZombieGameManager manager;

        public ZombieMatchActivity(ZombieGameManager manager)
        {
            this.manager = manager;
        }

        public bool CanCombatantsAct =>
            manager != null && !manager.IsGameOver && !manager.IsGamePaused;
    }

    public sealed class ZombieUpgradeProgress : IUpgradeProgress
    {
        private readonly ZombieGameManager manager;

        public ZombieUpgradeProgress(ZombieGameManager manager)
        {
            this.manager = manager;
        }

        public int UnlockLevel => manager != null
            ? Mathf.Max(1, manager.CurrentWave)
            : 1;
    }
}
