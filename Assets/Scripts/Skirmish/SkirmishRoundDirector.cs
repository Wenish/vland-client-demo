using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Skirmish
{
    public sealed class SkirmishRoundDirector : IDisposable
    {
        private readonly ISkirmishHost host;
        private readonly SkirmishPlayerService players;
        private readonly SkirmishScoreService scores;
        private CancellationTokenSource loopCts;

        public SkirmishRoundDirector(
            ISkirmishHost host,
            SkirmishPlayerService players,
            SkirmishScoreService scores)
        {
            this.host = host;
            this.players = players;
            this.scores = scores;
        }

        public bool IsRunning => loopCts != null;

        public void Start()
        {
            if (loopCts != null)
                return;

            loopCts = new CancellationTokenSource();
            RunRoundLoopAsync(loopCts.Token).Forget();
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

        private async UniTaskVoid RunRoundLoopAsync(CancellationToken ct)
        {
            try
            {
                await WaitForFirstPlayerUnit(ct);
                if (!host.IsServer)
                    return;

                host.ServerStartMatchLifecycle();

                while (host.IsServer && !host.MatchEnded && !ct.IsCancellationRequested)
                {
                    host.SetCurrentRound(host.CurrentRound + 1);
                    players.AssignTeamsToNewPlayers();
                    players.ReviveAndTeleportAllPlayersToTeamSpawns();

                    host.SetRoundState(SkirmishGameManager.RoundState.PreRoundCountdown);
                    await RunCountdown(host.PreRoundCountdownSeconds, ct);

                    host.SetRoundState(SkirmishGameManager.RoundState.InRound);

                    bool roundResolved = false;
                    while (host.IsServer && !host.MatchEnded && !roundResolved && !ct.IsCancellationRequested)
                    {
                        players.AssignTeamsToNewPlayers();
                        var roundOutcome = scores.GetRoundOutcome();
                        if (roundOutcome.HasValue)
                        {
                            var (winnerTeam, isDraw) = roundOutcome.Value;
                            scores.HandleRoundEnd(winnerTeam, isDraw);
                            roundResolved = true;
                            if (host.MatchEnded)
                                return;
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }

                    host.SetRoundState(SkirmishGameManager.RoundState.PostRoundDelay);
                    await RunCountdown(host.PostRoundDelaySeconds, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask WaitForFirstPlayerUnit(CancellationToken ct)
        {
            while (host.IsServer && !ct.IsCancellationRequested)
            {
                if (GameServices.PlayerUnits != null
                    && GameServices.PlayerUnits.playerUnits.Any(playerUnit => playerUnit.Unit != null))
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private async UniTask RunCountdown(float durationSeconds, CancellationToken ct)
        {
            float endTime = Time.time + Mathf.Max(0f, durationSeconds);
            while (host.IsServer && Time.time < endTime && !ct.IsCancellationRequested)
            {
                host.SetCountdownRemaining(Mathf.Max(0f, endTime - Time.time));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            host.SetCountdownRemaining(0f);
        }
    }
}
