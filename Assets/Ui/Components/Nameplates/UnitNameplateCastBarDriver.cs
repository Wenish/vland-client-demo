using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirror;
using ShadowInfection.DI;
using UnityEngine;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class UnitNameplateCastBarDriver
    {
        private readonly IGameDatabases databases;
        private readonly Action<bool, Texture2D, float> applyCastBar;

        private UnitActionState actionState;
        private CancellationToken destroyToken;
        private CancellationTokenSource runCts;

        public UnitNameplateCastBarDriver(
            IGameDatabases databases,
            Action<bool, Texture2D, float> applyCastBar)
        {
            this.databases = databases;
            this.applyCastBar = applyCastBar;
        }

        public void Bind(UnitActionState nextState, CancellationToken token)
        {
            if (actionState != null)
                actionState.OnActionStateChanged -= HandleActionStateChanged;

            actionState = nextState;
            destroyToken = token;

            if (actionState != null)
            {
                actionState.OnActionStateChanged += HandleActionStateChanged;
                EvaluateCurrentState();
            }
        }

        public void EvaluateCurrentState()
        {
            if (actionState == null)
                return;

            HandleActionStateChanged(actionState);
        }

        public void Unbind()
        {
            CancelRun();
            if (actionState != null)
                actionState.OnActionStateChanged -= HandleActionStateChanged;
            actionState = null;
            applyCastBar(false, null, 0f);
        }

        private void HandleActionStateChanged(UnitActionState unitActionState)
        {
            if (actionState == null)
                return;

            var showChild = unitActionState.HasChild;
            var displayState = showChild ? unitActionState.childState : unitActionState.state;
            var isRelevant = displayState.type == UnitActionState.ActionType.Attacking
                || displayState.type == UnitActionState.ActionType.Casting
                || displayState.type == UnitActionState.ActionType.Channeling;
            if (!isRelevant)
            {
                CancelRun();
                applyCastBar(false, null, 0f);
                return;
            }

            CancelRun();
            runCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            RunCastBar(displayState, showChild, runCts.Token).Forget();
        }

        private async UniTaskVoid RunCastBar(
            UnitActionState.ActionStateData actionStateData,
            bool isChild,
            CancellationToken ct)
        {
            var icon = ResolveIcon(actionStateData);
            applyCastBar(true, icon, 0f);

            var startTime = actionStateData.startTime;
            var endTime = startTime + actionStateData.duration;
            var currentTime = NetworkTime.time;

            try
            {
                while (currentTime < endTime)
                {
                    ct.ThrowIfCancellationRequested();
                    if (actionState == null)
                        break;

                    var currentType = isChild ? actionState.childState.type : actionState.state.type;
                    if (currentType != actionStateData.type)
                        break;

                    var progress = actionStateData.type == UnitActionState.ActionType.Channeling
                        ? (float)((endTime - currentTime) / actionStateData.duration)
                        : (float)((currentTime - startTime) / actionStateData.duration);
                    applyCastBar(true, icon, progress);

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    currentTime = NetworkTime.time;
                }
            }
            catch (OperationCanceledException)
            {
            }

            applyCastBar(false, null, 0f);
        }

        private Texture2D ResolveIcon(UnitActionState.ActionStateData actionStateData)
        {
            switch (actionStateData.type)
            {
                case UnitActionState.ActionType.Attacking:
                    return databases?.Weapons?.GetWeaponByName(actionStateData.name)?.iconTexture;
                case UnitActionState.ActionType.Casting:
                case UnitActionState.ActionType.Channeling:
                    return databases?.Skills?.GetSkillByName(actionStateData.name)?.iconTexture;
                default:
                    return null;
            }
        }

        private void CancelRun()
        {
            if (runCts == null)
                return;

            runCts.Cancel();
            runCts.Dispose();
            runCts = null;
        }
    }
}
