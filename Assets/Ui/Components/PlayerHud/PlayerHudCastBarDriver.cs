using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Text;
using LitMotion;
using Mirror;
using UnityEngine;

namespace ShadowInfection.UI.PlayerHud
{
    internal sealed class PlayerHudCastBarDriver
    {
        private readonly PlayerHudSettings settings;
        private readonly Func<string, Texture2D> resolveIcon;

        private PlayerHudView view;
        private Color castSuccessColor;
        private CancellationToken destroyToken;
        private CancellationTokenSource runCts;
        private MotionHandle fadeHandle;
        private UnitActionState actionState;

        public PlayerHudCastBarDriver(PlayerHudSettings settings, Func<string, Texture2D> resolveIcon)
        {
            this.settings = settings;
            this.resolveIcon = resolveIcon;
        }

        public void Bind(PlayerHudView nextView, Color successColor, CancellationToken token)
        {
            CancelRun();
            view = nextView;
            castSuccessColor = successColor;
            destroyToken = token;
        }

        public void SetActionState(UnitActionState nextState)
        {
            if (actionState != null)
                actionState.OnActionStateChanged -= HandleActionStateChanged;

            actionState = nextState;

            if (actionState != null)
                actionState.OnActionStateChanged += HandleActionStateChanged;
        }

        public void HandleInterrupted(UnitController unit, UnitController expected)
        {
            if (unit != expected)
                return;

            CancelRun();
            FadeOut(settings.CastBarInterruptFadeSeconds).Forget();
        }

        public void Unbind()
        {
            CancelRun();
            fadeHandle.TryCancel();
            SetActionState(null);
            view = null;
        }

        private void HandleActionStateChanged(UnitActionState unitActionState)
        {
            if (actionState == null || view == null)
                return;

            var showChild = unitActionState.HasChild;
            var displayState = showChild ? unitActionState.childState : unitActionState.state;
            var isCasting = displayState.type == UnitActionState.ActionType.Casting;
            var isChanneling = displayState.type == UnitActionState.ActionType.Channeling;
            if (!isCasting && !isChanneling)
                return;

            CancelRun();
            runCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            RunCastBar(displayState, showChild, runCts.Token).Forget();
        }

        private async UniTaskVoid RunCastBar(
            UnitActionState.ActionStateData actionStateData,
            bool isChild,
            CancellationToken ct)
        {
            var hud = view;
            if (hud == null)
                return;

            fadeHandle.TryCancel();
            hud.SetCastBarOpacity(1f);
            hud.SetCastBarProgress(0f);
            hud.SetCastBarIcon(resolveIcon(actionStateData.name));
            hud.ResetCastBar();
            hud.ShowCastBar();
            hud.SetCastBarName(actionStateData.name);

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

                    if (actionStateData.type == UnitActionState.ActionType.Channeling)
                        hud.SetCastBarProgress((float)((endTime - currentTime) / actionStateData.duration));
                    else
                        hud.SetCastBarProgress((float)((currentTime - startTime) / actionStateData.duration));

                    hud.SetCastBarTime(ZString.Format("{0:0.0}s", endTime - currentTime));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    currentTime = NetworkTime.time;
                }

                if (ct.IsCancellationRequested || view == null)
                    return;

                hud.SetCastBarProgress(actionStateData.type == UnitActionState.ActionType.Channeling ? 0f : 1f);
                ShowSuccess();
                await FadeOut(settings.CastBarSuccessFadeSeconds, ct);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ShowSuccess()
        {
            if (view == null)
                return;

            view.SetCastBarTime(string.Empty);
            view.SetCastBarFeedback(castSuccessColor, true);
        }

        private async UniTaskVoid FadeOut(float duration)
        {
            try
            {
                await FadeOut(duration, destroyToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask FadeOut(float duration, CancellationToken ct)
        {
            var hud = view;
            if (hud == null)
                return;

            fadeHandle.TryCancel();
            fadeHandle = LMotion.Create(1f, 0f, duration)
                .Bind(hud, static (value, target) => target.SetCastBarOpacity(value));

            try
            {
                await fadeHandle.ToUniTask(ct);
            }
            catch (OperationCanceledException)
            {
                fadeHandle.TryCancel();
                throw;
            }

            if (view == null)
                return;

            view.HideCastBar();
            view.SetCastBarOpacity(1f);
        }

        private void CancelRun()
        {
            if (runCts == null)
                return;

            runCts.Cancel();
            runCts.Dispose();
            runCts = null;
            fadeHandle.TryCancel();
        }
    }
}
