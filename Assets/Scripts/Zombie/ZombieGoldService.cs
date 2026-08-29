using System.Collections.Generic;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Zombie
{
    public sealed class ZombieGoldService
    {
        private readonly IZombieGoldHost host;
        private int deathsThisWave;

        public ZombieGoldService(IZombieGoldHost host)
        {
            this.host = host;
        }

        public void Reset()
        {
            deathsThisWave = 0;
        }

        public void OnWaveStarted()
        {
            deathsThisWave = 0;
        }

        public void OnPlayerDied()
        {
            deathsThisWave++;
        }

        public void OnZombieDied(UnitController zombie, UnitController killer)
        {
            var amount = GetGold().goldPerZombieKill;
            if (amount <= 0)
                return;

            host.BroadcastZombieDroppedGold(amount, zombie, killer);
            GrantToAlivePlayers(amount, zombie);
        }

        public void OnWaveCleared(int wave, bool isRecurringSpecial)
        {
            var gold = GetGold();
            int amount = gold.GetWavePayday(wave);
            if (deathsThisWave <= 0)
                amount += gold.GetSurvivalBonus(wave);
            if (isRecurringSpecial)
                amount += Mathf.Max(0, gold.specialWaveBounty);

            GrantToAlivePlayers(amount, goldDropUnit: null);
        }

        private ZombieModeConfig.GoldSettings GetGold()
        {
            var config = host.ModeConfig;
            if (config != null && config.gold != null)
                return config.gold;

            return new ZombieModeConfig.GoldSettings();
        }

        private void GrantToAlivePlayers(int amount, UnitController goldDropUnit)
        {
            if (amount <= 0)
                return;

            var alivePlayers = CollectAlivePlayers();
            for (int i = 0; i < alivePlayers.Count; i++)
                host.BroadcastPlayerReceivedGold(amount, alivePlayers[i], goldDropUnit);
        }

        private static List<UnitController> CollectAlivePlayers()
        {
            var result = new List<UnitController>();
            var playerUnits = GameServices.PlayerUnits;
            if (playerUnits == null)
                return result;

            for (int i = 0; i < playerUnits.playerUnits.Count; i++)
            {
                var entry = playerUnits.playerUnits[i];
                if (entry.ConnectionId < 0 || entry.Unit == null)
                    continue;

                var unit = entry.Unit.GetComponent<UnitController>();
                if (unit == null || unit.unitType != UnitType.Player || unit.IsDead)
                    continue;

                result.Add(unit);
            }

            return result;
        }
    }
}
