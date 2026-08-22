#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Events
{
    /// <summary>
    /// Fired when a new zombie wave starts.
    /// </summary>
    public class WaveStartedEvent
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
    /// Fired when zombie wave kill progress changes.
    /// </summary>
    public class WaveProgressChangedEvent
    {
        public int WaveNumber { get; }
        public int KilledCount { get; }
        public int TotalCount { get; }
        public float PercentKilled { get; }

        public WaveProgressChangedEvent(int waveNumber, int killedCount, int totalCount, float percentKilled)
        {
            WaveNumber = waveNumber;
            KilledCount = killedCount;
            TotalCount = totalCount;
            PercentKilled = percentKilled;
        }
    }

    public class ZombieGameOverEvent
    {
        public bool IsGameOver { get; }

        public ZombieGameOverEvent(bool isGameOver)
        {
            IsGameOver = isGameOver;
        }
    }

    public class ZombieReturnToLobbyCountdownEvent
    {
        public bool IsAutoReturnActive { get; }
        public float CountdownSeconds { get; }

        public ZombieReturnToLobbyCountdownEvent(bool isAutoReturnActive, float countdownSeconds)
        {
            IsAutoReturnActive = isAutoReturnActive;
            CountdownSeconds = countdownSeconds;
        }
    }

    public readonly struct ZombieLeaderboardRow
    {
        public int ConnectionId { get; }
        public string PlayerName { get; }
        public int Points { get; }
        public int Kills { get; }
        public int Deaths { get; }
        public int GoldGathered { get; }
        public bool IsConnected { get; }

        public ZombieLeaderboardRow(
            int connectionId,
            string playerName,
            int points,
            int kills,
            int deaths,
            int goldGathered,
            bool isConnected)
        {
            ConnectionId = connectionId;
            PlayerName = playerName ?? string.Empty;
            Points = points;
            Kills = kills;
            Deaths = deaths;
            GoldGathered = goldGathered;
            IsConnected = isConnected;
        }
    }

    public class ZombieLeaderboardChangedEvent
    {
        public IReadOnlyList<ZombieLeaderboardRow> Entries { get; }

        public ZombieLeaderboardChangedEvent(IReadOnlyList<ZombieLeaderboardRow> entries)
        {
            Entries = entries ?? Array.Empty<ZombieLeaderboardRow>();
        }
    }

    public enum ZombieRunEndReason
    {
        AllPlayersDead = 0,
        HostEndedEarly = 1,
        ReturnToLobbyAfterGameOver = 2
    }

    public class ZombieRunEndedEvent
    {
        public ZombieRunEndReason EndReason { get; }

        public ZombieRunEndedEvent(ZombieRunEndReason endReason)
        {
            EndReason = endReason;
        }
    }

    /// <summary>
    /// Fired when a unit receives damage.
    /// </summary>
    public class UnitDamagedEvent
    {
        public UnitController Unit { get; }
        public UnitController Attacker { get; }
        public int DamageAmount { get; }
        public int AppliedDamageAmount { get; }
        public bool WasCritical { get; }

        public UnitDamagedEvent(UnitController unit, UnitController attacker, int damageAmount, int appliedDamageAmount, bool wasCritical = false)
        {
            Unit = unit;
            Attacker = attacker;
            DamageAmount = damageAmount;
            AppliedDamageAmount = appliedDamageAmount;
            WasCritical = wasCritical;
        }
    }

    public class UnitHealedEvent
    {
        public UnitController Unit { get; }
        public int HealAmount { get; }
        public int OldHealth { get; }
        public int NewHealth { get; }
        public UnitController? Healer { get; }


        public UnitHealedEvent(UnitController unit, int healAmount, int oldHealth, int newHealth, UnitController healer)
        {
            Unit = unit;
            HealAmount = healAmount;
            OldHealth = oldHealth;
            NewHealth = newHealth;
            Healer = healer;
        }
    }

    public class UnitShieldedEvent
    {
        public UnitController Unit { get; }
        public int ShieldAmount { get; }
        public int OldShield { get; }
        public int NewShield { get; }
        public UnitController? Shielder { get; }

        public UnitShieldedEvent(UnitController unit, int shieldAmount, int oldShield, int newShield, UnitController? shielder = null)
        {
            Unit = unit;
            ShieldAmount = shieldAmount;
            OldShield = oldShield;
            NewShield = newShield;
            Shielder = shielder;
        }
    }

    public class UnitDiedEvent
    {
        public UnitController Unit { get; }
        public UnitController? Killer { get; }

        public UnitDiedEvent(UnitController unit, UnitController? killer = null)
        {
            Unit = unit;
            Killer = killer;
        }
    }

    public class PlayerUnitSpawnedEvent
    {
        public int ConnectionId { get; }
        public GameObject Unit { get; }

        public PlayerUnitSpawnedEvent(int connectionId, GameObject unit)
        {
            ConnectionId = connectionId;
            Unit = unit;
        }
    }

    public class MyPlayerUnitSpawnedEvent
    {
        public UnitController PlayerCharacter { get; }

        public MyPlayerUnitSpawnedEvent(UnitController playerCharacter)
        {
            PlayerCharacter = playerCharacter;
        }
    }

    public class UnitDroppedGoldEvent
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

    public class PlayerReceivesGoldEvent
    {
        public UnitController Player { get; }
        public int GoldAmount { get; }

        public PlayerReceivesGoldEvent(UnitController player, int goldAmount)
        {
            Player = player;
            GoldAmount = goldAmount;
        }
    }

    public class PlayerGoldChangedEvent
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

    public enum PlayerHudInfoKind
    {
        Info = 0,
        Error = 1
    }

    /// <summary>
    /// Local-player HUD info line. Gameplay publishes this; the HUD displays it.
    /// DurationSeconds of 0 or less uses the HUD default.
    /// </summary>
    public class PlayerHudInfoMessageEvent
    {
        public string Text { get; }
        public string Key { get; }
        public float DurationSeconds { get; }
        public PlayerHudInfoKind Kind { get; }

        public PlayerHudInfoMessageEvent(
            string text,
            string key = "",
            float durationSeconds = 0f,
            PlayerHudInfoKind kind = PlayerHudInfoKind.Info)
        {
            Text = text ?? string.Empty;
            Key = string.IsNullOrWhiteSpace(key) ? Text : key;
            DurationSeconds = durationSeconds;
            Kind = kind;
        }
    }

    public class UnitEnteredInteractionZone
    {
        public UnitController Unit { get; }
        public InteractionZone Zone { get; }

        public UnitEnteredInteractionZone(UnitController unit, InteractionZone zone)
        {
            Unit = unit;
            Zone = zone;
        }
    }
    
    public class UnitExitedInteractionZone
    {
        public UnitController Unit { get; }
        public InteractionZone Zone { get; }

        public UnitExitedInteractionZone(UnitController unit, InteractionZone zone)
        {
            Unit = unit;
            Zone = zone;
        }
    }

    public class OpenGateEvent
    {
        public int GateId { get; }
        public OpenGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }

    public class CloseGateEvent
    {
        public int GateId { get; }
        public CloseGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }

    public class OpenedGateEvent
    {
        public int GateId { get; }
        public OpenedGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }
    
    public class ClosedGateEvent
    {
        public int GateId { get; }
        public ClosedGateEvent(int gateId)
        {
            GateId = gateId;
        }
    }

    public class BuyUpgradeEvent
    {
        public InteractionZone Zone { get; }
        public PlayerController Buyer { get; }
        public string UpgradeId { get; }

        public BuyUpgradeEvent(InteractionZone zone, PlayerController buyer, string upgradeId = "")
        {
            Zone = zone;
            Buyer = buyer;
            UpgradeId = upgradeId;
        }
    }

    public class UpgradePurchaseResultEvent
    {
        public PlayerController Buyer { get; }
        public bool Success { get; }
        public string Message { get; }
        public string UpgradeId { get; }
        public int CostPaid { get; }

        public UpgradePurchaseResultEvent(PlayerController buyer, bool success, string message, string upgradeId, int costPaid)
        {
            Buyer = buyer;
            Success = success;
            Message = message;
            UpgradeId = upgradeId;
            CostPaid = costPaid;
        }
    }

    public class VendorTransactResultEvent
    {
        public PlayerController Buyer { get; }
        public bool Success { get; }
        public string Message { get; }
        public string EntryId { get; }
        public int TimesBought { get; }

        public VendorTransactResultEvent(PlayerController buyer, bool success, string message, string entryId, int timesBought)
        {
            Buyer = buyer;
            Success = success;
            Message = message ?? string.Empty;
            EntryId = entryId ?? string.Empty;
            TimesBought = timesBought;
        }
    }

    public class VendorSnapshotEvent
    {
        public PlayerController Buyer { get; }
        public string[] UpgradeIds { get; }
        public int[] PurchaseCounts { get; }
        public string[] BuyIds { get; }
        public int[] BuyStocks { get; }
        public int VendorGold { get; }

        public VendorSnapshotEvent(
            PlayerController buyer,
            string[] upgradeIds,
            int[] purchaseCounts,
            string[]? buyIds = null,
            int[]? buyStocks = null,
            int vendorGold = -1)
        {
            Buyer = buyer;
            UpgradeIds = upgradeIds ?? System.Array.Empty<string>();
            PurchaseCounts = purchaseCounts ?? System.Array.Empty<int>();
            BuyIds = buyIds ?? System.Array.Empty<string>();
            BuyStocks = buyStocks ?? System.Array.Empty<int>();
            VendorGold = vendorGold;
        }
    }

    public class WorldPingEvent
    {
        public Vector3 Position { get; }

        public WorldPingEvent(Vector3 position)
        {
            Position = position;
        }
    }

    public class ObjectiveDestroyedEvent
    {
        public UnitController ObjectiveUnit { get; }
        public string ObjectiveId { get; }
        public UnitController? Killer { get; }
        public int RewardTeamId { get; }

        public ObjectiveDestroyedEvent(UnitController objectiveUnit, string objectiveId, UnitController? killer, int rewardTeamId)
        {
            ObjectiveUnit = objectiveUnit;
            ObjectiveId = objectiveId;
            Killer = killer;
            RewardTeamId = rewardTeamId;
        }
    }

    public class ObjectiveRebuiltEvent
    {
        public UnitController ObjectiveUnit { get; }
        public string ObjectiveId { get; }

        public ObjectiveRebuiltEvent(UnitController objectiveUnit, string objectiveId)
        {
            ObjectiveUnit = objectiveUnit;
            ObjectiveId = objectiveId;
        }
    }

    public class MatchLifecycleStateChangedEvent
    {
        public MatchGameManagerBase.MatchLifecycleState State { get; }

        public MatchLifecycleStateChangedEvent(MatchGameManagerBase.MatchLifecycleState state)
        {
            State = state;
        }
    }

    public class MatchTeamSelectionLockChangedEvent
    {
        public bool IsLocked { get; }

        public MatchTeamSelectionLockChangedEvent(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }

    public class MatchEndedEvent
    {
        public int WinnerTeamId { get; }

        public MatchEndedEvent(int winnerTeamId)
        {
            WinnerTeamId = winnerTeamId;
        }
    }

    public class ReturnToLobbyCountdownEvent
    {
        public float RemainingSeconds { get; }

        public ReturnToLobbyCountdownEvent(float remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }

    public class MatchPlayerTeamAssignedEvent
    {
        public int ConnectionId { get; }
        public int TeamId { get; }

        public MatchPlayerTeamAssignedEvent(int connectionId, int teamId)
        {
            ConnectionId = connectionId;
            TeamId = teamId;
        }
    }

    public class CastleSiegePhaseChangedEvent
    {
        public CastleSiegeManager.MatchPhase Phase { get; }

        public CastleSiegePhaseChangedEvent(CastleSiegeManager.MatchPhase phase)
        {
            Phase = phase;
        }
    }

    public class CastleSiegePlayerJoinedEvent
    {
    }

    public class CastleSiegePlayerLeftEvent
    {
    }

    public class CastleSiegeUnitDiedEvent
    {
        public UnitController Unit { get; }

        public CastleSiegeUnitDiedEvent(UnitController unit)
        {
            Unit = unit;
        }
    }

    public class CastleSiegeLordSpawnedEvent
    {
        public int TeamId { get; }

        public CastleSiegeLordSpawnedEvent(int teamId)
        {
            TeamId = teamId;
        }
    }

    public class CastleSiegeTeamEliminatedEvent
    {
        public int TeamId { get; }

        public CastleSiegeTeamEliminatedEvent(int teamId)
        {
            TeamId = teamId;
        }
    }

    public class CastleSiegeMatchWinnerEvent
    {
        public int WinnerTeamId { get; }

        public CastleSiegeMatchWinnerEvent(int winnerTeamId)
        {
            WinnerTeamId = winnerTeamId;
        }
    }

    public class SkirmishRoundChangedEvent
    {
        public int Round { get; }

        public SkirmishRoundChangedEvent(int round)
        {
            Round = round;
        }
    }

    public class SkirmishRoundStateChangedEvent
    {
        public SkirmishGameManager.RoundState State { get; }

        public SkirmishRoundStateChangedEvent(SkirmishGameManager.RoundState state)
        {
            State = state;
        }
    }

    public class SkirmishCountdownChangedEvent
    {
        public float RemainingSeconds { get; }

        public SkirmishCountdownChangedEvent(float remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }

    public class SkirmishRoundEndedEvent
    {
        public int WinnerTeam { get; }
        public bool IsDraw { get; }

        public SkirmishRoundEndedEvent(int winnerTeam, bool isDraw)
        {
            WinnerTeam = winnerTeam;
            IsDraw = isDraw;
        }
    }

    public class SkirmishMatchEndedEvent
    {
        public int WinnerTeamId { get; }

        public SkirmishMatchEndedEvent(int winnerTeamId)
        {
            WinnerTeamId = winnerTeamId;
        }
    }
}
