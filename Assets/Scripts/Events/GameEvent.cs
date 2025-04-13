#nullable enable

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

    public class UnitDiedEvent : GameEvent
    {
        public UnitController Unit { get; }
        public UnitController? Killer { get; }

        public UnitDiedEvent(UnitController unit, UnitController? killer = null)
        {
            Unit = unit;
            Killer = killer;
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

    public class UnitDroppedGoldEvent : GameEvent
    {
        public UnitController Unit { get; }
        public int GoldAmount { get; }

        public UnitController? Killer { get; }
        public UnitDroppedGoldEvent(UnitController unit, int goldAmount, UnitController? killer = null)
        {
            Unit = unit;
            GoldAmount = goldAmount;
            Killer = killer;
        }
    }

    public class PlayerReceivesGoldEvent : GameEvent
    {
        public UnitController Player { get; }
        public int GoldAmount { get; }

        public PlayerReceivesGoldEvent(UnitController player, int goldAmount)
        {
            Player = player;
            GoldAmount = goldAmount;
        }
    }

    public class PlayerGoldChangedEvent : GameEvent
    {
        public PlayerController Player { get; }
        public int OldGoldAmount { get; }
        public int NewGoldAmount { get; }

        public PlayerGoldChangedEvent(PlayerController player, int oldGoldAmount, int newGoldAmount)
        {
            Player = player;
            OldGoldAmount = oldGoldAmount;
            NewGoldAmount = newGoldAmount;
        }
    }

    public class UnitEnteredInteractionZone : GameEvent
    {
        public UnitController Unit { get; }
        public InteractionZone Zone { get; }

        public UnitEnteredInteractionZone(UnitController unit, InteractionZone zone)
        {
            Unit = unit;
            Zone = zone;
        }
    }
    
    public class UnitExitedInteractionZone : GameEvent
    {
        public UnitController Unit { get; }
        public InteractionZone Zone { get; }

        public UnitExitedInteractionZone(UnitController unit, InteractionZone zone)
        {
            Unit = unit;
            Zone = zone;
        }
    }

    public class OpenGateEvent : GameEvent
    {
        public int GateId { get; }
        public OpenGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }

    public class CloseGateEvent : GameEvent
    {
        public int GateId { get; }
        public CloseGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }

    public class OpenedGateEvent : GameEvent
    {
        public int GateId { get; }
        public OpenedGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }
    
    public class ClosedGateEvent : GameEvent
    {
        public int GateId { get; }
        public ClosedGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }
}
