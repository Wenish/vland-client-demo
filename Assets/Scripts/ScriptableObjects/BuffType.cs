using UnityEngine;

[CreateAssetMenu(fileName = "BuffType", menuName = "Game/Buffs/Buff Type")]
public class BuffType : ScriptableObject
{
    [SerializeField]
    [Tooltip("Optional display name. If empty, the asset's name will be used.")]
    private string displayName;

    [SerializeField]
    [Tooltip("Whether this buff type is a negative effect (debuff). Used by dispel mechanics.")]
    private bool isNegative;

    [SerializeField]
    [Tooltip("Whether this buff can be removed by dispel/cleanse effects. Disable for passive or innate buffs.")]
    private bool isDispellable = true;

    [SerializeField]
    [Tooltip("If disabled, buffs of this type are not shown in the unit buff bar UI.")]
    private bool showInUnitUiBuffBar = true;

    // Public name to use in UI/logic. Falls back to the asset's name if not set.
    public string Name => string.IsNullOrWhiteSpace(displayName) ? this.name : displayName;

    /// <summary>True for debuffs (e.g. slows, DoTs), false for beneficial buffs (e.g. haste, shields).</summary>
    public bool IsNegative => isNegative;

    /// <summary>Whether this buff can be removed by dispel effects. False for passive/innate buffs.</summary>
    public bool IsDispellable => isDispellable;

    /// <summary>Whether this buff type should be displayed in the unit buff bar UI.</summary>
    public bool ShowInUnitUiBuffBar => showInUnitUiBuffBar;
}
