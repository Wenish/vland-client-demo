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

            if (_sessions.TryGetValue(PreviewSessionId, out var preview) && preview != null)
            {
                preview.SetAimPoint(aimPoint);
                preview.SetFollowTarget(followTarget);
                preview.Tick();
            }
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

            EndPreview();
            _localUnit = caster;
            _latestAimPoint = aimPoint;
            _latestFollowTarget = followTarget;
            _hasAimPoint = true;
            CreateOrReplaceSession(
                sessionId,
                caster,
                display,
                aimPoint,
                null,
                followTarget,
                skillInstance);
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
            if (_sessions.Count == 0)
                return;

            var ids = new List<int>(_sessions.Keys);
            for (int i = 0; i < ids.Count; i++)
                EndSession(ids[i]);
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

            var visual = evt.Skill != null ? evt.Skill.aimPreviewIndicator : null;
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
