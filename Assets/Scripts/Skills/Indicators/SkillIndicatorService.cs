using System.Collections.Generic;
using MyGame.Events;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace ShadowInfection.Skills.Indicators
{
    public sealed class SkillIndicatorService : ISkillIndicatorService, IStartable, ITickable, System.IDisposable
    {
        private const int PreviewSessionId = -1;
        private const float PendingCastPreviewTimeoutSeconds = 0.05f;

        private readonly ISkillAimPreviewSessionNotifier _aimPreviewSession;
        private readonly Dictionary<int, SkillIndicatorSessionView> _sessions = new();
        private DisposableBag _subscriptions;
        private UnitController _localUnit;
        private Vector3 _latestAimPoint;
        private Vector2 _latestMoveInput;
        private bool _hasAimPoint;
        private UnitController _latestFollowTarget;

        /// <summary>
        /// Preview was confirmed and is held until the cast Show arrives (or timeout).
        /// </summary>
        private bool _holdingPreviewForCast;
        private float _holdingPreviewForCastSince;

        public SkillIndicatorService(ISkillAimPreviewSessionNotifier aimPreviewSession)
        {
            _aimPreviewSession = aimPreviewSession;
        }

        public void Start()
        {
            _subscriptions.Dispose();
            _subscriptions = new DisposableBag();

            GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref _subscriptions, OnMyPlayerUnitSpawned);
            GameMessages.Subscribe<SkillAimPreviewStartedEvent>(ref _subscriptions, OnAimPreviewStarted);
            GameMessages.Subscribe<SkillAimPreviewUpdatedEvent>(ref _subscriptions, OnAimPreviewUpdated);
            GameMessages.Subscribe<SkillAimPreviewEndedEvent>(ref _subscriptions, OnAimPreviewEnded);
            GameMessages.Subscribe<SkillIndicatorShowEvent>(ref _subscriptions, OnIndicatorShow);
            GameMessages.Subscribe<SkillIndicatorHideEvent>(ref _subscriptions, OnIndicatorHide);
            GameMessages.Subscribe<SkillIndicatorHideAllEvent>(ref _subscriptions, _ => EndAllSessions());
        }

        public void Dispose()
        {
            EndAllSessions();
            EndPreview();
            _subscriptions.Dispose();
            _subscriptions = new DisposableBag();
        }

        public void Tick()
        {
            if (_holdingPreviewForCast
                && Time.time - _holdingPreviewForCastSince > PendingCastPreviewTimeoutSeconds)
            {
                _holdingPreviewForCast = false;
                EndPreview();
            }

            if (_sessions.Count == 0)
                return;

            foreach (var session in _sessions.Values)
            {
                if (session == null)
                    continue;

                if (session.Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                    && _hasAimPoint
                    && !_holdingPreviewForCast)
                {
                    session.SetAimPoint(_latestAimPoint);
                    session.SetMoveInput(_latestMoveInput);
                    if (_latestFollowTarget != null)
                        session.SetFollowTarget(_latestFollowTarget);
                }

                session.Tick();
            }
        }

        public void BeginPreview(UnitController caster, SkillIndicatorDisplayParams display, Vector3 aimPoint)
        {
            BeginPreview(caster, display, aimPoint, null);
        }

        public void BeginPreview(
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource,
            UnitController followTarget = null,
            NetworkedSkillInstance skillInstance = null)
        {
            _holdingPreviewForCast = false;
            EndPreview();
            _localUnit = caster;
            _latestAimPoint = aimPoint;
            _latestFollowTarget = followTarget;
            _hasAimPoint = true;
            if (visualSource != null)
                SkillIndicatorVisualCatalog.Register(visualSource);

            // Preview always follows the cursor. LockOnConfirm only applies after cast confirm.
            display.aimFollowMode = SkillIndicatorData.AimFollowMode.FollowWhileActive;

            CreateOrReplaceSession(
                PreviewSessionId,
                caster,
                display,
                aimPoint,
                visualSource,
                followTarget,
                skillInstance);

            _aimPreviewSession.Begin(new SkillAimPreviewState(
                display.snapToTarget,
                followTarget,
                aimPoint,
                ResolveSkillData(skillInstance)));
        }

        public void UpdateAim(
            Vector3 aimPoint,
            UnitController followTarget = null,
            Vector2 moveInput = default)
        {
            _latestAimPoint = aimPoint;
            _latestFollowTarget = followTarget;
            _latestMoveInput = moveInput;
            _hasAimPoint = true;

            // While holding confirm→cast, keep the frozen preview; don't track mouse again.
            if (_holdingPreviewForCast)
                return;

            // Shift+aim preview always tracks the cursor, including LockOnConfirm skills
            // (lock applies after confirm / during the cast session, not while previewing).
            if (_sessions.TryGetValue(PreviewSessionId, out var preview) && preview != null)
            {
                preview.SetAimPoint(aimPoint);
                preview.SetFollowTarget(followTarget);
                preview.SetMoveInput(moveInput);
                preview.Tick();
            }

            ApplyLatestAimToFollowSessions(immediateTick: true);

            if (!_holdingPreviewForCast)
                _aimPreviewSession.Update(aimPoint, followTarget);
        }

        public void EndPreview()
        {
            _holdingPreviewForCast = false;
            EndSession(PreviewSessionId);
            _aimPreviewSession.End();
        }

        public void BeginSession(
            int sessionId,
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            UnitController followTarget = null,
            NetworkedSkillInstance skillInstance = null)
        {
            if (sessionId == PreviewSessionId)
                return;

            bool preferLocalAim = _hasAimPoint;
            Vector3 resolvedAim = preferLocalAim ? _latestAimPoint : aimPoint;
            UnitController resolvedFollow = preferLocalAim && _latestFollowTarget != null
                ? _latestFollowTarget
                : followTarget;

            _localUnit = caster;

            if (!preferLocalAim)
            {
                _latestAimPoint = aimPoint;
                _latestFollowTarget = followTarget;
                _hasAimPoint = true;
            }

            // Promote existing preview in place to avoid destroy→create flicker.
            if (TryPromotePreviewToCast(sessionId, display, resolvedAim, resolvedFollow))
                return;

            EndAllSessions(clearCachedAim: false);

            CreateOrReplaceSession(
                sessionId,
                caster,
                display,
                resolvedAim,
                null,
                resolvedFollow,
                skillInstance);

            if (display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                && !preferLocalAim
                && _sessions.TryGetValue(sessionId, out var session)
                && session != null)
            {
                session.SetVisible(false);
            }
        }

        public void EndSession(int sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return;

            _sessions.Remove(sessionId);
            if (session != null)
                session.Dispose();
        }

        public void EndAllSessions()
        {
            EndAllSessions(clearCachedAim: true);
        }

        private void EndAllSessions(bool clearCachedAim)
        {
            _holdingPreviewForCast = false;

            if (_sessions.Count > 0)
            {
                var ids = new List<int>(_sessions.Keys);
                for (int i = 0; i < ids.Count; i++)
                    EndSession(ids[i]);
            }

            if (clearCachedAim)
            {
                _hasAimPoint = false;
                _latestFollowTarget = null;
            }

            _aimPreviewSession.End();
        }

        private bool TryPromotePreviewToCast(
            int sessionId,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            UnitController followTarget)
        {
            if (!_sessions.TryGetValue(PreviewSessionId, out var preview) || preview == null)
                return false;

            // Different indicator asset/shape (e.g. Echo phase change) — rebuild.
            if (preview.Display.shape != display.shape
                || preview.Display.indicatorAssetName != display.indicatorAssetName)
            {
                return false;
            }

            _sessions.Remove(PreviewSessionId);
            _holdingPreviewForCast = false;

            // Drop any other cast sessions without disposing the promoted preview.
            if (_sessions.Count > 0)
            {
                var ids = new List<int>(_sessions.Keys);
                for (int i = 0; i < ids.Count; i++)
                    EndSession(ids[i]);
            }

            preview.ApplyCastConfirm(display, aimPoint, followTarget);
            _sessions[sessionId] = preview;
            return true;
        }

        private void ApplyLatestAimToFollowSessions(bool immediateTick)
        {
            if (!_hasAimPoint || _sessions.Count == 0)
                return;

            foreach (var pair in _sessions)
            {
                if (pair.Key == PreviewSessionId)
                    continue;

                var session = pair.Value;
                if (session == null)
                    continue;

                if (session.Display.aimFollowMode != SkillIndicatorData.AimFollowMode.FollowWhileActive)
                    continue;

                session.SetAimPoint(_latestAimPoint);
                session.SetMoveInput(_latestMoveInput);
                if (_latestFollowTarget != null)
                    session.SetFollowTarget(_latestFollowTarget);
                session.SetVisible(true);

                if (immediateTick)
                    session.Tick();
            }
        }

        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
        {
            _localUnit = evt.PlayerCharacter;
            EndAllSessions();
        }

        private void OnAimPreviewStarted(SkillAimPreviewStartedEvent evt)
        {
            if (evt?.Caster == null)
                return;

            var visual = SkillIndicatorVisualCatalog.Get(evt.Display.indicatorAssetName);
            if (visual == null && evt.Skill != null)
                visual = SkillAimPreviewUtil.Resolve(evt.SkillInstance) ?? evt.Skill.aimPreviewIndicator;
            BeginPreview(
                evt.Caster,
                evt.Display,
                evt.AimPoint,
                visual,
                evt.FollowTarget,
                evt.SkillInstance);
        }

        private void OnAimPreviewUpdated(SkillAimPreviewUpdatedEvent evt)
        {
            UpdateAim(evt.AimPoint, evt.FollowTarget);
        }

        private void OnAimPreviewEnded(SkillAimPreviewEndedEvent evt)
        {
            _aimPreviewSession.End();

            if (evt != null && evt.ConfirmedCast)
            {
                // Keep preview visible until cast Show promotes/replaces it.
                _holdingPreviewForCast = true;
                _holdingPreviewForCastSince = Time.time;

                if (_sessions.TryGetValue(PreviewSessionId, out var preview) && preview != null && _hasAimPoint)
                {
                    var lockedDisplay = preview.Display;
                    lockedDisplay.aimFollowMode = SkillIndicatorData.AimFollowMode.LockOnConfirm;
                    preview.ApplyCastConfirm(lockedDisplay, _latestAimPoint, _latestFollowTarget);
                }

                return;
            }

            EndPreview();
            _hasAimPoint = false;
            _latestFollowTarget = null;
        }

        private void OnIndicatorShow(SkillIndicatorShowEvent evt)
        {
            if (evt?.Caster == null)
                return;

            if (_localUnit != null && evt.Caster != _localUnit)
                return;

            BeginSession(
                evt.SessionId,
                evt.Caster,
                evt.Display,
                evt.AimPoint,
                evt.FollowTarget,
                evt.SkillInstance);
        }

        private void OnIndicatorHide(SkillIndicatorHideEvent evt)
        {
            EndSession(evt.SessionId);
        }

        private void CreateOrReplaceSession(
            int sessionId,
            UnitController caster,
            SkillIndicatorDisplayParams display,
            Vector3 aimPoint,
            SkillIndicatorData visualSource,
            UnitController followTarget,
            NetworkedSkillInstance skillInstance)
        {
            EndSession(sessionId);

            var view = SkillIndicatorSessionView.Create(
                caster,
                display,
                aimPoint,
                visualSource,
                followTarget,
                skillInstance,
                isPreviewSession: sessionId == PreviewSessionId);
            _sessions[sessionId] = view;
            view.Tick();
        }

        private static SkillData ResolveSkillData(NetworkedSkillInstance skillInstance)
        {
            if (skillInstance == null)
                return null;

            var skill = skillInstance.skillData;
            if (skill != null)
                return skill;

            skillInstance.ResolveSkillData();
            return skillInstance.skillData;
        }
    }
}
