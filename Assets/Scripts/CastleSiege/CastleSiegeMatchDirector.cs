using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.CastleSiege
{
    public sealed class CastleSiegeMatchDirector : IDisposable
    {
        private readonly ICastleSiegeHost host;
        private readonly CastleSiegePlayerService players;
        private readonly CastleSiegeObjectiveService objectives;
        private CancellationTokenSource loopCts;

        public CastleSiegeMatchDirector(
            ICastleSiegeHost host,
            CastleSiegePlayerService players,
            CastleSiegeObjectiveService objectives)
        {
            this.host = host;
            this.players = players;
            this.objectives = objectives;
        }

        public bool IsRunning => loopCts != null;

        public void Start()
        {
            if (loopCts != null)
                return;

            loopCts = new CancellationTokenSource();
            RunMatchLoopAsync(loopCts.Token).Forget();
        }

        public void Stop()
        {
            if (loopCts == null)
                return;

            loopCts.Cancel();
            loopCts.Dispose();
            loopCts = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private async UniTaskVoid RunMatchLoopAsync(CancellationToken ct)
        {
            try
            {
                host.SetPhase(CastleSiegeManager.MatchPhase.Setup);
                host.WinnerTeamId = -1;
                host.InGameStartServerTime = -1d;
                host.LordsSpawned = false;

                while (host.IsServer && host.CurrentPhase == CastleSiegeManager.MatchPhase.Setup && !ct.IsCancellationRequested)
                {
                    players.SyncPlayersAndTeams();
                    if (CanLeaveSetup())
                        break;

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                if (!host.IsServer)
                    return;

                host.SetPhase(CastleSiegeManager.MatchPhase.Warmup);
                await RunPhaseTimer(host.MapConfig.WarmupSeconds, ct);
                if (!host.IsServer)
                    return;

                host.SetPhase(CastleSiegeManager.MatchPhase.Countdown);
                await RunPhaseTimer(host.MapConfig.StartCountdownSeconds, ct);
                if (!host.IsServer)
                    return;

                TransitionToInGame();

                while (host.IsServer && host.CurrentPhase == CastleSiegeManager.MatchPhase.InGame && !ct.IsCancellationRequested)
                {
                    players.SyncPlayersAndTeams();
                    objectives.CheckVictory();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask RunPhaseTimer(float durationSeconds, CancellationToken ct)
        {
            float endTime = Time.time + Mathf.Max(0f, durationSeconds);
            while (host.IsServer && Time.time < endTime && !ct.IsCancellationRequested)
            {
                players.SyncPlayersAndTeams();
                host.PhaseRemainingSeconds = Mathf.Max(0f, endTime - Time.time);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            host.PhaseRemainingSeconds = 0f;
        }

        private void TransitionToInGame()
        {
            host.ServerStartMatchLifecycle();
            host.SetPhase(CastleSiegeManager.MatchPhase.InGame);
            host.InGameStartServerTime = Mirror.NetworkTime.time;
            host.PhaseRemainingSeconds = 0f;
            objectives.SpawnLordsOnce();
            players.SyncPlayersAndTeams();
        }

        private bool CanLeaveSetup()
        {
            if (!host.AutoStartWhenMinPlayersReached || GameServices.PlayerUnits == null)
                return false;

            int connectedPlayers = GameServices.PlayerUnits.playerUnits.Count(unit => unit.Unit != null);
            return connectedPlayers >= Mathf.Max(1, host.MinPlayersToStart);
        }
    }
}
