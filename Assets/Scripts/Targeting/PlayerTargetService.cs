using MyGame.Events;
using ShadowInfection.Units;
using UnityEngine;
using VContainer.Unity;

namespace ShadowInfection.Targeting
{
    public sealed class PlayerTargetService : IPlayerTarget, ITickable, System.IDisposable
    {
        private readonly IUnitRegistry unitRegistry;
        private R3.DisposableBag subscriptions;
        private UnitController current;
        private UnitController localPlayer;
        private bool selectedWhileAlive;

        public PlayerTargetService(IUnitRegistry unitRegistry)
        {
            this.unitRegistry = unitRegistry;
            unitRegistry.UnitUnregistered += OnUnitUnregistered;
            GameMessages.Subscribe<MyPlayerUnitSpawnedEvent>(ref subscriptions, OnMyPlayerUnitSpawned);
            GameMessages.Subscribe<UnitDiedEvent>(ref subscriptions, OnUnitDied);
        }

        public UnitController Current
        {
            get
            {
                if (ReferenceEquals(current, null) || current == null)
                    return null;
                return current;
            }
        }

        public bool HasTarget => Current != null;

        public void Set(UnitController unit)
        {
            if (unit == null)
            {
                Clear();
                return;
            }

            if (current == unit)
                return;

            var previous = Current;
            current = unit;
            selectedWhileAlive = !unit.IsDead;
            GameMessages.Publish(new PlayerTargetChangedEvent(previous, current));
        }

        public void Clear()
        {
            if (ReferenceEquals(current, null))
                return;

            var previous = current == null ? null : current;
            current = null;
            selectedWhileAlive = false;
            GameMessages.Publish(new PlayerTargetChangedEvent(previous, null));
        }

        public bool TryGetSnapshot(out PlayerTargetSnapshot snapshot)
        {
            var unit = Current;
            if (unit == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new PlayerTargetSnapshot(
                unit,
                unit.unitName,
                unit.health,
                unit.maxHealth,
                unit.shield,
                unit.maxShield,
                unit.team,
                localPlayer != null && localPlayer == unit,
                unit.IsDead);
            return true;
        }

        public void Tick()
        {
            if (ReferenceEquals(current, null))
                return;

            if (current == null)
            {
                Clear();
                return;
            }

            if (!current.IsDead)
                selectedWhileAlive = true;
            else if (selectedWhileAlive)
                ClearIfDeadEnemy(current);
        }

        public void Dispose()
        {
            unitRegistry.UnitUnregistered -= OnUnitUnregistered;
            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            current = null;
            localPlayer = null;
            selectedWhileAlive = false;
        }

        private void OnUnitUnregistered(UnitController unit)
        {
            if (unit == null)
                return;

            if (current == unit)
                Clear();

            if (localPlayer == unit)
                localPlayer = null;
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt == null || evt.Unit == null)
                return;

            if (current != evt.Unit)
                return;

            ClearIfDeadEnemy(evt.Unit);
        }

        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
        {
            localPlayer = evt.PlayerCharacter;
        }

        private void ClearIfDeadEnemy(UnitController unit)
        {
            if (unit == null || !unit.IsDead)
                return;

            if (!IsEnemyOfLocalPlayer(unit))
                return;

            Clear();
        }

        private bool IsEnemyOfLocalPlayer(UnitController unit)
        {
            if (unit == null || localPlayer == null || localPlayer == unit)
                return false;

            return unit.team != localPlayer.team;
        }
    }
}
