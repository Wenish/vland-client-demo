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

        private readonly Dictionary<int, SkillIndicatorSessionView> _sessions = new();
        private DisposableBag _subscriptions;
        private UnitController _localUnit;
        private Vector3 _latestAimPoint;
        private bool _hasAimPoint;
        private UnitController _latestFollowTarget;

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
            if (_sessions.Count == 0)
                return;

            foreach (var session in _sessions.Values)
            {
                if (session == null)
                    continue;

                if (session.Display.aimFollowMode == SkillIndicatorData.AimFollowMode.FollowWhileActive
                    && _hasAimPoint)
                {
                    session.SetAimPoint(_latestAimPoint);
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
        }

        public void UpdateAim(Vector3 aimPoint, UnitController followTarget = null)
        {
            _latestAimPoint = aimPoint;
            _latestFollowTarget = followTarget;
            _hasAimPoint = true;

            // Shift+aim preview always tracks the cursor, including LockOnConfirm skills
            // (lock applies after confirm / during the cast session, not while previewing).
            if (_sessions.TryGetValue(PreviewSessionId, out var preview) && preview != null)
            {
                preview.SetAimPoint(aimPoint);
                preview.SetFollowTarget(followTarget);
                preview.Tick();
            }

            ApplyLatestAimToFollowSessions(immediateTick: true);
        }

        public void EndPreview()
        {
            EndSession(PreviewSessionId);
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

            // Keep cached local aim across replace — LockOnConfirm must lock to the player's
            // confirm/mouse aim, not a lagged server snapshot (FollowWhileActive can correct later;
            // LockOnConfirm cannot).
            bool preferLocalAim = _hasAimPoint;
            Vector3 resolvedAim = preferLocalAim ? _latestAimPoint : aimPoint;
            UnitController resolvedFollow = preferLocalAim && _latestFollowTarget != null
                ? _latestFollowTarget
                : followTarget;

            EndAllSessions(clearCachedAim: false);
            _localUnit = caster;

            if (!preferLocalAim)
            {
                _latestAimPoint = aimPoint;
                _latestFollowTarget = followTarget;
                _hasAimPoint = true;
            }

            CreateOrReplaceSession(
                sessionId,
                caster,
                display,
                resolvedAim,
                null,
                resolvedFollow,
                skillInstance);

            // No trusted local aim yet: hide FollowWhileActive until PlayerInput pushes UpdateAim.
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
            EndPreview();
            // Cancelled preview: drop cached aim so the next instant cast doesn't reuse it.
            // Confirmed preview: keep aim so LockOnConfirm cast session can lock to it.
            if (evt == null || !evt.ConfirmedCast)
            {
                _hasAimPoint = false;
                _latestFollowTarget = null;
            }
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
                skillInstance);
            _sessions[sessionId] = view;
            view.Tick();
        }
    }
}
