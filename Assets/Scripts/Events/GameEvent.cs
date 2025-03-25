using UnityEngine;

namespace MyGame.Events
{
    /// <summary>
    /// Base class for all game events.
    /// </summary>
    public abstract class GameEvent { }

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
    /// Fired when a unit receives damage.
    /// </summary>
    public class UnitDamagedEvent : GameEvent
    {
        public UnitController Unit { get; }
        public UnitController Attacker { get; }
        public int DamageAmount { get; }

        public UnitDamagedEvent(UnitController unit, UnitController attacker, int damageAmount)
        {
            Unit = unit;
            Attacker = attacker;
            DamageAmount = damageAmount;
        }
    }

    public class UnitHealedEvent : GameEvent
    {
        public UnitController Unit { get; }
        public int HealAmount { get; }

        public UnitHealedEvent(UnitController unit, int healAmount)
        {
            Unit = unit;
            HealAmount = healAmount;
        }
    }

    public class UnitShieldedEvent : GameEvent
    {
        public UnitController Unit { get; }
        public int ShieldAmount { get; }

        public UnitShieldedEvent(UnitController unit, int shieldAmount)
        {
            Unit = unit;
            ShieldAmount = shieldAmount;
        }
    }

    public class MyPlayerUnitSpawnedEvent : GameEvent
    {
        public UnitController PlayerCharacter { get; }

        public MyPlayerUnitSpawnedEvent(UnitController playerCharacter)
        {
            PlayerCharacter = playerCharacter;
        }
    }
}
