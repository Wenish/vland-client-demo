using System.Threading;
using MessagePipe;
using MyGame.Events;
using R3;
using ShadowInfection;
using ShadowInfection.DI;
using ShadowInfection.Targeting;
using ShadowInfection.Units;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.Nameplates
{
    internal sealed class NameplateLayerPresenter
    {
        private readonly NameplateLayerSettings settings;
        private readonly IUnitRegistry unitRegistry;
        private readonly IGameDatabases databases;
        private readonly ITeamColorService teamColors;
        private readonly ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned;
        private readonly IPlayerTarget playerTarget;
        private readonly ISubscriber<PlayerTargetChangedEvent> targetChanged;

        private NameplateLayerView view;
        private NameplatePositionResolver positionResolver;
        private readonly System.Collections.Generic.Dictionary<UnitController, UnitNameplateBinding> bindings = new();
        private readonly System.Collections.Generic.List<UnitNameplateBinding> tickList = new();
        private UnitController localPlayerUnit;
        private UnitController selectedUnit;
        private CancellationToken destroyToken;
        private bool enabled;
        private R3.DisposableBag subscriptions;

        public NameplateLayerPresenter(
            NameplateLayerSettings settings,
            IUnitRegistry unitRegistry,
            IGameDatabases databases,
            ITeamColorService teamColors,
            ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned,
            IPlayerTarget playerTarget,
            ISubscriber<PlayerTargetChangedEvent> targetChanged)
        {
            this.settings = settings;
            this.unitRegistry = unitRegistry;
            this.databases = databases;
            this.teamColors = teamColors;
            this.myUnitSpawned = myUnitSpawned;
            this.playerTarget = playerTarget;
            this.targetChanged = targetChanged;
        }

        public void Bind(NameplateLayerView nextView, CancellationToken token)
        {
            Unbind();

            view = nextView;
            destroyToken = token;
            positionResolver = new NameplatePositionResolver(settings);

            subscriptions.Add(myUnitSpawned.Subscribe(OnMyPlayerUnitSpawned));
            subscriptions.Add(targetChanged.Subscribe(OnPlayerTargetChanged));
            unitRegistry.UnitRegistered += RegisterUnit;
            unitRegistry.UnitUnregistered += UnregisterUnit;

            foreach (var unit in unitRegistry.Units)
                RegisterUnit(unit);

            ApplySelected(playerTarget != null ? playerTarget.Current : null);
            enabled = true;
        }

        public void Unbind()
        {
            enabled = false;
            subscriptions.Dispose();
            subscriptions = new R3.DisposableBag();
            unitRegistry.UnitRegistered -= RegisterUnit;
            unitRegistry.UnitUnregistered -= UnregisterUnit;

            foreach (var binding in bindings.Values)
                binding.Dispose();

            bindings.Clear();
            tickList.Clear();
            view = null;
            positionResolver = null;
            selectedUnit = null;
        }

        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void Tick(float deltaTime)
        {
            if (!enabled || view == null)
                return;

            tickList.Clear();
            tickList.AddRange(bindings.Values);

            foreach (var binding in tickList)
                binding.Tick(deltaTime);
        }

        public void SyncPositions(Camera camera)
        {
            if (!enabled || view == null || camera == null)
                return;

            var container = view.Container;
            if (container == null || container.panel == null)
                return;

            tickList.Clear();
            tickList.AddRange(bindings.Values);

            foreach (var binding in tickList)
            {
                if (binding.Element.resolvedStyle.display == DisplayStyle.None)
                    continue;

                if (positionResolver.TryResolve(
                        binding.Unit,
                        binding.ColliderAnchorHeight,
                        container,
                        camera,
                        out var panelPosition))
                    binding.Element.SetScreenPosition(panelPosition);
                else
                    binding.Element.style.visibility = Visibility.Hidden;
            }
        }

        private void RegisterUnit(UnitController unit)
        {
            if (unit == null || bindings.ContainsKey(unit) || view == null)
                return;

            var element = view.Acquire();
            var binding = new UnitNameplateBinding(settings, databases, teamColors, OnBindingChanged);
            binding.Attach(unit, element, destroyToken);
            binding.SetLocalPlayer(localPlayerUnit != null && localPlayerUnit == unit);
            binding.SetSelected(selectedUnit != null && selectedUnit == unit);
            bindings.Add(unit, binding);

            var snapshot = binding.BuildSnapshot();
            element.Apply(in snapshot);
        }

        private void UnregisterUnit(UnitController unit)
        {
            if (unit == null || !bindings.TryGetValue(unit, out var binding) || view == null)
                return;

            view.Release(binding.Element);
            binding.Dispose();
            bindings.Remove(unit);
            if (selectedUnit == unit)
                selectedUnit = null;
        }

        private void OnBindingChanged(UnitNameplateBinding binding)
        {
            if (!enabled || binding?.Element == null)
                return;

            var snapshot = binding.BuildSnapshot();
            binding.Element.Apply(in snapshot);
        }

        private void OnMyPlayerUnitSpawned(MyPlayerUnitSpawnedEvent evt)
        {
            localPlayerUnit = evt.PlayerCharacter;
            foreach (var binding in bindings.Values)
                binding.SetLocalPlayer(binding.Unit == localPlayerUnit);
        }

        private void OnPlayerTargetChanged(PlayerTargetChangedEvent evt)
        {
            ApplySelected(evt.Current);
        }

        private void ApplySelected(UnitController unit)
        {
            if (unit != null && unit.Equals(null))
                unit = null;

            if (selectedUnit == unit)
                return;

            if (selectedUnit != null && bindings.TryGetValue(selectedUnit, out var previous))
                previous.SetSelected(false);

            selectedUnit = unit;
            if (selectedUnit != null && bindings.TryGetValue(selectedUnit, out var next))
                next.SetSelected(true);
        }
    }
}
