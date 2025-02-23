using UnityEngine;

namespace MyGame.Events
{
    /// <summary>
    /// Base class for all game events.
    /// </summary>
    public abstract class GameEvent { }

    /// <summary>
    /// Fired when a new player joins the game.
    /// </summary>
    public class PlayerJoinedEvent : GameEvent
    {
        public string PlayerName { get; }
        public int PlayerId { get; }

        public PlayerJoinedEvent(string playerName, int playerId)
        {
            PlayerName = playerName;
            PlayerId = playerId;
        }
    }

    /// <summary>
    /// Fired when a player dies.
    /// </summary>
    public class PlayerDiedEvent : GameEvent
    {
        public string PlayerName { get; }
        public int PlayerId { get; }
        public string DeathReason { get; }

        public PlayerDiedEvent(string playerName, int playerId, string deathReason)
        {
            PlayerName = playerName;
            PlayerId = playerId;
            DeathReason = deathReason;
        }
    }

    /// <summary>
    /// Fired when a new zombie wave starts.
    /// </summary>
    public class WaveStartedEvent : GameEvent
    {
        public int WaveNumber { get; }
        public int TotalZombies { get; }

        public WaveStartedEvent(int waveNumber, int totalZombies)
        {
            WaveNumber = waveNumber;
            TotalZombies = totalZombies;
        }
    }

    /// <summary>
    /// Fired when a zombie wave is successfully completed.
    /// </summary>
    public class WaveCompletedEvent : GameEvent
    {
        public int WaveNumber { get; }
        public int ZombiesKilled { get; }
        public bool IsSuccessful { get; }

        public WaveCompletedEvent(int waveNumber, int zombiesKilled, bool isSuccessful)
        {
            WaveNumber = waveNumber;
            ZombiesKilled = zombiesKilled;
            IsSuccessful = isSuccessful;
        }
    }

    /// <summary>
    /// Fired when a zombie is spawned into the arena.
    /// </summary>
    public class ZombieSpawnedEvent : GameEvent
    {
        public int ZombieId { get; }
        public string ZombieType { get; }
        public Vector3 SpawnPosition { get; }

        public ZombieSpawnedEvent(int zombieId, string zombieType, Vector3 spawnPosition)
        {
            ZombieId = zombieId;
            ZombieType = zombieType;
            SpawnPosition = spawnPosition;
        }
    }

    /// <summary>
    /// Fired when a zombie is killed by a player.
    /// </summary>
    public class ZombieKilledEvent : GameEvent
    {
        public int ZombieId { get; }
        public string ZombieType { get; }
        public string KillerPlayerName { get; }
        public int KillerPlayerId { get; }
        public int PointsAwarded { get; }

        public ZombieKilledEvent(int zombieId, string zombieType, string killerPlayerName, int killerPlayerId, int pointsAwarded)
        {
            ZombieId = zombieId;
            ZombieType = zombieType;
            KillerPlayerName = killerPlayerName;
            KillerPlayerId = killerPlayerId;
            PointsAwarded = pointsAwarded;
        }
    }
}
