using UnityEngine;

[CreateAssetMenu(fileName = "BuffType", menuName = "Game/Buffs/Buff Type")]
public class BuffType : ScriptableObject
{
    [SerializeField]
    [Tooltip("Optional display name. If empty, the asset's name will be used.")]
    private string displayName;

    // Public name to use in UI/logic. Falls back to the asset's name if not set.
    public string Name => string.IsNullOrWhiteSpace(displayName) ? this.name : displayName;
}
