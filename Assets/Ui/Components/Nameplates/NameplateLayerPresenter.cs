using System.Threading;
using MessagePipe;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
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

        private NameplateLayerView view;
        private NameplatePositionResolver positionResolver;
        private readonly System.Collections.Generic.Dictionary<UnitController, UnitNameplateBinding> bindings = new();
        private readonly System.Collections.Generic.List<UnitNameplateBinding> tickList = new();
        private UnitController localPlayerUnit;
        private CancellationToken destroyToken;
        private bool enabled;
        private R3.DisposableBag subscriptions;

        public NameplateLayerPresenter(
            NameplateLayerSettings settings,
            IUnitRegistry unitRegistry,
            IGameDatabases databases,
            ITeamColorService teamColors,
            ISubscriber<MyPlayerUnitSpawnedEvent> myUnitSpawned)
        {
            this.settings = settings;
            this.unitRegistry = unitRegistry;
            this.databases = databases;
            this.teamColors = teamColors;
            this.myUnitSpawned = myUnitSpawned;
        }

        public void Bind(NameplateLayerView nextView, CancellationToken token)
        {
            Unbind();

            view = nextView;
            destroyToken = token;
            positionResolver = new NameplatePositionResolver(settings);

            subscriptions.Add(myUnitSpawned.Subscribe(OnMyPlayerUnitSpawned));
            unitRegistry.UnitRegistered += RegisterUnit;
            unitRegistry.UnitUnregistered += UnregisterUnit;

            foreach (var unit in unitRegistry.Units)
                RegisterUnit(unit);

            enabled = true;
        }

        public void Unbind()
        {
            enabled = false;
            subscriptions.Dispose();
            unitRegistry.UnitRegistered -= RegisterUnit;
            unitRegistry.UnitUnregistered -= UnregisterUnit;

            foreach (var binding in bindings.Values)
                binding.Dispose();

            bindings.Clear();
            tickList.Clear();
            view = null;
            positionResolver = null;
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
            {
                binding.Tick(deltaTime);
                var snapshot = binding.BuildSnapshot();
                binding.Element.Apply(in snapshot);
            }
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

                if (positionResolver.TryResolve(binding.Unit, container, camera, out var panelPosition))
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
    }
}
