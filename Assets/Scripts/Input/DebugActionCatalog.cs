using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShadowInfection.Input
{
    public enum DebugActionId
    {
        None = 0,
        PauseWaves = 1,
        DamageAll = 2,
        HealAll = 3,
        OpenGates = 4,
        CloseGates = 5,
        DebugScenes = 6,
        ShowFps = 7,
        ShowDps = 8
    }

    [Serializable]
    public sealed class DebugActionDefinition
    {
        public DebugActionId id;
        public string label;
        public InputBindingKey defaultPrimary;
    }

    [CreateAssetMenu(fileName = "DebugActionCatalog", menuName = "Game/Input/Debug Action Catalog")]
    public sealed class DebugActionCatalog : ScriptableObject
    {
        [SerializeField]
        private List<DebugActionDefinition> actions = new();

        public IReadOnlyList<DebugActionDefinition> Actions
        {
            get
            {
                EnsureDefaults();
                return actions;
            }
        }

        private void OnEnable() => EnsureDefaults();

        private void Reset() => actions = CreateDefaults();

        [ContextMenu("Fill Spec Defaults")]
        private void FillSpecDefaults()
        {
            actions = CreateDefaults();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void EnsureDefaults()
        {
            if (actions == null || actions.Count == 0)
                actions = CreateDefaults();
        }

        private static List<DebugActionDefinition> CreateDefaults()
        {
            return new List<DebugActionDefinition>
            {
                new DebugActionDefinition { id = DebugActionId.PauseWaves, label = "Pause Waves", defaultPrimary = InputBindingKey.Keyboard(Key.P) },
                new DebugActionDefinition { id = DebugActionId.DamageAll, label = "Damage All", defaultPrimary = InputBindingKey.Keyboard(Key.O) },
                new DebugActionDefinition { id = DebugActionId.HealAll, label = "Heal All", defaultPrimary = InputBindingKey.Keyboard(Key.L) },
                new DebugActionDefinition { id = DebugActionId.OpenGates, label = "Open Gates", defaultPrimary = InputBindingKey.Keyboard(Key.N) },
                new DebugActionDefinition { id = DebugActionId.CloseGates, label = "Close Gates", defaultPrimary = InputBindingKey.Keyboard(Key.M) },
                new DebugActionDefinition { id = DebugActionId.DebugScenes, label = "Debug Scenes", defaultPrimary = InputBindingKey.Keyboard(Key.F4) },
                new DebugActionDefinition { id = DebugActionId.ShowFps, label = "Show FPS", defaultPrimary = InputBindingKey.Keyboard(Key.F2) },
                new DebugActionDefinition { id = DebugActionId.ShowDps, label = "Show DPS", defaultPrimary = InputBindingKey.Keyboard(Key.F3) }
            };
        }
    }
}
