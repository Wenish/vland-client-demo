using UnityEngine;

namespace ShadowInfection.Zombie
{
    public interface IZombieWaveRuntime
    {
        bool IsServer { get; }
        bool IsGameOver { get; }
        bool IsPaused { get; }
        ZombieModeConfig ModeConfig { get; }
        int CurrentWave { get; }
        int ZombiesAlive { get; }
        void SetCurrentWave(int wave);
        void SetWaveRunning(bool running);
        void SetQueuedSpawnCount(int count);
        void SetZombiesAlive(int count);
        void BeginWaveProgress(int total);
        void NotifySpawnFailure();
        void NotifyWaveStarted(int wave, int total);
        void NotifyWaveCompleted(int wave, bool isRecurringSpecial);
        bool TrySpawnZombie(string unitName, float healthMultiplier, float damageMultiplier, out uint netId);
        void DestroyZombie(GameObject zombie);
        int GetActivePlayerCount();
    }
}
