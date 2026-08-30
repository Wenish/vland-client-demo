using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Input
{
    [CreateAssetMenu(fileName = "PlayerActionCatalog", menuName = "Game/Input/Player Action Catalog")]
    public sealed class PlayerActionCatalog : ScriptableObject
    {
        [SerializeField]
        private List<PlayerActionDefinition> actions = new();

        public IReadOnlyList<PlayerActionDefinition> Actions =>
            actions ?? (IReadOnlyList<PlayerActionDefinition>)System.Array.Empty<PlayerActionDefinition>();

        public bool HasActions => actions != null && actions.Count > 0;

        public static PlayerActionCatalog CreateRuntime()
        {
            var catalog = CreateInstance<PlayerActionCatalog>();
            catalog.actions = PlayerActionCatalogDefaults.Create();
            return catalog;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            if (!HasActions)
            {
                actions = PlayerActionCatalogDefaults.Create();
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }

            var changed = MergeEmptyDefaultsFromSpec();
            if (MergeMissingFromSpec())
                changed = true;
            if (changed)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private bool MergeEmptyDefaultsFromSpec()
        {
            var spec = PlayerActionCatalogDefaults.CreateLookup();
            var changed = false;
            for (var i = 0; i < actions.Count; i++)
            {
                var def = actions[i];
                if (def == null || !spec.TryGetValue(def.id, out var specDef))
                    continue;
                var primary = PlayerActionCatalogDefaults.Coalesce(def.defaultPrimary, specDef.defaultPrimary);
                var secondary = PlayerActionCatalogDefaults.Coalesce(def.defaultSecondary, specDef.defaultSecondary);
                var gamepad = PlayerActionCatalogDefaults.Coalesce(def.defaultGamepad, specDef.defaultGamepad);
                if (primary.Equals(def.defaultPrimary)
                    && secondary.Equals(def.defaultSecondary)
                    && gamepad.Equals(def.defaultGamepad))
                    continue;
                def.defaultPrimary = primary;
                def.defaultSecondary = secondary;
                def.defaultGamepad = gamepad;
                changed = true;
            }

            return changed;
        }

        private bool MergeMissingFromSpec()
        {
            var spec = PlayerActionCatalogDefaults.Create();
            var have = new HashSet<PlayerActionId>();
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i] != null)
                    have.Add(actions[i].id);
            }

            var changed = false;
            for (var i = 0; i < spec.Count; i++)
            {
                var def = spec[i];
                if (def == null || def.id == PlayerActionId.None || have.Contains(def.id))
                    continue;
                actions.Add(def);
                have.Add(def.id);
                changed = true;
            }

            return changed;
        }

        private void Reset()
        {
            actions = PlayerActionCatalogDefaults.Create();
        }

        [ContextMenu("Fill Spec Defaults")]
        private void FillSpecDefaults()
        {
            actions = PlayerActionCatalogDefaults.Create();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
