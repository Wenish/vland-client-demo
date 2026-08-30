using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowInfection.Input
{
    [Serializable]
    public sealed class PlayerActionDefinition
    {
        public PlayerActionId id;
        public string settingsLabel;
        public PlayerActionGroup group;
        public InputBindingKey defaultPrimary;
        public InputBindingKey defaultSecondary;
        public InputBindingKey defaultGamepad;
        public bool defaultQuickCast;
        public bool isModifier;
        public bool isRequired;
        public bool hasQuickCast;
        public bool showInSettings = true;
        public List<PlayerActionId> allowedOverlaps = new();
    }
}
