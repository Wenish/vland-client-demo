using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyGame.Events;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.Zombie
{
    public sealed class ZombieRunEndService : IDisposable
    {
        public const float ServerOnlyAutoReturnDelaySeconds = 10f;

        private readonly IZombieRunEndHost host;
        private CancellationTokenSource autoReturnCts;

        public ZombieRunEndService(IZombieRunEndHost host)
        {
            this.host = host;
        }

        public void ResetRun()
        {
            CancelAutoReturn();
            host.ReturnToLobbyRequested = false;
            host.SetGameOver(false);
            host.SetAutoReturnEnabled(false);
            host.SetReturnCountdown(0f);
        }

        public void TryTriggerGameOverFromAllHumanPlayersDead()
        {
            if (host.IsGameOver || !AreAllHumanPlayersDead())
                return;

            HandleGameOver();
        }

        public void RequestReturnToLobby()
        {
            if (host.ReturnToLobbyRequested)
                return;

            host.PublishRunEnded(host.IsGameOver
                ? ZombieRunEndReason.ReturnToLobbyAfterGameOver
                : ZombieRunEndReason.HostEndedEarly);

            host.ReturnToLobbyRequested = true;
            CancelAutoReturn();
            host.SetAutoReturnEnabled(false);
            host.SetReturnCountdown(0f);
            host.ChangeToRoomScene();
        }

        public void Dispose()
        {
            CancelAutoReturn();
        }

        private void HandleGameOver()
        {
            if (host.IsGameOver)
                return;

            host.SetGameOver(true);
            host.StopWaves();
            host.PublishGameOver(true);
            host.PublishRunEnded(ZombieRunEndReason.AllPlayersDead);

            if (host.IsServerOnly && autoReturnCts == null)
            {
                autoReturnCts = new CancellationTokenSource();
                ReturnToLobbyAfterDelayAsync(ServerOnlyAutoReturnDelaySeconds, autoReturnCts.Token).Forget();
            }
        }

        private async UniTaskVoid ReturnToLobbyAfterDelayAsync(float delaySeconds, CancellationToken ct)
        {
            try
            {
                float endTime = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
                host.SetAutoReturnEnabled(true);

                while (host.IsServer && !host.ReturnToLobbyRequested && Time.unscaledTime < endTime)
                {
                    host.SetReturnCountdown(Mathf.Max(0f, endTime - Time.unscaledTime));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                host.SetReturnCountdown(0f);
                host.SetAutoReturnEnabled(false);

                if (host.IsServer && !host.ReturnToLobbyRequested)
                    RequestReturnToLobby();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (autoReturnCts != null)
                {
                    autoReturnCts.Dispose();
                    autoReturnCts = null;
                }
            }
        }

        private void CancelAutoReturn()
        {
            autoReturnCts?.Cancel();
        }

        private static bool AreAllHumanPlayersDead()
        {
            var playerUnits = GameServices.PlayerUnits;
            if (playerUnits == null)
                return false;

            bool hasAnyHuman = false;
            for (int i = 0; i < playerUnits.playerUnits.Count; i++)
            {
                var playerUnitEntry = playerUnits.playerUnits[i];
                if (playerUnitEntry.ConnectionId < 0 || playerUnitEntry.Unit == null)
                    continue;

                hasAnyHuman = true;
                var unitController = playerUnitEntry.Unit.GetComponent<UnitController>();
                if (unitController == null || !unitController.IsDead)
                    return false;
            }

            return hasAnyHuman;
        }
    }
}
