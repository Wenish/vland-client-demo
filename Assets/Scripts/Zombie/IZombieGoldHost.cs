namespace ShadowInfection.Zombie
{
    public interface IZombieGoldHost
    {
        ZombieModeConfig ModeConfig { get; }
        void BroadcastZombieDroppedGold(int amount, UnitController zombie, UnitController killer);
        void BroadcastPlayerReceivedGold(int amount, UnitController player, UnitController goldDropUnit);
    }
}
